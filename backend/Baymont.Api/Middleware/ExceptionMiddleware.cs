using System.Net;
using System.Text.Json;
using Baymont.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Baymont.Api.Middleware;

/// <summary>Every failure leaves the API as {"message": "..."} in Arabic. Technical details go to the log only.</summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The browser closed the page; nobody is waiting for an answer.
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
            ValidationException => (HttpStatusCode.BadRequest, ex.Message),
            ConcurrencyException => (HttpStatusCode.Conflict, ex.Message),
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict,
                "تم تعديل البيانات بواسطة مستخدم آخر. يرجى إعادة تحميل السجل."),
            DbUpdateException => (HttpStatusCode.Conflict, DescribeDatabaseConflict(ex)),
            BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge =>
                (HttpStatusCode.RequestEntityTooLarge, "حجم البيانات أو الملف أكبر من المسموح."),
            BadHttpRequestException or JsonException or FormatException =>
                (HttpStatusCode.BadRequest, "البيانات المرسلة غير صالحة. تأكد من ملء الحقول بشكل صحيح."),
            _ => (HttpStatusCode.InternalServerError, "حدث خطأ غير متوقع. حاول مرة أخرى، وإن تكرر أبلغ المسؤول.")
        };

        if (status == HttpStatusCode.InternalServerError)
            logger.LogError(ex, "Unhandled exception");
        else if (ex is DbUpdateException)
            logger.LogWarning(ex, "Database conflict");

        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
    }

    private static string DescribeDatabaseConflict(Exception ex)
    {
        var text = ex.GetBaseException().Message;
        if (text.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            return "هذه القيمة مسجلة مسبقاً (رقم أو كود مكرر).";
        if (text.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            return "تعذر الحفظ: السجل مرتبط ببيانات أخرى أو المرجع المختار لم يعد موجوداً.";
        return "تعذر الحفظ بسبب تعارض في البيانات. أعد تحميل الصفحة وحاول مرة أخرى.";
    }
}
