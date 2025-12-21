using RogueLearn.User.Application.Exceptions;
using Supabase.Postgrest.Exceptions;
using System.Net;
using System.Text.Json;

namespace RogueLearn.User.Api.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    error = new
                    {
                        message = "Validation failed",
                        details = validationEx.Errors
                    }
                };
                break;

            case PostgrestException postgrestEx when postgrestEx.Message.Contains("Network connection lost"):
            case NetworkException:
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                response = new
                {
                    error = new
                    {
                        message = "We're experiencing network connectivity issues. Please try again in a moment.",
                        code = "NETWORK_ERROR"
                    }
                };
                break;

            case PostgrestException postgrestEx when postgrestEx.Message.Contains("gateway error"):
                context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                response = new
                {
                    error = new
                    {
                        message = "The service is temporarily unavailable. Please try again shortly.",
                        code = "SERVICE_UNAVAILABLE"
                    }
                };
                break;

            case BadRequestException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            case ConflictException:
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            case UnauthorizedException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            case ForbiddenException:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            case NotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            case MethodNotAllowedException:
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            case ArgumentException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            case UnprocessableEntityException:
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableContent;
                response = new
                {
                    error = new
                    {
                        message = exception.Message,
                        details = exception.InnerException?.Message
                    }
                };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new
                {
                    error = new
                    {
                        message = "An unexpected error occurred. Please try again later.",
                        code = "INTERNAL_SERVER_ERROR"
                    }
                };
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
