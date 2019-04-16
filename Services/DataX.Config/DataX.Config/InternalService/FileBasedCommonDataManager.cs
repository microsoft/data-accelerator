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
    /// Provide common static data by loading resource files from local disk
    /// 
    /// This is currently disabled as we switch to <see cref="DesignTimeCommonDataManager"/>
    /// </summary>
    public class FileBasedCommonDataManager : ICommonDataManager
    {
        public const string ConfigSettingNamePrefix = "commondata-";

        [ImportingConstructor]
        public FileBasedCommonDataManager(ConfigGenConfiguration conf)
        {
            Configuration = conf;
        }

        private ConfigGenConfiguration Configuration { get; }

        public async Task<JsonConfig> GetByName(string name)
        {
            if(!Configuration.TryGet(ComposeConfigSettingName(name), out var filePath))
            {
                return null;
            }
            
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);
                return JsonConfig.From(content);
            }
            else
            {
                throw new ConfigGenerationException($"File '{filePath}' does not exist for common data '{name}'");
            }
        }

        public static string ComposeConfigSettingName(string name)
        {
            return ConfigSettingNamePrefix + name;
        }

        public Task<Result> SaveByName(string name, JsonConfig json)
        {
            throw new NotImplementedException();
        }
    }
}
