namespace PixelArt.External.Infrastructure.Auth;

// Strongly-typed view of the "Jwt" section in appsettings.
public class JwtSettings
{
    public const string SectionName = "Jwt";

    // Secret signing key — kept out of appsettings, supplied via user-secrets/env.
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; }
}
