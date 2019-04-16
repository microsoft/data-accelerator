// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Templating;
using DataX.Contract;
using DataX.Contract.Exception;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config
{
    /// <summary>
    /// Represents a session to deploy flow runtime config
    /// </summary>
    public class FlowDeploymentSession : DeploymentSession
    {
        public FlowDeploymentSession(FlowConfig config) : base(config.Name, null)
        {
            Attachments = new ConcurrentDictionary<string, object>();
            ProgressMessages = new List<string>();

            UpdateFlowConfig(config);
        }

        public void UpdateFlowConfig(FlowConfig config)
        {
            Ensure.NotNull(config, "config");
            Config = config;

            SetStringToken(FlowConfig.TokenName_Name, config.Name);
            Tokens.AddDictionary(config.Properties?.ToTokens());

            _jobs = null;
        }

        public Dictionary<string, object> ResultProperties = new Dictionary<string, object>();

        public FlowConfig Config { get; set; }
        public Result Result { get; set; }

        public async Task ExecuteProcessor(IFlowDeploymentProcessor processor)
        {
            try
            {
                var progress = await processor.Process(this);
                this.LogProgress(processor, progress);
            }
            catch(Exception e)
            {
                this.LogProgress(processor, $"Encounter error: {e.Message}");
                throw new GeneralException($"Hit error in processor '{processor.GetType()}':{e.Message}", e);
            }
        }

        #region Progress Messages
        public void LogProgress(string msg)
        {
            // this.ProgressMessages.Add(msg);
            Console.WriteLine(msg);
        }

        public void LogProgress(IFlowDeploymentProcessor processor, string msg)
        {
            this.LogProgress($"{processor.GetOrder()} - {processor.GetType().Name} : {msg}");
        }

        public List<string> ProgressMessages { get; }

        #endregion

        #region Jobs
        private JobDeploymentSession[] _jobs = null;
        public JobDeploymentSession[] GetJobs()
        {
            if (this._jobs == null)
            {
                // resolved the token values
                var tokens = Tokens.Clone();

                // collect job common tokens
                var cp = Config?.CommonProcessor;
                var commonJobTokens = cp?.JobCommonTokens?.ToTokens();
                tokens.AddDictionary(commonJobTokens);
                
                // for each job, append job sepcific tokens and resolve them
                this._jobs = this.Config?
                    .GetJobs(tokens)?
                    .Select(job => new JobDeploymentSession(JobMetadata.GetJobName(job.Tokens), job.Tokens, this))?
                    .ToArray();
            }

            return this._jobs;
        }
        #endregion

        #region Attachments
        protected ConcurrentDictionary<string, object> Attachments { get; }

        public void SetAttachment(string key, object value)
        {
            this.Attachments.AddOrUpdate(key, value, (k, v) => value);
        }

        public T GetAttachment<T>(string key) where T : class
        {
            this.Attachments.TryGetValue(key, out object value);
            if (value == null)
            {
                return null;
            }
            else
            {
                return value as T;
            }
        }
        #endregion
    }
}
