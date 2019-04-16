// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigDataModel.CommonData;
using DataX.Contract;
using System;
using System.Composition;
using System.IO;
using System.Threading.Tasks;

namespace DataX.Config
{
    /// <summary>
    /// provide common static data from the same design time storage
    /// </summary>
    [Shared]
    [Export(typeof(ICommonDataManager))]
    public class DesignTimeCommonDataManager : ICommonDataManager
    {
        public const string DataCollectionName = "commons";

        [ImportingConstructor]
        public DesignTimeCommonDataManager(IDesignTimeConfigStorage storage)
        {
            this.Storage = storage;
        }

        private IDesignTimeConfigStorage Storage { get; }

        public async Task<JsonConfig> GetByName(string name)
        {
            var data = await Storage.GetByName(name, DataCollectionName);
            if(data == null)
            {
                return null;
            }
            
            var content = JsonConfig.From(data).ConvertTo<CommonDataConfig>()?.Content?.ToString();

            return JsonConfig.From(content);
        }

        public async Task<Result> SaveByName(string name, JsonConfig content)
        {
            var config = new CommonDataConfig()
            {
                Name = name,
                Content = content._jt
            };

            return await Storage.SaveByName(name, config.ToString(), DataCollectionName);
        }
    }
}
