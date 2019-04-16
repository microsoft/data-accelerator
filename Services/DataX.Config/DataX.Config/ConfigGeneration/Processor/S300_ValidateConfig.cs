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
    /// Validate config
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ValidateConfig : ProcessorBase
    {
        public ValidateConfig()
        {
        }

        public override int GetOrder()
        {
            return 300;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            await Task.Yield();

            // Go through a pass of validation to ensure required fields are present
            var result = this.ValidateFlowConfig(flowToDeploy.Config);

            return $"result is {result.IsSuccess}";
        }

        /// <summary>
        /// validate if the given flow config has all the required fields present
        /// </summary>
        /// <param name="config">the given config</param>
        /// <returns>Success result if validation passes, else a FailedResult</returns>
        private Result ValidateFlowConfig(FlowConfig config)
        {
            //TODO: implement the validations.
            return new SuccessResult();
        }
    }
}
