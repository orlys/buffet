using System;
using BaGetter.Core;
using BaGetter.Database.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BaGetter;

public static class SqliteApplicationExtensions
{
    public static BaGetterApplication AddSqliteDatabase(this BaGetterApplication app)
    {
        app.Services.AddBaGetDbContextProvider<SqliteContext>("Sqlite");

        return app;
    }

    public static BaGetterApplication AddSqliteDatabase(
        this BaGetterApplication app,
        Action<DatabaseOptions> configure)
    {
        app.AddSqliteDatabase();
        app.Services.Configure(configure);
        return app;
    }

    /// <summary>
    /// Stores package and symbol files in the SQLite database configured by <see cref="DatabaseOptions"/>.
    /// </summary>
    public static BaGetterApplication AddSqliteStorage(this BaGetterApplication app)
    {
        app.Services.TryAddTransient<SqliteStorageService>();
        app.Services.TryAddTransient<IStorageService>(provider => provider.GetRequiredService<SqliteStorageService>());
        app.Services.AddProvider<IStorageService>((provider, config) => provider.GetRequiredService<SqliteStorageService>());

        return app;
    }
}
