// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.EventHubs;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DataX.SimulatedData.DataGenService.Model;
using DataX.Utilities.KeyVault;
using Confluent.Kafka;

namespace DataX.SimulatedData.DataGenService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class DataGenService : StatelessService
    {
        private static int _counter = 1;

        public DataGenService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return Array.Empty<ServiceInstanceListener>();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
            var inputConfig = configPackage.Settings.Sections["InputConfig"];

            KeyVaultManager keyManager = new KeyVaultManager();
            string keyVaultName = inputConfig.Parameters["KeyVaultName"].Value;
            string iotDeviceConnectionStringKeyVaultKeyName = inputConfig.Parameters["IotDeviceConnectionStringKeyVaultKeyName"].Value;
            string eventhubConnectionStringKeyVaultKeyName = inputConfig.Parameters["EventhubConnectionStringKeyVaultKeyName"].Value;
            string dataSchemaStorageAccountKeyValueKeyVaultKeyName = inputConfig.Parameters["DataSchemaStorageAccountKeyValueKeyVaultKeyName"].Value;
            string iotDeviceConnectionString = (iotDeviceConnectionStringKeyVaultKeyName.Length > 0) ? await keyManager.GetSecretStringAsync(keyVaultName, iotDeviceConnectionStringKeyVaultKeyName) : "";
            string ehConnectionString = (eventhubConnectionStringKeyVaultKeyName.Length > 0) ? await keyManager.GetSecretStringAsync(keyVaultName, eventhubConnectionStringKeyVaultKeyName) : "";
            string dataSchemaStorageAccountName = inputConfig.Parameters["DataSchemaStorageAccountName"].Value;
            string dataSchemaStorageAccountKeyValue = (dataSchemaStorageAccountKeyValueKeyVaultKeyName.Length > 0) ? await keyManager.GetSecretStringAsync(keyVaultName, dataSchemaStorageAccountKeyValueKeyVaultKeyName) : "";
            string dataSchemaStorageContainerName = inputConfig.Parameters["DataSchemaStorageContainerName"].Value;
            string dataSchemaPathWithinContainer = inputConfig.Parameters["DataSchemaPathWithinContainer"].Value;

            KafkaConnection kafkaConn = new KafkaConnection
            {
                Broker = inputConfig.Parameters["KafkaBroker"].Value,
                Topics = (inputConfig.Parameters["KafkaTopics"].Value.Length > 0) ? Array.ConvertAll(inputConfig.Parameters["KafkaTopics"].Value.Split(','), p => p.Trim()).ToList() : new List<string>(),
                ConnectionString = (inputConfig.Parameters["KafkaConnectionStringKeyVaultKeyName"].Value.Length > 0) ? await keyManager.GetSecretStringAsync(keyVaultName, inputConfig.Parameters["KafkaConnectionStringKeyVaultKeyName"].Value) : ""
            };

            var dataSchemaFileContent = await GetDataSchemaAndRules(dataSchemaStorageAccountName, dataSchemaStorageAccountKeyValue, dataSchemaStorageContainerName, dataSchemaPathWithinContainer);

            Stopwatch stopwatchDelay = new Stopwatch();
            Stopwatch stopwatchThreshold = new Stopwatch();
            stopwatchThreshold.Start();
            DataGen dataGenInstance = new DataGen();
            while (true)
            {
                stopwatchDelay.Restart();
                cancellationToken.ThrowIfCancellationRequested();

                if (stopwatchThreshold.Elapsed.TotalMinutes >= 1440)
                {
                    stopwatchThreshold.Restart();
                }
                if (_counter >= dataSchemaFileContent.rulesCounterRefreshInMinutes)
                {
                    _counter = 1;
                }

                List<JObject> dataStreams = new List<JObject>();
                foreach (var ds in dataSchemaFileContent.dataSchema)
                {
                    if (stopwatchThreshold.Elapsed.Minutes % ds.simulationPeriodInMinute == 0)
                    {
                        //generate random data
                        dataGenInstance.GenerateRandomData(dataStreams, ds);

                        //generate rules triggering data only for the 0th node in SF to avoid data duplication
                        if ((ds.rulesData != null) && (this.Context.NodeContext.NodeName.Substring(this.Context.NodeContext.NodeName.Length - 1) == "0"))
                        {
                            dataGenInstance.GenerateDataRules(dataStreams, ds, _counter);
                        }
                    }
                }

                if (dataStreams.Count > 0)
                {
                    await SendData(dataStreams, ehConnectionString, iotDeviceConnectionString, kafkaConn);
                }

                _counter++;
                var setDelay = ((60 - stopwatchDelay.Elapsed.TotalSeconds) > 0) ? (60 - stopwatchDelay.Elapsed.TotalSeconds) : 1;
                await Task.Delay(TimeSpan.FromSeconds(setDelay), cancellationToken);
            }
        }

        /// <summary>
        /// Gather Data that defines the schema from the blob 
        /// </summary>
        /// <param name="storageAccountKeyValue"></param>
        /// <returns></returns>
        public static async Task<DataSchema> GetDataSchemaAndRules(string storageAccountName, string storageAccountKeyValue, string containerName, string dataSchemaFilePath)
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageAccountName, storageAccountKeyValue), true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob dataBlob = container.GetBlockBlobReference(dataSchemaFilePath);
            var dataContent = await dataBlob.DownloadTextAsync();
            var dataSchemaFileContent = JsonConvert.DeserializeObject<DataSchema>(dataContent);
            return dataSchemaFileContent;
        }

        /// <summary>
        /// Send data to the Inputs i.e. Event hub and/or IoTHub
        /// </summary>
        /// <param name="dataStreams">Data to send</param>
        /// <param name="ehConnectionString">Cxn string to an EventHub to send data to; skip sending if lenght is 0</param>
        /// <param name="iotHubDeviceConnectionString">Cxn string to an IoT to send data to; skip sending if lenght is 0</param>
        /// <param name="kafkaConn">Cxn data to kafka</param>
        /// <returns></returns>
        private async Task SendData(List<JObject> dataStreams, string ehConnectionString, string iotHubDeviceConnectionString, KafkaConnection kafkaConn)
        {
            if (ehConnectionString.Length == 0 && iotHubDeviceConnectionString.Length == 0 && kafkaConn.ConnectionString.Length == 0)
            {
                throw new Exception("No output specificied; an EventHub and/or an IoT hub and/or Kafka needs to be specific.");
            }

            List<Task> tasks = new List<Task>();
            if (ehConnectionString.Length > 0)
            {
                Task sendToEhTask = Task.Run(() => SendToEventHubBatch(ehConnectionString, dataStreams));
                tasks.Add(sendToEhTask);
            }
            if (iotHubDeviceConnectionString.Length > 0)
            {
                Task sendToIoTTask = Task.Run(() => SendToIotHubBatch(iotHubDeviceConnectionString, dataStreams));
                tasks.Add(sendToIoTTask);
            }
            if (kafkaConn.ConnectionString.Length > 0)
            {
                Task sendToKafkaTask = Task.Run(() => SendToKafka(kafkaConn, dataStreams));
                tasks.Add(sendToKafkaTask);
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Send data to event hubs defined in appconfig
        /// </summary>
        /// <param name="ehConnectionString"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendToEventHubBatch(string ehConnectionString, List<JObject> data)
        {
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(ehConnectionString);
            var batchevents = eventHubClient.CreateBatch();
            foreach (var deviceData in data)
            {
                var eventData = new EventData(Encoding.ASCII.GetBytes(deviceData.ToString(Formatting.None)));
                if (!batchevents.TryAdd(eventData))
                {
                    await eventHubClient.SendAsync(batchevents);
                    batchevents = eventHubClient.CreateBatch();
                    batchevents.TryAdd(eventData);
                }
            }
            if (batchevents.Count > 0)
            {
                await eventHubClient.SendAsync(batchevents);
            }
            await eventHubClient.CloseAsync();
        }

        /// <summary>
        /// Send data to IoT hub from app.config
        /// </summary>
        /// <param name="deviceConnectionString"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendToIotHubBatch(string deviceConnectionString, List<JObject> data)
        {
            int msgCollectionBatchSize = 500;
            string devicePrefix = "sensortestdevice";
            int deviceNumber = 1;
            List<DeviceClient> iotDevices = new List<DeviceClient>();
            List<Message> msgCollection = new List<Message>();
            List<Task> sendToIotTasks = new List<Task>();
            foreach (var deviceData in data)
            {
                var dataMessage = new Message(Encoding.ASCII.GetBytes(deviceData.ToString(Formatting.None)));
                if (msgCollection.Count >= msgCollectionBatchSize)
                {
                    var msgSendList = msgCollection.ToList();
                    string connString = string.Format(deviceConnectionString, devicePrefix + deviceNumber + this.Context.NodeContext.NodeName.Substring(this.Context.NodeContext.NodeName.Length - 2));
                    DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                    iotDevices.Add(deviceClient);
                    deviceNumber++;
                    Task sendToIotTask = Task.Run(() => deviceClient.SendEventBatchAsync(msgSendList));
                    sendToIotTasks.Add(sendToIotTask);
                    msgCollection.Clear();
                    msgCollection.Add(dataMessage);
                }
                else
                {
                    msgCollection.Add(dataMessage);
                }
            }
            if (msgCollection.Count > 0)
            {
                var msgSendList = msgCollection.ToList();
                string connString = string.Format(deviceConnectionString, devicePrefix + deviceNumber + this.Context.NodeContext.NodeName.Substring(this.Context.NodeContext.NodeName.Length - 2));
                DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                iotDevices.Add(deviceClient);
                deviceNumber++;
                Task sendToIotTask = Task.Run(() => deviceClient.SendEventBatchAsync(msgSendList));
                sendToIotTasks.Add(sendToIotTask);
                msgCollection.Clear();
            }
            try
            {
                await Task.WhenAll(sendToIotTasks);
            }
            catch (Exception)
            {

            }
            foreach (var deviceClient in iotDevices)
            {
                await deviceClient.CloseAsync();
            }
        }

        /// <summary>
        /// Send data to Kakfa 
        /// </summary>
        /// <param name="kafkaConn"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendToKafka(KafkaConnection kafkaConn, List<JObject> data)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = kafkaConn.Broker,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "$ConnectionString",
                SaslPassword = kafkaConn.ConnectionString,
                SslCaLocation = ".\\cacert.pem"
            };

            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                List<Task> tasks = new List<Task>();
                int numberOfParallelTasks = 10;
                Random random = new Random();
                foreach (var deviceData in data)
                {
                    var msg = deviceData.ToString(Formatting.None);
                    if(tasks.Count >= numberOfParallelTasks)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                        Task sendToKafka = Task.Run(() => producer.ProduceAsync(kafkaConn.Topics[random.Next(kafkaConn.Topics.Count)], new Message<string, string> { Key = null, Value = msg }));
                        tasks.Add(sendToKafka);
                    }
                    else
                    {
                        Task sendToKafka = Task.Run(() => producer.ProduceAsync(kafkaConn.Topics[random.Next(kafkaConn.Topics.Count)], new Message<string, string> { Key = null, Value = msg }));
                        tasks.Add(sendToKafka);
                    }
                }
                await Task.WhenAll(tasks);
            }
        }
    }
}
