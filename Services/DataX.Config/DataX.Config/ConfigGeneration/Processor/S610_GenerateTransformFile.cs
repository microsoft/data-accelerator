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
            return 610;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var jobs = flowToDeploy.GetJobs();
            if (jobs == null || !jobs.Where(j => j.JobConfigs.Any()).Any())
            {
                return "no jobs, skipped";
            }

            var config = flowToDeploy.Config;
            var guiConfig = config?.GetGuiConfig();
            if (guiConfig == null)
            {
                return "no gui input, skipped.";
            }

            var rulesCode = flowToDeploy.GetAttachment<RulesCode>(PrepareTransformFile.AttachmentName_CodeGenObject);
            Ensure.NotNull(rulesCode, "rulesCode");
            
            var runtimeConfigBaseFolder = flowToDeploy.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            Ensure.NotNull(runtimeConfigBaseFolder, "runtimeConfigBaseFolder");

            var filePath = ResourcePathUtil.Combine(runtimeConfigBaseFolder, $"{config.Name}-combined.txt");
            var transformFilePath = await RuntimeStorage.SaveFile(filePath, rulesCode.Code);
            var transformFileSecret = flowToDeploy.GetTokenString(PrepareTransformFile.TokenName_TransformFile);
            await KeyVaultClient.SaveSecretAsync(transformFileSecret, transformFilePath);

            return "done";
        }
    }
}
