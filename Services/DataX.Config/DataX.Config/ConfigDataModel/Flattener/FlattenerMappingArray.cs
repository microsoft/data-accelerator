// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataX.Config.ConfigDataModel
{
    public class FlattenerMappingArray : FlattenerConfig
    {
        [JsonProperty("element")]
        public FlattenerMappingScopedObject Element { get; set; }

        public override IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            if (jt != null && this.Element != null)
            {
                if (!(jt is JArray ja))
                {
                    throw new ConfigGenerationException($"expected array but encounter json:'{jt.ToString()}'");
                }

                return ja.SelectMany(p => Element.FlattenJToken(p)
                .Select(t => Tuple.Create(BuildPropertyName(Namespace, t.Item1), t.Item2)));
            }
            else
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }
        }
    }
}
