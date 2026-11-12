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
            LogException(exception, statusCode, httpContext);

            // Create RFC 7807-compliant ProblemDetails response
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = GetClientDetail(exception),
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
                UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                // Thrown by HttpContextTenantService when the request has no tenantId claim
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                DomainException => (StatusCodes.Status400BadRequest, "Domain Error"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };
        }

        /// <summary>
        /// Only domain exceptions carry messages written for users. Any other message (EF/Npgsql errors,
        /// internal InvalidOperationException, ...) can contain SQL, table names, hosts or ids, so the client
        /// gets a fixed text; the full exception is in the log under the response's traceId.
        /// </summary>
        private static string GetClientDetail(Exception exception)
        {
            return exception switch
            {
                DomainException => exception.Message,
                UnauthorizedAccessException => "Authentication required.",
                _ => "An unexpected error occurred. Quote the traceId when reporting it."
            };
        }

        private void LogException(Exception exception, int statusCode, HttpContext httpContext)
        {
            // 500s keep the stack trace; 4xx are expected outcomes, so one line with who (IP) and where (path)
            if (statusCode >= 500)
            {
                _logger.LogError(exception, "Internal server error: {Message}", exception.Message);
            }
            else
            {
                _logger.LogWarning("Client error ({StatusCode}) on {Method} {Path} from {ClientIp}: {Message}",
                    statusCode, httpContext.Request.Method, httpContext.Request.Path,
                    httpContext.Connection.RemoteIpAddress, exception.Message);
            }
        }
    }
}
