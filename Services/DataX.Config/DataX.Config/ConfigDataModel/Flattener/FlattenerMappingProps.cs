// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DataX.Config.ConfigDataModel
{
    public class FlattenerMappingProps : FlattenerConfig
    {
        public override IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            if (jt != null)
            {
                if(!(jt is JObject jo))
                {
                    throw new ConfigGenerationException($"Expecting an array but encounter type '{jt.Type}', json:{jt}");
                }

                foreach(var prop in jo.Properties())
                {
                    var ns = BuildPropertyName(Namespace, prop.Name);
                    yield return Tuple.Create(ns, prop.Value.ToString());
                }
            }
        }
    }
}
