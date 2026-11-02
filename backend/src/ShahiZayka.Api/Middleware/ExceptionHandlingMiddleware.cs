using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ShahiZayka.Domain.Exceptions;

namespace ShahiZayka.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception occurred during request execution: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var title = "An unexpected server error occurred.";
        var detail = exception.Message;

        switch (exception)
        {
            case ProductNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                title = "Resource Not Found";
                break;
            case InsufficientStockException:
                statusCode = HttpStatusCode.Conflict;
                title = "Inventory Conflict";
                break;
            case DuplicateOrderException:
                statusCode = HttpStatusCode.Conflict;
                title = "Duplicate Request";
                break;
            case DomainException:
                statusCode = HttpStatusCode.BadRequest;
                title = "Business Rule Violation";
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                title = "Unauthorized Access";
                break;
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = context.TraceIdentifier,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
