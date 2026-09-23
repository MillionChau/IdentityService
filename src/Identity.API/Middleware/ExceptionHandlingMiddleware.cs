using System.Net;
using System.Text.Json;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Models;

namespace Identity.API.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var code = HttpStatusCode.InternalServerError;
        var errors = new List<string>();
        string message = "Đã xảy ra lỗi máy chủ.";

        switch (exception)
        {
            case ValidationException validationException:
                code = HttpStatusCode.BadRequest;
                message = "Dữ liệu không hợp lệ.";
                errors = validationException.Errors.SelectMany(x => x.Value).ToList();
                break;
            case BadRequestException badRequestException:
                code = HttpStatusCode.BadRequest;
                message = badRequestException.Message;
                break;
            case NotFoundException notFoundException:
                code = HttpStatusCode.NotFound;
                message = notFoundException.Message;
                break;
            case UnauthorizedException unauthorizedException:
                code = HttpStatusCode.Unauthorized;
                message = unauthorizedException.Message;
                break;
            default:
                message = exception.Message;
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var response = ResponseModel<object>.Failure(message, errors);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
