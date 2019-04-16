// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.ConfigDataModel
{
    /// <summary>
    /// Represents a config for flattener
    /// </summary>
    public class FlattenerConfig
    {
        public static FlattenerConfig From(JsonConfig config)
        {
            return From(config?._jt);
        }

        /// <summary>
        /// the namespace as prefix for the flattened properties 
        /// </summary>
        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        /// <summary>
        /// type of flatten method
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// return concret type of flattener based on type of the flattener config
        /// </summary>
        /// <param name="jt">jtoken node from the flattener config</param>
        /// <returns></returns>
        public static FlattenerConfig From(JToken jt)
        {
            var type = jt?.ToObject<FlattenerConfig>()?.Type;
            switch (type)
            {
                case null:
                    return null;
                case "object":
                    return jt.ToObject<FlattenerMappingObject>();
                case "map":
                    return jt.ToObject<FlattenerMappingDictionary>();
                case "array":
                    return jt.ToObject<FlattenerMappingArray>();
                case "scopedObject":
                    return jt.ToObject<FlattenerMappingScopedObject>();
                case "stringList":
                    return jt.ToObject<FlattenerMappingStringList>();
                case "mapProps":
                    return jt.ToObject<FlattenerMappingProps>();
                case "excludeDefaultValue":
                    return jt.ToObject<FlattenerMappingExcludeDefaultValue>();
                default:
                    throw new ConfigGenerationException($"Unknown mapping type '{type}' in flattening the config.");
            }
        }

        /// <summary>
        /// Flatten the given json
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public IEnumerable<Tuple<string, string>> FlattenJsonConfig(JsonConfig json)
        {
            var jt = json?._jt;
            if(jt==null)
            {
                return null;
            }

            return FlattenJToken(jt);
        }

        /// <summary>
        /// Flatten the given jtoken
        /// </summary>
        /// <param name="jt"></param>
        /// <returns></returns>
        public virtual IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            yield return Tuple.Create(Namespace, jt.ToString());
        }

        /// <summary>
        /// build the property name
        /// </summary>
        /// <param name="ns"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected string BuildPropertyName(string ns, string key)
        {
            if (key != null && key.StartsWith("^"))
            {
                return key.Substring(1);
            }
            else
            {
                var prefix = ns == null ? "" : ns + ".";
                return $"{prefix}{key ?? ""}";
            }
        }
    }
}
