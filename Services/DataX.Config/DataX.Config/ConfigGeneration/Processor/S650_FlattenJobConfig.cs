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
        public const string TokenName_InputBatching = "inputBatching";

        [ImportingConstructor]
        public FlattenJobConfig(ConfigFlattenerManager flatteners, IKeyVaultClient keyVaultClient)
        {
            this.ConfigFlatteners = flatteners;
            this.KeyVaultClient = keyVaultClient;
        }

        private ConfigFlattenerManager ConfigFlatteners { get; }
        private IKeyVaultClient KeyVaultClient { get; }

        public override int GetOrder()
        {
            return 650;
        }

        public override async Task<string> Process(FlowDeploymentSession flowToDeploy)
        {
            var flowConfig = flowToDeploy.Config;

            var inputConfig = flowConfig?.GetGuiConfig();

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
                    var destFolder = job.GetTokenString(PrepareJobConfigVariables.TokenName_RuntimeConfigFolder);
                    destFolder = this.GetJobConfigFilePath(jc.IsOneTime, jc.ProcessingTime, destFolder);

                    if (jsonContent != null)
                    {
                        if (inputConfig.Input.Mode == Constants.InputMode_Batching)
                        {
                            jsonContent = await GetBatchConfigContent(inputConfig, jc, job);
                        }

                        var json = JsonConfig.From(jsonContent);
                        var name = job.Name;
                        var destinationPath = ResourcePathUtil.Combine(destFolder, name + ".conf");


                        jc.Content = flattener.Flatten(json);
                        jc.FilePath = destinationPath;

                    }
                }
            }

            return "done";
        }

        private async Task<string> GetBatchConfigContent(FlowGuiConfig inputConfig, JobConfig jc, JobDeploymentSession job)
        {
            var inputBatching = inputConfig.Input.Batching.Inputs ?? Array.Empty<FlowGuiInputBatchingInput>();
            var specsTasks = inputBatching.Select(async rd =>
            {
                var connectionString = await KeyVaultClient.ResolveSecretUriAsync(rd.InputConnection).ConfigureAwait(false);
                var inputPath = await KeyVaultClient.ResolveSecretUriAsync(rd.InputPath).ConfigureAwait(false);

                return new InputBatchingSpec()
                {
                    Name = ParseBlobAccountName(connectionString),
                    Path = rd.InputPath,
                    Format = "JSON",
                    CompressionType = "None",
                    ProcessStartTime = jc.ProcessStartTime,
                    ProcessEndTime = jc.ProcessEndTime,
                    PartitionIncrement = GetPartitionIncrement(inputPath).ToString(CultureInfo.InvariantCulture),
                };
            }).ToArray();

            var specs = await Task.WhenAll(specsTasks).ConfigureAwait(false);

            job.SetObjectToken(TokenName_InputBatching, specs);

            var jsonContent = job.Tokens.Resolve(jc.Content);

            return jsonContent;
        }

        private static long GetPartitionIncrement(string path)
        {
            Regex regex = new Regex(@"\{([yMdHhmsS\-\/.,: ]+)\}*", RegexOptions.IgnoreCase);
            Match mc = regex.Match(path);

            if (mc != null && mc.Success && mc.Groups.Count > 1/*&& mc..Count > 0*/)
            {
                var value = mc.Groups[1].Value.Trim();

                value = value.Replace(@"[\/:\s-]", "", StringComparison.InvariantCultureIgnoreCase).Replace(@"(.)(?=.*\1)", "", StringComparison.InvariantCultureIgnoreCase);

                if (value.Contains("h", StringComparison.InvariantCultureIgnoreCase))
                {
                    return 1 * 60;
                }
                else if (value.Contains("d", StringComparison.InvariantCultureIgnoreCase))
                {
                    return 1 * 60 * 24;
                }
                else if (value.Contains("M", StringComparison.InvariantCulture))
                {
                    return 1 * 60 * 24 * 30;
                }
                else if (value.Contains("y", StringComparison.InvariantCultureIgnoreCase))
                {
                    return 1 * 60 * 24 * 30 * 12;
                }

            }

            return 1;
        }

        private string GetJobConfigFilePath(bool isOneTime, string partitionName, string baseFolder)
        {
            var oneTimeFolderName = "";

            if (isOneTime)
            {
                oneTimeFolderName = $"OneTime/{Regex.Replace(partitionName, "[^0-9]", "")}";
            }

            return ResourcePathUtil.Combine(baseFolder, oneTimeFolderName);
        }

        private static string ParseBlobAccountName(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<=AccountName=)(.*)(?=;AccountKey)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have AccountName";
            }

            return matched;
        }
    }
}
