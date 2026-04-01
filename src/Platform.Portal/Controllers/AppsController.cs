using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Portal.Models; // Aggiunto per i nuovi modelli

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

        var companies = _configuration.GetSection("Companies").Get<List<CompanyInfo>>()
            ?? new List<CompanyInfo>();

        ApplicationInfo? app = null;
        foreach (var company in companies)
        {
            app = company.Applications.FirstOrDefault(a => a.AppId == appId);
            if (app != null)
            {
                break;
            }
        }

        if (app == null)
        {
            _logger.LogWarning("Applicazione non trovata: {AppId}", appId);
            return NotFound();
        }

        // La logica dei permessi è ora gestita dal PermissionMiddleware
        // e non più dal RequiredRole in ApplicationInfo.
        // Se l'utente non ha accesso, il middleware dovrebbe già aver bloccato la richiesta.

        return View(app);
    }
}
