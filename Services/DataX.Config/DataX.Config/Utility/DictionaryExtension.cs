// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.Templating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config
{
    public static class DictionaryExtensions
    {
        public static void AppendDictionary<T>(this IDictionary<string, T> origin, IDictionary<string, T> append)
        {
            if (append != null)
            {
                foreach(var item in append)
                {
                    origin[item.Key] = item.Value;
                }
            }
        }

        public static TV GetOrDefault<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV defaultValue)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            else
            {
                return defaultValue;
            }
        }

        public static TokenDictionary ToTokens(this IDictionary<string, string> origin)
        {
            if (origin == null)
            {
                return null;
            }

            var result = new TokenDictionary();
            result.AddBatch(origin.Select(kv => KeyValuePair.Create(kv.Key, Token.FromString(kv.Value))));
            return result;
        }

        public static void MergeKeyList(this IDictionary<string, HashSet<string>> dict, string key, HashSet<string> values)
        {
            if (dict.ContainsKey(key))
            {
                dict[key].UnionWith(values);
            }
            else
            {
                dict.Add(key, values);
            }
        }
    }
}
