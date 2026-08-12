using Microsoft.Extensions.DependencyInjection;
using PixelArt.Core.Application.Auth;

namespace PixelArt.Core.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthenticationService>();

        return services;
    }
}
