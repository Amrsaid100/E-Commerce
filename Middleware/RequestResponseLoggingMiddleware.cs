using System.Text;

namespace E_Commerce.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for health checks and swagger
            if (context.Request.Path.StartsWithSegments("/health") || 
                context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            var requestBody = await ReadRequestBody(context.Request);
            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await _next(context);
                stopwatch.Stop();

                var response = await ReadResponseBody(context.Response);

                LogRequest(context.Request, requestBody, context.Response.StatusCode, response, stopwatch.ElapsedMilliseconds);

                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private static async Task<string> ReadRequestBody(HttpRequest request)
        {
            request.EnableBuffering();

            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                return body;
            }
        }

        private static async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);

            using (var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
                return body;
            }
        }

        private void LogRequest(HttpRequest request, string requestBody, int statusCode, string responseBody, long elapsedMilliseconds)
        {
            var logMessage = new StringBuilder();
            logMessage.AppendLine($"=== HTTP Request/Response ===");
            logMessage.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            logMessage.AppendLine($"Method: {request.Method}");
            logMessage.AppendLine($"Path: {request.Path}");
            logMessage.AppendLine($"Query: {request.QueryString}");
            logMessage.AppendLine($"Status Code: {statusCode}");
            logMessage.AppendLine($"Duration: {elapsedMilliseconds}ms");

            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                logMessage.AppendLine($"Request Body: {requestBody}");
            }

            if (!string.IsNullOrWhiteSpace(responseBody) && responseBody.Length < 1000)
            {
                logMessage.AppendLine($"Response Body: {responseBody}");
            }

            if (statusCode >= 400)
            {
                _logger.LogWarning(logMessage.ToString());
            }
            else
            {
                _logger.LogInformation(logMessage.ToString());
            }
        }
    }
}
