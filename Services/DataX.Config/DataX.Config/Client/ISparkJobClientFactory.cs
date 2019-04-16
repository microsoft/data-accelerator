// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System.Threading.Tasks;

namespace DataX.Config
{
    public interface ISparkJobClientFactory
    {
        Task<ISparkJobClient> GetClient(string connectionString);
    }
}
