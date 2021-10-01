// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
namespace DataX.ServiceHost.AspNetCore.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Extensions for IApplicationBuilder
    /// </summary>
    public static class DataXApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures and ApplicationBuilder with DataX defaults.
        /// </summary>
        /// <param name="app">The ApplicationBuilder to be configured</param>
        /// <returns>The configured ApplicationBuilder</returns>
        public static IApplicationBuilder UseDataXApplicationDefaults(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseMvc();

            return app;
        }
    }
}
