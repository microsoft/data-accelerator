// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace DataX.Config.ConfigDataModel
{
    public class FlattenerMappingDictionary : FlattenerConfig
    {

        [JsonProperty("fields")]
        public Dictionary<string, JToken> Fields { get; set; }

        public override IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            if (jt != null && this.Fields != null && this.Fields.Count > 0)
            {
                var jo = jt.Value<JObject>();
                var mapping = new FlattenerMappingObject()
                {
                    Fields = Fields
                };

                foreach (var prop in jo.Properties())
                {
                    var ns = BuildPropertyName(Namespace, prop.Name);

                    foreach (var p in mapping.FlattenJToken(prop.Value))
                    {
                        yield return Tuple.Create(BuildPropertyName(ns, p.Item1), p.Item2);
                    }
                }
            }
        }
    }
}
