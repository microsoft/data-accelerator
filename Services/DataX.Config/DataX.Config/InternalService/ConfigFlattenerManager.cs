// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config
{
    [Shared]
    [Export]
    public class ConfigFlattenerManager
    {
        public const string DefaultConfigName = "flattener";

        [ImportingConstructor]
        public ConfigFlattenerManager(ICommonDataManager commonData)
        {
            this.CommonData = commonData;
        }

        private ICommonDataManager CommonData { get; }

        private async Task<ConfigFlattener> GetByName(string name)
        {
            var config = await CommonData.GetByName(DefaultConfigName);
            return ConfigFlattener.From(config);
        }

        public Task<ConfigFlattener> GetDefault()
        {
            return GetByName(DefaultConfigName);
        }
    }
}
