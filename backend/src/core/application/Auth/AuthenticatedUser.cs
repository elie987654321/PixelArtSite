namespace PixelArt.Core.Application.Auth;

// What a successful register/login hands back. Success payload only —
// failures travel as exceptions.
public sealed record AuthenticatedUser(string Username, string Token);
