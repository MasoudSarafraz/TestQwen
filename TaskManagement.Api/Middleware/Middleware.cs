using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TaskManagement.Api.Middleware;

public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
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
            var controllerName = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ControllerName ?? "Unknown";
            var actionName = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ActionName ?? "Unknown";

            _logger.LogError(ex, 
                "Error occurred in Controller: {Controller}, Action: {Action}. Error: {ErrorMessage}",
                controllerName, actionName, ex.Message);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            
            var response = new
            {
                success = false,
                message = "An unexpected error occurred. Please try again later.",
                error = ex.Message
            };
            
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}

public class OperationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationLoggingMiddleware> _logger;

    public OperationLoggingMiddleware(RequestDelegate next, ILogger<OperationLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var controllerName = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ControllerName ?? "Unknown";
        var actionName = context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()?.ActionName ?? "Unknown";

        try
        {
            await _next(context);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Request completed - Controller: {Controller}, Action: {Action}, Status: {StatusCode}, Duration: {Duration}ms",
                controllerName, actionName, context.Response.StatusCode, duration.TotalMilliseconds);
        }
        catch
        {
            // Exception is handled by ExceptionLoggingMiddleware
            throw;
        }
    }
}

public static class JwtTokenHelper
{
    public static int? GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        return null;
    }

    public static string? GetCurrentUserRole(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value;
    }

    public static bool IsUserInRole(ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role);
    }
}
