using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller per la homepage e dashboard
/// </summary>
[Authorize]
public class HomeController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Dashboard principale con lista delle applicazioni disponibili
    /// </summary>
    public IActionResult Index()
    {
        var applications = _configuration.GetSection("Applications").Get<List<ApplicationInfo>>() 
            ?? new List<ApplicationInfo>();
        
        // Filtra le applicazioni in base al ruolo dell'utente
        var userRoles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var filteredApps = applications.Where(app => 
            string.IsNullOrEmpty(app.RequiredRole) || 
            userRoles.Contains(app.RequiredRole) ||
            userRoles.Contains("Admin"))
            .ToList();

        return View(filteredApps);
    }

    /// <summary>
    /// Pagina di errore
    /// </summary>
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }
}

/// <summary>
/// Informazioni su un'applicazione della piattaforma
/// </summary>
public class ApplicationInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = "apps";
    public string RequiredRole { get; set; } = string.Empty;
}
