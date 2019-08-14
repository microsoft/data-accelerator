// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using DataX.Config.ConfigDataModel.RuntimeConfig;
using DataX.Contract;
using DataX.Flow.CodegenRules;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataX.Config.ConfigDataModel;

namespace DataX.Config.ConfigGeneration.Processor
{
    /// <summary>
    /// Produce the accumulation/state tables section
    /// </summary>
    [Shared]
    [Export(typeof(IFlowDeploymentProcessor))]
    public class ResolveStateTable: ProcessorBase
    {
        public const string TokenName_StateTables = "processStateTables";

        [ImportingConstructor]
        public ResolveStateTable(ConfigGenConfiguration conf)
        {
            Configuration = conf;
        }
        private ConfigGenConfiguration Configuration { get; }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var config = flowToDeploy.Config;
            var guiConfig = config?.GetGuiConfig();
            if (guiConfig == null)
            {
                return "no gui input, skipped.";
            }

            var rulesCode = flowToDeploy.GetAttachment<RulesCode>(PrepareTransformFile.AttachmentName_CodeGenObject);
            Ensure.NotNull(rulesCode, "rulesCode");
            Ensure.NotNull(rulesCode.MetricsRoot, "rulesCode.MetricsRoot");
            Ensure.NotNull(rulesCode.MetricsRoot.metrics, "rulesCode.MetricsRoot.metrics");

            var stateTables = rulesCode.AccumlationTables.Select(t =>
            {
                return new StateTableSpec()
                {
                    Name = t.Key,
                    Schema = t.Value,
                    Location = ConstructStateTablePath(guiConfig.Name, t.Key)
            };
            }).ToArray();

            flowToDeploy.SetObjectToken(TokenName_StateTables, stateTables);

            await Task.Yield();

            return "done";
        }


        private string ConstructStateTablePath(string flowName, string tableName)
        {
            var sparkType = Configuration.TryGet(Constants.ConfigSettingName_SparkType, out string value) ? value : null;
            var prefix = (sparkType == Constants.SparkTypeDataBricks) ? Constants.PrefixDbfs : Constants.PrefixHdfs;
            return $"{prefix}mycluster/datax/{flowName}/{tableName}/";
        }
    }
}
