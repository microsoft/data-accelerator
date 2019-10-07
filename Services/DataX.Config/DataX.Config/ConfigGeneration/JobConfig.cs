// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Templating;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config
{
    public class JobConfig
    {
        public string Name { get; set; }

        public string SparkJobName { get; set; }

        public string Content { get; set; }

        public string FilePath { get; set; }

        public string SparkFilePath { get; set; }

        public string ProcessStartTime { get; set; }

        public string ProcessEndTime { get; set; }

        public string ProcessingTime { get; set; }

        public bool IsOneTime { get; set; }
    }
}
