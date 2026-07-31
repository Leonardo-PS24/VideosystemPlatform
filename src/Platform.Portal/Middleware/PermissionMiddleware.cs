using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Platform.Portal.Models;
using Platform.Portal.Services;
using Platform.Portal.Services.PBAC;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Platform.Portal.Middleware;

/// <summary>
/// Middleware per il controllo automatico dei permessi PBAC sulle route
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

    // Route amministrative (gestite da [Authorize])
    private static readonly string[] AdminRoutes = new[]
    {
        "/admin",
        "/permissions",
        "/api/pbacadmin"
    };

    // Mapping route -> PBAC Key prefix
    private static readonly (string Route, string KeyPrefix, string CompanyId)[] RouteMapping = new[]
    {
        ("/kiosk", "pharmaself:kiosk", "Pharmaself24"),
        ("/api/kiosk", "pharmaself:kiosk", "Pharmaself24"),
        ("/skriptkiosk", "skript:checklist", "Skriptkiosk"),
        ("/api/skriptkiosk", "skript:checklist", "Skriptkiosk")
    };

    public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService, IPbacService pbacService)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        // Skip per file statici
        if (path.StartsWith("/css") || path.StartsWith("/js") || 
            path.StartsWith("/lib") || path.StartsWith("/images") ||
            path.Contains("."))
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
        if (context.User.IsInRole("Admin") || await permissionService.IsAdminAsync(userId))
        {
            await _next(context);
            return;
        }

        // Trova la configurazione dalla route
        var routeConfig = RouteMapping.FirstOrDefault(m => path.StartsWith(m.Route));

        if (string.IsNullOrEmpty(routeConfig.KeyPrefix))
        {
            // Route non mappata, procedi normalmente
            await _next(context);
            return;
        }

        // Determina il tipo di azione dal metodo HTTP
        var action = context.Request.Method.ToUpper() switch
        {
            "GET" => "view",
            "POST" => "create",
            "PUT" => "edit",
            "PATCH" => "edit",
            "DELETE" => "delete",
            _ => "view"
        };

        string pbacKey = $"{routeConfig.KeyPrefix}:{action}";

        // Verifica via PBAC
        var hasPbacAccess = await pbacService.HasPermissionAsync(userId, pbacKey, routeConfig.CompanyId);

        if (!hasPbacAccess)
        {
            // Fallback al legacy PermissionService per compatibilità transitoria
            var legacyPermissionType = action switch
            {
                "view" => PermissionType.View,
                "create" => PermissionType.Create,
                "edit" => PermissionType.Edit,
                "delete" => PermissionType.Delete,
                _ => PermissionType.View
            };

            string legacyAppName = routeConfig.KeyPrefix.Contains("pharmaself") 
                ? ApplicationName.ConfigurationKiosk 
                : ApplicationName.SkriptkioskChecklist;

            bool hasLegacyAccess = await permissionService.HasPermissionAsync(userId, legacyAppName, legacyPermissionType);

            if (!hasLegacyAccess)
            {
                _logger.LogWarning(
                    "Access denied for user {UserId} ({Username}) to {Key} (Company: {Company})",
                    userId,
                    context.User.Identity?.Name,
                    pbacKey,
                    routeConfig.CompanyId);

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { message = "Accesso negato. Permesso non sufficiente." });
                }
                else
                {
                    context.Response.Redirect("/Account/AccessDenied");
                }
                return;
            }
        }

        // Permesso concesso, procedi
        await _next(context);
    }
}

public static class PermissionMiddlewareExtensions
{
    public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PermissionMiddleware>();
    }
}