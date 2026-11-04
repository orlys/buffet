using System;
using System.Collections.Generic;
using Buffet.Core;
using Buffet.Database.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Buffet.Tests;

public class HostIntegrationTests
{
    private readonly string ConnectionStringKey = "Database:ConnectionString";

    [Fact]
    public void IgnoresLegacyDatabaseType()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string>
        {
            { "Database:Type", "MySql" },
            { ConnectionStringKey, "..." }
        });

        Assert.IsType<SqliteContext>(provider.GetRequiredService<IContext>());
    }

    [Fact]
    public void ReturnsDatabaseContext()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string>
        {
            { ConnectionStringKey, "..." }
        });

        Assert.NotNull(provider.GetRequiredService<IContext>());
    }

    [Fact]
    public void ReturnsSqliteContext()
    {
        var provider = BuildServiceProvider(new Dictionary<string, string>
        {
            { ConnectionStringKey, "..." }
        });

        Assert.NotNull(provider.GetRequiredService<SqliteContext>());
    }

    [Fact]
    public void DefaultsToSqlite()
    {
        var provider = BuildServiceProvider();

        var context = provider.GetRequiredService<IContext>();

        Assert.IsType<SqliteContext>(context);
    }

    [Fact]
    public void StoresFilesInSqlite()
    {
        var provider = BuildServiceProvider();

        Assert.IsType<SqliteStorageService>(provider.GetRequiredService<IStorageService>());
    }

    private IServiceProvider BuildServiceProvider(Dictionary<string, string> configs = null)
    {
        var host = Program
            .CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(configs ?? new Dictionary<string, string>());
            })
            .Build();

        return host.Services;
    }
}
