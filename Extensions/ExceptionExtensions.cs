using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FlameTrack.API.Extensions;

public static class ExceptionExtensions
{
    public static IActionResult ToActionResult(this Exception ex, ILogger logger, string? customMessage = null)
    {
        // Internal logging with full detail
        logger.LogError(ex, "An unhandled exception occurred in the terminal logic.");

        // Production-friendly message
        var message = customMessage ?? "A synchronization error occurred. System integrity remains intact.";
        
        // In development, we could allow ex.Message, but let's be strict for production-ready status
        return new BadRequestObjectResult(new { message });
    }
}
