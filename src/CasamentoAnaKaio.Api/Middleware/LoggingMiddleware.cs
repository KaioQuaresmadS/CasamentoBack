using System.Diagnostics;

namespace CasamentoAnaKaio.Api.Middleware;

public sealed class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            try
            {
                await next(context);

                stopwatch.Stop();

                if (context.Response.StatusCode >= 400)
                {
                    logger.LogWarning(
                        "HTTP {Method} {Path} respondido com {StatusCode} em {ElapsedMilliseconds}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    logger.LogInformation(
                        "HTTP {Method} {Path} respondido com {StatusCode} em {ElapsedMilliseconds}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }
}
