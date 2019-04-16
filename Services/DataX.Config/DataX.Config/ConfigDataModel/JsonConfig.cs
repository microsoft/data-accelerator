// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataX.Config.ConfigDataModel
{
    /// <summary>
    /// Represents an immutable json object enclosing a dynamic object parsed from a JSON string.
    /// </summary>
    public class JsonConfig 
    {
        internal readonly JToken _jt;

        private JsonConfig(string jsonString) : this(JToken.Parse(jsonString))
        {
        }

        private JsonConfig(JToken jt)
        {
            this._jt = jt;
        }

        public static JsonConfig From(string jsonString)
        {
            if (jsonString == null)
            {
                return null;
            }
            else
            {
                return new JsonConfig(jsonString);
            }
        }

        public static JsonConfig From(JToken jt)
        {
            if(jt==null)
            {
                return null;
            }
            else
            {
                return new JsonConfig(jt);
            }
        }

        public static JsonConfig CreateEmpty()
        {
            return From("{}");
        }

        public override bool Equals(object obj)
        {
            if(obj is JsonConfig)
            {
                return _jt == ((JsonConfig)obj)._jt;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get JSON string
        /// </summary>
        /// <returns>A string in JSON</returns>
        public override string ToString()
        {
            return this._jt.ToString();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_jt);
        }

        /// <summary>
        /// Comparing two <see cref="JsonConfig"/> by flattenning the json tree, and then perform full-outer-joining the two key-value pairs on json path
        /// </summary>
        /// <param name="j1">first json config</param>
        /// <param name="j2">second json config</param>
        /// <returns>
        /// List of tuples with first item being the json path,
        /// second item the value from first input json config,
        /// third item the value from the second input json config.
        ///
        /// if one json config doesn't have value at the json path, the corresponding value will be null
        /// </returns>
        public static IEnumerable<Tuple<string, string, string>> Match(JsonConfig j1, JsonConfig j2)
        {
            return Match(j1?._jt, j2?._jt);
        }

        private static IEnumerable<Tuple<string, string, string>> Match(JToken jt1, JToken jt2)
        {
            if (jt1 == null || jt2 == null)
            {
                if (jt1 != null)
                {
                    yield return Tuple.Create(jt1.Path, jt1.ToString(), (string)null);
                }
                else if (jt2 != null)
                {
                    yield return Tuple.Create(jt2.Path, (string)null, jt2.ToString());
                }
            }
            else if(jt1.Type != jt2.Type)
            {
                yield return Tuple.Create(jt1.Path, jt1.ToString(), jt2.ToString());
            }
            else if(jt1.Type == JTokenType.Object)
            {
                foreach(var m in MatchJObject(jt1.Value<JObject>(), jt2.Value<JObject>()))
                {
                    yield return m;
                }
            }
            else if (jt1.Type == JTokenType.Array)
            {
                foreach (var m in MatchJArray(jt1.Value<JArray>(), jt2.Value<JArray>()))
                {
                    yield return m;
                }
            }
            else
            {
                yield return Tuple.Create(jt1.Path, jt1.ToString(), jt2.ToString());
            }
        }

        private static IEnumerable<Tuple<string, string, string>> MatchJObject(JObject jo1, JObject jo2)
        {
            var props1 = jo1.Properties().Select(p => p.Name).ToList();
            var props2 = jo2.Properties().Select(p => p.Name).ToList();

            var exceptions1 = props1.ToHashSet();
            exceptions1.ExceptWith(props2);
            foreach(var name in exceptions1)
            {
                var prop = jo1[name];
                yield return Tuple.Create(prop.Path, prop.ToString(), (string)null);
            }

            var exceptions2 = props2.ToHashSet();
            exceptions2.ExceptWith(props1);
            foreach (var name in exceptions2)
            {
                var prop = jo2[name];
                yield return Tuple.Create(prop.Path, (string)null, prop.ToString());
            }

            var commons = props1.ToHashSet();
            commons.ExceptWith(exceptions1);
            foreach (var name in commons)
            {
                var prop1 = jo1[name];
                var prop2 = jo2[name];
                foreach(var m in Match(prop1, prop2))
                {
                    yield return m;
                }
            }
        }

        private static IEnumerable<Tuple<string, string, string>> MatchJArray(JArray ja1, JArray ja2)
        {
            var i = 0;
            while (i < ja1.Count && i < ja2.Count)
            {
                foreach(var m in Match(ja1[i], ja2[i]))
                {
                    yield return m;
                }

                i++;
            }

            while (i < ja1.Count)
            {
                var jt = ja1[i];
                yield return Tuple.Create(jt.Path, jt.ToString(), (string)null);

                i++;
            }

            while (i < ja2.Count)
            {
                var jt = ja2[i];
                yield return Tuple.Create(jt.Path, (string)null, jt.ToString());

                i++;
            }
        }

        /// <summary>
        /// Merge with the given base config, taking the content of it as the default and overwrite them with the current content in this config
        /// </summary>
        /// <param name="baseConfig">the given base config to provide default values</param>
        /// <returns>A merged config</returns>
        public JsonConfig RebaseOn(JsonConfig baseConfig)
        {
            if (baseConfig == null)
            {
                return this;
            }

            return From(RebaseJToken(baseConfig._jt, _jt));
        }

        public static JToken RebaseJToken(JToken jtBase, JToken jt)
        {
            if (jtBase == null)
            {
                return jt;
            }

            if (jt == null)
            {
                return jtBase;
            }

            if(jt.Type != jtBase.Type)
            {
                return jt;
            }

            switch (jt.Type)
            {
                case JTokenType.Object:
                    return RebaseJObject((JObject)jtBase, (JObject)jt);
                default:
                    return jt;
            }
        }

        public static JObject RebaseJObject(JObject joBase, JObject jo)
        {
            var props = jo.Properties().ToDictionary(p => p.Name, p => p.Value);
            jo.RemoveAll();

            foreach(var prop in joBase)
            {
                var propName = prop.Key;
                if (props.ContainsKey(propName))
                {
                    jo.Add(propName, RebaseJToken(prop.Value, props[propName]));
                    props.Remove(propName);
                }
                else
                {
                    jo.Add(propName, prop.Value);
                }
            }

            foreach(var prop in props)
            {
                jo.Add(prop.Key, prop.Value);
            }

            return jo;
        }

        public T ConvertTo<T>()
        {
            return _jt.ToObject<T>();
        }

        public T GetValue<T>(string path)
        {
            var token = this._jt.SelectToken(path);
            if (token == null)
            {
                return default(T);
            }
            else
            {
                return this._jt.SelectToken(path).Value<T>();
            }
        }

        public static JsonConfigBuilder StartBuild()
        {
            return new JsonConfigBuilder();
        }
        
        public class JsonConfigBuilder
        {
            public JsonConfig _base;
            private readonly List<Func<JObject, JObject>> _operations = new List<Func<JObject, JObject>>();

            public JsonConfigBuilder WithBase(JsonConfig baseConfig)
            {
                _base = baseConfig;
                return this;
            }

            /// <summary>
            /// Replace a top-level member value with the given <see cref="JsonConfig"/>
            /// </summary>
            /// <param name="field">name of the field to replace</param>
            /// <param name="embeded">the json config to put as the value of the field</param>
            /// <returns>the builder</returns>
            public JsonConfigBuilder ReplaceFieldWithConfig(string field, JsonConfig embeded)
            {
                if (embeded != null && !string.IsNullOrWhiteSpace(field))
                {
                    this._operations.Add((jo) => UpsertJToken(jo, field, embeded._jt));
                }

                return this;
            }

            /// <summary>
            /// Replace a top-level member value with the given string
            /// </summary>
            /// <param name="field">name of the field to replace</param>
            /// <param name="value">the string to put as the value of the field</param>
            /// <returns>the builder</returns>
            public JsonConfigBuilder ReplaceFieldWithString(string field, string value)
            {
                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(field))
                {
                    this._operations.Add((jo) => UpsertJToken(jo, field, value));
                }

                return this;
            }

            public JsonConfig Build()
            {
                if (_operations.Count == 0)
                {
                    return _base;
                }
                else
                {
                    var baseJo = (_base?._jt.DeepClone() as JObject) ?? new JObject();

                    foreach(var oper in _operations)
                    {
                        baseJo = oper(baseJo);
                    }

                    return new JsonConfig(baseJo);
                }
            }

            private JObject UpsertJToken(JObject jo, string field, JToken jt)
            {
                var token = jo[field];
                if (token == null)
                {
                    jo.Add(field, jt);
                }
                else
                {
                    token.Replace(jt);
                }

                return jo;
            }
        }
    }
}
