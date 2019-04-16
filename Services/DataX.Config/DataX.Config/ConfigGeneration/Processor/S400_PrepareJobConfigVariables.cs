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
    /// setup some variables for deployment to be used in later processors.
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class PrepareJobConfigVariables : ProcessorBase
    {
        public const string TokenName_ConfigVersion = "runtimeConfigVersion";
        public const string TokenName_RuntimeConfigFolder = "runtimeConfigFolder";
        public const string ResultPropertyName_RuntimeConfigFolder = "runtimeConfigFolder";

        [ImportingConstructor]
        public PrepareJobConfigVariables(JobDataManager jobs)
        {
            this.JobData = jobs;
        }

        private JobDataManager JobData { get; }

        public override int GetOrder()
        {
            return 400;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;

            // determine the runtime config folder
            var version = VersionGeneration.Next();
            flowToDeploy.SetStringToken(TokenName_ConfigVersion, version);
            var runtimeConfigFolder = this.JobData.FigureOutDestinationFolder(flowConfig.GetJobConfigDestinationFolder(), version);
            flowToDeploy.SetStringToken(TokenName_RuntimeConfigFolder, runtimeConfigFolder);
            flowToDeploy.ResultProperties[ResultPropertyName_RuntimeConfigFolder] = flowToDeploy.GetTokenString(TokenName_RuntimeConfigFolder);

            await Task.Yield();
            return "done";
        }
    }
}
