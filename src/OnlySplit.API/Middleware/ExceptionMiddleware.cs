using System.Net;
using FluentValidation;
using OnlySplit.Shared.Responses;
using OnlySplit.Domain.Exceptions;

namespace OnlySplit.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors, logAsError) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                validationException.Errors.Select(error => error.ErrorMessage).ToArray(),
                false),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                exception.Message,
                Array.Empty<string>(),
                false),
            ForbiddenException => (
                HttpStatusCode.Forbidden,
                exception.Message,
                Array.Empty<string>(),
                false),
            NotFoundException => (
                HttpStatusCode.NotFound,
                exception.Message,
                Array.Empty<string>(),
                false),
            ConflictException => (
                HttpStatusCode.Conflict,
                exception.Message,
                Array.Empty<string>(),
                false),
            PaymentException => (
                HttpStatusCode.BadRequest,
                exception.Message,
                Array.Empty<string>(),
                false),
            AppException => (
                HttpStatusCode.BadRequest,
                exception.Message,
                Array.Empty<string>(),
                false),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected server error occurred.",
                Array.Empty<string>(),
                true)
        };

        if (logAsError)
        {
            logger.LogError(exception, "Request failed with an unhandled exception.");
        }
        else
        {
            logger.LogWarning(exception, "Request failed with a handled exception.");
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object>.Fail(message, errors);
        await context.Response.WriteAsJsonAsync(response);
    }
}
