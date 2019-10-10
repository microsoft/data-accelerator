// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

using Microsoft.Extensions.Configuration;
using System.Composition;

namespace JobRunner
{
    /// <summary>
    /// The class object encompassing the appsettings parameters
    /// </summary>
    [Export]
    [Shared]
    public class AppConfig
    {
        [ImportingConstructor]
        public AppConfig(IConfiguration configuration)
        {
            configuration.Bind("JobRunner", this);
        }

        public string StorageConnection { get; set; }
        public string ServiceBusConnectionString { get; set; }
        public string PrimaryQueueName { get; set; }
        public string TestQueueName { get; set; }
        public string ActiveQueueName { get; set; }
        public string ServiceUrl { get; set; }
        public string ServiceKeyVaultName { get; set; }
        public string AppInsightsIntrumentationKey { get; set; }
        public string MicrosoftAuthority { get; set; }
        public string ApplicationIdentifierUri { get; set; }
        public string ApplicationId { get; set; }
        public string SecretKey { get; set; }
        public string BlobConnectionString { get; set; }
        public string BlobUri { get; set; }
        public string EvenHubConnectionString { get; set; }
        public string EventHubName { get; set; }
        public string IsIotHub { get; set; }
        public string Seconds { get; set; }
        public string FlowName { get; set; }
        public string NormalizationSnippet { get; set; }
        public string DatabricksToken { get; set; }
        public string SparkType { get; set; }
        /// <summary>
        /// HttpClient calls to external services will skip server certification validations on http calls if true, use it for development purposes
        /// </summary>
        public bool SkipServerCertificateValidation { get; set; } = false;
    }
}
