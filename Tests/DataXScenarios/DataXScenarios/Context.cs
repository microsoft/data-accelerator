// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

namespace DataX.ServerScenarios
{
    /// <summary>
    /// Shared context for all the scenario steps. Used to pass in parameters
    /// as input and output for steps.
    /// </summary>
    public class Context
    {
        public const string ServiceUrl = "server";
        public const string AuthToken = "authToken";
        public const string MicrosoftAuthority = "microsoftAuthority";
        public const string ApplicationIdentifierUri = "applicationIdentifierUri";
        public const string ApplicationId = "applicationId";
        public const string ApplicationName = "applicationName";
        public const string SecretKey = "secretKey";
        public const string AccessTokenType = "accessTokenType";
        public const string FlowName = "flowName";
        public const string GenerateConfigsRuntimeConfigFolder = "generateConfigsRuntimeConfigFolder";
        public const string AuthResult = "authResult";
        public const string RestartJobsName = "restartJobsName";
        public const string FlowConfig = "flowConfig";
        public const string FlowConfigContent = "flowConfigContent";
        public const string InputSchema = "InputSchema";
        public const string EventhubConnectionString = "eventhubConnectionString";
        public const string EventHubName = "eventHubName";
        public const string IsIotHub = "isIotHub";
        public const string InferSchemaInputJson = "inferSchemaInputJson";
        public const string Seconds = "seconds";
        public const string KernelId = "kernelId";
        public const string InitializeKernelJson = "initializeKernelJson";
        public const string DeleteKernelJson = "deleteKernelJson";
        public const string NormalizationSnippet = "normalizationSnippet";
        public const string SparkType = "sparkType";
        public const string StartJobName = "startJobName";
        /// <summary>
        /// Boolean
        /// This context param specifies if the current job should skip validating server certificates on http calls if true
        /// </summary>
        public const string SkipServerCertificateValidation = "skipServerCertificateValidation";
    }
}