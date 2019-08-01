// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Utility;
using DataX.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the projection file section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class GenerateProjectionFile : ProcessorBase
    {
        [ImportingConstructor]
        public GenerateProjectionFile(IRuntimeConfigStorage runtimeStorage, IKeyVaultClient keyvaultClient, ConfigGenConfiguration conf)
        {
            RuntimeStorage = runtimeStorage;
            KeyVaultClient = keyvaultClient;
            Configuration = conf;
        }

        public override int GetOrder()
        {
            return 630;
        }

        private ConfigGenConfiguration Configuration { get; }
        private IRuntimeConfigStorage RuntimeStorage { get; }
        private IKeyVaultClient KeyVaultClient { get; }
        
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

            var projectColumns = guiConfig.Input?.Properties?.NormalizationSnippet?.Trim('\t', ' ', '\r', '\n');
            //TODO: make the hardcoded "Raw.*" configurable?
            var finalProjections = string.IsNullOrEmpty(projectColumns) ? "Raw.*" : projectColumns;

            var runtimeConfigBaseFolder = flowToDeploy.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
            Ensure.NotNull(runtimeConfigBaseFolder, "runtimeConfigBaseFolder");

            var runtimeKeyVaultName = flowToDeploy.GetTokenString(PortConfigurationSettings.TokenName_RuntimeKeyVaultName);
            Ensure.NotNull(runtimeKeyVaultName, "runtimeKeyVaultName");

            var filePath = ResourcePathUtil.Combine(runtimeConfigBaseFolder, "projection.txt");
            var savedFile = await RuntimeStorage.SaveFile(filePath, finalProjections);
            var tokenValue = flowToDeploy.GetTokenString(PrepareProjectionFile.TokenName_ProjectionFiles);
            var projectionFileSecret = JArray.Parse(tokenValue).FirstOrDefault()?.Value<string>();
            if (!string.IsNullOrEmpty(projectionFileSecret))
            {
                await KeyVaultClient.SaveSecretAsync(projectionFileSecret, savedFile);
            }

            return "done";
        }
    }
}
