namespace DataX.ServiceHost.AspNetCore.Startup
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides a common interface for DataX startup classes to use for instantiation in ASP.NET Core
    /// </summary>
    public interface IDataXServiceStartup
    {
        // These methods follow IStartup's interface,
        // but it's not extended as we don't want autoload from default WebHost builder

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        void ConfigureServices(IServiceCollection services);

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        void Configure(IApplicationBuilder app);
    }
}
