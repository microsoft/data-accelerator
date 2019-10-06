// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using DataX.Flow.CodegenRules;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Update the metrics section in the flow config
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class UpdateFlowMetrics : ProcessorBase
    {
        [ImportingConstructor]
        public UpdateFlowMetrics(FlowDataManager flowData)
        {
            FlowData = flowData;
        }

        private FlowDataManager FlowData { get; }

        public override int GetOrder()
        {
            return 850;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config.GetGuiConfig();

            if (guiConfig == null)
            {
                return "no gui input, skipped";
            }

            // Get code gen data from earlier step
            var rulesCode = flowToDeploy.GetAttachment<RulesCode>(PrepareTransformFile.AttachmentName_CodeGenObject);
            Ensure.NotNull(rulesCode, "rulesCode");
            Ensure.NotNull(rulesCode.MetricsRoot, "rulesCode.MetricsRoot");
            Ensure.NotNull(rulesCode.MetricsRoot.metrics, "rulesCode.MetricsRoot.metrics");

            // Set the metrics interval to the batch interval
            var intervalInSeconds = guiConfig?.Input?.Properties?.WindowDuration;
            Ensure.NotNull(intervalInSeconds, "input.properties.windowDuration");

            foreach (var s in rulesCode.MetricsRoot.metrics.sources)
            {
                s.input.pollingInterval = Convert.ToInt32(intervalInSeconds) * 1000;
            }

            string metricsRaw = JsonConvert.SerializeObject(rulesCode.MetricsRoot.metrics);
            string metrics = metricsRaw.Replace("_FLOW_", config.Name);

            var newMetrics = JsonConvert.DeserializeObject<MetricsConfig>(metrics);
            var defaultConfig = flowToDeploy.GetAttachment<FlowConfig>(MergeDefaultConfig.AttachementName_DefaultFlowConfig);
            var defaultMetrics = defaultConfig?.Metrics;
            newMetrics.Sources = MergeLists(defaultMetrics?.Sources, newMetrics?.Sources);
            newMetrics.Widgets = MergeLists(defaultMetrics?.Widgets, newMetrics?.Widgets);

            await FlowData.UpdateMetricsForFlow(config.Name, newMetrics);
            return "done";
        }

        private static T[] MergeLists<T>(T[] list1, T[] list2)
        {
            if (list1 == null)
            {
                return list2;
            }

            if(list2 == null)
            {
                return list1;
            }

            if (list1.Length == 0)
            {
                return list2;
            }

            if (list2.Length == 0)
            {
                return list1;
            }

            var result = new List<T>(list1);
            result.AddRange(list2);
            return result.ToArray();
        }
    }
}
