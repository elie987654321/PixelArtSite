using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PixelArt.Core.Abstraction.Auth;
using PixelArt.Core.Abstraction.Persistence;
using PixelArt.External.Infrastructure.Auth;
using PixelArt.External.Infrastructure.Persistence;

namespace PixelArt.External.Infrastructure;

// Binds every port to its concrete implementation, so the host never names one.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}
