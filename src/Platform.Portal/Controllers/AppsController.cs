using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller per la visualizzazione delle applicazioni integrate
/// </summary>
[Authorize]
public class AppsController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppsController> _logger;

    public AppsController(IConfiguration configuration, ILogger<AppsController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Visualizza un'applicazione in un iframe
    /// </summary>
    /// <param name="appId">ID dell'applicazione da visualizzare</param>
    [HttpGet]
    [ActionName("View")]
    public new IActionResult View(string appId)
    {
        if (string.IsNullOrEmpty(appId))
        {
            return RedirectToAction("Index", "Home");
        }

        var applications = _configuration.GetSection("Applications").Get<List<ApplicationInfo>>()
            ?? new List<ApplicationInfo>();

        var app = applications.FirstOrDefault(a => a.AppId == appId);

        if (app == null)
        {
            _logger.LogWarning("Applicazione non trovata: {AppId}", appId);
            return NotFound();
        }

        // Verifica permessi
        var userRoles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!string.IsNullOrEmpty(app.RequiredRole) &&
            !userRoles.Contains(app.RequiredRole) &&
            !userRoles.Contains("Admin"))
        {
            _logger.LogWarning("Accesso negato all'applicazione {AppId} per l'utente {User}",
                appId, User.Identity?.Name);
            return Forbid();
        }

        return View(app);
    }
}
