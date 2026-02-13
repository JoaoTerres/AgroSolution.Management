using System.Net;
using System.Text.Json;
using AgroSolution.Core.Domain;

namespace AgroSolution.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        context.Response.StatusCode = exception switch
        {
            DomainException => (int)HttpStatusCode.BadRequest, 
            _ => (int)HttpStatusCode.InternalServerError     
        };

        var response = new 
        {
            Success = false,
            Message = exception.Message,
            Detail = exception is DomainException ? "Business Rule Violation" : "Internal Server Error"
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}