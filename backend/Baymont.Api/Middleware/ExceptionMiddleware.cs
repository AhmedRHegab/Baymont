using System.Net;
using System.Text.Json;
using Baymont.Application.Common;

namespace Baymont.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, message) = ex switch
        {
            ConcurrencyException => (HttpStatusCode.Conflict, ex.Message),
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "حدث خطأ غير متوقع.")
        };

        if (status == HttpStatusCode.InternalServerError)
            logger.LogError(ex, "Unhandled exception");

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
    }
}
