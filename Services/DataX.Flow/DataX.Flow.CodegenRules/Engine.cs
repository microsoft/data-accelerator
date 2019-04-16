// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace DataX.Flow.CodegenRules
{
    public class Engine
    {
        private List<Rule> _allrules = new List<Rule>();
        private queries _queryTemplates = null;
        private outputTemplates _outputTemplates = null;
        private string _code = "";
        private int _statementNumber = 0;
        private int _ruleCounter = 1;
        private RulesCode _rulesCode = new RulesCode();
        private MetricsRoot _metricsRoot = new MetricsRoot();
        private readonly string _defaultQueryTemplateFile = "defaultQueryTemplate.xml";
        private readonly string _defaultOutputTemplateFile = "defaultOutputTemplate.xml";
        private const string _DefaultTargetName = "DataXProcessedInput";

        public RulesCode GenerateCode(string code, string rules, string queryTemplates, string outputTemplates, string productId)
        {
            try
            {
                // Initialize
                _code = code;

                LoadRules(rules);
                LoadQueryTemplates(queryTemplates);
                LoadOutputTemplates(outputTemplates);

                GenerateCodeHelper(productId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
            return _rulesCode;
        }

        public RulesCode GenerateCode(string code, string rules, string productId)
        {
            try
            {
                // Initialize
                _code = code;
                string queryTemplate = GetEmbeddedResource(_defaultQueryTemplateFile);
                string outputTemplate = GetEmbeddedResource(_defaultOutputTemplateFile);
                LoadRules(rules);
                LoadQueryTemplates(queryTemplate);
                LoadOutputTemplates(outputTemplate);

                GenerateCodeHelper(productId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw;
            }
            return _rulesCode;
        }

        public void GenerateCodeHelper(string productId)
        {
            // Auto code gen alerts code
            AutoCodegenAlerts(productId);

            // Codegen
            ProcessAlertsCodeGen(productId);
            ProcessRulesCodeGen(productId);
            ProcessAggregateRulesCodeGen(productId);
            ProcessAggregateAlertsCodeGen(productId);

            ProcessCreateMetrics(productId);

            var outputList = ProcessOutputs();
            var accumulationTables = ProcessAccumulationTables();
            var timeWindows = ProcessTimeWindows();
            var metricsRoot = GenerateMetricsConfig(outputList, productId);
            ProcessUpsert();

            string formattedSQL = Formatter.Format(_code);
            formattedSQL = formattedSQL.Replace("\n", "\r\n");
            formattedSQL = formattedSQL.Replace("--DataXQuery--", "\r\n--DataXQuery--\r\n");
            formattedSQL = formattedSQL.Replace("--Rule", "\r\n\r\n--Rule");
            formattedSQL = formattedSQL.Replace(";", "");

            formattedSQL = RemoveSpuriousQueryStringsAndChars(formattedSQL);

            _rulesCode.Code = formattedSQL;
            _rulesCode.Outputs = outputList;
            _rulesCode.AccumlationTables = accumulationTables;
            _rulesCode.TimeWindows = timeWindows;
            _rulesCode.MetricsRoot = metricsRoot;
        }

       

        private void LoadRules(string rulesJson)
        {
            _allrules = JsonConvert.DeserializeObject<List<Rule>>(rulesJson);
        }      

        private void LoadQueryTemplates(string content)
        {
            XmlSerializer ser = new XmlSerializer(typeof(queries));
            using (StringReader sreader = new StringReader(content))
            {
                using (XmlReader reader = XmlReader.Create(sreader))
                {
                    _queryTemplates = (queries)ser.Deserialize(reader);
                }
            }
        }
      

        private void LoadOutputTemplates(string content)
        {
            XmlSerializer ser = new XmlSerializer(typeof(outputTemplates));

            using (StringReader sreader = new StringReader(content))
            {
                using (XmlReader reader = XmlReader.Create(sreader))
                {
                    _outputTemplates = (outputTemplates)ser.Deserialize(reader);
                }
            }
        }

        private void AutoCodegenAlerts(string productId)
        {
            List<Rule> rules = new List<Rule>();
            if (string.IsNullOrEmpty(productId))
            {
                rules = _allrules.Where(r => r.Isalert).ToList();
            }
            else
            {
                rules = _allrules.Where(r => r.ProductId == productId && r.Isalert).ToList();
            }

            if (rules == null || rules.Count() <= 0)
            {
                return;
            }

            // Check all the rules to determine what kinds of alerts user has set up
            Dictionary<string, List<string>> keyValues = new Dictionary<string, List<string>>();
            foreach (Rule rule in rules)
            {
                if (!keyValues.ContainsKey(rule.TargetTable))
                {
                    keyValues.Add(rule.TargetTable, new List<string>() { rule.RuleType });
                }
                else if(!keyValues[rule.TargetTable].Contains(rule.RuleType))
                {
                    keyValues[rule.TargetTable].Add(rule.RuleType);
                }
            }

            foreach(string key in keyValues.Keys)
            {
                foreach (string ruleType in keyValues[key])
                {
                    // Check if the code already contains the API call for processing alert. If not, then add it
                    if (ruleType == "SimpleRule")
                    {
                        Regex simpleRegex = new Regex(@"ProcessAlerts\s{0,}\(\s{0,}" + key + @"\s{0,}\)", RegexOptions.IgnoreCase);
                        MatchCollection m1 = simpleRegex.Matches(_code);
                        if (m1 == null || m1.Count <= 0)
                        {
                            _code += $"\nProcessAlerts({key});";
                        }
                    }
                    else
                    {
                        Regex aggRegex = new Regex(@"ProcessAggregateAlerts\s{0,}\(\s{0,}" + key + @"\s{0,}\)", RegexOptions.IgnoreCase);
                        MatchCollection m2 = aggRegex.Matches(_code);
                        if (m2 == null || m2.Count <= 0)
                        {
                            _code += $"\nProcessAggregateAlerts({key});";
                        }
                    }
                }
            }
        }

        private void ProcessAlertsCodeGen(string productId)
        {
            Regex r1 = new Regex(@"ProcessAlerts\s{0,}\(\s{0,}(.*?)\s{0,}\)", RegexOptions.IgnoreCase);
            MatchCollection m1 = r1.Matches(_code);
            if (m1 != null && m1.Count > 0)
            {
                foreach (Match m in m1)
                {
                    _statementNumber++;
                    string targetTable = m.Groups[1].Value;
                    if (string.IsNullOrEmpty(targetTable))
                    {
                        targetTable = _DefaultTargetName;
                    }

                    List<Rule> rules = new List<Rule>();
                    if (string.IsNullOrEmpty(productId))
                    {
                        rules = _allrules.Where(r => r.RuleType == "SimpleRule" && r.TargetTable == targetTable && r.Isalert).ToList();
                    }
                    else
                    {
                        rules = _allrules.Where(r => r.ProductId == productId && r.RuleType == "SimpleRule" && r.TargetTable == targetTable && r.Isalert).ToList();
                    }

                    queriesQuery queryTemplate = _queryTemplates.Items.Where(q => q.type == "SimpleAlert").FirstOrDefault();
                    string s = GenerateCodeHelper(rules, queryTemplate, targetTable);
                    _code = _code.Replace(m.Groups[0].Value, s);
                }
            }
        }

        private void ProcessRulesCodeGen(string productId)
        {
            Regex r1 = new Regex(@"(.*?)\s{0,}=\s{0,}ProcessRules\s{0,}\(\s{0,}(.*?)\s{0,}\)", RegexOptions.IgnoreCase);
            MatchCollection m1 = r1.Matches(_code);
            if (m1 != null && m1.Count > 0)
            {
                foreach (Match m in m1)
                {
                    _statementNumber++;
                    string targetTable = m.Groups[2].Value;
                    if (string.IsNullOrEmpty(targetTable))
                    {
                        targetTable = _DefaultTargetName;
                    }

                    List<Rule> rules = new List<Rule>();
                    if (string.IsNullOrEmpty(productId))
                    {
                        rules = _allrules.Where(r => r.RuleType == "SimpleRule" && r.TargetTable == targetTable).ToList();
                    }
                    else
                    {
                        rules = _allrules.Where(r => r.ProductId == productId && r.RuleType == "SimpleRule" && r.TargetTable == targetTable).ToList();
                    }

                    queriesQuery queryTemplate = _queryTemplates.Items.Where(q => q.type == "SimpleRule").FirstOrDefault();
                    string s = queryTemplate.Value.Replace("$arrayConditions", CreateArrayConditions(rules));
                    s = s.Replace("\n", "\r\n");
                    s = s.Replace("$return", m.Groups[1].Value);
                    s = s.Replace(_DefaultTargetName, targetTable);

                    _code = _code.Replace(m.Groups[0].Value, s);
                }
            }
        }

        private void ProcessAggregateAlertsCodeGen(string productId)
        {
            Regex r1 = new Regex(@"ProcessAggregateAlerts\s{0,}\(\s{0,}(.*?)\s{0,}\)", RegexOptions.IgnoreCase);
            MatchCollection m1 = r1.Matches(_code);
            if (m1 != null && m1.Count > 0)
            {
                foreach (Match m in m1)
                {
                    _statementNumber++;
                    string targetTable = m.Groups[1].Value;
                    if (string.IsNullOrEmpty(targetTable))
                    {
                        targetTable = _DefaultTargetName;
                    }

                    List<Rule> rules = new List<Rule>();
                    if (string.IsNullOrEmpty(productId))
                    {
                        rules = _allrules.Where(r => r.RuleType == "AggregateRule" && r.TargetTable == targetTable && r.Isalert).ToList();
                    }
                    else
                    {
                        rules = _allrules.Where(r => r.ProductId == productId && r.RuleType == "AggregateRule" && r.TargetTable == targetTable && r.Isalert).ToList();
                    }

                    queriesQuery queryTemplate = _queryTemplates.Items.Where(q => q.type == "AggregateAlert").FirstOrDefault();
                    string s = GenerateCodeHelper(rules, queryTemplate, targetTable);
                    _code = _code.Replace(m.Groups[0].Value, s);
                }
            }
        }

        private void ProcessAggregateRulesCodeGen(string productId)
        {
            Regex r1 = new Regex(@"(.*?)\s{0,}=\s{0,}ProcessAggregateRules\s{0,}\(\s{0,}(.*?)\s{0,}\)", RegexOptions.IgnoreCase);
            MatchCollection m1 = r1.Matches(_code);
            if (m1 != null && m1.Count > 0)
            {
                foreach (Match m in m1)
                {
                    _statementNumber++;
                    string targetTable = m.Groups[2].Value;
                    if (string.IsNullOrEmpty(targetTable))
                    {
                        targetTable = _DefaultTargetName;
                    }


                    List<Rule> rules = new List<Rule>();
                    if (string.IsNullOrEmpty(productId))
                    {
                        rules = _allrules.Where(r => r.RuleType == "AggregateRule" && r.TargetTable == targetTable).ToList();
                    }
                    else
                    {
                        rules = _allrules.Where(r => r.ProductId == productId && r.RuleType == "AggregateRule" && r.TargetTable == targetTable).ToList();
                    }

                    queriesQuery queryTemplate = _queryTemplates.Items.Where(q => q.type == "AggregateRule").FirstOrDefault();
                    string s = GenerateCodeHelper(rules, queryTemplate, targetTable);

                    // Now generate the Union statement
                    s += "\n\n--DataXQuery--\n";
                    s += $"ar4_{ _statementNumber} = ";
                    for (int i = 1; i < _ruleCounter; i++)
                    {
                        if (i == _ruleCounter - 1)
                        {
                            s += $"SELECT * FROM ar3_{ _statementNumber}_{i}";
                        }
                        else
                        {
                            s += $"SELECT * FROM ar3_{ _statementNumber}_{i} UNION ";
                        }
                    }

                    // Now return the dataset to user
                    s += "\n\n--DataXQuery--\n";
                    s += $"$return = SELECT * FROM ar4_{ _statementNumber}";

                    s = s.Replace("\n", "\r\n");
                    s = s.Replace("$return", m.Groups[1].Value);

                    _code = _code.Replace(m.Groups[0].Value, s);
                }
            }
        }

        private void ProcessCreateMetrics(string productId)
        {
            Regex r = new Regex(@"(.*?)\s{0,}=\s{0,}CreateMetric\s{0,}\(\s{0,}(.*?)\s{0,},\s{0,}(.*?)\s{0,}\)", RegexOptions.IgnoreCase);
            MatchCollection mc = r.Matches(_code);
            if (mc != null && mc.Count > 0)
            {
                foreach (Match m in mc)
                {
                    if(m==null || m.Groups==null || m.Groups.Count!=4)
                    {
                        throw new Exception("CreateMetrics function call is incorrect. 2 parameters are expected and output needs to be assigned to a table");
                    }

                    string outputTableName = m.Groups[1].Value;
                    string fromTable = m.Groups[2].Value;
                    string metric = m.Groups[3].Value;

                    string s = "\n\n--DataXQuery--\n";
                    s += $"{outputTableName} = SELECT DISTINCT DATE_TRUNC('second', current_timestamp()) AS EventTime, '{outputTableName}' AS MetricName, {metric} AS Metric, '{productId}' AS Product, '' AS Pivot1 FROM {fromTable} GROUP BY EventTime, MetricName, Metric, Product, Pivot1;";
                    _code = _code.Replace(m.Groups[0].Value, s);
                }
            }
        }

        private string CreateArrayConditions(List<Rule> rules)
        {
            if (rules == null || rules.Count <= 0)
            {
                return "'NULL'";
            }

            string result = "filterNull(Array(\n";
            foreach (Rule rule in rules)
            {
                result += "IF($condition, $ruleObject, NULL),\n";
                result = result.Replace("$condition", rule.Condition);
                result = result.Replace("$ruleObject", rule.RulesObject());
            }
            result = result.TrimEnd(',', ' ', '\n');
            result += "\n))";
            return result;
        }

        private string GenerateCodeHelper(List<Rule> rules, queriesQuery queryTemplate, string inputTable = "DataXProcessedInput")
        {
            if (rules == null || rules.Count <= 0)
            {
                return "";
            }

            _ruleCounter = 1;
            string result = "";
            foreach (Rule rule in rules)
            {
                result += queryTemplate.Value.Trim();

                // Apply outputtemplate
                Regex r = new Regex(@"ApplyTemplate\s{0,}\(\s{0,}(.*?)\s{0,},\s{0,}(.*?)\s{0,}\)", RegexOptions.IgnoreCase);
                MatchCollection m1 = r.Matches(result);
                if (m1 != null && m1.Count > 0)
                {
                    foreach (Match m in m1)
                    {
                        outputTemplatesOutputTemplate o = null;
                        if (m.Groups[2].Value == "$outputTemplate")
                        {
                            if (!string.IsNullOrEmpty(rule.OutputTemplate))
                            {
                                o = _outputTemplates.Items.Where(ot => ot.id == rule.OutputTemplate).FirstOrDefault();
                            }
                            else if (rule.RuleType.ToLower().Contains("aggregate"))
                            {
                                o = _outputTemplates.Items.Where(ot => ot.id == "defaultAggOutputTemplate").FirstOrDefault();
                            }
                        }
                        else
                        {
                            o = _outputTemplates.Items.Where(ot => ot.id == m.Groups[2].Value).FirstOrDefault();
                        }

                        if (o == null)
                        {
                            string s = $"SELECT * FROM {m.Groups[1].Value}";
                            result = result.Replace(m.Groups[0].Value, s);
                        }
                        else
                        {
                            string templateValue = o.Value;
                            templateValue = templateValue.Replace("$aggstemplate", rule.AggsToTemplate());
                            templateValue = templateValue.Replace("$pivotstemplate", rule.PivotsToTemplate());
                            string s = $"SELECT {templateValue} FROM {m.Groups[1].Value}";
                            result = result.Replace(m.Groups[0].Value, s);
                        }
                    }
                }

                // If there are no alert sinks OR the output for the simple or aggregate alert is Metrics table only, then we take out the Output statement as we want to send the $tagAlert table to Metrics
                if (rule.Alertsinks == null || (rule.Alertsinks.Count == 1 && rule.Alertsinks[0] == "Metrics"))
                {
                    result = result.Replace("OUTPUT aa3_$ruleCounter TO $alertsinks;", "");
                    result = result.Replace("OUTPUT sa2_$ruleCounter TO $alertsinks;", "");
                }
                else // Else if there are more Alertsinks, then we take out Metrics as a sink since we send $tagAlert table to Metrics explicitly
                {
                    result = result.Replace("$alertsinks", rule.ListToString(rule.Alertsinks.Where(item => item != "Metrics").ToList()));
                }

                result = result.Replace("$productId", rule.ProductId);
                result = result.Replace("$ruleId", rule.RuleId);
                result = result.Replace("$ruleCounter", $"{_statementNumber}_{_ruleCounter}");
                result = result.Replace("$ruleDescription", rule.RuleDescription);
                result = result.Replace("$ruleCategory", rule.RuleCategory);
                result = result.Replace("$ruleType", rule.RuleType);
                result = result.Replace("$severity", rule.Severity);
                result = result.Replace("$aggs", rule.AggsToSelect());
                result = result.Replace("$condition", rule.ConditionToSQL());
                result = result.Replace("$tagname", rule.Tagname);
                result = result.Replace("$tag", rule.Tag);
                result = result.Replace("$sinks", rule.ListToString(rule.Sinks));
                result = result.Replace("$ruleObject", rule.RulesObject());
                result = result.Replace("$id", rule.Id);
                result = result.Replace("$fact", rule.Fact);
                result = result.Replace("DataXProcessedInput", inputTable);
                if (rule.Pivots == null || rule.Pivots.Count <= 0)
                {
                    result = result.Replace("GROUP BY $pivots", "");
                    result = result.Replace("$pivots,", "");
                }
                else
                {
                    result = result.Replace("$pivots", rule.ListToString(rule.Pivots));
                }

                _ruleCounter++;
            }
            result = result.Replace("\n", "\r\n");
            return result;
        }

        private List<Tuple<string, string>> ProcessOutputs()
        {
            Regex r = new Regex(@"OUTPUT\s{1,}(.*?)\s{1,}TO\s{1,}([^;]*);", RegexOptions.IgnoreCase);
            MatchCollection m1 = r.Matches(_code);

            List<Tuple<string, string>> tableSinkMap = new List<Tuple<string, string>>();


            foreach (System.Text.RegularExpressions.Match m in m1)
            {
                var groups = m.Groups;
                var sinks = groups[2].Value.Split(new char[] { ',' });
                foreach (string sink in sinks)
                {
                    tableSinkMap.Add(new Tuple<string, string>(groups[1].Value, sink.Trim()));
                }
                _code = _code.Replace(groups[0].Value, string.Empty);
            }
            return tableSinkMap;
        }

        private MetricsRoot GenerateMetricsConfig(List<Tuple<string, string>> outputSinks, string productId)
        {
            _metricsRoot = new MetricsRoot();
            if (outputSinks == null || outputSinks.Count <= 0)
            {
                return _metricsRoot;
            }

            foreach (var os in outputSinks)
            {
                if (os.Item2.Trim().ToLower() == "metrics")
                {
                    _metricsRoot.AddMetric(os.Item1);
                }
            }

            return _metricsRoot;
        }

        private string RemoveSpuriousQueryStringsAndChars(string formattedSQL)
        {
            formattedSQL = formattedSQL.Trim();
            formattedSQL = formattedSQL.Trim(new char[] { '\n', '\r', '\t' });
            // remove consecutive query strings with empty query section
            Regex r = new Regex(@"--DataXQuery--\s{0,}--DataXQuery--");
            MatchCollection m1 = r.Matches(formattedSQL);

            foreach (System.Text.RegularExpressions.Match m in m1)
            {
                var groups = m.Groups;

                formattedSQL = formattedSQL.Replace(groups[0].Value, @"--DataXQuery--");
                formattedSQL = formattedSQL.Trim(new char[] { '\n', '\r', '\t' });
            }
            //remove any query string appearing at the very end
            formattedSQL = Regex.Replace(formattedSQL, @"--DataXQuery--$", string.Empty);
            formattedSQL = formattedSQL.Trim();

            return formattedSQL;
        }


        private Dictionary<string, string> ProcessAccumulationTables()
        {
            Regex r1 = new Regex(@"CREATE TABLE\s{1,}(.*?)\s{0,}\((.*?)\)\s{0,};", RegexOptions.IgnoreCase);
            MatchCollection m1 = r1.Matches(_code);

            Dictionary<string, string> accumulationTableInfo = new Dictionary<string, string>();

            if (m1 != null && m1.Count > 0)
            {
                foreach (Match m in m1)
                {
                    accumulationTableInfo.Add(m.Groups[1].Value, m.Groups[2].Value);
                    _code = _code.Replace(m.Groups[0].Value, string.Empty);
                }
            }

            _code = _code.Replace("--DataXStates--", string.Empty);

            return accumulationTableInfo;
        }


        private void ProcessUpsert()
        {
            Regex r2 = new Regex(@"\s{0,}--DataXQuery--\s{0,}([^;]*)WITH\s{1,}UPSERT\s{1,}([^;]*)", RegexOptions.IgnoreCase);

            MatchCollection m2 = r2.Matches(_code);
            if (m2 != null && m2.Count > 0)
            {
                foreach (Match m in m2)
                {
                    string newQuery = "\r\n\n--DataXQuery--\r\n" + m.Groups[2].Value.Trim() + " = " + m.Groups[1].Value.Trim() + "\r\n";
                    _code = _code.Replace(m.Groups[0].Value, newQuery);
                }
            }
        }

        private Dictionary<string, string> ProcessTimeWindows()
        {
            Dictionary<string, string> timeWindowInfo = new Dictionary<string, string>();
            Regex r3 = new Regex(@"\s{0,}--DataXQuery--\s{0,}([^;]*?)FROM\s{1,}([^\s]+)(\s{1,})TIMEWINDOW\s{0,}\(\s{0,}(.*?)\s{0,}\)\s{0,}([^;]*?)", RegexOptions.IgnoreCase);

            MatchCollection m3 = r3.Matches(_code);
            if (m3 != null && m3.Count > 0)
            {
                foreach (Match m in m3)
                {
                    var timeWindowStr = Regex.Replace(m.Groups[4].Value.Trim(), "'", string.Empty, RegexOptions.IgnoreCase);
                    var newTableName = m.Groups[2].Value.Trim() + "_" + timeWindowStr.Replace(" ", string.Empty);
                    string newQuery = m.Groups[0].Value;
                    if (!Regex.Equals(m.Groups[2].Value.Trim().ToLower(), @"dataxprocessedinput"))
                    {
                        throw new Exception("'DataXProcessedInput' is the only table for which the TIMEWINDOW can be specified");
                    }
                    else
                    {
                        newQuery = Regex.Replace(newQuery, @"\bDataXProcessedInput\b", newTableName, RegexOptions.IgnoreCase);
                        newQuery = Regex.Replace(newQuery, m.Groups[4].Value.Trim(), "");
                        newQuery = Regex.Replace(newQuery, @"(TIMEWINDOW\s{0,}\(\s{0,}\)\s{0,})", string.Empty, RegexOptions.IgnoreCase);
                        if (!timeWindowInfo.ContainsKey(newTableName))
                        {
                            timeWindowInfo.Add(newTableName, timeWindowStr);
                        }

                        _code = _code.Replace(m.Groups[0].Value, newQuery);
                    }
                }
            }
            return timeWindowInfo;
        }

        private string GetEmbeddedResource(string fileName)
        {
            var assembly = Assembly.GetAssembly(this.GetType());
            string resourceFile = assembly.GetManifestResourceNames().SingleOrDefault(n => n.EndsWith(fileName, StringComparison.InvariantCultureIgnoreCase));

            using (var stream = assembly.GetManifestResourceStream(resourceFile))
            {
                using (var reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    return content;
                }
            }
        }
    }
}
