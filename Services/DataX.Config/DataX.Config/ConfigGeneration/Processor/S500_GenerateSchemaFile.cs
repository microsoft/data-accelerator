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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the schema file section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class GenerateSchemaFile: ProcessorBase
    {
        public const string TokenName_InputSchemaFilePath = "inputSchemaFilePath";

        [ImportingConstructor]
        public GenerateSchemaFile(IKeyVaultClient keyvaultClient, IRuntimeConfigStorage runtimeStorage, ConfigGenConfiguration conf)
        {
            KeyVaultClient = keyvaultClient;
            RuntimeStorage = runtimeStorage;
            Configuration = conf;
        }

        private ConfigGenConfiguration Configuration { get; }
        private IKeyVaultClient KeyVaultClient { get; }
        private IRuntimeConfigStorage RuntimeStorage { get; }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config?.GetGuiConfig();
            if (guiConfig == null)
            {
                return "no gui input, skipped.";
            }

            var schema = guiConfig.Input?.Properties?.InputSchemaFile;
            Ensure.NotNull(schema, "guiConfig.input.properties.inputschemafile");

            var runtimeConfigBaseFolder = flowToDeploy.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            Ensure.NotNull(runtimeConfigBaseFolder, "runtimeConfigBaseFolder");

            var runtimeKeyVaultName = flowToDeploy.GetTokenString(PortConfigurationSettings.TokenName_RuntimeKeyVaultName);
            Ensure.NotNull(runtimeKeyVaultName, "runtimeKeyVaultName");

            var filePath = ResourcePathUtil.Combine(runtimeConfigBaseFolder, "inputschema.json");
            var schemaFile = await RuntimeStorage.SaveFile(filePath, schema);
            var secretName = $"{config.Name}-inputschemafile";
            var schemaFileSecret = await KeyVaultClient.SaveSecretAsync(runtimeKeyVaultName, secretName, schemaFile, Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string sparkType) ? sparkType : null);
            flowToDeploy.SetStringToken(TokenName_InputSchemaFilePath, schemaFileSecret);

            return "done";
        }
    }
}
