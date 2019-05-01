using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference.Kafka
{
    public class KafkaMessageBus : IMessageBus
    {
        private readonly string _brokerList = string.Empty;
        private readonly string _connectionString = null;
        private readonly List<string> _topics = null;
        private readonly string _cacertLocation = string.Empty;
        private readonly string _consumerGroup = string.Empty;
        private readonly string _inputType = string.Empty;
        private readonly ILogger _logger;

        //public KafkaMessageBus(List<string> eventhubNames, string consumerGroup, string eventhubConnectionString, string inputType, ILogger logger)
        public KafkaMessageBus(string brokerList, string connectionString, List<string> topics, string cacertLocation, string consumerGroup, string inputType, ILogger logger)
        {
            _brokerList = brokerList;
            _connectionString = connectionString;
            _topics = topics;
            _cacertLocation = cacertLocation;
            _consumerGroup = consumerGroup;
            _inputType = inputType;
            _logger = logger;
        }


        public async Task<EventsData> GetSampleEvents(int seconds)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _brokerList,
                GroupId = _consumerGroup,
                EnableAutoCommit = false,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = true,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "$ConnectionString",
                SaslPassword = _connectionString,
                SslCaLocation = _cacertLocation,
            };

            const int commitPeriod = 5;

            using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
            {
                EventsData eventsData = new EventsData();

                consumer.Subscribe(_topics);

                try
                {
                    while (true)
                    {
                        try
                        {
                            CancellationTokenSource cts = new CancellationTokenSource();
                            cts.CancelAfter(seconds * 1000);
                            var consumeResult = consumer.Consume(cts.Token);

                            if (consumeResult.IsPartitionEOF)
                            {
                                
                                _logger.LogInformation($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                                continue;
                            }

                            _logger.LogInformation($"Received message at {consumeResult.TopicPartitionOffset}: {consumeResult.Value}");

                            // Get raw data
                            EventRaw er = new EventRaw
                            {
                                Raw = consumeResult.Value
                            };

                            // Set properties (using the Headers)
                            if (consumeResult.Headers != null && consumeResult.Headers.Count > 0)
                            {
                                for (int i = 0; i < consumeResult.Headers.Count; i++)
                                {
                                    string key = consumeResult.Headers[i].Key;
                                    string val = System.Text.Encoding.UTF8.GetString(consumeResult.Headers[i].GetValueBytes());
                                    er.Properties.Add(key, val);
                                }
                            }

                            // Set the SystemProperties
                            er.SystemProperties.Add("Topic", consumeResult.Topic);
                            er.SystemProperties.Add("Partition", consumeResult.Partition.ToString());
                            er.SystemProperties.Add("Offset", consumeResult.Offset.ToString());
                            er.SystemProperties.Add("UtcDateTime", consumeResult.Timestamp.UtcDateTime.ToString());
                            er.SystemProperties.Add("UnixTimestampMs", consumeResult.Timestamp.UnixTimestampMs.ToString());

                            er.Json = JsonConvert.SerializeObject(er);

                            eventsData.EventsJson += er.Json + "\r\n";
                            eventsData.Events.Add(er);

                            if (consumeResult.Offset % commitPeriod == 0)
                            {
                                try
                                {
                                    consumer.Commit(consumeResult);
                                }
                                catch (KafkaException e)
                                {
                                    _logger.LogError($"Commit error: {e.Error.Reason}\n{e.ToString()}");
                                }
                            }
                        }
                        catch (ConsumeException e)
                        {
                            _logger.LogError($"Consume error: {e.Error.Reason}\n{e.ToString()}");
                        }
                    }
                }
                catch (OperationCanceledException e)
                {
                    _logger.LogInformation($"Closing consumer");
                    consumer.Close();
                    return eventsData;
                }
            }
        }

    }
}
