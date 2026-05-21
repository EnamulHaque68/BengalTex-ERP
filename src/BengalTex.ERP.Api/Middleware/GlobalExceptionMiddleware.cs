using System.Net;
using System.Text.Json;
using BengalTex.ERP.Domain.Exceptions;
using BengalTex.ERP.Shared.Common;
using FluentValidation;

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

        HttpStatusCode statusCode;
        string message;
        string code;
        List<ApiError> errors;

        switch (exception)
        {
            // FluentValidation failures → 400 with one ApiError per failed field.
            case ValidationException ve:
                statusCode = HttpStatusCode.BadRequest;
                code = "VALIDATION_ERROR";
                errors = ve.Errors
                    .Select(e => new ApiError
                    {
                        Code = string.IsNullOrEmpty(e.ErrorCode) ? "VALIDATION_ERROR" : e.ErrorCode,
                        Message = e.ErrorMessage,
                        Field = e.PropertyName
                    })
                    .ToList();
                // Surface the first message as the headline (the frontend shows .message).
                message = errors.FirstOrDefault()?.Message ?? "One or more validation errors occurred.";
                break;

            case NotFoundException nfe:
                statusCode = HttpStatusCode.NotFound;
                message = nfe.Message;
                code = nfe.Code ?? "NOT_FOUND";
                errors = new() { new() { Code = code, Message = message } };
                break;

            case ConflictException cfe:
                statusCode = HttpStatusCode.Conflict;
                message = cfe.Message;
                code = cfe.Code ?? "CONFLICT";
                errors = new() { new() { Code = code, Message = message } };
                break;

            case BusinessRuleException bre:
                statusCode = HttpStatusCode.UnprocessableEntity;
                message = bre.Message;
                code = bre.Code ?? "BUSINESS_RULE";
                errors = new() { new() { Code = code, Message = message } };
                break;

            case DomainException de:
                statusCode = HttpStatusCode.BadRequest;
                message = de.Message;
                code = de.Code ?? "DOMAIN_ERROR";
                errors = new() { new() { Code = code, Message = message } };
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Forbidden;
                message = "Access denied.";
                code = "FORBIDDEN";
                errors = new() { new() { Code = code, Message = message } };
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.";
                code = "INTERNAL_ERROR";
                errors = new() { new() { Code = code, Message = message } };
                break;
        }

        // Server faults (5xx) are real bugs → log as Error with the stack trace.
        // Client faults (4xx) are expected → log at Warning without the noise of a stack trace.
        if ((int)statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);
        else
            _logger.LogWarning("Request failed: {Code} ({Status}) on {Method} {Path} — {Message}. TraceId: {TraceId}",
                code, (int)statusCode, context.Request.Method, context.Request.Path, message, traceId);

        var response = ApiResponse.Fail(message, errors) with { TraceId = traceId };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}