// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.DatabricksClient
{
    [Export(typeof(ISparkJobClientFactory))]
    public class DatabricksClientFactory : ISparkJobClientFactory
    {
        [ImportingConstructor]
        public DatabricksClientFactory(IDatabricksHttpClientFactory httpClientFactory)
        {
            this.HttpClientFactory = httpClientFactory;
        }

        private IDatabricksHttpClientFactory HttpClientFactory { get; }

        public Task<ISparkJobClient> GetClient(string connectionString)
        {
            return Task.FromResult((ISparkJobClient)new DatabricksClient(connectionString, HttpClientFactory));
        }
    }
}
