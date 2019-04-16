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
    public class FlattenerMappingStringList : FlattenerConfig
    {
        public const string ValueSeparator = ";";

        public override IEnumerable<Tuple<string, string>> FlattenJToken(JToken jt)
        {
            if (jt != null)
            {
                if(!(jt is JArray ja))
                {
                    throw new ConfigGenerationException($"Expecting an array but encounter type '{jt.Type}', json:{jt}");
                }

                var list = ja.Select(i => i.ToString()).ToList();
                if (list.Count > 0)
                {
                    var value = string.Join(ValueSeparator, list);
                    yield return Tuple.Create(Namespace, value);
                }
            }
        }
    }
}
