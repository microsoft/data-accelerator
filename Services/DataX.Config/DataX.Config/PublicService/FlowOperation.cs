// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.InternalService;
using DataX.Contract;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.PublicService
{
    [Shared]
    [Export]
    public class FlowOperation
    {
        public const string FlowConfigPropertyName = "flowConfig";
        /// <summary>
        /// Initialize an instance of <see cref="FlowOperation"/>
        /// </summary>
        /// <param name="serviceHub">services hub</param>
        /// <param name="jobOpers">job operation service</param>
        [ImportingConstructor]
        public FlowOperation(FlowDataManager flows,
                IKeyVaultClient keyvault,
                FlowConfigBuilder configBuilder,
                JobOperation jobOpers)
        {
            FlowData = flows;
            JobOperation = jobOpers;
            ConfigBuilder = configBuilder;
            KeyVaultClient = keyvault;
        }

        private FlowDataManager FlowData { get; }
        private JobOperation JobOperation { get; }
        public FlowConfigBuilder ConfigBuilder { get; }
        private IKeyVaultClient KeyVaultClient { get; }

        /// <summary>
        /// Get all flows
        /// </summary>
        /// <returns></returns>
        public Task<FlowConfig[]> GetAllFlows()
        {
            return this.FlowData.GetAll();
        }

        /// <summary>
        /// Get all active flows
        /// </summary>
        /// <returns></returns>
        public Task<FlowConfig[]> GetActiveFlows()
        {
            return this.FlowData.GetAllActive();
        }

        /// <summary>
        /// get a flow by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<FlowConfig> GetFlowByName(string name)
        {
            return this.FlowData.GetByName(name);
        }

        /// <summary>
        /// Save a flow with the gui input
        /// </summary>
        /// <param name="flowConfig"></param>
        /// <returns></returns>
        public async Task<Result> SaveFlowConfig(FlowGuiConfig flowConfig)
        {
            var config = await ConfigBuilder.Build(flowConfig);
            var existingFlow = await FlowData.GetByName(config.Name);
            Result result = null;
            if (existingFlow != null)
            {
                existingFlow.Gui = config.Gui;
                existingFlow.CommonProcessor.Template = config.CommonProcessor.Template;
                existingFlow.CommonProcessor.SparkJobTemplateRef = config.CommonProcessor.SparkJobTemplateRef;
                existingFlow.DisplayName = config.DisplayName;
                config = existingFlow;
                result = await FlowData.UpdateGuiForFlow(config.Name, config.Gui);

                if (result.IsSuccess)
                {
                    result = await FlowData.UpdateCommonProcessorForFlow(config.Name, config.CommonProcessor);
                }
            }
            else
            {
                result =  await this.FlowData.Upsert(config);
            }

            if (result.IsSuccess)
            {
                // pass the generated config back with the result
                var properties = new Dictionary<string, object>() { { FlowConfigPropertyName, config } };

                if (result.Properties != null)
                {
                    result.Properties.AppendDictionary(properties);
                }
                else
                {
                    result.Properties = properties;
                }
            }
            return result;
        }

        /// <summary>
        /// Start all jobs associated with the flow
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<Result> StartJobsForFlow(string name)
        {
            return ExecuteJobOperationForFlow(name, JobOperation.StartJob);
        }

        /// <summary>
        /// stop all jobs associated with the flow
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<Result> StopJobsForFlow(string name)
        {
            return ExecuteJobOperationForFlow(name, JobOperation.StopJob);
        }

        /// <summary>
        /// Restart all jobs associated with the flow
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Task<Result> RestartJobsForFlow(string name)
        {
            return ExecuteJobOperationForFlow(name, JobOperation.RestartJob);
        }

        /// <summary>
        /// Execute the passed-in job operation on flow with the given name
        /// </summary>
        /// <param name="flowName"></param>
        /// <param name="oper"></param>
        /// <returns></returns>
        private async Task<Result> ExecuteJobOperationForFlow(string flowName, Func<string, Task<Result>> oper)
        {
            var flow = await this.FlowData.GetByName(flowName);
            if (flow == null)
            {
                return new FailedResult($"flow '{flowName}' is not found");
            }

            return await ExecuteJobOperationForFlow(flow, oper);
        }

        /// <summary>
        /// Execute the passed-in job operation callback on the given flow
        /// </summary>
        /// <param name="flow"></param>
        /// <param name="oper"></param>
        /// <returns></returns>
        private async Task<Result> ExecuteJobOperationForFlow(FlowConfig flow, Func<string, Task<Result>> oper)
        {
            Ensure.NotNull(flow, "flow");
            var flowName = flow.Name;

            var jobNames = flow.JobNames;
            if (jobNames == null || jobNames.Length == 0)
            {
                return new FailedResult($"No jobs are associated with flow '{flowName}'");
            }

            try
            {
                var results = (await Task.WhenAll(jobNames.Select(oper))).ToList();
                var failedOnes = results.Where(r => !r.IsSuccess).Select(r=>r.Message).ToList();
                if (failedOnes.Count > 0)
                {
                    return new FailedResult($"Some jobs failed for flow '{flowName}': {string.Join(",", failedOnes)}");
                }

                return new SuccessResult($"done with {results.Count} jobs");
            }
            catch (Exception e)
            {
                return new FailedResult($"encounter exception:'{e.Message}' when processing job '{flowName}'");
            }
        }

        /// <summary>
        /// Stop all active flows' jobs
        /// </summary>
        /// <returns></returns>
        public Task<Result> StopJobsForAllActiveFlows()
        {
            return ExecuteJobOperationForAllActiveFlow(JobOperation.StopJob);
        }

        /// <summary>
        /// Restart all active flows' jobs
        /// </summary>
        /// <returns></returns>
        public Task<Result> RestartJobsForAllActiveFlows()
        {
            return ExecuteJobOperationForAllActiveFlow(JobOperation.RestartJob);
        }

        /// <summary>
        /// Start all active flows' jobs
        /// </summary>
        /// <returns></returns>
        public Task<Result> StartJobsForAllActiveFlows()
        {
            return ExecuteJobOperationForAllActiveFlow(JobOperation.StartJob);
        }

        /// <summary>
        /// Execute the passed-in job operation callback for all active flows' jobs
        /// </summary>
        /// <param name="oper"></param>
        /// <returns></returns>
        private async Task<Result> ExecuteJobOperationForAllActiveFlow(Func<string, Task<Result>> oper)
        {
            var flows = await GetActiveFlows();
            var results = await Task.WhenAll(flows.Select(f => ExecuteJobOperationForFlow(f, oper)));
            var failedOnes = results.Where(r => !r.IsSuccess).Select(r=>r.Message).ToList();
            if (failedOnes.Count > 0)
            {
                return new FailedResult($"Some flows failed: {string.Join(",", failedOnes)}");
            }

            return new SuccessResult($"done with {results.Length} flows");
        }
    }
}
