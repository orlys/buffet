using System;
using System.Threading;
using System.Threading.Tasks;
using Buffet.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Buffet.Web;

public static class IHostExtensions
{
    public static IHostBuilder UseBuffet(this IHostBuilder host, Action<BuffetApplication> configure)
    {
        return host.ConfigureServices(services =>
        {
            services.AddBuffetWebApplication(configure);
        });
    }

    public static async Task RunMigrationsAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        // Run migrations if necessary.
        var options = host.Services.GetRequiredService<IOptions<BuffetOptions>>();

        if (options.Value.RunMigrationsAtStartup)
        {
            using var scope = host.Services.CreateScope();
            var ctx = scope.ServiceProvider.GetService<IContext>();
            if (ctx != null)
            {
                await ctx.RunMigrationsAsync(cancellationToken);
            }
        }
    }

    public static bool ValidateStartupOptions(this IHost host)
    {
        return host
            .Services
            .GetRequiredService<ValidateStartupOptions>()
            .Validate();
    }
}
