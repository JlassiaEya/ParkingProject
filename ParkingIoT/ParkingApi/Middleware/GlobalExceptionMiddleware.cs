using ParkingApi.Exceptions;
using ParkingApi.Models;
using System.Net;
using System.Text.Json;

namespace ParkingApi.Middleware;

/// <summary>
/// Middleware global pour capturer toutes les exceptions non gérées
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Continuer le pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Une exception non gérée s'est produite : {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Déterminer le code de statut selon le type d'exception
        int statusCode = exception switch
        {
            PlaceNotFoundException => (int)HttpStatusCode.NotFound,
            PlaceAlreadyInStateException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            success = false,
            message = exception.Message,
            errors = new List<string> { exception.GetType().Name },
            timestamp = DateTime.UtcNow,
            details = _env.IsDevelopment() ? exception.Message : null,
            stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        return context.Response.WriteAsync(jsonResponse);
    }
}
