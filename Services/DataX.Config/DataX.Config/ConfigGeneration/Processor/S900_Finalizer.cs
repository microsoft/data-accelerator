// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Finish up and prepare a result of the deployment
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class Finalizer : ProcessorBase
    {
        [ImportingConstructor]
        public Finalizer([Import] FlowDataManager flows)
        {
            this.FlowData = flows;
        }

        private FlowDataManager FlowData { get; }

        public override int GetOrder()
        {
            return 900;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            // Update jobs name in the flow config
            var jobNames = flowToDeploy.GetAttachment<string[]>(DeploySparkJob.AttachmentName_SparkJobNames);
            if (jobNames == null)
            {
                flowToDeploy.Result = new SuccessResult($"no jobs are deployed", flowToDeploy.ResultProperties);

                return "no jobs, skipped";
            }

            var result = await this.FlowData.UpdateJobNamesForFlow(flowToDeploy.Name, jobNames);
            if (!result.IsSuccess)
            {
                throw new ConfigGenerationException(result.Message);
            }

            flowToDeploy.Result = new SuccessResult($"Deployed jobs:[{string.Join(",", jobNames)}]", flowToDeploy.ResultProperties);

            return "done";
        }

        /// <summary>
        /// Delete the flow config.
        /// </summary>
        /// <param name="flowToDeploy"></param>
        /// <returns></returns>
        public override async Task<string> Delete(FlowDeploymentSession flowToDelete)
        {
            flowToDelete.Result = await this.FlowData.DeleteByName(flowToDelete.Config.Name);
            return "done";
        }
    }
}
