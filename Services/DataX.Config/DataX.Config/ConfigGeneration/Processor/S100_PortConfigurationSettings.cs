// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Utility;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// porting some configuration settings
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class PortConfigurationSettings : ProcessorBase
    {
        public const string TokenName_RuntimeKeyVaultName = Constants.ConfigSettingName_RuntimeKeyVaultName;

        [ImportingConstructor]
        public PortConfigurationSettings(ConfigGenConfiguration conf)
        {
            this.Configuration = conf;
        }

        private ConfigGenConfiguration Configuration { get; }

        public override int GetOrder()
        {
            return 100;
        }

        private void PortConfigurationSetting(FlowDeploymentSession session, string settingName, bool isRequired = false)
        {
            if(Configuration.TryGet(settingName, out var value))
            {
                session.SetStringToken(settingName, value);
            }
            else
            {
                if (isRequired)
                {
                    throw new ConfigGenerationException($"Required setting '{settingName}' is not found in global configuration");
                }
                else
                {
                    session.SetNullToken(settingName);
                }
            }
        }

        public override Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            return PortSettings(flowToDeploy);
        }

        public override Task<string> Delete(FlowDeploymentSession flowToDeploy)
        {
            return PortSettings(flowToDeploy);
        }

        private async Task<string> PortSettings(FlowDeploymentSession flowToDeploy)
        {
            // TODO: currently this is a whitelist only port a portion of configuration settings
            // but we might want to port all configuration settings into the token dictionary
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_RuntimeApplicationInsightKey, false);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_ClusterName, true);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_ServiceKeyVaultName, true);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_RuntimeKeyVaultName, true);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_ConfigFolderHost, false);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_ConfigFolderContainerPath, false);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_MetricEventHubConnectionKey, true);
            PortConfigurationSetting(flowToDeploy, "eventHubResourceGroupName", false);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_LocalRoot, false);
            PortConfigurationSetting(flowToDeploy, Constants.ConfigSettingName_LocalMetricsHttpEndpoint, false);

            await Task.Yield();
            return "done";
        }
    }
}
