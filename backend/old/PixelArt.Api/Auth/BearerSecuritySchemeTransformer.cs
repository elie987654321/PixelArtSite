using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace PixelArt.Api.Auth;

// Declares a "Bearer" JWT security scheme on the OpenAPI document so Scalar
// shows an Authorize box where you can paste a token. Without this, the
// configured JWT authentication is invisible to the generated docs.
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Paste the JWT returned by /api/auth/login (no 'Bearer ' prefix).",
            Reference = new OpenApiReference
            {
                Id = "Bearer",
                Type = ReferenceType.SecurityScheme
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        // Apply the scheme to every operation so each request sends the token.
        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [scheme] = Array.Empty<string>()
        });

        return Task.CompletedTask;
    }
}
