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

            foreach (var batchInput in _batchInputs)
            {
                var connection = Helper.GetSecretFromKeyvaultIfNeeded(batchInput.Properties.Connection);
                var wasbPath = Helper.GetSecretFromKeyvaultIfNeeded(batchInput.Properties.Path);

                var path = TranslateBlobPath(wasbPath);
                var containerName = ParseContainerName(path);
                var prefix = ParsePrefix(path, containerName);
                var pathPattern = GenerateRegexPatternFromPath(path);

                var contents = await BlobHelper.GetLastModifiedBlobContentsInBlobPath(connection, containerName, prefix, pathPattern, 3).ConfigureAwait(false);

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

        private static string GenerateRegexPatternFromPath(string path)
        {
            path = NormalizeBlobPath(path);
            var mc = Regex.Matches(path, @"{(.*?)}");
            if (mc == null || mc.Count < 1)
            {
                return path;
            }

            foreach (Match m in mc)
            {
                var r3 = Regex.Match(m.Value, @"^({)*([yMdHhmsS\-\/.,: ]+)(})*$");
                if (!r3.Success)
                {
                    throw new GeneralException("Token in the blob path should be a data time format. e.g. {yyyy-MM-dd}");
                }

                path = path.Replace(m.Value, @"(\w+)", StringComparison.InvariantCulture);
            }

            return path;

        }

        private static string NormalizeBlobPath(string path)
        {
            path = path.TrimEnd('/');
            var mc = Regex.Matches(path, @"{(.*?)}");
            if (mc == null || mc.Count < 1 || mc.Count > 1)
            {
                return path;
            }

            var tokenValue = mc[0].Value.Trim(new char[] { '{', '}' });

            var mc2 = Regex.Matches(tokenValue, @"[A-Za-z]+");
            foreach (Match m in mc2)
            {
                tokenValue = tokenValue.Replace(m.Value, "{" + m.Value + "}", StringComparison.InvariantCulture);
            }

            path = path.Replace(mc[0].Value, tokenValue, StringComparison.InvariantCulture);

            return path;
        }

        private static string TranslateBlobPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            path = path.Replace("wasbs://", "", StringComparison.OrdinalIgnoreCase);
            var parts = path.Split(new char[] { '@', '/' });
            path = parts[1] + "/" + parts[0] + "/" + string.Join("/", parts, 2, parts.Length - 2);
            return path;
        }

        private static string ParseContainerName(string blobUri)
        {
            var mc = Regex.Matches(blobUri, @"\/(.*?)\/");
            if (mc == null || mc.Count < 1)
            {
                throw new GeneralException("Unable to parse a container name from the blob path. The blob path should be a wasbs url. e.g. wasbs://mycontainer@myaccount.blob.core.windows.net/mypath");
            }

            var val = mc[0].Value.Replace(@"/", "", StringComparison.InvariantCulture);
            return val;
        }


        private static string ParsePrefix(string blobUri, string containerName)
        {
            var mc = Regex.Match(blobUri, containerName + @"\/(.*?)\/{(.*?)}");

            return mc.Groups[0].Value;
        }

    }
}
