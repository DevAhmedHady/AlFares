using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };

        ctx.Response.StatusCode = problem.Status.Value;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
