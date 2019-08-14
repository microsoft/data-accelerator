// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigDataModel.RuntimeConfig;
using DataX.Config.Utility;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the azure function section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveAzureFunction : ProcessorBase
    {
        public const string TokenName_AzureFunctions = "processAzureFunctions";
        public const string FunctionTypeName = "azureFunction";

        [ImportingConstructor]
        public ResolveAzureFunction(ConfigGenConfiguration conf, IKeyVaultClient keyvaultClient)
        {
            Configuration = conf;
            KeyVaultClient = keyvaultClient;
            RuntimeKeyVaultName = new Lazy<string>(() => Configuration[Constants.ConfigSettingName_RuntimeKeyVaultName], true);
        }

        private ConfigGenConfiguration Configuration { get; }
        private IKeyVaultClient KeyVaultClient { get; }
        protected Lazy<string> RuntimeKeyVaultName { get; }

        public override async Task<FlowGuiConfig> HandleSensitiveData(FlowGuiConfig guiConfig)
        {
            var functions = guiConfig?.Process?.Functions;
            if (functions != null && functions.Count > 0)
            {
                foreach (var func in functions.Where(f => f.Type == FunctionTypeName))
                {
                    var code = func.Properties?.Code;
                    if (!string.IsNullOrEmpty(code) && !KeyVaultUri.IsSecretUri(code))
                    {
                        var secretName = $"{guiConfig.Name}-azurefunc";
                        var secretUri = await KeyVaultClient.SaveSecretAsync(
                            keyvaultName: RuntimeKeyVaultName.Value,
                            secretName: secretName,
                            secretValue: code,
                            sparkType: Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string sparkType) ? sparkType : null,
                            hashSuffix: true);

                        func.Properties.Code = secretUri;
                    }
                }
            }

            return guiConfig;
        }

        public override Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var functions = config.GetGuiConfig()?.Process?.Functions;
            if (functions == null)
            {
                return Task.FromResult("no functions defined in gui input, skipped.");
            }

            var azureFuncs = GetAzureFuncDefinitions(functions);
            flowToDeploy.SetObjectToken(TokenName_AzureFunctions, azureFuncs);

            return Task.FromResult("done");
        }

        private AzureFunctionSpec[] GetAzureFuncDefinitions(IList<FlowGuiFunction> functions)
        {
            return functions.Where(f => f.Type == FunctionTypeName).Select(f =>
            {
                var properties = f.Properties;
                Ensure.NotNull(properties, $"guiConfig.process.functions['{f.Id}'].properties");
                Ensure.NotNull(properties.ServiceEndpoint, $"guiConfig.process.functions['{f.Id}'].properties.serviceEndpoint");
                Ensure.NotNull(properties.Api, $"guiConfig.process.functions['{f.Id}'].properties.api");
                Ensure.NotNull(properties.Code, $"guiConfig.process.functions['{f.Id}'].properties.code");
                Ensure.NotNull(properties.MethodType, $"guiConfig.process.functions['{f.Id}'].properties.methodType");

                return new AzureFunctionSpec()
                {
                    Name = f.Id,
                    ServiceEndpoint = properties.ServiceEndpoint,
                    Api = properties.Api,
                    Code = properties.Code,
                    MethodType = properties.MethodType,
                    ParameterNames = properties.ParameterNames
                };
            }).ToArray();
        }
    }
}
