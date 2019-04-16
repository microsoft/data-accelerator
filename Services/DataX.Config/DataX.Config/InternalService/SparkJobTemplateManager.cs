// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config
{
    [Shared]
    [Export]
    public class SparkJobTemplateManager
    {
        public const string DataCollectionName = "sparkJobTemplates";

        [ImportingConstructor]
        public SparkJobTemplateManager([Import] ICommonDataManager storage)
        {
            this.Storage = storage;
        }

        private ICommonDataManager Storage { get; }

        public Task<JsonConfig> GetByName(string name)
        {
            return Storage.GetByName(name);
        }
    }
}
