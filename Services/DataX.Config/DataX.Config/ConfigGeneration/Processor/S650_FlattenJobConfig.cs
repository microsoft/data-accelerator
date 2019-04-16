// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigGeneration.Processor;
using DataX.Config.Utility;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Flatten the job config to properties format
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class FlattenJobConfig : ProcessorBase
    {
        [ImportingConstructor]
        public FlattenJobConfig(ConfigFlattenerManager flatteners)
        {
            this.ConfigFlatteners = flatteners;
        }

        private ConfigFlattenerManager ConfigFlatteners { get; }

        public override int GetOrder()
        {
            return 650;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;

            // get a flattener
            var flattener = await this.ConfigFlatteners.GetDefault();
            if (flattener == null)
            {
                return "no flattern config, skipped";
            }
            
            // flatten each job config
            var jobs = flowToDeploy.GetJobs();
            if (jobs == null)
            {
                return "no jobs, skipped";
            }

            foreach(var job in jobs)
            {
                var jsonContent = job.GetTokenString(GenerateJobConfig.TokenName_JobConfigContent);
                var destFolder = job.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);

                if (jsonContent != null)
                {
                    var json = JsonConfig.From(jsonContent);
                    job.SetStringToken(GenerateJobConfig.TokenName_JobConfigContent, flattener.Flatten(json));

                    var destinationPath = ResourcePathUtil.Combine(destFolder, job.Name + ".conf");
                    job.SetStringToken(GenerateJobConfig.TokenName_JobConfigFilePath, destinationPath);
                }
            }
            
            return "done";
        }
    }
}
