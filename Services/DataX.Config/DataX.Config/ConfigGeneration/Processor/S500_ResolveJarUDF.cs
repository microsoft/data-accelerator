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
    /// Produce the jarUDF and jarUDAF section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveJarUDF : ProcessorBase
    {
        public const string TokenName_JarUDFs = "processJarUDFs";
        public const string TokenName_JarUDAFs = "processJarUDAFs";
        [ImportingConstructor]
        public ResolveJarUDF(ConfigGenConfiguration conf, IKeyVaultClient keyvaultClient)
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
                foreach (var func in functions.Where(f => f.Type == "jarUDF" || f.Type == "jarUDAF"))
                {
                    var path = func.Properties?.Path;
                    if (path != null && !KeyVaultUri.IsSecretUri(path))
                    {
                        var secretName = $"{guiConfig.Name}-jarpath";
                        var secretUri = await KeyVaultClient.SaveSecretAsync(
                            keyvaultName: RuntimeKeyVaultName.Value,
                            secretName: secretName,
                            secretValue: path,
                            sparkType: Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string sparkType) ? sparkType : null,
                            hashSuffix: true);

                        func.Properties.Path = secretUri;
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

            var jarUDFs = GetJarUdfDefinitions(functions, "jarUDF");
            flowToDeploy.SetObjectToken(TokenName_JarUDFs, jarUDFs);

            var jarUDAFs = GetJarUdfDefinitions(functions, "jarUDAF");
            flowToDeploy.SetObjectToken(TokenName_JarUDAFs, jarUDAFs);

            return Task.FromResult("done");
        }

        private JarUdfSpec[] GetJarUdfDefinitions(IList<FlowGuiFunction> functions, string udfType)
        {
            return functions.Where(f => f.Type == udfType).Select(f =>
            {
                var properties = f.Properties;
                Ensure.NotNull(f.Id, "function alias");
                Ensure.NotNull(properties, $"guiConfig.process.functions['{f.Id}'].properties");
                Ensure.NotNull(properties.Path, $"guiConfig.process.functions['{f.Id}'].properties.path");
                Ensure.NotNull(properties.Class, $"guiConfig.process.functions['{f.Id}'].properties.class");

                return new JarUdfSpec()
                {
                    Path = properties.Path,
                    Class = properties.Class,
                    LibPaths = properties.LibPaths,
                    Name = f.Id
                };
            }).ToArray();
        }
    }
}
