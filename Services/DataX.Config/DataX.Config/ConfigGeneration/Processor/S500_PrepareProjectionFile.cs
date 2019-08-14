// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Utility;
using DataX.Contract;
using DataX.Utility.KeyVault;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the projection file section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class PrepareProjectionFile: ProcessorBase
    {
        public const string TokenName_ProjectionFiles = "processProjections";

        [ImportingConstructor]
        public PrepareProjectionFile(IRuntimeConfigStorage runtimeStorage, IKeyVaultClient keyvaultClient, ConfigGenConfiguration conf)
        {
            RuntimeStorage = runtimeStorage;
            KeyVaultClient = keyvaultClient;
            Configuration = conf;
        }

        private ConfigGenConfiguration Configuration { get; }
        private IRuntimeConfigStorage RuntimeStorage { get; }
        private IKeyVaultClient KeyVaultClient { get; }

        /// <summary>
        /// Generate and set the info for the projection file which will be used to generate JobConfig
        /// </summary>
        /// <returns></returns>
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var runtimeConfigBaseFolder = flowToDeploy.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            Ensure.NotNull(runtimeConfigBaseFolder, "runtimeConfigBaseFolder");

            var runtimeKeyVaultName = flowToDeploy.GetTokenString(PortConfigurationSettings.TokenName_RuntimeKeyVaultName);
            Ensure.NotNull(runtimeKeyVaultName, "runtimeKeyVaultName");

            var secretName = $"{config.Name}-projectionfile";
            Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string sparkType);
            var uriPrefix = KeyVaultClient.GetUriPrefix(sparkType);
            var projectionFileSecret = SecretUriParser.ComposeUri(runtimeKeyVaultName, secretName, uriPrefix);
            flowToDeploy.SetObjectToken(TokenName_ProjectionFiles, new string[] { projectionFileSecret });

            await Task.CompletedTask;
            return "done";
        }
    }
}
