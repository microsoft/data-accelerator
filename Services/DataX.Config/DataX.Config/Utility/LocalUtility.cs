// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config.Utility
{
    public static class LocalUtility
    {
        public static bool IsLocalEnabled(ConfigGenConfiguration configuration)
        {
            if (configuration!=null && configuration.TryGet(Constants.ConfigSettingName_EnableOneBox, out string enableOneBox))
            {
                return enableOneBox?.ToLower() == "true";
            }
            {
                return false;
            }
        }
    }
}
