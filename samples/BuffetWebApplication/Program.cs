using Buffet;
using Buffet.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder();

// This will add the Buffet services and options to the container.
builder.Services.AddBuffetWebApplication(buffet =>
{
    buffet.AddSqliteDatabase();
    buffet.AddSqliteStorage();
});
var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

// Add Buffet's endpoints.
new BuffetEndpointBuilder().MapEndpoints(app);

await app.RunMigrationsAsync();
await app.RunAsync();
