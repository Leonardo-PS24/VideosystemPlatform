using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Platform.Portal.Services.PBAC;
using System.Threading.Tasks;

namespace Platform.Portal.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PermissionHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        using var scope = _scopeFactory.CreateScope();
        var pbacService = scope.ServiceProvider.GetRequiredService<IPbacService>();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

        var httpContext = httpContextAccessor.HttpContext;
        bool isDevMode = httpContext?.Request.Cookies["dev_mode"] == "true";

        // 1. Bypass per Dev Mode attiva per Developer o Admin
        if (isDevMode && (context.User.IsInRole("Developer") || context.User.IsInRole("Admin")))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Accesso root automatico per l'amministratore (Admin)
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Mappa la stringa di requisito (es. "ConfigurationKiosk.Create" -> "pharmaself:kiosk:create")
        string pbacKey = MapToPbacKey(requirement.Permission);

        // Estraggo eventuale header o query param per Company o Department se presente nella request
        string? companyId = httpContext?.Request.Headers["X-Company-Scope"].ToString();
        string? departmentId = httpContext?.Request.Headers["X-Department-Scope"].ToString();

        bool hasAccess = await pbacService.HasPermissionAsync(userId, pbacKey, companyId, departmentId);
        if (hasAccess)
        {
            context.Succeed(requirement);
        }
    }

    private string MapToPbacKey(string permission)
    {
        if (permission.Contains(":")) return permission; // già formato PBAC

        var parts = permission.Split('.');
        if (parts.Length != 2) return permission;

        string app = parts[0];
        string act = parts[1].ToLower();

        return app switch
        {
            "ConfigurationKiosk" => $"pharmaself:kiosk:{act}",
            "SkriptkioskChecklist" => $"skript:checklist:{act}",
            _ => $"{app.ToLower()}:module:{act}"
        };
    }
}
