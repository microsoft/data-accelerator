// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config.Local
{
    /// <summary>
    /// Factory class for local spark client
    /// </summary>
    [Export(typeof(ISparkJobClientFactory))]
    public class LocalSparkClientFactory : ISparkJobClientFactory
    {
        private ILogger _logger { get; }

        [ImportingConstructor]
        public LocalSparkClientFactory(ILogger<LocalSparkClientFactory> logger)
        {
            this._logger = logger;
        }

        public async Task<ISparkJobClient> GetClient(string connectionString)
        {
            await Task.Yield();
            return (ISparkJobClient)new LocalSparkClient(_logger);
        }
    }
}
