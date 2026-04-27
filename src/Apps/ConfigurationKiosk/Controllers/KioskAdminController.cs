using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Platform.Shared.Models;
using Platform.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ConfigurationKiosk.Controllers;

[Authorize(Roles = "Admin")]
public class KioskAdminController : Controller
{
    private readonly IKioskService _kioskService;
    private readonly ILogger<KioskAdminController> _logger;

    public KioskAdminController(IKioskService kioskService, ILogger<KioskAdminController> logger)
    {
        _kioskService = kioskService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var templates = await _kioskService.GetAllTemplatesAsync();
        return View(templates);
    }

    [Authorize(Policy = "Kiosk.Create")]
    public IActionResult Create()
    {
        return View(new KioskChecklistTemplate { IsActive = true });
    }

    [HttpPost]
    [Authorize(Policy = "Kiosk.Create")]
    public async Task<IActionResult> Create(KioskChecklistTemplate template, IFormFile? jsonFile)
    {
        if (jsonFile != null)
        {
            using var reader = new StreamReader(jsonFile.OpenReadStream());
            template.StructureJson = await reader.ReadToEndAsync();
            ModelState.Remove(nameof(template.StructureJson));
        }
        else if (string.IsNullOrWhiteSpace(template.StructureJson))
        {
            ModelState.AddModelError(nameof(template.StructureJson), "Devi caricare un file JSON o inserire la struttura.");
        }

        ModelState.Remove(nameof(template.CreatedBy));
        ModelState.Remove(nameof(template.UpdatedBy));
        ModelState.Remove(nameof(template.CreatedAt));
        ModelState.Remove(nameof(template.UpdatedAt));

        if (ModelState.IsValid)
        {
            try 
            {
                var userId = User.Identity?.Name ?? "Unknown";
                await _kioskService.CreateTemplateAsync(template, userId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il salvataggio del template");
                ModelState.AddModelError("", "Si è verificato un errore durante il salvataggio.");
            }
        }
        else
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning("Validazione fallita per Create Template: {Errors}", string.Join(", ", errors));
        }

        return View(template);
    }
    
    [HttpPost]
    [Authorize(Policy = "Kiosk.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _kioskService.DeleteTemplateAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "Kiosk.Edit")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await _kioskService.ToggleTemplateStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
