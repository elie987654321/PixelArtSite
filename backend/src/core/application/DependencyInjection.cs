using Microsoft.Extensions.DependencyInjection;
using PixelArt.Core.Application.Auth;
using PixelArt.Core.Application.Drawings;

namespace PixelArt.Core.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthenticationService>();
        services.AddScoped<DrawingService>();

        return services;
    }
}
