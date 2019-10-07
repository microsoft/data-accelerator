// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.ConfigDataModel.RuntimeConfig;
using DataX.Config.Utility;
using System;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;

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
                foreach (var jc in job.JobConfigs)
                {
                    var jsonContent = job.Tokens.Resolve(jc.Content);

                    if (jsonContent != null)
                    {
                        var json = JsonConfig.From(jsonContent);
                        jc.Content = flattener.Flatten(json);
                    }
                }
            }

            return "done";
        }
    }
}
