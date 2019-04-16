// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataX.Flow.SchemaInference
{
    public class Engine
    {
        private object _result = null;
        private List<string> _errors = new List<string>();

        /// <summary>
        /// Gets schema of the event message. It will return the union schema across all events of the array
        /// </summary>
        /// <param name="events">JSON array whose each item is an event</param>
        /// <returns></returns>
        public SchemaResult GetSchema(string events)
        {
            JArray array = JArray.Parse(events);

            if(array==null || array.Count<=0)
            {
                _errors.Add("No data to evaluate schema for.");
                return new SchemaResult() { Errors = _errors };
            }

            if(array.First.GetType()==typeof(JObject))
            {
                _result = new StructObject();
                foreach (JObject jObject in array)
                {
                    GetSchemaStruct(jObject, _result as StructObject, null);
                }
            }
            else if(array.First.GetType()== typeof(JArray))
            {
                _result = new Type();
                foreach (JArray jArray in array)
                {
                    GetSchemaArray(jArray, _result as Type, null);
                }
            }

            string output = JsonConvert.SerializeObject(_result);

            SchemaResult schemaResult = new SchemaResult
            {
                Schema = output,
                Errors = _errors
            };
            return schemaResult;
        }

        /// <summary>
        /// Gets the schema of the event message. It will return the union schema of all events in the list
        /// </summary>
        /// <param name="events">List of events whose schema needs to be determined. Each event is a JSON string</param>
        /// <returns></returns>
        public SchemaResult GetSchema(List<string> events)
        {
            if(events==null || events.Count<=0)
            {
                return new SchemaResult();
            }

            if(events[0].Trim().StartsWith('{'))
            {
                _result = new StructObject();
                foreach (string str in events)
                {
                    JObject jObject = JObject.Parse(str);
                    GetSchemaStruct(jObject, _result as StructObject, null);
                }
            }
            else if(events[0].Trim().StartsWith('['))
            {
                _result = new Type();
                foreach (string str in events)
                {
                    JArray jArray = JArray.Parse(str);
                    GetSchemaArray(jArray, _result as Type, null);
                }
            }

            string output = JsonConvert.SerializeObject(_result);

            SchemaResult schemaResult = new SchemaResult
            {
                Schema = output,
                Errors = _errors
            };
            return schemaResult;
        }

        /// <summary>
        /// Determine the schema of each element of the jArray
        /// </summary>
        /// <param name="jArray">Array data to determine the schema for</param>
        /// <param name="type">Schema is populated in this field</param>
        /// <param name="keyPath">Path to the key to determine schema for</param>
        private void GetSchemaArray(JArray jArray, Type type, string keyPath)
        {
            if (type == null && string.IsNullOrEmpty(_errors.Where(e => e.Contains(keyPath)).FirstOrDefault()))
            {
                _errors.Add($"Error in generating schema for '{keyPath}'. Result holder is null");
                return;
            }

            if (jArray == null || jArray.Count<=0)
            {
                type.elementType = new Type();
                (type.elementType as Type).elementType = "string";
                return;
            }

            // This means that the array itself has children that are arrays
            if(jArray.First.GetType() == typeof(JArray))
            {
                keyPath = keyPath + ".array";

                // Check if element type already exists. If not create it. If it exists, ensure the type is as expected.
                if (type.elementType == null)
                {
                    type.elementType = new Type();
                }
                else if (type.elementType.GetType() != typeof(Type))
                {
                    if (string.IsNullOrEmpty(_errors.Where(e => e.Contains($"'{keyPath}'")).FirstOrDefault()))
                    {
                        _errors.Add($"Conflict in schema. Key with path '{keyPath}' has different types");
                    }
                    return;
                }

                foreach (JArray arrayItem in jArray)
                {
                    GetSchemaArray(arrayItem as JArray, type.elementType as Type, keyPath);
                }
            }

            // This means that the array has children of type Struct objects
            if(jArray.First.GetType() == typeof(JObject))
            {
                // Check if element type already exists. If not create it. If it exists, ensure the type is as expected.
                if (type.elementType == null)
                {
                    type.elementType = new StructObject();
                }
                else if (type.elementType.GetType() != typeof(StructObject))
                {
                    if (string.IsNullOrEmpty(_errors.Where(e => e.Contains($"'{keyPath}'")).FirstOrDefault()))
                    {
                        _errors.Add($"Conflict in schema. Key with path '{keyPath}' has different types");
                    }
                    return;
                }

                foreach(JObject arrayItem in jArray)
                {
                    GetSchemaStruct(arrayItem, type.elementType as StructObject, keyPath);
                }
            }

            // This means that the array has children of simple type
            if (jArray.First.GetType() == typeof(JValue))
            {
                string childType = GetObjectType(jArray.First);

                if (type.elementType == null)
                {
                    type.elementType = childType;
                }
                else if (type.elementType.GetType()!=childType.GetType() || type.elementType.ToString().ToLower() != childType.ToLower())
                {
                    if (string.IsNullOrEmpty(_errors.Where(e => e.Contains($"'{keyPath}'")).FirstOrDefault()))
                    {
                        _errors.Add($"Conflict in schema. Key with path '{keyPath}' has different types");
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Determine the schema of the Struct
        /// </summary>
        /// <param name="jObject">Object to determine the schema for</param>
        /// <param name="structObject">Schema is populated in this field</param>
        /// <param name="keyPath">Path to the key to determine schema for</param>
        private void GetSchemaStruct(JObject jObject, StructObject structObject, string keyPath)
        {
            if (structObject == null && string.IsNullOrEmpty(_errors.Where(e => e.Contains(keyPath)).FirstOrDefault()))
            {
                _errors.Add($"Error in generating schema for '{keyPath}'. Result holder is null");
                return;
            }

            foreach (KeyValuePair<string, JToken> child in jObject)
            {
                string childkeyPath = $"{keyPath}.{child.Key}";

                // This means the child is of type Struct object
                if (child.Value.GetType() == typeof(JObject))
                {
                    // Check if the field already exists. If not, create it. If it exists, ensure the type is as expected.
                    Field fKey = structObject.fields.Where(field => field.name == child.Key).FirstOrDefault();
                    StructObject childStructObject = null;
                    if (fKey == null)
                    {
                        childStructObject = new StructObject();
                        structObject.fields.Add(new Field(child.Key, childStructObject));
                    }
                    else if (fKey.type.GetType() == typeof(StructObject))
                    {
                        childStructObject = fKey.type as StructObject;
                    }
                    else 
                    {
                        if (string.IsNullOrEmpty(_errors.Where(e => e.Contains($"'{childkeyPath}'")).FirstOrDefault()))
                        {
                            _errors.Add($"Conflict in schema. Key with path '{childkeyPath}' has different types");
                        }
                        continue;
                    }

                    GetSchemaStruct(child.Value as JObject, childStructObject, childkeyPath);
                }

                // This means the child is of type Array
                if (child.Value.GetType() == typeof(JArray))
                {
                    // Check if the field already exists. If not, create it. If it exists, ensure the type is as expected.
                    Field fKey = structObject.fields.Where(f => f.name == child.Key).FirstOrDefault();
                    Type childType = null;
                    if (fKey == null)
                    {
                        childType = new Type();
                        structObject.fields.Add(new Field(child.Key, childType));
                    }
                    else if (fKey.type.GetType() == typeof(Type))
                    {
                        childType = fKey.type as Type;
                    }
                    else 
                    {
                        if (string.IsNullOrEmpty(_errors.Where(e => e.Contains($"'{childkeyPath}'")).FirstOrDefault()))
                        {
                            _errors.Add($"Conflict in schema. Key with path '{childkeyPath}' has different types");
                        }
                        continue;
                    }

                    GetSchemaArray(child.Value as JArray, childType, childkeyPath);
                }

                // This means the child is of simple type
                if (child.Value.GetType() == typeof(JValue))
                {
                    // Check if the field already exists. If not, create it. If it exists, ensure the type is as expected.
                    Field fKey = structObject.fields.Where(field => field.name == child.Key).FirstOrDefault();
                    string type = GetObjectType(child.Value);

                    if (fKey == null)
                    {
                        structObject.fields.Add(new Field(child.Key, type));
                    }
                    // If the key already exists, but is not of the same type then there is conflict in schema
                    else if (fKey.type.GetType() != type.GetType() || fKey.type.ToString() != type.ToString())
                    {
                        if (string.IsNullOrEmpty(_errors.Where(error => error.Contains($"'{childkeyPath}'")).FirstOrDefault()))
                        {
                            _errors.Add($"Conflict in schema. Key with path '{childkeyPath}' has different types");
                        }
                    }
                }

            }
        }

        private string GetObjectType(JToken token)
        {
            string type = token.Type.ToString().ToLower();

            // Note: Order is important below. Do not change the following order
            if (type == "integer")
            {
                type = "long";
            }
            else if (type == "float")
            {
                type = "double";
            }
            else if (double.TryParse(token.ToString(), out double d2))
            {
                type = "string";
            }
            else if (DateTime.TryParse(token.ToString(), out DateTime d))
            {
                type = "string";
            }

            return type;
        }

    }
}
