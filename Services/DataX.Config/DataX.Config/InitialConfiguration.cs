// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;

namespace DataX.Config
{
    [Shared]
    [Export(typeof(IConfigGenConfigurationProvider))]
    public class InitialConfiguration : IConfigGenConfigurationProvider
    {
        private static Dictionary<string, string> _InternalDict = new Dictionary<string, string>();

        public static InitialConfiguration Instance = new InitialConfiguration();

        public int GetOrder()
        {
            return 99999;
        }

        public bool TryGet(string key, out string value)
        {
            if (_InternalDict.ContainsKey(key))
            {
                value = _InternalDict[key];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public static void Set(string key, string value)
        {
            _InternalDict[key] = value;
        }

        public static string Get(string key)
        {
            if (_InternalDict.ContainsKey(key))
            {
                return _InternalDict[key];
            }
            else
            {
                throw new ArgumentException($"key '{key}' does not exist in the InitialConfiguration");
            }
        }

        public static void Clear()
        {
            _InternalDict.Clear();
        }

        public IDictionary<string, string> GetAllSettings()
        {
            return _InternalDict;
        }
    }
}
