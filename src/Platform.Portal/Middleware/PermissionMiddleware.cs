using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Platform.Portal.Models;
using Platform.Portal.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Platform.Portal.Middleware;

/// <summary>
/// Middleware per il controllo automatico dei permessi
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionMiddleware> _logger;

    // Route pubbliche che non richiedono permessi
    private static readonly string[] PublicRoutes = new[]
    {
        "/",
        "/home",
        "/home/index",
        "/account/login",
        "/account/logout",
        "/account/accessdenied",
        "/error"
    };

    // Route amministrative (già protette da [Authorize])
    private static readonly string[] AdminRoutes = new[]
    {
        "/admin",
        "/permissions"
    };

    // Mapping route -> applicazione
    private static readonly (string Route, string Application)[] RouteMapping = new[]
    {
        ("/kiosk", ApplicationName.KioskRegistration),
        ("/bugs", ApplicationName.BugTracking),
        ("/features", ApplicationName.FeatureRequest),
        ("/developer", ApplicationName.DeveloperDashboard)
    };

    public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip per file statici
        if (path.StartsWith("/css") || path.StartsWith("/js") || 
            path.StartsWith("/lib") || path.StartsWith("/images"))
        {
            await _next(context);
            return;
        }

        // Skip per route pubbliche
        if (PublicRoutes.Any(r => path == r || path.StartsWith(r + "/")))
        {
            await _next(context);
            return;
        }

        // Skip per route admin (gestite da [Authorize])
        if (AdminRoutes.Any(r => path.StartsWith(r)))
        {
            await _next(context);
            return;
        }

        // Verifica autenticazione
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        // Ottieni userId
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        // Admin bypass: Admin ha sempre tutti i permessi
        if (await permissionService.IsAdminAsync(userId))
        {
            await _next(context);
            return;
        }

        // Trova l'applicazione dalla route
        var applicationName = RouteMapping
            .FirstOrDefault(m => path.StartsWith(m.Route))
            .Application;

        if (string.IsNullOrEmpty(applicationName))
        {
            // Route non mappata, procedi normalmente
            await _next(context);
            return;
        }

        // Determina il permesso richiesto dal metodo HTTP
        var requiredPermission = context.Request.Method.ToUpper() switch
        {
            "GET" => PermissionType.View,
            "POST" => PermissionType.Create,
            "PUT" => PermissionType.Edit,
            "PATCH" => PermissionType.Edit,
            "DELETE" => PermissionType.Delete,
            _ => PermissionType.View
        };

        // Verifica permesso
        var hasPermission = await permissionService.HasPermissionAsync(
            userId, 
            applicationName, 
            requiredPermission);

        if (!hasPermission)
        {
            _logger.LogWarning(
                "Access denied for user {UserId} ({Username}) to {Application} with {Permission}",
                userId,
                context.User.Identity?.Name,
                applicationName,
                requiredPermission);

            context.Response.Redirect("/Account/AccessDenied");
            return;
        }

        // Permesso concesso, procedi
        await _next(context);
    }
}

/// <summary>
/// Extension method per registrare il middleware
/// </summary>
public static class PermissionMiddlewareExtensions
{
    public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PermissionMiddleware>();
    }
}