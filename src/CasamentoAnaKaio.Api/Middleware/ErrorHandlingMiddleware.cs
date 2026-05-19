using Microsoft.AspNetCore.Mvc;

namespace CasamentoAnaKaio.Api.Middleware;

public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, logger);
        }
    }

    private static Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        logger.LogError(
            exception,
            "Erro nao tratado na requisicao {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        context.Response.ContentType = "application/json";

        var response = new ProblemDetails
        {
            Instance = context.Request.Path,
            Title = "Erro no servidor",
        };

        switch (exception)
        {
            case ArgumentException or ArgumentOutOfRangeException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Requisicao invalida";
                response.Detail = exception.Message;
                break;

            case InvalidOperationException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Status = StatusCodes.Status400BadRequest;
                response.Title = "Operacao invalida";
                response.Detail = exception.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Status = StatusCodes.Status401Unauthorized;
                response.Title = "Acesso nao autorizado";
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Status = StatusCodes.Status500InternalServerError;
                response.Title = "Erro interno do servidor";
                response.Detail = "Ocorreu um erro inesperado. Por favor, tente novamente.";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}
