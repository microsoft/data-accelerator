using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference
{
    public class EventhubMessageBus : IMessageBus
    {
        private readonly EventProcessorHost _eventProcessorHost = null;
        private readonly List<string> _eventhubNames = null;
        private readonly string _eventhubConnectionString = null;
        private readonly string _checkpointContainerName = "";
        private readonly string _storageConnectionString = "";
        private readonly string _inputType = "";
        private readonly ILogger _logger;

        public EventhubMessageBus(List<string> eventhubNames, string consumerGroup, string eventhubConnectionString, string storageConnectionString, string checkpointContainerName, string inputType, ILogger logger)
        {
            _logger = logger;
            _eventhubNames = eventhubNames;
            _eventhubConnectionString = eventhubConnectionString;
            _storageConnectionString = storageConnectionString;
            _checkpointContainerName = checkpointContainerName;
            _inputType = inputType;


            _eventProcessorHost = new EventProcessorHost(eventhubNames[0], consumerGroup, eventhubConnectionString, storageConnectionString, checkpointContainerName);
        }

        /// <summary>
        /// Gets Sample Events
        /// </summary>
        /// <param name="seconds">seconds for which the sample data is fetched</param>
        /// <returns>Returns EventsData object</returns>
        public async Task<EventsData> GetSampleEvents(int seconds)
        {
            // Registers the Event Processor Host and starts receiving messages
            EventProcessorFactory eventProcessorFactory = new EventProcessorFactory();
            await _eventProcessorHost.RegisterEventProcessorFactoryAsync(eventProcessorFactory, GetEventProcessorOptions());

            Thread.Sleep(seconds * 1000);

            // Disposes of the Event Processor 
            await _eventProcessorHost.UnregisterEventProcessorAsync();

            return eventProcessorFactory.EventsData;
        }

        /// <summary>
        ///  Returns the event process options 
        /// </summary>
        private EventProcessorOptions GetEventProcessorOptions()
        {
            var options = new EventProcessorOptions()
            {
                MaxBatchSize = 500,
                PrefetchCount = 500,
                ReceiveTimeout = TimeSpan.FromSeconds(20),
                InitialOffsetProvider = (partitionId) => DateTime.UtcNow.AddSeconds(-60)  // start from 60 second back since it is possible that the data may not be flowing for the last 40 seconds or so.
            };
            return options;
        }
    }
}
