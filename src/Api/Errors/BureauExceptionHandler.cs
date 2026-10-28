using Api.Integrations.Bureaus;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

/// <summary>
/// Maps bureau failures to 502 with a generic ProblemDetails body. The bureau's response
/// is never echoed back to the client.
/// </summary>
public sealed class BureauExceptionHandler(ILogger<BureauExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not BureauException bureauException)
        {
            return false;
        }

        logger.LogWarning("Bureau {Bureau} call failed with status {StatusCode}", bureauException.Bureau, bureauException.StatusCode);

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status502BadGateway,
            Title = "Credit bureau unavailable",
            Detail = "The bureau request failed. Try again later."
        }, ct);

        return true;
    }
}
