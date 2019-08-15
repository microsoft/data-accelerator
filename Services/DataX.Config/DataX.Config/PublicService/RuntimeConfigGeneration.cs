// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.Extensions.Logging;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config
{
    /// <summary>
    /// The service for generating runtime configs
    /// </summary>
    [Shared]
    [Export]
    public class RuntimeConfigGeneration
    {
        /// <summary>
        /// Initialize an instance of <see cref="RuntimeConfigGeneration"/>
        /// </summary>
        [ImportingConstructor]
        public RuntimeConfigGeneration(
            ILogger<RuntimeConfigGeneration> logger,
            FlowDataManager flows,
            JobDataManager jobData,
            [ImportMany] IEnumerable<IFlowDeploymentProcessor> flowProcessors
            )
        {
            this.Logger = logger;
            this.GenerationLocks = new GenerationLockDictionary();
            this.FlowData = flows;
            this.JobData = jobData;
            this.FlowProcessors = (flowProcessors?? Array.Empty<IFlowDeploymentProcessor>()).OrderBy(p => p.GetOrder()).ToArray();
        }

        private GenerationLockDictionary GenerationLocks { get; }

        private FlowDataManager FlowData { get; }

        private JobDataManager JobData { get; }

        private IFlowDeploymentProcessor[] FlowProcessors { get; }

        private ILogger Logger { get; }


        /// <summary>
        /// Generate and deploy all configs required by runtime
        /// </summary>
        /// <param name="flowName">name of the flow for which to generate the configs</param>
        /// <returns>Result</returns>
        public async Task<Result> GenerateRuntimeConfigs(string flowName)
        {
            return await GenerateRunTimeConfigsWithProcessors(flowName, FlowProcessors);
        }

        /// <summary>
        /// Delete all the configs associated with the job. This includes both designtime and runtime configs
        /// </summary>
        /// <param name="flowName"></param>
        /// <returns></returns>
        public async Task<Result> DeleteConfigs(string flowName)
        {
            return await DeleteConfigsWithProcessors(flowName, FlowProcessors);
        }

        public async Task<Result> GenerateRuntimeConfigsWithSpecificProcessor(string flowName, Type[] processorTypes)
        {
            var set = processorTypes.ToHashSet();
            return await GenerateRunTimeConfigsWithProcessors(flowName, FlowProcessors.Where(p=>set.Contains(p.GetType())));
        }

        
        private async Task<Result> DeleteConfigsWithProcessors(string flowName, IEnumerable<IFlowDeploymentProcessor> processors)
        {
            Ensure.NotNull(flowName, "flowName");

            // Ensure no other generation process is going with the same flow with generation locks per flows
            var generationLock = this.GenerationLocks.GetLock(flowName);
            if (generationLock == null)
            {
                return new FailedResult($"config for flow '{flowName}' is being generated, please try again later.");
            }

            using (generationLock)
            {
                using (Logger.BeginScope(("datax/runtimeConfigGeneration/flowConfigsDelete", new Dictionary<string, string>() { { "flowName", flowName } })))
                {
                    // Call Storage client to get back the associated flow config
                    var config = await FlowData.GetByName(flowName);
                    Ensure.NotNull(config, "config", $"could not find flow for flow name:'{flowName}'");

                    // Initialize a deploy session
                    var session = new FlowDeploymentSession(config);

                    // Run through a chain of processors
                    await processors.ChainTask(p => p.Delete(session));

                    // Return result
                    return session.Result;
                }
            }
        }

        private async Task<Result> GenerateRunTimeConfigsWithProcessors(string flowName, IEnumerable<IFlowDeploymentProcessor> processors)
        {
            // Ensure no other generation process is going with the same flow with generation locks per flows
            var generationLock = this.GenerationLocks.GetLock(flowName);
            if (generationLock == null)
            {
                return new FailedResult($"config for flow '{flowName}' is being generated, please try again later.");
            }

            using (generationLock)
            {
                using (Logger.BeginScope(("datax/runtimeConfigGeneration/flowConfigsGenerate", new Dictionary<string, string>() { { "flowName", flowName } })))
                {
                    // Call Storage client to get back the associated flow config
                    var config = await FlowData.GetByName(flowName);
                    Ensure.NotNull(config, "config", $"could not find flow for flow name:'{flowName}'");

                    // Initialize a deploy session
                    var session = new FlowDeploymentSession(config);

                    // Run through a chain of processors
                    await processors.ChainTask(session.ExecuteProcessor);

                    // Return result
                    return session.Result;
                }
            }
        }
    }
}
