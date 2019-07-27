// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Utility;
using DataX.Contract;
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
    public class GenerateProjectionFile: ProcessorBase
    {
        public const string TokenName_ProjectionFiles = "processProjections";

        [ImportingConstructor]
        public GenerateProjectionFile(IRuntimeConfigStorage runtimeStorage, IKeyVaultClient keyvaultClient, ConfigGenConfiguration conf)
        {
            RuntimeStorage = runtimeStorage;
            KeyVaultClient = keyvaultClient;
            Configuration = conf;
        }

        private ConfigGenConfiguration Configuration { get; }
        private IRuntimeConfigStorage RuntimeStorage { get; }
        private IKeyVaultClient KeyVaultClient { get; }
        
        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config?.GetGuiConfig();
            if (guiConfig == null)
            {
                return "no gui input, skipped.";
            }

            var projectColumns = guiConfig.Input?.Properties?.NormalizationSnippet?.Trim('\t', ' ', '\r', '\n');
            //TODO: make the hardcoded "Raw.*" configurable?
            var finalProjections = string.IsNullOrEmpty(projectColumns) ? "Raw.*" : projectColumns;

            var runtimeConfigBaseFolder = flowToDeploy.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            Ensure.NotNull(runtimeConfigBaseFolder, "runtimeConfigBaseFolder");

            var runtimeKeyVaultName = flowToDeploy.GetTokenString(PortConfigurationSettings.TokenName_RuntimeKeyVaultName);
            Ensure.NotNull(runtimeKeyVaultName, "runtimeKeyVaultName");

            var filePath = ResourcePathUtil.Combine(runtimeConfigBaseFolder, "projection.txt");
            var savedFile = await RuntimeStorage.SaveFile(filePath, finalProjections);
            var secretName = $"{config.Name}-projectionfile";
            var savedSecretId = await KeyVaultClient.SaveSecretAsync(runtimeKeyVaultName, secretName, savedFile, Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string sparkType) ? sparkType : null);
            flowToDeploy.SetObjectToken(TokenName_ProjectionFiles, new string[] {savedSecretId});

            return "done";
        }
    }
}
