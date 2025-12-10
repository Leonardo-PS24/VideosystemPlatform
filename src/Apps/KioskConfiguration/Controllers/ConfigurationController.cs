using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Apps.KioskConfiguration.Data;
using Platform.Apps.KioskConfiguration.Models;
using Platform.Apps.KioskConfiguration.Services;

namespace Platform.Apps.KioskConfiguration.Controllers;

/// <summary>
/// Controller per la gestione delle configurazioni dei kiosk
/// </summary>
public class ConfigurationController : Controller
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly ITemplateService _templateService;
    private readonly ApplicationDbContext _context;

    public ConfigurationController(
        ILogger<ConfigurationController> logger,
        ITemplateService templateService,
        ApplicationDbContext context)
    {
        _logger = logger;
        _templateService = templateService;
        _context = context;
    }

    /// <summary>
    /// Pagina principale - Lista delle configurazioni
    /// </summary>
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Accesso alla pagina delle configurazioni");

        var configurations = await _context.KioskConfigurations
            .Include(c => c.Template)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(configurations);
    }

    /// <summary>
    /// Mostra il form per creare una nuova configurazione
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        // Carica i template disponibili
        var templates = await _context.ConfigurationTemplates
            .Where(t => t.IsActive)
            .ToListAsync();

        ViewBag.Templates = templates;
        return View();
    }

    /// <summary>
    /// Salva una nuova configurazione
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string serialNumber, Guid templateId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                TempData["ErrorMessage"] = "Il numero seriale è obbligatorio";
                return RedirectToAction(nameof(Create));
            }

            // Verifica che il template esista
            var template = await _context.ConfigurationTemplates.FindAsync(templateId);
            if (template == null)
            {
                TempData["ErrorMessage"] = "Template non trovato";
                return RedirectToAction(nameof(Create));
            }

            // Crea la nuova configurazione
            var configuration = new KioskConfigurationEntity
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                SerialNumber = serialNumber,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.KioskConfigurations.Add(configuration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Configurazione creata: {Id} - {SerialNumber}", configuration.Id, serialNumber);

            TempData["SuccessMessage"] = "Configurazione creata con successo";

            // Reindirizza alla pagina di modifica per compilare i campi
            return RedirectToAction(nameof(Edit), new { id = configuration.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione della configurazione");
            TempData["ErrorMessage"] = "Errore durante la creazione della configurazione";
            return RedirectToAction(nameof(Create));
        }
    }

    /// <summary>
    /// Mostra il form per modificare una configurazione
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var configuration = await _context.KioskConfigurations
            .Include(c => c.Template)
            .Include(c => c.FieldValues)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (configuration == null)
        {
            TempData["ErrorMessage"] = "Configurazione non trovata";
            return RedirectToAction(nameof(Index));
        }

        return View(configuration);
    }

    /// <summary>
    /// Salva le modifiche a una configurazione
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string status)
    {
        try
        {
            var configuration = await _context.KioskConfigurations.FindAsync(id);
            if (configuration == null)
            {
                TempData["ErrorMessage"] = "Configurazione non trovata";
                return RedirectToAction(nameof(Index));
            }

            // Aggiorna lo stato se fornito
            if (!string.IsNullOrEmpty(status))
            {
                configuration.Status = status;
            }

            configuration.UpdatedAt = DateTime.UtcNow;

            if (status == "Completed")
            {
                configuration.CompletedAt = DateTime.UtcNow;
                configuration.CompletedBy = User.Identity?.Name ?? "System";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Configurazione aggiornata: {Id}", id);
            TempData["SuccessMessage"] = "Configurazione salvata con successo";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento della configurazione");
            TempData["ErrorMessage"] = "Errore durante il salvataggio";
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    /// <summary>
    /// Elimina una configurazione
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var configuration = await _context.KioskConfigurations.FindAsync(id);
            if (configuration == null)
            {
                TempData["ErrorMessage"] = "Configurazione non trovata";
                return RedirectToAction(nameof(Index));
            }

            _context.KioskConfigurations.Remove(configuration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Configurazione eliminata: {Id}", id);
            TempData["SuccessMessage"] = "Configurazione eliminata con successo";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione della configurazione");
            TempData["ErrorMessage"] = "Errore durante l'eliminazione";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Visualizza i dettagli di una configurazione
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var configuration = await _context.KioskConfigurations
            .Include(c => c.Template)
            .Include(c => c.FieldValues)
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (configuration == null)
        {
            TempData["ErrorMessage"] = "Configurazione non trovata";
            return RedirectToAction(nameof(Index));
        }

        return View(configuration);
    }
}
