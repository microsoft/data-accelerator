// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Threading.Tasks;
using DataX.Config.ConfigDataModel;
using DataX.Contract;

namespace DataX.Config
{
    public interface ICommonDataManager
    {
        Task<JsonConfig> GetByName(string name);

        Task<Result> SaveByName(string name, JsonConfig json);
    }
}
