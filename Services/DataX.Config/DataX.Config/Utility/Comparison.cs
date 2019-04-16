// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataX.Config.Utility
{
    public static class Comparison
    {
        public static bool StringListEquals(IList<string> list1, IList<string> list2)
        {
            if (list1 == null || list2==null)
            {
                return list1 == list2;
            }

            if (list1.Count != list2.Count)
            {
                return false;
            }

            for(var i=0;i<list1.Count;i++)
            {
                if (list1[i] != list2[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool JsonConfigEquals(JsonConfig j1, JsonConfig j2)
        {
            foreach(var m in JsonConfig.Match(j1, j2))
            {
                if (m.Item2 != m.Item3)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool DictionaryEquals(Dictionary<string, string> d1, Dictionary<string, string> d2)
        {
            if (d1 == null || d2 == null)
            {
                return d1 == d2;
            }

            var props1 = d1.Keys;
            var props2 = d2.Keys;
            var set1 = props1.ToHashSet();
            set1.ExceptWith(props2);
            if (set1.Count > 0)
            {
                return false;
            }

            var set2 = props2.ToHashSet();
            set2.ExceptWith(props1);
            if (set2.Count > 0)
            {
                return false;
            }

            foreach(var p in props1)
            {
                if (d1[p] != d2[p])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
