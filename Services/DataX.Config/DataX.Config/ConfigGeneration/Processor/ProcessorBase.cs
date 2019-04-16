// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataX.Config.ConfigDataModel;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Base class of all deployment processor.
    /// Note the default order number in the execution sequence is 500,
    /// derived processor can override GetOrder increase/decrease to choose their position in the execution sequence.
    /// </summary>
    public abstract class ProcessorBase : IFlowDeploymentProcessor
    {
        public virtual int GetOrder()
        {
            return 500;
        }

        public virtual Task<FlowGuiConfig> HandleSensitiveData(FlowGuiConfig guiConfig)
        {
            return Task.FromResult(guiConfig);
        }

        public abstract Task<string> Process(FlowDeploymentSession flowToDeploy);

        public virtual Task<string> Delete(FlowDeploymentSession flowToDelete)
        {
            return Task.FromResult("skipped");
        }
    }
}
