// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Flow.Common;
using DataX.Gateway.Contract;
using DataX.Utility.ServiceCommunication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Flow.Scheduler
{
    public class TimedScheduler : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly EngineEnvironment _engineEnvironment;

        // Frequency at which to run the scheduler
        private readonly int _schedulerWakeupFrequencyInMin = 60;
        private readonly int _oneMinInMilliSeconds = 60 * 1000;

        internal static InterServiceCommunicator Communicator
        {
            private get;
            set;
        }

        public TimedScheduler(ILogger<TimedScheduler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _engineEnvironment = new EngineEnvironment(_configuration);

            Communicator = new InterServiceCommunicator(new TimeSpan(0, 4, 0));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{DateTime.UtcNow}:TimedScheduler is starting.");

            stoppingToken.Register(() =>
               _logger.LogInformation($"{DateTime.UtcNow}: TimedScheduler background task is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"{DateTime.UtcNow}:TimedScheduler task doing background work.");

                await StartBatchJobs().ConfigureAwait(false);

                await Task.Delay(_schedulerWakeupFrequencyInMin * _oneMinInMilliSeconds, stoppingToken).ConfigureAwait(false);
            }

            _logger.LogInformation($"{DateTime.UtcNow}: TimedScheduler background task is stopping.");
        }

        private async Task<Dictionary<string, string>> SetRequestHeader()
        {
            var response = await _engineEnvironment.GetEnvironmentVariables().ConfigureAwait(false);
            if (response.Error.HasValue && response.Error.Value)
            {
                _logger.LogError(response.Message);
                throw new Exception("Can't get environment variables.");
            }
            var clientId = _engineEnvironment.EngineFlowConfig.ConfiggenClientId;
            var clientSecret = Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.ConfiggenClientSecret);
            var clientResourceId = Helper.GetSecretFromKeyvaultIfNeeded(_engineEnvironment.EngineFlowConfig.ConfiggenClientResourceId);
            var tenantId = _engineEnvironment.EngineFlowConfig.ConfiggenTenantId;

            var authenticationContext = new AuthenticationContext($"https://login.windows.net/{tenantId}");
            var credential = new ClientCredential(clientId, clientSecret);
            var apiToken = authenticationContext.AcquireTokenAsync(clientResourceId, credential).Result.AccessToken;

            var jwtHandler = new JwtSecurityTokenHandler();
            var readableToken = jwtHandler.CanReadToken(apiToken);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (readableToken == true)
            {
                var token = jwtHandler.ReadJwtToken(apiToken);

                var roleValue = token.Claims.FirstOrDefault(c => c.Type == "roles")?.Value;
                if (!string.IsNullOrEmpty(roleValue))
                {
                    headers.Add(Constants.UserRolesHeader, roleValue);

                }
            }

            return headers;
        }

        private async Task StartBatchJobs()
        {
            var headers = await SetRequestHeader().ConfigureAwait(false);

            _logger.LogInformation($"{DateTime.UtcNow}:TimedScheduler Starting batch jobs");

            try
            {
                await Communicator.InvokeServiceAsync(HttpMethod.Post, "DataX.Flow", "Flow.ManagementService", "flow/schedulebatch", headers).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var message = e.InnerException == null ? e.Message : e.InnerException.Message;
                _logger.LogInformation($"{DateTime.UtcNow}:TimedScheduler an exception is thrown:" + message);
            }
        }
    }
}
