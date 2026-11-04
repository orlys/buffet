using System;
using Buffet.Core;
using Buffet.Database.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Buffet;

public static class SqliteApplicationExtensions
{
    public static BuffetApplication AddSqliteDatabase(this BuffetApplication app)
    {
        app.Services.AddBuffetDbContextProvider<SqliteContext>("Sqlite");

        return app;
    }

    public static BuffetApplication AddSqliteDatabase(
        this BuffetApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddSqliteDatabase();
        app.Services.Configure(configure);
        return app;
    }

    /// <summary>
    /// Stores package and symbol files in the SQLite database configured by <see cref="DatabaseOptions"/>.
    /// </summary>
    public static BuffetApplication AddSqliteStorage(this BuffetApplication app)
    {
        app.Services.TryAddTransient<SqliteStorageService>();
        app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<SqliteStorageService>());
        app.Services.AddProvider<IStorageService>((provider, config) => provider.GetRequiredService<SqliteStorageService>());

        return app;
    }
}
