// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract.Exception;
using DataX.Flow.Common;
using DataX.Utilities.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataX.Flow.SchemaInference.Blob
{
    public class BlobMessageBus : IMessageBus
    {
        private readonly List<FlowGuiInputBatchInput> _batchInputs = null;
        private readonly ILogger _logger;

        public BlobMessageBus(List<FlowGuiInputBatchInput> batchInputs, ILogger logger)
        {
            _logger = logger;
            _batchInputs = batchInputs;
        }

        /// <summary>
        /// Gets Sample Events
        /// </summary>
        /// <param name="seconds">seconds for which the sample data is fetched</param>
        /// <returns>Returns EventsData object</returns>
        public async Task<EventsData> GetSampleEvents(int seconds)
        {
            EventsData eventsData = new EventsData();
            const int numberOfDocumentsToRead = 500;

            foreach (var batchInput in _batchInputs)
            {
                var connection = Helper.GetSecretFromKeyvaultIfNeeded(batchInput.Properties.Connection);
                var wasbPath = Helper.GetSecretFromKeyvaultIfNeeded(batchInput.Properties.Path);

                if (!Uri.TryCreate(wasbPath, UriKind.Absolute, out var uri))
                {
                    throw new ArgumentException($"Malformed Uri for the blob path:'{wasbPath}'. The blob path should be a wasbs url. e.g. wasbs://mycontainer@myaccount.blob.core.windows.net/mypath");
                }

                var path = uri.Host + "/" + uri.UserInfo + uri.LocalPath;
                var pathPattern = BlobHelper.GenerateRegexPatternFromPath(path);
                var containerName = uri.UserInfo;
                var prefix = BlobHelper.ParsePrefix(wasbPath);

                var contents = await BlobHelper.GetLastModifiedBlobContentsInBlobPath(connection, containerName, prefix, pathPattern, numberOfDocumentsToRead).ConfigureAwait(false);

                foreach (var content in contents)
                {
                    // Get raw data
                    EventRaw er = new EventRaw
                    {
                        Raw = content,
                        Properties = new Dictionary<string, string>() { { "Length", content.Length.ToString() } },
                        SystemProperties = new Dictionary<string, string>() { { "Length", content.Length.ToString() } }
                    };

                    er.Json = JsonConvert.SerializeObject(er);

                    eventsData.EventsJson += er.Json + "\r\n";

                    eventsData.Events.Add(er);
                }
            }

            return eventsData;
        }
    }
}
