using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace E_Commerce.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case ArgumentNullException:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response = new ErrorResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Required argument is null",
                        Details = exception.Message
                    };
                    break;

                case InvalidOperationException:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response = new ErrorResponse
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Invalid operation",
                        Details = exception.Message
                    };
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response = new ErrorResponse
                    {
                        StatusCode = StatusCodes.Status401Unauthorized,
                        Message = "Unauthorized access",
                        Details = exception.Message
                    };
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response = new ErrorResponse
                    {
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Resource not found",
                        Details = exception.Message
                    };
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response = new ErrorResponse
                    {
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Message = "An unexpected error occurred",
                        Details = "Please contact support if the problem persists"
                    };
                    break;
            }

            return context.Response.WriteAsJsonAsync(response);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
