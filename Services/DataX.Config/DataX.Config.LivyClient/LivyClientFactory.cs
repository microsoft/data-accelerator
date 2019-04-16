// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.LivyClient
{
    [Export(typeof(ISparkJobClientFactory))]
    public class LivyClientFactory : ISparkJobClientFactory
    {
        [ImportingConstructor]
        public LivyClientFactory(ILivyHttpClientFactory httpClientFactory)
        {
            this.HttpClientFactory = httpClientFactory;
        }
        
        private ILivyHttpClientFactory HttpClientFactory { get; }

        /// <summary>
        /// Return job management client for a given cluster connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public Task<ISparkJobClient> GetClient(string connectionString)
        {
            return Task.FromResult((ISparkJobClient)new LivyClient(connectionString, HttpClientFactory));
        }
    }
}
