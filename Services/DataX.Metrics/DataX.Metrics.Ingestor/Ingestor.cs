// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using DataX.Metrics.Ingestor.Helper;
using DataX.Utilities.KeyVault;
using DataX.ServiceHost.ServiceFabric;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace DataX.Metrics.Ingestor
{
    public enum ServiceState
    {
        Idle = 0,
        Running = 1,
        NeedRestart = 2
    }

    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class Ingestor : StatelessService
    {
        private ServiceState _serviceState = ServiceState.Idle;
        private ILogger _logger;

        public Ingestor(StatelessServiceContext context)
            : base(context)
        {
        }       

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");
                        var host = 
                         new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatelessServiceContext>(serviceContext))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()                                    
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    
                                    .UseUrls(url)
                                    .Build();
                                    _logger = host.Services.GetRequiredService<ILogger<Ingestor>>();
                                    return host;
                                }))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken )
        {
            var eventProcessorHost = await InitalizeEventProcessorHostAsync();
            _logger.LogInformation("MetricsIngestor/Initialize");

            while (true)
            {

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("MetricsIngestor/Heartbeat");

                switch (_serviceState)
                {
                    case ServiceState.Idle:
                        await StartListenerAsync(eventProcessorHost, cancellationToken);
                        break;
                    case ServiceState.NeedRestart:
                        await StopListenerAsync(eventProcessorHost, cancellationToken);
                        await StartListenerAsync(eventProcessorHost, cancellationToken);
                        break;
                    case ServiceState.Running:
                    default:
                        break;
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }

        private async Task StartListenerAsync(EventProcessorHost eventProcessorHost, CancellationToken cancellationToken)
        {
            _logger.LogInformation("StartListenerAsync: MetricsIngestor/Starting");
            // start the event listener  
            await eventProcessorHost.RegisterEventProcessorFactoryAsync(new IngestorEventProcessorFactory(cancellationToken, _logger), GetEventProcessorOptions(60));
            _serviceState = ServiceState.Running;

            _logger.LogInformation("StartListenerAsync: MetricsIngestor/Started");
        }

        private async Task StopListenerAsync(EventProcessorHost eventProcessorHost, CancellationToken cancellationToken)
        {
            _logger.LogInformation("StopListenerAsync: MetricsIngestor/Stopping");

            // detach all readers cleanly
            await eventProcessorHost.UnregisterEventProcessorAsync();
            _serviceState = ServiceState.Idle;

            _logger.LogInformation("StopListenerAsync: MetricsIngestor/Stropped");
        }

        /// <summary>
        /// Initialze a new instance of the event process host 
        /// </summary>
        /// <returns>event processor host</returns>
        private async Task<EventProcessorHost> InitalizeEventProcessorHostAsync()
        {
            // start listening to the event hub 
            var eventHubName = ServiceFabricUtil.GetServiceFabricConfigSetting("EventHubName").Result?.ToString();
            var consumerGroup = ServiceFabricUtil.GetServiceFabricConfigSetting("ConsumerGroupName").Result?.ToString();

            var eventProcessorHost = new EventProcessorHost(
                    eventHubName,
                    consumerGroup,
                    await SecretsStore.Instance.GetMetricsEventHubListenerConnectionStringAsync(),
                    await SecretsStore.Instance.GetMetricsStorageConnectionStringAsync(),
                    "metricsingestor");

            return eventProcessorHost;
        }

        /// <summary>
        ///  Returns the event process options 
        /// </summary>
        private EventProcessorOptions GetEventProcessorOptions(int offsetInSeconds)
        {
            var options = new EventProcessorOptions()
            {
                MaxBatchSize = 500,
                PrefetchCount = 500,
                ReceiveTimeout = TimeSpan.FromSeconds(20),
                InitialOffsetProvider = (partitionId) => EventPosition.FromEnqueuedTime(DateTime.UtcNow.AddSeconds(-offsetInSeconds)),
            };

            options.SetExceptionHandler(new Action<ExceptionReceivedEventArgs>(EventHub_ExceptionReceived));

            return options;
        }

        private void EventHub_ExceptionReceived(ExceptionReceivedEventArgs e)
        {
            // Log the exception.
            var props = new Dictionary<string, string>
            {
                { "action", e.Action }
            };
            _logger.LogError(e.Exception, e.Exception.Message, props);

            _serviceState = ServiceState.NeedRestart;
        }
    }
}
