// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using DataX.SimulatedData.DataGenService.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.SimulatedData.DataGenService
{
    /// <summary>
    /// Class to randomize data from a given schema
    /// Useful to create data to be sent to an IoT hub or Event Hub to simulate device data
    /// </summary>
    public class DataGen
    {
        private Random _random = new Random();

        /// <summary>
        /// Enables to start with a seed to get consistent results i.e. for testing
        /// </summary>
        /// <param name="randomSeed"></param>
        public DataGen(int randomSeed)
        {
            this._random = new Random(randomSeed);
        }

        public DataGen()
        {
        }

        /// <summary>
        /// Generate random data in the schema specified in inputs
        /// </summary>
        /// <param name="dataStreams"></param>
        /// <param name="ds"></param>
        /// <param name="nodeName"></param>
        /// <param name="counter"></param>
        public void GenerateDataRules(List<JObject> dataStreams, DataObject ds, int counter)
        {
            foreach (var rule in ds.rulesData)
            {
                dataStreams.Add(GenerateRulesData(rule, counter));
            }
        }

        /// <summary>
        /// Generate rules data in the randomized data
        /// </summary>
        /// <param name="dataStreams"></param>
        /// <param name="ds"></param>
        public void GenerateRandomData(List<JObject> dataStreams, DataObject ds)
        {
            for (int i = 0; i < ds.numEventsPerBatch; i++)
            {
                string dataString = "";
                List<string> fieldStrings = new List<string>();
                foreach (var field in ds.fields)
                {
                    string propertyString = "";
                    if (field.type.ToLower() == "struct")
                    {
                        propertyString = GeneratePropertyString(field.properties);
                        string fieldString = $"\"{field.name}\":{{{propertyString}}}";
                        fieldStrings.Add(fieldString);
                    }
                    else
                    {
                        propertyString = GeneratePropertyString(field, true);
                        fieldStrings.Add(propertyString);
                    }
                }

                bool isFirstFieldString = true;
                foreach (var fieldString in fieldStrings)
                {
                    dataString += (isFirstFieldString) ? ("{" + fieldString) : ("," + fieldString);
                    isFirstFieldString = false;
                }
                dataString += "}";
                dataStreams.Add(JObject.Parse(dataString));
            }
        }

        /// <summary>
        /// Return random double
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        public double GetRandomDouble(double minimum, double maximum)
        {
            return _random.NextDouble() * (maximum - minimum) + minimum;
        }

        /// <summary>
        /// Return random int
        /// </summary>
        /// <param name="minimum"></param>
        /// <param name="maximum"></param>
        /// <returns></returns>
        public int GetRandomInt(int minimum, int maximum)
        {
            return _random.Next(minimum, maximum);
        }

        /// <summary>
        /// Generate JSON properties
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="isFirstProperty"></param>
        /// <returns></returns>
        public string GeneratePropertyString(Properties property, bool isFirstProperty)
        {
            string result = "";
            string propertyName = "";
            try
            {
                propertyName = property.name;
                if (property.type.ToLower() != "struct")
                {
                    if (!string.IsNullOrEmpty(property.value))
                    {
                        if (property.type.ToLower() == "long" || property.type.ToLower() == "int" || property.type.ToLower() == "double")
                        {
                            result += (isFirstProperty) ? ($"\"{property.name}\":{property.value}")
                                : ($",\"{property.name}\":{property.value}");
                        }
                        else if (property.type.ToLower() == "string")
                        {
                            result += (isFirstProperty) ? ($"\"{property.name}\":\"{property.value}\"")
                                : ($",\"{property.name}\":\"{property.value}\"");
                        }
                    }
                    else if (!string.IsNullOrEmpty(property.minRange) && !string.IsNullOrEmpty(property.maxRange))
                    {
                        if (!property.castAsString)
                        {
                            if (property.type.ToLower() == "array")
                            {
                                //add first number
                                result += (isFirstProperty) ? ($"\"{property.name}\":[") : ($",\"{property.name}\":[");
                                if (property.length > 0)
                                {
                                    result += GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange));
                                }
                                //add rest of array
                                for (int i = 0; i < property.length - 1; i++)
                                {
                                    result += "," + GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange));
                                }
                                //close array
                                result += "]";
                            }
                            else if (property.type.ToLower() == "long" || property.type.ToLower() == "int")
                            {
                                result += (isFirstProperty) ? ($"\"{property.name}\":{GetRandomInt(int.Parse(property.minRange), int.Parse(property.maxRange))}")
                                                                : ($",\"{property.name}\":{GetRandomInt(int.Parse(property.minRange), int.Parse(property.maxRange))}");
                            }
                            else if (property.type.ToLower() == "decimal" || property.type.ToLower() == "double")
                            {
                                result += (isFirstProperty) ? ($"\"{property.name}\":{GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange))}")
                                                                : ($",\"{property.name}\":{GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange))}");
                            }
                            else
                            {
                                throw new Exception("Unknown type of data being requested");
                            }
                        }
                        else
                        {
                            if (property.type.ToLower() == "array")
                            {
                                //add first number
                                result += (isFirstProperty) ? ($"\"{property.name}\":[") : ($",\"{property.name}\":[");
                                if (property.length > 0)
                                {
                                    result += "\"" + GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange)) + "\"";
                                }
                                //add rest of array
                                for (int i = 0; i < property.length - 1; i++)
                                {
                                    result += "," + "\"" + GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange)) + "\"";
                                }
                                //close array
                                result += "]";
                            }
                            else if (property.type.ToLower() == "long" || property.type.ToLower() == "int")
                            {
                                result += (isFirstProperty) ? ($"\"{property.name}\":\"{GetRandomInt(int.Parse(property.minRange), int.Parse(property.maxRange))}\"")
                                                                : ($",\"{property.name}\":\"{GetRandomInt(int.Parse(property.minRange), int.Parse(property.maxRange))}\"");
                            }
                            else if (property.type.ToLower() == "decimal" || property.type.ToLower() == "double")
                            {
                                result += (isFirstProperty) ? ($"\"{property.name}\":\"{GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange))}\"")
                                                                : ($",\"{property.name}\":\"{GetRandomDouble(double.Parse(property.minRange), double.Parse(property.maxRange))}\"");
                            }
                            else
                            {
                                throw new Exception("Unknown type of data being requested");
                            }
                        }
                    }
                    else if (property.type.ToLower() == "datetime")
                    {
                        result += (isFirstProperty) ? ($"\"{property.name}\":\"{DateTime.UtcNow.AddSeconds(property.utcAddSeconds).ToString(property.datetimeStringFormat)}\"")
                            : ($",\"{property.name}\":\"{DateTime.UtcNow.AddSeconds(property.utcAddSeconds).ToString(property.datetimeStringFormat)}\"");
                    }
                    else if (property.valueList != null)
                    {
                        result += (isFirstProperty) ? ($"\"{property.name}\":\"{property.valueList[GetRandomInt(0, property.valueList.Count)]}\"")
                            : ($",\"{property.name}\":\"{property.valueList[GetRandomInt(0, property.valueList.Count)]}\"");
                    }
                }
                else
                {
                    result += (isFirstProperty) ? ($"\"{property.name}\":{{{GeneratePropertyString(property.properties)}}}") : ($",\"{property.name}\":{{{GeneratePropertyString(property.properties)}}}");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message + " on " + propertyName);
            }
            return result;
        }

        /// <summary>
        /// Generate JSON properties
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        public string GeneratePropertyString(List<Properties> properties)
        {
            string result = "";
            bool isFirstProperty = true;
            foreach (var property in properties)
            {
                result += GeneratePropertyString(property, isFirstProperty);
                isFirstProperty = false;
            }
            return result;
        }

        /// <summary>
        /// Generate JSON based on rules to create alert conditions
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="counter"></param>
        /// <returns></returns>
        public JObject GenerateRulesData(RulesData rule, int counter)
        {
            JObject dataStream = JObject.Parse(rule.dataStream);
            if (rule.triggerConditions != null)
            {
                foreach (var tc in rule.triggerConditions)
                {
                    var parent = dataStream.SelectToken(tc.parentJsonPropertyPath);
                    if (tc.propertyType.ToLower() == "datetime")
                    {
                        parent[tc.propertyName] = DateTime.UtcNow.AddSeconds(tc.utcAddSeconds).ToString(tc.datetimeStringFormat);
                    }
                    else if (tc.propertyType.ToLower() == "double" || tc.propertyType.ToLower() == "decimal")
                    {
                        if (!(tc.ruleNotTriggerTimeInMinutes.Contains(counter)))
                        {
                            if (tc.castAsString)
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleTriggerValue)) ? tc.ruleTriggerValue : GetRandomDouble(tc.ruleTriggerMinRange, tc.ruleTriggerMaxRange).ToString();
                            }
                            else
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleTriggerValue)) ? double.Parse(tc.ruleTriggerValue) : GetRandomDouble(tc.ruleTriggerMinRange, tc.ruleTriggerMaxRange);
                            }
                        }
                        else
                        {
                            if (tc.castAsString)
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleNotTriggerValue)) ? tc.ruleNotTriggerValue : GetRandomDouble(tc.ruleNotTriggerMinRange, tc.ruleNotTriggerMaxRange).ToString();
                            }
                            else
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleNotTriggerValue)) ? double.Parse(tc.ruleNotTriggerValue) : GetRandomDouble(tc.ruleNotTriggerMinRange, tc.ruleNotTriggerMaxRange);
                            }
                        }
                    }
                    else if (tc.propertyType.ToLower() == "int" || tc.propertyType.ToLower() == "long")
                    {
                        if (!(tc.ruleNotTriggerTimeInMinutes.Contains(counter)))
                        {
                            if (tc.castAsString)
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleTriggerValue)) ? tc.ruleTriggerValue : GetRandomInt((int)tc.ruleTriggerMinRange, (int)tc.ruleTriggerMaxRange).ToString();
                            }
                            else
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleTriggerValue)) ? int.Parse(tc.ruleTriggerValue) : GetRandomInt((int)tc.ruleTriggerMinRange, (int)tc.ruleTriggerMaxRange);
                            }
                        }
                        else
                        {
                            if (tc.castAsString)
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleNotTriggerValue)) ? tc.ruleNotTriggerValue : GetRandomInt((int)tc.ruleNotTriggerMinRange, (int)tc.ruleNotTriggerMaxRange).ToString();
                            }
                            else
                            {
                                parent[tc.propertyName] = (!string.IsNullOrEmpty(tc.ruleNotTriggerValue)) ? int.Parse(tc.ruleNotTriggerValue) : GetRandomInt((int)tc.ruleNotTriggerMinRange, (int)tc.ruleNotTriggerMaxRange);
                            }
                        }
                    }
                    else if (tc.propertyType.ToLower() == "string")
                    {
                        if (!(tc.ruleNotTriggerTimeInMinutes.Contains(counter)))
                        {
                            parent[tc.propertyName] = tc.ruleTriggerValue;
                        }
                        else
                        {
                            parent[tc.propertyName] = tc.ruleNotTriggerValue;
                        }
                    }
                }
            }
            return dataStream;
        }
    }
}
