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
    /// Produce the reference data section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveReferenceData : ProcessorBase
    {
        public const string TokenName_ReferenceData = "inputReferenceData";

        [ImportingConstructor]
        public ResolveReferenceData(ConfigGenConfiguration conf, IKeyVaultClient keyvaultClient)
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
            var referenceData = guiConfig?.Input?.ReferenceData;
            if (referenceData != null && referenceData.Length > 0)
            {
                foreach(var rd in referenceData)
                {
                    var path = rd.Properties?.Path;
                    if (path != null && !KeyVaultUri.IsSecretUri(path))
                    {
                        var secretName = $"{guiConfig.Name}-referencedata";
                        var secretUri = await KeyVaultClient.SaveSecretAsync(
                            keyvaultName: RuntimeKeyVaultName.Value,
                            secretName: secretName,
                            secretValue: path,
                            sparkType: Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string sparkType) ? sparkType : null,
                            hashSuffix: true);

                        rd.Properties.Path = secretUri;
                    }
                }
            }

            return guiConfig;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config.GetGuiConfig();

            if (guiConfig == null)
            {
                return "no gui input, skipped";
            }

            var referenceData = guiConfig?.Input?.ReferenceData ?? Array.Empty<FlowGuiReferenceData>();
            var specs = referenceData.Select(rd =>
            {
                var props = rd.Properties;
                Ensure.NotNull(props, $"guiConfig.input.referenceData['{rd.Id}'].properties");
                Ensure.NotNull(rd.Type, $"guiConfig.input.referenceData['{rd.Id}'].type");
                Ensure.NotNull(props.Path, $"guiConfig.input.referenceData['{rd.Id}'].properties.path");

                return new ReferenceDataSpec()
                {
                    Name = rd.Id,
                    Path = props.Path,
                    Format = rd.Type,
                    Delimiter = props.Delimiter,
                    Header = props.Header.ToString()
                };
            }).ToArray();

            flowToDeploy.SetObjectToken(TokenName_ReferenceData, specs);

            await Task.Yield();
            return "done";
        }
    }
}
