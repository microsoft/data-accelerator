// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataX.Flow.CodegenRules
{
    public class Rule
    {
        [JsonProperty("$ruleId")]
        public string RuleId { get; set; }

        [JsonProperty("$productId")]
        public string ProductId { get; set; }

        [JsonProperty("$ruleType")]
        public string RuleType { get; set; }

        [JsonProperty("$ruleDescription")]
        public string RuleDescription { get; set; }

        [JsonProperty("$ruleCategory")]
        public string RuleCategory { get; set; }

        [JsonProperty("$severity")]
        public string Severity { get; set; }

        [JsonProperty("$condition")]
        public string Condition { get; set; }

        [JsonProperty("$aggs")]
        public List<string> Aggs { get; set; }

        [JsonProperty("$pivots")]
        public List<string> Pivots { get; set; }

        [JsonProperty("$fmpivots")]
        public List<string> Fmpivots { get; set; }

        [JsonProperty("$tagname")]
        public string Tagname { get; set; }

        [JsonProperty("$tag")]
        public string Tag { get; set; }

        [JsonProperty("$fact")]
        public string Fact { get; set; }

        [JsonProperty("$id")]
        public string Id { get; set; }

        [JsonProperty("$outputTemplate")]
        public string OutputTemplate { get; set; }

        [JsonProperty("$sinks")]
        public List<string> Sinks { get; set; }

        [JsonProperty("$alertsinks")]
        public List<string> Alertsinks { get; set; }

        [JsonProperty("$isalert")]
        public bool Isalert { get; set; }

        [JsonProperty("schemaTableName")]
        public string TargetTable { get; set; }

        public string AggsToSelect()
        {
            try
            {
                if (Aggs == null || Aggs.Count <= 0)
                {
                    return "";
                }

                string result = "";
                foreach (string agg in Aggs)
                {
                    result += agg + " AS ";
                    Regex r = new Regex("(.*)\\((.*?)\\)");

                    // handle column names which contain escaping like min(`device.msg.received`)"]
                    if (r.Matches(agg)[0].Groups[2].Value.EndsWith("`"))
                    {
                        result += r.Matches(agg)[0].Groups[2].Value.TrimEnd(new char[] { '`' }) + "_" + r.Matches(agg)[0].Groups[1].Value + "`, ";
                    }
                    else
                    {
                        result += r.Matches(agg)[0].Groups[2].Value.Replace(".", "") + "_" + r.Matches(agg)[0].Groups[1].Value + ", ";
                    }
                }
                result = result.TrimEnd(' ', ',');
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return "";
            }
        }

        public string ConditionToSQL()
        {
            try
            {
                if (Aggs == null || Aggs.Count <= 0)
                {
                    return Condition;
                }

                string result = Condition;
                foreach (string agg in Aggs)
                {
                    Regex r = new Regex("(.*)\\((.*?)\\)");

                    // handle column names which contain escaping like min(`device.msg.received`)"]
                    string s = "";
                    if (r.Matches(agg)[0].Groups[2].Value.EndsWith("`"))
                    {
                        s = r.Matches(agg)[0].Groups[2].Value.TrimEnd(new char[] { '`' }) + "_" + r.Matches(agg)[0].Groups[1].Value + "`";
                    }
                    else
                    {
                        s = r.Matches(agg)[0].Groups[2].Value.Replace(".", "") + "_" + r.Matches(agg)[0].Groups[1].Value;
                    }
                    result = result.Replace(agg, s);
                }
                foreach(string pivot in Pivots)
                {
                    string s = "";
                    if(!pivot.StartsWith('`') && pivot.Contains('.'))
                    {
                        string[] parts = pivot.Split('.');
                        s = parts[parts.Count() - 1];
                        result = result.Replace(pivot, s);
                    }
                }
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return "";
            }
        }

        public string AggsToTemplate()
        {
            try
            {
                if (Aggs == null || Aggs.Count <= 0)
                {
                    return "";
                }

                Dictionary<string, List<string>> aggtemplate = new Dictionary<string, List<string>>();
                Regex r = new Regex("(.*)\\((.*?)\\)");
                foreach (string agg in Aggs)
                {
                    string op = r.Matches(agg)[0].Groups[1].Value;
                    string col = r.Matches(agg)[0].Groups[2].Value;
                    if (aggtemplate.ContainsKey(col))
                    {
                        if (aggtemplate[col] == null || aggtemplate[col].Count <= 0)
                        {
                            aggtemplate[col] = new List<string>() { op };
                        }
                        else
                        {
                            aggtemplate[col].Add(op);
                        }
                    }
                    else
                    {
                        aggtemplate.Add(col, new List<string>() { op });
                    }
                }

                string result = "MAP(\n";
                foreach (string key in aggtemplate.Keys)
                {
                    result += $"'{key}', ";

                    result += "MAP(\n";
                    foreach (string op in aggtemplate[key])
                    {
                        // handle column names which contain escaping like min(`device.msg.received`)"]
                        if (key.EndsWith("`"))
                        {
                            string keyVal = key.TrimEnd(new char[] { '`' });
                            result += $"'{op}', {keyVal}_{op}`,";
                        }
                        else
                        {
                            result += $"'{op}', {key.Replace(".", "")}_{op},";
                        }
                    }
                    result = result.TrimEnd(' ', ',');
                    result += $"), \n";
                }
                result = result.TrimEnd(' ', ',', '\n');
                result += "\n) AS aggs";
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return "";
            }
        }

        public string ListToString(List<string> list)
        {
            if (list == null || list.Count <= 0)
            {
                return "";
            }

            string result = "";
            foreach (string s in list)
            {
                result += s + ", ";
            }

            result = result.TrimEnd(' ', ',');
            return result;
        }

        public string PivotsToTemplate()
        {
            if (Pivots == null || Pivots.Count <= 0)
            {
                return "";
            }

            string result = "";
            foreach (string pivot in Pivots)
            {
                if (pivot.Trim().EndsWith('`'))
                {
                    result += $"'{pivot}', {pivot},\n";
                }
                else
                {
                    result += $"'{pivot}', {GetLastSection(pivot)},\n";
                }
            }
            result = result.TrimEnd(' ', ',', '\n');
            return result;
        }

        private string GetLastSection(string s)
        {
            // If s is "device.status.home" then return "home"
            if (!s.Contains("."))
            {
                return s;
            }
            string[] parts = s.Split('.');
            return parts[parts.Count() - 1];
        }

        public string RulesObject()
        {
            string result = "MAP(";
            result += $"'ruleId', '{RuleId}', ";
            result += $"'ruleDescription', '{RuleDescription}', ";
            result += $"'severity', '{Severity}', ";
            result += $"'{Tagname}', '{Tag}'";
            result += ")";
            return result;
        }

    }


    public class RulesCode
    {
        public string Code { get; set; }
        public List<Tuple<string, string>> Outputs { get; set; }
        public Dictionary<string, string> AccumlationTables { get; set; }
        public Dictionary<string, string> TimeWindows { get; set; }
        public MetricsRoot MetricsRoot { get; set; }
    }
}
