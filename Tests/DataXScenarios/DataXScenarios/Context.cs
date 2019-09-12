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
        public const string DataHubIdentifier = "dataHubIdentifier";
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
    }
}