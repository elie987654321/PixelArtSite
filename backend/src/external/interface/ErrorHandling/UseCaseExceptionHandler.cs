using Microsoft.AspNetCore.Diagnostics;
using PixelArt.Core.Application.Auth.Exceptions;
using PixelArt.Core.Application.Exceptions;

namespace PixelArt.External.Interface.ErrorHandling;

// Turns deliberate business failures into ProblemDetails responses. Anything
// that is not a UseCaseException falls through to the default handler, so its
// message — which may carry connection strings or schema details — never
// reaches the client.
public sealed class UseCaseExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public UseCaseExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UseCaseException useCaseException)
            return false;

        // An unmapped use-case failure is still a client error, never a 500.
        var status = useCaseException switch
        {
            UsernameTakenException => StatusCodes.Status409Conflict,
            InvalidCredentialsException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };

        httpContext.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = useCaseException,
            ProblemDetails =
            {
                Status = status,
                Title = useCaseException.Message
            }
        });
    }
}
