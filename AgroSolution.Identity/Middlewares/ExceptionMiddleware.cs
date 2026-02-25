using System.Net;
using System.Text.Json;
using AgroSolution.Identity.Domain;

namespace AgroSolution.Identity.Middlewares;

public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        context.Response.StatusCode = exception switch
        {
            DomainException => (int)HttpStatusCode.BadRequest,
            _               => (int)HttpStatusCode.InternalServerError
        };

        var response = new
        {
            Success = false,
            Message = exception.Message,
            Detail  = exception is DomainException ? "Business Rule Violation" : "Internal Server Error"
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
