using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using Sigo.ApiGateway.Logger;

namespace Sigo.ApiGateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddJwtBearer(Configuration.GetSection("Auth:AuthenticationProviderKey").Value, options =>
                {
                    options.Authority = Configuration.GetSection("Auth:BaseUrl").Value;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });

            services.AddOcelot();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            var serilog = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File(@"Logs\log.txt", rollingInterval: RollingInterval.Day);

            loggerFactory.WithFilter(new FilterLoggerSettings
            {
                {"IdentityServer4", LogLevel.Debug},
                {"Microsoft", LogLevel.Warning},
                {"System", LogLevel.Warning},
            }).AddSerilog(serilog.CreateLogger());

            await app.UseOcelot();
        }
    }
}