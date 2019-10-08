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
    public class FlattenerMappingObject: FlattenerConfig
    {

        [JsonProperty("fields")]
        public Dictionary<string, JToken> Fields { get; set; }

        public override IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            if (jt != null && this.Fields != null && this.Fields.Count > 0)
            {
                foreach (var field in this.Fields)
                {
                    var fieldValue = field.Value;
                    var subValue = jt[field.Key];

                    if (subValue?.Type == JTokenType.Date)
                    {
                        subValue = subValue.Value<DateTime>().ToString("o");
                    }

                    switch (fieldValue.Type)
                    {
                        case JTokenType.Object:
                            var subConfig = From(fieldValue);
                            foreach (var subProp in subConfig.FlattenJToken(subValue))
                            {
                                yield return Tuple.Create(BuildPropertyName(Namespace, subProp.Item1), subProp.Item2);
                            }
                            break;

                        default:
                            var settingName = BuildPropertyName(Namespace, fieldValue?.ToString());
                            yield return Tuple.Create(settingName, subValue?.ToString());
                            break;
                    }
                }
            }
        }
    }
}
