// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System;

namespace DataX.Config
{
    public class ConfigFlattener
    {
        private readonly FlattenerConfig _flattenConfig;

        private ConfigFlattener(JsonConfig config)
        {
            _flattenConfig = FlattenerConfig.From(config);
        }

        public static ConfigFlattener From(JsonConfig config)
        {
            return config == null ? null : new ConfigFlattener(config);
        }

        public string Flatten(JsonConfig config)
        {
            var dict = PropertiesDictionary.From(_flattenConfig?.FlattenJsonConfig(config));
            return dict?.ToString();
        }
    }
}
