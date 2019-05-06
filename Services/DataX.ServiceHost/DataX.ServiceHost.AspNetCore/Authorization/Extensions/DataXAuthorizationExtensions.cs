using DataX.ServiceHost.Settings;
using DataX.Utilities.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DataX.ServiceHost.AspNetCore.Authorization.Requirements;

namespace DataX.ServiceHost.AspNetCore.Authorization.Extensions
{
    public static class DataXAuthorizationExtensions
    {
        public static IServiceCollection AddDataXAuthorization(this IServiceCollection services)
        {
            return services.AddDataXAuthorization(null);
        }

        public static IServiceCollection AddDataXAuthorization(this IServiceCollection services, Action<AuthorizationPolicyBuilder> configurePolicy)
        {
            var settings = services.BuildServiceProvider().GetService<DataXSettings>();

            // EnableOneBox scenario as it requires the least configuration and we can't assume cloud connection settings
            if (settings == null)
            {
                settings = new DataXSettings()
                {
                    EnableOneBox = true,
                    LocalRoot = "",
                    MetricsHttpEndpoint = "http://localhost:2020/",
                    SparkHome = "",
                };
            }

            return services.AddAuthorization(options =>
            {
                new DataXPolicyBuilder(options, settings, configurePolicy)
                    .AddPolicy<DataXWriterRequirement>(DataXAuthConstants.WriterPolicyName)
                    .AddPolicy<DataXReaderRequirement>(DataXAuthConstants.ReaderPolicyName);
            });
        }
    }
}
