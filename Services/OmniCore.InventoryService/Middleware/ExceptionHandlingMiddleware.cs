using OmniCore.InventoryService.Models.DTOs;
using OmniCore.InventoryService.Models.Exceptions;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmniCore.InventoryService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (InventoryConcurrencyException exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status409Conflict,
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status409Conflict,
                exception.Message);
        }
        catch (ArgumentException exception)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status400BadRequest,
                exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                context.TraceIdentifier);

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse
        {
            Status = statusCode,
            Message = message,
            TraceId =
            context.Items.TryGetValue(
                CorrelationIdMiddleware.HeaderName,
                out var correlationId)
                ? correlationId?.ToString() ?? context.TraceIdentifier
                : context.TraceIdentifier
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}