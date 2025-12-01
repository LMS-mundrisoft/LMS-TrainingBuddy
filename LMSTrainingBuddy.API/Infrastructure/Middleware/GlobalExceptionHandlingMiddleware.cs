using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMSTrainingBuddy.API.Infrastructure.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);

        await PersistErrorAsync(exception, context, traceId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var problemDetails = new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Please refer to the trace identifier for more information.",
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static async Task PersistErrorAsync(Exception exception, HttpContext context, string traceId)
    {
        var errorDirectory = Path.Combine(AppContext.BaseDirectory, "Error");
        Directory.CreateDirectory(errorDirectory);

        var fileName = $"error-{DateTime.UtcNow:yyyy-MM-dd}.txt";
        var filePath = Path.Combine(errorDirectory, fileName);

        var errorBuilder = new StringBuilder();
        errorBuilder.AppendLine($"Timestamp (UTC): {DateTime.UtcNow:O}");
        errorBuilder.AppendLine($"TraceId: {traceId}");
        errorBuilder.AppendLine($"Request: {context.Request.Method} {context.Request.Path}");
        errorBuilder.AppendLine($"Message: {exception.Message}");
        errorBuilder.AppendLine("StackTrace:");
        errorBuilder.AppendLine(exception.StackTrace ?? "No stack trace available.");
        errorBuilder.AppendLine(new string('-', 80));

        await File.AppendAllTextAsync(filePath, errorBuilder.ToString());
    }
}
