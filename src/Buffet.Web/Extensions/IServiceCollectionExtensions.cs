using System;
using System.Text.Json.Serialization;
using Buffet.Authentication;
using Buffet.Core;
using Buffet.Web;
using Buffet.Web.Authentication;
using Buffet.Web.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Buffet;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddBuffetWebApplication(
        this IServiceCollection services,
        Action<BuffetApplication> configureAction)
    {
        services
            .AddRouting(options => options.LowercaseUrls = true)
            .AddControllers()
            .AddApplicationPart(typeof(PackageContentController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddRazorPages();

        services.AddHttpContextAccessor();
        services.AddTransient<IUrlGenerator, BuffetUrlGenerator>();

        services.AddSingleton(ApplicationVersionHelper.GetVersion());

        var app = services.AddBuffetApplication(configureAction);

        return services;
    }
}
