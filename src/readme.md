# Buffet Source Code

These folders contain the core components of Buffet:

* `Buffet` - The app's entry point that glues everything together.
* `Buffet.Core` - Buffet's core logic and services.
* `Buffet.Web` - The [NuGet server APIs](https://docs.microsoft.com/en-us/nuget/api/overview) and web UI.
* `Buffet.Protocol` - Libraries to interact with [NuGet servers' APIs](https://docs.microsoft.com/en-us/nuget/api/overview).

These folders contain database-specific components of Buffet:

* `Buffet.Database.MySql` - Buffet's MySQL database provider.
* `Buffet.Database.PostgreSql` - Buffet's PostgreSql database provider.
* `Buffet.Database.Sqlite` - Buffet's SQLite database provider.
* `Buffet.Database.SqlServer` - Buffet's Microsoft SQL Server database provider.

These folders contain cloud-specific components of Buffet:

* `Buffet.Aliyun` - Buffet's Alibaba Cloud(Aliyun) provider.
* `Buffet.Aws` - Buffet's Amazon Web Services provider.
* `Buffet.Azure` - Buffet's Azure provider.
* `Buffet.Gcp` - Buffet's Google Cloud Platform provider.
* `Buffet.Tencent` - Buffet's Tencent Cloud Platform provider.

