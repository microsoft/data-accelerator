// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Metrics.Ingestor.Helper
{
    /// <summary>
    /// Event Processor to handle reading from the event hub and handling the metrics ingestion
    /// </summary>
    public sealed class IngestorEventProcessor : IEventProcessor
    {
        private readonly CancellationToken _cancellationToken;
        private static readonly DateTime _EpochZero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private readonly IDatabase _cache;
        private readonly ILogger _logger;

        public IngestorEventProcessor(CancellationToken cancellationToken, ConnectionMultiplexer redisConnection, ILogger logger)
        {
            _cancellationToken = cancellationToken;

            _cache = redisConnection.GetDatabase();
            _logger = logger;
        }

        Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            var props = new Dictionary<string, string>
            {
                { "Partition", context.Lease.PartitionId },
                { "Offset", context.Lease.Offset },
                { "Reason", reason.ToString() }
            };

            _logger.LogInformation("EventHubReader/Closed", props);
            return Task.FromResult<object>(null);
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            var props = new Dictionary<string, string>
            {
                { "Partition", context.Lease.PartitionId },
                { "Offset", context.Lease.Offset }
            };

            _logger.LogInformation("EventHubReader/Open", props);

            return Task.FromResult<object>(null);
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Handle each batch of events we get from the event hub and inject into redis
        /// </summary>
        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                int messageCount = 0;
                int metricCount = 0;
                foreach (EventData eventData in messages)
                {
                    messageCount++;
                    var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    StringReader reader = new StringReader(data);

                    string line = null;
                    while (null != (line = await reader.ReadLineAsync()))
                    {
                        try
                        {
                            var metricOutput = GenerateRow(line);
                            if (metricOutput != null)
                            {
                                metricCount++;
                                _cache.SortedSetAdd(metricOutput.RedisKey, metricOutput.Content, metricOutput.EpochTime, When.NotExists, CommandFlags.None);
                            }
                        }

                        catch (Exception e)
                        {
                            _logger.LogError(e, e.Message, new Dictionary<string, string>()
                        {
                            {"MetricData", line},
                            {"PartitionId", context.Lease.PartitionId},
                            {"Offset", context.Lease.Offset},
                        });
                        }
                    }
                }

                sw.Stop();

                _logger.LogInformation($"EventHubReader/ProcessMessages/Completed",
                    new Dictionary<string, string>()
                    {
                        {"PartitionId", context.Lease.PartitionId},
                        {"Offset", context.Lease.Offset},
                    },
                    new Dictionary<string, double>()
                    {
                        {"MessagesRecieved", messageCount},
                        {"MetricsSent", metricCount},
                        {"ElapsedTimeInMs",  sw.ElapsedMilliseconds}
                    });
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Exception thrown in ProcessEventsAsync :"+e.Message);
            }
        }

        /// <summary>
        /// Generate a row of metrics for the dashboard based on inputs
        /// The eventtime will be in millisecond from epoch
        /// </summary>
        /// <param name="line">raw data</param>
        /// <param name="redisKey">flowname:metricname</param>
        /// <param name="content">uts:long, value:double</param>
        /// <param name="epochTime">currenttime</param>
        public static MetricOutput GenerateRow(string line)
        {
            try
            {
                MetricItem metricItem = new MetricItem();
                MetricOutput metricOutput = new MetricOutput();

                var jObj = JObject.Parse(line);
                foreach (JProperty property in jObj.Properties())
                {
                    switch (property.Name.ToLower())
                    {
                        case "eventtime":
                        case "uts":
                            {
                                metricItem.EventTime = property.Value.ToString();
                                break;
                            }
                        case "metricname":
                        case "met":
                            {
                                metricItem.MetricName = property.Value.ToString();
                                break;
                            }
                        case "metric":
                        case "val":
                            {
                                metricItem.Metric = property.Value.ToString();
                                break;
                            }
                        case "product":
                        case "app":
                            {
                                metricItem.Product = property.Value.ToString();
                                break;
                            }
                        case "pivot1":
                            {
                                metricItem.Pivot = property.Value.ToString();
                                break;
                            }
                    }
                }

                //sets up time
                metricOutput.EpochTime = GetEpochTime();
                ulong eventTimeInMilliSeconds = (ulong)metricOutput.EpochTime;

                metricOutput.RedisKey = metricItem.Product + ":" + metricItem.MetricName;
                var pivot = $"\"{metricItem.Pivot}\"";
                metricOutput.Content = "{\"uts\":" + eventTimeInMilliSeconds + ", \"val\":" + metricItem.Metric + ", \"pivot1\":" + pivot + "}";

                return metricOutput;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Format time from incoming data into exepcted fromEpoch time
        /// </summary>
        private static double GetEpochTime()
        {
            //always use server time when possible to avoid delays from client
            return (ulong)DateTime.UtcNow.Subtract(_EpochZero).TotalMilliseconds;
        }
    }
}
