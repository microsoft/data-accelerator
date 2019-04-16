// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract;
using System;
using System.Fabric;

namespace DataX.ServiceHost.ServiceFabric
{
    public static class ServiceFabricUtil
    {
        /// <summary>
        /// Adding a helper to get the keyVaultName being used by startup.cs
        /// </summary>
        /// <returns></returns>
        public static ApiResult GetServiceKeyVaultName()
        {
            return GetServiceFabricConfigSetting("ServiceKeyvaultName");           
        }

        // Helper to get setting value from service fabric settings file
        public static ApiResult GetServiceFabricConfigSetting(string settingName)
        {
            try
            {
                ConfigurationPackage package = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
                System.Fabric.Description.ConfigurationSection serviceEnvironmentSection = package.Settings.Sections["ServiceEnvironment"];
                var settingValue = serviceEnvironmentSection.Parameters[settingName].Value;
                return ApiResult.CreateSuccess(settingValue);
            }
            catch (Exception ex)
            {
                return ApiResult.CreateError(ex.Message);
            }
        }
    }
}
