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
    public class FlattenerMappingExcludeDefaultValue : FlattenerConfig
    {
        [JsonProperty("defaultValue")]
        public JToken DefaultValue { get; set; }

        public override IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            if (jt != null)
            {
                var defaultValue = DefaultValue?.ToString();
                var newValue = jt.ToString();
                if (newValue != defaultValue)
                {
                    yield return Tuple.Create(Namespace, newValue);
                }
            }
        }
    }
}
