// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Config.Templating;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.Config
{
    public class JobDeploymentSession : DeploymentSession
    {
        public JobDeploymentSession(string name, TokenDictionary tokens, FlowDeploymentSession flow) 
            : base(name, tokens)
        {
            this.Flow= flow;
            this.JobConfigs = new List<JobConfig>();
        }

        public FlowDeploymentSession Flow { get; }
        
        public List<JobConfig> JobConfigs { get; }

        public string SparkJobConfigFilePath
        {
            get => this.GetTokenString(Constants.TokenName_SparkJobConfigFilePath);
            set => this.SetStringToken(Constants.TokenName_SparkJobConfigFilePath, value);
        }

        public string SparkJobName
        {
            get => this.GetTokenString(Constants.TokenName_SparkJobName);
            set => this.SetStringToken(Constants.TokenName_SparkJobName, value);
        }

        public T GetAttachment<T>(string key) where T: class
        {
            return this.Flow.GetAttachment<T>(key);
        }
    }
}
