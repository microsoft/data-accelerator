// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.Utility;
using DataX.Contract;
using DataX.Flow.CodegenRules;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using DataX.Config.ConfigDataModel;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the transform/query file section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class GenerateTransformFile : ProcessorBase
    {
        public const string TokenName_TransformFile = "processTransforms";
        public const string AttachmentName_CodeGenObject = "rulesCode";

        private ConfigGenConfiguration Configuration { get; }
        private IKeyVaultClient KeyVaultClient { get; }
        private IRuntimeConfigStorage RuntimeStorage { get; }

        [ImportingConstructor]
        public GenerateTransformFile(IKeyVaultClient keyvaultClient, IRuntimeConfigStorage runtimeStorage, ConfigGenConfiguration conf)
        {
            KeyVaultClient = keyvaultClient;
            RuntimeStorage = runtimeStorage;
            Configuration = conf;
        }

        public override int GetOrder()
        {
            return 450;
        }


        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config?.GetGuiConfig();
            if (guiConfig == null)
            {
                return "no gui input, skipped.";
            }
            string queries = string.Join("\n", guiConfig.Process?.Queries);

            string ruleDefinitions =  RuleDefinitionGenerator.GenerateRuleDefinitions(guiConfig.Rules, config.Name);
            RulesCode rulesCode = CodeGen.GenerateCode(queries, ruleDefinitions, config.Name);

            Ensure.NotNull(rulesCode, "rulesCode");

            // Save the rulesCode object for downstream processing
            flowToDeploy.SetAttachment(AttachmentName_CodeGenObject, rulesCode);

            var runtimeConfigBaseFolder = flowToDeploy.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            Ensure.NotNull(runtimeConfigBaseFolder, "runtimeConfigBaseFolder");

            var runtimeKeyVaultName = flowToDeploy.GetTokenString(PortConfigurationSettings.TokenName_RuntimeKeyVaultName);
            Ensure.NotNull(runtimeKeyVaultName, "runtimeKeyVaultName");

            var filePath = ResourcePathUtil.Combine(runtimeConfigBaseFolder, $"{config.Name}-combined.txt");
            var transformFilePath = await RuntimeStorage.SaveFile(filePath, rulesCode.Code);
            var secretName = $"{config.Name}-transform";
            var transformFileSecret = await KeyVaultClient.SaveSecretAsync(runtimeKeyVaultName, secretName, transformFilePath, (Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string sparkType) ? sparkType : null));
            flowToDeploy.SetStringToken(TokenName_TransformFile, transformFileSecret);

            return "done";
        }
    }
}
