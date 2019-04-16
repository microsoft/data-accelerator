// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json.Linq;
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System.Threading.Tasks;

namespace DataX.Config
{
    /// <summary>
    /// Contract for job management client
    /// </summary>
    public interface ISparkJobClient
    {
        Task<SparkJobSyncResult> SubmitJob(JToken jobSubmissionData);
        Task<SparkJobSyncResult[]> GetJobs();
        Task<SparkJobSyncResult> StopJob(JToken jobClientData);
        Task<SparkJobSyncResult> GetJobInfo(JToken jobClientData);
    }
}
