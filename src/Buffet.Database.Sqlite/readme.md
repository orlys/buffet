# Buffet's SQLite Database Provider

This project contains Buffet's SQLite database provider.

## Migrations

Check for pending changes with:
```
dotnet ef migrations has-pending-model-changes --context SqliteContext --startup-project ..\Buffet\Buffet.csproj
```

Add a migration with:

```
dotnet ef migrations add MigrationName --context SqliteContext --output-dir Migrations --startup-project ..\Buffet\Buffet.csproj
```

Apply the migration to your database with:

```
dotnet ef database update --context SqliteContext
```
