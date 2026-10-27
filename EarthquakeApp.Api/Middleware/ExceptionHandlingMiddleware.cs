using System.Text.Json;
using EarthquakeApp.Api.Exceptions;
using EarthquakeApp.Dtos;

namespace EarthquakeApp.Api.Middleware;

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

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, message) = ex switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, ex.Message),
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
            ExternalServiceException => (StatusCodes.Status503ServiceUnavailable, ex.Message),
            HttpRequestException => (StatusCodes.Status503ServiceUnavailable,
                "No se pudo comunicar con un servicio externo. Intente nuevamente más tarde."),
            _ => (StatusCodes.Status500InternalServerError,
                "Ocurrió un error inesperado. Intente nuevamente más tarde.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Error no controlado");
        else
            _logger.LogWarning(ex, "Error controlado: {Message}", message);

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var body = new ErrorResponseDto { Status = status, Message = message };
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}