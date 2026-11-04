using System;
using Microsoft.Extensions.DependencyInjection;

namespace Buffet.Core;

public class BuffetApplication
{
    public BuffetApplication(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }
}
