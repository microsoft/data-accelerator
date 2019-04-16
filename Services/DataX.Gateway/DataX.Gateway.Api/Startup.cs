// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using System.Fabric;
using System.Net.Http.Formatting;
using System.Web.Http;

namespace DataX.Gateway.Api
{
    public static class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public static void ConfigureApp(IAppBuilder appBuilder)
        {
            var configPackage = FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
            var authConfig = configPackage.Settings.Sections["AuthConfig"];
            var audience = authConfig.Parameters["Audience"].Value;
            var tenant = authConfig.Parameters["Tenant"].Value;
            appBuilder.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidAudience = audience,
                        RoleClaimType = "roles"
                    },
                    Tenant = tenant
                }
            );
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            var jsonFormatter = new JsonMediaTypeFormatter();
            config.Services.Replace(typeof(IContentNegotiator), new JsonContentNegotiator(jsonFormatter));

            config.MapHttpAttributeRoutes();
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            appBuilder.UseWebApi(config);

            // Set content-type options header to honor the server's mimetype
            appBuilder.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Content-Type-Options", new string[] { "nosniff" });
                await next();
            });
        }
    }
}
