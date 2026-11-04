using System;
using Buffet.Authentication;
using Buffet.Core;
using Buffet.Core.Extensions;
using Buffet.Web;
using Buffet.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using HealthCheckOptions = Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions;

namespace Buffet;

public class Startup
{
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureOptions<ValidateBuffetOptions>();
        services.ConfigureOptions<ConfigureBuffetServer>();

        services.AddBuffetOptions<IISServerOptions>(nameof(IISServerOptions));
        services.AddBuffetWebApplication(ConfigureBuffetApplication);

        // You can swap between implementations of subsystems like storage and search using Buffet's configuration.
        // Each subsystem's implementation has a provider that reads the configuration to determine if it should be
        // activated. Buffet will run through all its providers until it finds one that is active.
        services.AddScoped(DependencyInjectionExtensions.GetServiceFromProviders<IContext>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IStorageService>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<IPackageDatabase>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchService>);
        services.AddTransient(DependencyInjectionExtensions.GetServiceFromProviders<ISearchIndexer>);

        services.AddSingleton<IConfigureOptions<MvcRazorRuntimeCompilationOptions>, ConfigureRazorRuntimeCompilation>();

        services.AddHealthChecks();

        services.AddCors();
    }

    private void ConfigureBuffetApplication(BuffetApplication app)
    {
        //Add base authentication and authorization
        app.AddNugetBasicHttpAuthentication();
        app.AddNugetBasicHttpAuthorization();

        // One SQLite file holds the metadata, the search index and the package files; nothing to select.
        app.AddSqliteDatabase();
        app.AddSqliteStorage();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var options = Configuration.Get<BuffetOptions>();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseStatusCodePages();
        }

        app.UseForwardedHeaders();
        app.UsePathBase(options.PathBase);

        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();

        app.UseCors(ConfigureBuffetServer.CorsPolicy);

        app.UseOperationCancelledMiddleware();

        app.UseEndpoints(endpoints =>
        {
            var buffet = new BuffetEndpointBuilder();

            buffet.MapEndpoints(endpoints);
        });

        app.UseHealthChecks(options.HealthCheck.Path,
            new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    await report.FormatAsJson(context.Response.Body, options.Statistics.ListConfiguredServices, options.HealthCheck.StatusPropertyName,
                        context.RequestAborted);
                },
                Predicate = check => check.IsConfigured(options)
            }
        );
    }
}
