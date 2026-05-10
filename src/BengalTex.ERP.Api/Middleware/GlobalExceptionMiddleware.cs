using System.Net;
using System.Text.Json;
using BengalTex.ERP.Domain.Exceptions;
using BengalTex.ERP.Shared.Common;

namespace BengalTex.ERP.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
        var traceId = context.TraceIdentifier;
        _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

        var (statusCode, message, code) = exception switch
        {
            NotFoundException nfe => (HttpStatusCode.NotFound, nfe.Message, nfe.Code),
            ConflictException cfe => (HttpStatusCode.Conflict, cfe.Message, cfe.Code),
            BusinessRuleException bre => (HttpStatusCode.UnprocessableEntity, bre.Message, bre.Code),
            DomainException de => (HttpStatusCode.BadRequest, de.Message, de.Code),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied.", "FORBIDDEN"),
            _ => (HttpStatusCode.InternalServerError,
                  _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                  "INTERNAL_ERROR")
        };

        var response = ApiResponse.Fail(
            message,
            new List<ApiError>
            {
                new() { Code = code ?? "ERROR", Message = message }
            }) with
        {
            TraceId = traceId
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}