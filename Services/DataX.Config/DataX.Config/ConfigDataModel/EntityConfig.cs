// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataX.Config.ConfigDataModel
{
    public abstract class EntityConfig
    {
        public const string ConfigFieldName_Name = "name";

        protected EntityConfig()
        {
        }

        public static T ConvertFrom<T>(JsonConfig json) where T: EntityConfig
        {
            if (json == null)
            {
                return null;
            }
            else
            {
                var result = json.ConvertTo<T>();
                return result;
            }
        }

        public static IEnumerable<Tuple<string, string, string>> Match(EntityConfig e1, EntityConfig e2)
        {
            if (e1 != null && e2 != null)
            {
                return JsonConfig.Match(e1.ToJson(), e2.ToJson());
            }
            else
            {
                return Enumerable.Empty<Tuple<string, string, string>>();
            }
        }

        public JsonConfig ToJson()
        {
            return JsonConfig.From(ToString());
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
