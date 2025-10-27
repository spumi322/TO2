using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace webAPI.Middleware
{
    /// <summary>
    /// Global exception handler that maps domain exceptions to HTTP status codes
    /// and returns RFC 7807-compliant ProblemDetails JSON responses.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Map exception to status code and title
            var (statusCode, title) = MapExceptionToStatusCode(exception);

            // Log the exception with appropriate level
            LogException(exception, statusCode);

            // Create RFC 7807-compliant ProblemDetails response
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message,
                Instance = httpContext.Request.Path,
                Type = $"https://httpstatuses.io/{statusCode}"
            };

            // Add trace ID for debugging
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

            // Set response status code and content type
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            // Write JSON response
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; // Exception handled
        }

        private (int StatusCode, string Title) MapExceptionToStatusCode(Exception exception)
        {
            return exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict Occurred"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Operation Forbidden"),
                DomainException => (StatusCodes.Status400BadRequest, "Domain Error"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };
        }

        private void LogException(Exception exception, int statusCode)
        {
            // Log with appropriate level based on status code
            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Internal server error: {Message}", exception.Message);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(exception, "Client error ({StatusCode}): {Message}", statusCode, exception.Message);
            }
            else
            {
                _logger.LogInformation(exception, "Request error ({StatusCode}): {Message}", statusCode, exception.Message);
            }
        }
    }
}
