// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Text;

namespace DataX.Config.Test.Mock
{
    [Shared]
    [Export]
    [Export(typeof(IConfigGenConfigurationProvider))]
    public class ConfigurationProvider : Dictionary<string, string>, IConfigGenConfigurationProvider
    {
        public IDictionary<string, string> GetAllSettings()
        {
            throw new NotImplementedException();
        }

        public int GetOrder()
        {
            return 1000;
        }

        public bool TryGet(string key, out string value)
        {
            if (this.ContainsKey(key))
            {
                value = this[key];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public void AddCommonData(string dataName, string filePath)
        {
            this.Add(FileBasedCommonDataManager.ComposeConfigSettingName(dataName), filePath);
        }
    }
}
