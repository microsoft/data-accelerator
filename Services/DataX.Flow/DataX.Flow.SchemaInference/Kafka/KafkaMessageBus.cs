// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Confluent.Kafka;
using DataX.Config;
using DataX.Config.ConfigDataModel;
using DataX.ServiceHost.ServiceFabric;
using DataX.Utilities.KeyVault;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DataX.Flow.SchemaInference.Kafka
{
    public class KafkaMessageBus : IMessageBus, IDisposable
    {
        private readonly string _brokerList = string.Empty;
        private readonly string _connectionString = null;
        private readonly List<string> _topics = null;
        private readonly string _cacertLocation = @".\cacert.pem";
        private readonly string _consumerGroup = string.Empty;
        private readonly string _inputType = string.Empty;
        private readonly ILogger _logger;
        private bool _timeout = false;
        private Timer _timer;

        public KafkaMessageBus(string brokerList, string connectionString, List<string> topics, string consumerGroup, string inputType, ILogger logger)
        {
            if (!File.Exists(_cacertLocation))
            {
                var certSource = KeyVault.GetSecretFromKeyvault(ServiceFabricUtil.GetServiceFabricConfigSetting("CACertificateLocation").Result.ToString());

                WebClient webClient = new WebClient();
                webClient.DownloadFile(certSource, _cacertLocation);
            }

            _brokerList = brokerList;
            _connectionString = connectionString;
            _topics = topics;
            _consumerGroup = consumerGroup;
            _inputType = inputType;
            _logger = logger;

        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void StartTimer(int seconds)
        {
            _timer = new Timer(seconds * 1000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _timeout = true;
            _timer.Stop();
        }

        public async Task<EventsData> GetSampleEvents(int seconds)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _brokerList,
                GroupId = _consumerGroup,
                EnableAutoCommit = false,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Latest
            };

            // Set the authentication for EventHub Kafka
            if (_inputType == Constants.InputType_KafkaEventHub)
            {
                config.SecurityProtocol = SecurityProtocol.SaslSsl;
                config.SaslMechanism = SaslMechanism.Plain;
                config.SaslUsername = "$ConnectionString";
                config.SaslPassword = _connectionString;
                config.SslCaLocation = _cacertLocation;
            }

            StartTimer(seconds);

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
                            var consumeResult = consumer.Consume(new TimeSpan(0,0, seconds));

                            if (_timeout)
                            {
                                _logger.LogInformation($"Closing consumer");
                                consumer.Close();
                                return await Task.FromResult(eventsData).ConfigureAwait(false);
                            }

                            if (consumeResult.IsPartitionEOF)
                            {

                                _logger.LogInformation($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                                continue;
                            }

                            _logger.LogInformation($"Received message at {consumeResult.TopicPartitionOffset}: {consumeResult.Value}");

                            // Get raw data
                            EventRaw er = new EventRaw
                            {
                                Raw = consumeResult.Value,
                                Properties = new Dictionary<string, string>() { { "HeadersCount", consumeResult.Headers.Count.ToString() } },
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
                            er.SystemProperties.Add("Partition", consumeResult.Partition.Value.ToString());
                            er.SystemProperties.Add("Offset", consumeResult.Offset.Value.ToString());
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
                    return await Task.FromResult(eventsData).ConfigureAwait(false);
                }
            }
        }

    }
}
