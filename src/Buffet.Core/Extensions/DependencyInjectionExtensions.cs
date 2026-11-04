using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using Buffet.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Buffet.Core;

public static partial class DependencyInjectionExtensions
{
    public static BuffetApplication AddBuffetApplication(
        this IServiceCollection services,
        Action<BuffetApplication> configureAction)
    {
        var app = new BuffetApplication(services);

        services.AddConfiguration();
        services.AddBuffetServices();

        configureAction(app);

        services.AddFallbackServices();

        return app;
    }

    /// <summary>
    /// Configures and validates options.
    /// </summary>
    /// <typeparam name="TOptions">The options type that should be added.</typeparam>
    /// <param name="services">The dependency injection container to add options.</param>
    /// <param name="key">
    /// The configuration key that should be used when configuring the options.
    /// If null, the root configuration will be used to configure the options.
    /// </param>
    /// <returns>The dependency injection container.</returns>
    public static IServiceCollection AddBuffetOptions<TOptions>(
        this IServiceCollection services,
        string key = null)
        where TOptions : class
    {
        services.AddSingleton<IValidateOptions<TOptions>>(new ValidateBuffetOptions<TOptions>(key));
        services.AddSingleton<IConfigureOptions<TOptions>>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            if (key != null)
            {
                config = config.GetSection(key);
            }

            return new BindOptions<TOptions>(config);
        });

        return services;
    }

    private static void AddConfiguration(this IServiceCollection services)
    {
        services.AddBuffetOptions<BuffetOptions>();
        services.AddBuffetOptions<DatabaseOptions>(nameof(BuffetOptions.Database));
        services.AddBuffetOptions<MirrorOptions>(nameof(BuffetOptions.Mirror));
        services.AddBuffetOptions<RetentionOptions>(nameof(BuffetOptions.Retention));
        services.AddBuffetOptions<StatisticsOptions>(nameof(BuffetOptions.Statistics));
    }

    private static void AddBuffetServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IFrameworkCompatibilityService, FrameworkCompatibilityService>();
        services.TryAddSingleton<IPackageDownloadsSource, PackageDownloadsJsonSource>();

        services.TryAddSingleton<ISearchResponseBuilder, SearchResponseBuilder>();
        services.TryAddSingleton<NuGetClient>();
        services.TryAddSingleton<NullSearchIndexer>();
        services.TryAddSingleton<NullSearchService>();
        services.TryAddSingleton<RegistrationBuilder>();
        services.TryAddSingleton<SystemTime>();
        services.TryAddSingleton<ValidateStartupOptions>();

        services.TryAddSingleton(HttpClientFactory);
        services.TryAddSingleton(NuGetClientFactoryFactory);

        services.TryAddScoped<DownloadsImporter>();

        services.TryAddTransient<IAuthenticationService, ApiKeyAuthenticationService>();
        services.TryAddTransient<IPackageContentService, DefaultPackageContentService>();
        services.TryAddTransient<IPackageDeletionService, PackageDeletionService>();
        services.TryAddTransient<IPackageIndexingService, PackageIndexingService>();
        services.TryAddTransient<IPackageMetadataService, DefaultPackageMetadataService>();
        services.TryAddTransient<IPackageService, PackageService>();
        services.TryAddTransient<IPackageStorageService, PackageStorageService>();
        services.TryAddTransient<IServiceIndexService, BuffetServiceIndex>();
        services.TryAddTransient<ISymbolIndexingService, SymbolIndexingService>();
        services.TryAddTransient<ISymbolStorageService, SymbolStorageService>();

        services.TryAddTransient<DatabaseSearchService>();
        services.TryAddTransient<PackageService>();
        services.TryAddTransient<V2UpstreamClient>();
        services.TryAddTransient<V3UpstreamClient>();
        services.TryAddTransient<DisabledUpstreamClient>();
        services.TryAddTransient<PackageDatabase>();

        services.TryAddTransient(UpstreamClientFactory);
    }

    private static void AddFallbackServices(this IServiceCollection services)
    {
        services.TryAddScoped<IContext, NullContext>();

        // Buffet's services have multiple implementations that live side-by-side.
        // The application will choose the implementation using one of two ways:
        //
        // 1. Using the first implementation that was registered in the dependency injection
        //    container. This is the strategy used by applications that embed Buffet.
        // 2. Using "providers". The providers will examine the application's configuration to
        //    determine whether its service implementation is active. Thsi is the strategy used
        //    by the default Buffet application.
        //
        // Buffet has database and search services, but the database services are special
        // in that they may also act as search services. If an application registers the
        // database service first and the search service second, the application should
        // use the search service even though it wasn't registered first. Furthermore,
        // if an application registers a database service without a search service, the
        // database service should be used for search. This effect is achieved by deferring
        // the database search service's registration until the very end.
        services.TryAddTransient<ISearchIndexer>(provider => provider.GetRequiredService<NullSearchIndexer>());
        services.TryAddTransient<ISearchService>(provider => provider.GetRequiredService<DatabaseSearchService>());
    }

    private static HttpClient HttpClientFactory(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<MirrorOptions>>().Value;

        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly.GetName().Name;
        var assemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        var client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        });

        client.DefaultRequestHeaders.Add("User-Agent", $"{assemblyName}/{assemblyVersion}");
        client.Timeout = TimeSpan.FromSeconds(options.PackageDownloadTimeoutSeconds);

        return client;
    }

    private static NuGetClientFactory NuGetClientFactoryFactory(IServiceProvider provider)
    {
        var httpClient = provider.GetRequiredService<HttpClient>();
        var options = provider.GetRequiredService<IOptions<MirrorOptions>>().Value;

        if (options.Authentication is { } auth)
        {
            switch (auth.Type)
            {
                case MirrorAuthenticationType.Basic:
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.Username}:{auth.Password}"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    break;

                case MirrorAuthenticationType.Bearer:
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                    break;

                case MirrorAuthenticationType.Custom:
                    foreach (var (header, value) in auth.CustomHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(header, value);
                    }
                    break;
            }
        }

        return new NuGetClientFactory(
            httpClient,
            options.PackageSource.ToString());
    }

    private static IUpstreamClient UpstreamClientFactory(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptionsSnapshot<MirrorOptions>>();

        return options.Value.Enabled switch
        {
            false => provider.GetRequiredService<DisabledUpstreamClient>(),
            true when options.Value.Legacy => provider.GetRequiredService<V2UpstreamClient>(),
            _ => provider.GetRequiredService<V3UpstreamClient>()
        };
    }
}
