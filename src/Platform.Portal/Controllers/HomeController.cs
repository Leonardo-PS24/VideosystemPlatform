using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Portal.Models; // Aggiunto per i nuovi modelli

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
    /// Dashboard principale con lista delle aziende e le loro applicazioni disponibili
    /// </summary>
    public IActionResult Index()
    {
        var companies = _configuration.GetSection("Companies").Get<List<CompanyInfo>>() 
            ?? new List<CompanyInfo>();
        
        // Qui potresti voler filtrare le aziende in base ai ruoli dell'utente,
        // ma per ora mostriamo tutte le aziende e le loro app.
        // La logica di filtro per le app sarà gestita a livello di singola app o in una vista dedicata.

        return View(companies);
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
