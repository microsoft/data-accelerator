// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Test.Mock
{
    [Shared]
    [Export(typeof(ISparkJobClientFactory))]
    public class SparkJobClientFactory : ISparkJobClientFactory
    {
        public Task<ISparkJobClient> GetClient(string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
