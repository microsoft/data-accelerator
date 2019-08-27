// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************

namespace JobRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = MefContainer.BuildConfigurationForConsoleApp();
            var mef = MefContainer.CreateWithConfiguration(config);            
            mef.GetExport<JobRunner>().RunForeverAsync().Wait();
        }
    }
}
