// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataX.Config.ConfigDataModel
{
    public class FlattenerMappingScopedObject : FlattenerConfig
    {
        [JsonProperty("namespaceField")]
        public string NamespaceField { get; set; }

        [JsonProperty("fields")]
        public Dictionary<string, JToken> Fields { get; set; }

        public override IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            if (jt != null && this.Fields != null && this.Fields.Count > 0)
            {
                var ns = jt[NamespaceField].ToString();

                var mapping = new FlattenerMappingObject()
                {
                    Fields = Fields,
                    Namespace = ns
                };

                foreach (var p in mapping.FlattenJToken(jt))
                {
                    yield return Tuple.Create(BuildPropertyName(Namespace, p.Item1), p.Item2);
                }
            }
        }
    }
}
