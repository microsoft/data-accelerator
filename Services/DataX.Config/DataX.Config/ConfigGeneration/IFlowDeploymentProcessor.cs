// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using System.Threading.Tasks;

namespace DataX.Config
{
    public interface IFlowDeploymentProcessor: ISequentialProcessor
    {
        /// <summary>
        /// called at designtime config generation,
        /// this function should replace the secret in the <see cref="FlowGuiConfig"/> passed in from ui input and return it back
        /// </summary>
        /// <param name="guiConfig"></param>
        /// <returns></returns>
        Task<FlowGuiConfig> HandleSensitiveData(FlowGuiConfig guiConfig);

        /// <summary>
        /// called at runtime configuration generation
        /// </summary>
        /// <param name="flowToDeploy">the generation session which carries the variables</param>
        /// <returns></returns>
        Task<string> Process(FlowDeploymentSession flowToDeploy);

        /// <summary>
        /// Delete all the configs and resources associated with the flow
        /// </summary>
        /// <param name="flowToDelete"></param>
        /// <returns></returns>
        Task<string> Delete(FlowDeploymentSession flowToDelete);
    }
}
