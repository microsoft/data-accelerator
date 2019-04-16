// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Threading;

namespace DataX.Metrics.Ingestor.Helper
{
    /// <summary>
    /// Factory class to enable dependency injection for <see cref="RMapEventProcessor"/>.
    /// </summary>
    internal class IngestorEventProcessorFactory : IEventProcessorFactory
    {
        private readonly CancellationToken _cancellationToken;
        private readonly ILogger _logger;

        public IngestorEventProcessorFactory(CancellationToken cancellationToken, ILogger logger)
        {
            _cancellationToken = cancellationToken;
            _logger = logger;
        }

        IEventProcessor IEventProcessorFactory.CreateEventProcessor(PartitionContext context)
        {
            var redisConnString = SecretsStore.Instance.GetMetricsRedisConnectionStringAsync().Result;
            var connection = ConnectionMultiplexer.Connect(redisConnString);
            return new IngestorEventProcessor(_cancellationToken, connection, _logger);
        }
    }
}
