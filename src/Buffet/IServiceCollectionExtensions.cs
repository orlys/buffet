using Buffet.Authentication;
using Buffet.Core;
using Buffet.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Buffet;

internal static class IServiceCollectionExtensions
{
    internal static BuffetApplication AddNugetBasicHttpAuthentication(this BuffetApplication app)
    {
        app.Services.AddAuthentication(options =>
        {
            // Breaks existing tests if the contains check is not here.
            if (!options.SchemeMap.ContainsKey(AuthenticationConstants.NugetBasicAuthenticationScheme))
            {
                options.AddScheme<NugetBasicAuthenticationHandler>(AuthenticationConstants.NugetBasicAuthenticationScheme, AuthenticationConstants.NugetBasicAuthenticationScheme);
                options.DefaultAuthenticateScheme = AuthenticationConstants.NugetBasicAuthenticationScheme;
                options.DefaultChallengeScheme = AuthenticationConstants.NugetBasicAuthenticationScheme;
            }
        });

        return app;
    }

    internal static BuffetApplication AddNugetBasicHttpAuthorization(this BuffetApplication app, Action<AuthorizationPolicyBuilder>? configurePolicy = null)
    {
        app.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthenticationConstants.NugetUserPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                configurePolicy?.Invoke(policy);
            });
        });

        return app;
    }
}
