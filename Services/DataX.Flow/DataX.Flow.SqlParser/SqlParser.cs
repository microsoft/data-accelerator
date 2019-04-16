// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataX.Flow.SqlParser
{
    public class SqlParser
    {
        public List<string> GetTables(string code)
        {
            List<string> result = new List<string>();
            try
            {
                result.Add("DataXProcessedInput");

                if (string.IsNullOrEmpty(code))
                {
                    return result;
                }

                Regex regex = new Regex(@"(\S+)\s*=");
                MatchCollection mc = regex.Matches(code);
                if (mc == null || mc.Count <= 0)
                {
                    return result;
                }

                foreach (Match m in mc)
                {
                    string table = m.Groups[1].Value.Trim();
                    if (!result.Contains(table))
                    {
                        result.Add(table);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return result;
            }
        }

        public List<TableData> Analyze(string userCode, string codegen, string inputSchema, bool getUserTablesOnly = true)
        {
            List<TableData> tables = new List<TableData>
            {
                GetProcessedInputTableData(inputSchema)
            };

            List<TableData> result = new List<TableData>
            {
                GetProcessedInputTableData(inputSchema)
            };

            try
            {
                if (string.IsNullOrEmpty(codegen))
                {
                    return result;
                }

                Regex regex = new Regex("(\\S+)\\s*=\\s*SELECT([^\"]*?)FROM[^\"]*?(\\S+)", RegexOptions.IgnoreCase);
                MatchCollection mc = regex.Matches(codegen);

                foreach (Match m in mc)
                {
                    TableData td = new TableData
                    {
                        Name = m.Groups[1].Value
                    };
                    string fromTable = m.Groups[3].Value;

                    if (fromTable.Contains("DataXProcessedInput_"))
                    {
                        fromTable = "DataXProcessedInput";
                    }

                    string columns = m.Groups[2].Value;

                    List<string> cols = GetColumns(columns);
                    foreach (string col in cols)
                    {
                        string c = col.Trim();
                        if (c == "*")
                        {
                            TableData t = tables.Where(tdata => tdata.Name == fromTable).FirstOrDefault();
                            if (t != null && t.Columns != null)
                            {
                                foreach (string column in t.Columns)
                                {
                                    td.Columns.Add(column);
                                }
                            }
                        }
                        else
                        {
                            td.Columns.Add(c.Trim());
                        }
                    }
                    tables.Add(td);
                }

                if (getUserTablesOnly)
                {
                    List<string> userTables = GetTables(userCode);
                    foreach (TableData td in tables)
                    {
                        if (userTables.Contains(td.Name) && td.Name != "DataXProcessedInput")
                        {
                            result.Add(td);
                        }
                    }
                    return result;
                }
                else
                {
                    return tables;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return result;
            }
        }

        private List<string> GetColumns(string code)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(code))
            {
                return result;
            }

            List<string> cols = BreakDown(code);
            foreach (string col in cols)
            {
                string colName = GetColumnName(col);
                result.Add(colName);
                if (IsKeywordStatement(col, "MAP"))
                {
                    string content = ExtractContent(col, "MAP");
                    result.AddRange(GetColumnsFromMap(content, colName));
                }
                else if (IsKeywordStatement(col, "STRUCT"))
                {
                    string content = ExtractContent(col, "STRUCT");

                    // Check if the syntax is like MAP or STRUCT
                    List<string> lines = BreakDown(content);
                    if (lines != null && lines.Count > 0)
                    {
                        Regex regex = new Regex("([^\"]*)AS", RegexOptions.IgnoreCase);
                        if (regex.Match(lines[0]).Success)
                        {
                            result.AddRange(GetColumnsFromStruct(content, colName));
                        }
                        else
                        {
                            result.AddRange(GetColumnsFromMap(content, colName));
                        }
                    }
                }
            }

            return result;
        }

        private string GetColumnName(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return "";
            }

            Regex regex = new Regex("([^\"]*)AS\\s", RegexOptions.IgnoreCase);
            MatchCollection mc = regex.Matches(code);
            if (mc == null || mc.Count <= 0)
            {
                return code.Trim();
            }

            return code.Replace(mc[0].Value, "").Trim();
        }

        private List<string> GetColumnsFromMap(string code, string parentCol)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(code))
            {
                return result;
            }

            List<string> lines = BreakDown(code);
            int i = 0;
            string colName = parentCol;
            foreach (string line in lines)
            {
                if (i % 2 == 0)
                {
                    colName = $"{parentCol}.{line.Trim().Trim('\'')}";
                    result.Add(colName);
                }
                else
                {
                    if (IsKeywordStatement(line, "MAP"))
                    {
                        string content = ExtractContent(line, "MAP");
                        result.AddRange(GetColumnsFromMap(content, colName));
                    }
                    else if (IsKeywordStatement(line, "STRUCT"))
                    {
                        string content = ExtractContent(line, "STRUCT");
                        result.AddRange(GetColumnsFromStruct(content, colName));
                    }
                }
                i++;
            }
            return result;
        }

        private List<string> GetColumnsFromStruct(string code, string parentCol)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(code))
            {
                return result;
            }

            List<string> lines = BreakDown(code);
            foreach (string line in lines)
            {
                string name = GetColumnName(line);
                string colName = $"{parentCol}.{name}";
                result.Add(colName);
                if (IsKeywordStatement(line, "MAP"))
                {
                    string content = ExtractContent(line, "MAP");
                    result.AddRange(GetColumnsFromMap(content, colName));
                }
                else if (IsKeywordStatement(line, "STRUCT"))
                {
                    string content = ExtractContent(line, "STRUCT");
                    result.AddRange(GetColumnsFromStruct(content, colName));
                }
            }
            return result;
        }

        private bool IsKeywordStatement(string code, string keyword)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            string[] lines = code.Split('(');
            if (lines == null || lines.Count() <= 0)
            {
                return false;
            }

            if (lines[0].Trim().ToLower() == keyword.ToLower())
            {
                return true;
            }

            return false;
        }

        private string ExtractContent(string code, string keyword)
        {
            if (string.IsNullOrEmpty(code))
            {
                return "";
            }

            //Regex regex = new Regex(keyword + "([^\"]*)AS\\s");
            Regex regex = new Regex(keyword + "\\s?\\(([^\"]*)\\)");
            Match mc = regex.Match(code);
            return mc.Groups[1].Value.Trim();
        }

        private List<string> BreakDown(string code)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(code))
            {
                return result;
            }

            int level = 0;
            int startIndex = 0;
            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '(')
                {
                    level++;
                }

                if (code[i] == ')')
                {
                    level--;
                }

                if ((code[i] == ',' && level == 0) || i == code.Length - 1)
                {
                    result.Add(code.Substring(startIndex, i - startIndex + 1).Trim(','));
                    startIndex = i + 1;
                }
            }
            return result;
        }

        private TableData GetProcessedInputTableData(string inputSchema)
        {
            TableData t = new TableData
            {
                Name = "DataXProcessedInput"
            };

            if (string.IsNullOrEmpty(inputSchema))
            {
                return t;
            }

            t = GetProcessedInputTableDataHelper(t, string.Empty, inputSchema);

            return t;
        }

        private TableData GetProcessedInputTableDataHelper(TableData t, string name, string inputSchema)
        {
            if (string.IsNullOrEmpty(inputSchema))
            {
                return t;
            }

            InputSchema schema = JsonConvert.DeserializeObject<InputSchema>(inputSchema);
            foreach (Field f in schema.fields)
            {
                string tempName = "";
                if (string.IsNullOrEmpty(name))
                {
                    tempName = f.name;
                }
                else
                {
                    tempName = $"{name}.{f.name}";
                }

                t.Columns.Add(tempName);
                if (f.type.GetType() == typeof(JObject) && (f.type as JObject)["type"] != null && (f.type as JObject)["type"].ToString() == "struct")
                {
                    t = GetProcessedInputTableDataHelper(t, tempName, JsonConvert.SerializeObject(f.type));
                }
            }

            return t;
        }
    }

    public class TableData
    {
        public TableData()
        {
            Columns = new List<string>();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("columns")]
        public List<string> Columns { get; set; }
    }
}
