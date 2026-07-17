using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Models;
using Platform.Shared.Services;
using Microsoft.Extensions.Logging;

namespace ConfigurationKiosk.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class KioskAdminController : ControllerBase
{
    private readonly IKioskService _kioskService;
    private readonly ILogger<KioskAdminController> _logger;

    public KioskAdminController(IKioskService kioskService, ILogger<KioskAdminController> logger)
    {
        _kioskService = kioskService;
        _logger = logger;
    }

    /// <summary>
    /// Restituisce tutti i template delle checklist (compresi quelli inattivi)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _kioskService.GetAllTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// Crea un nuovo template
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Kiosk.Create")]
    public async Task<IActionResult> CreateTemplate([FromBody] KioskChecklistTemplate template)
    {
        ModelState.Remove(nameof(template.CreatedBy));
        ModelState.Remove(nameof(template.UpdatedBy));
        ModelState.Remove(nameof(template.CreatedAt));
        ModelState.Remove(nameof(template.UpdatedAt));

        if (string.IsNullOrWhiteSpace(template.StructureJson))
        {
            return BadRequest(new { message = "La struttura del template (JSON) è obbligatoria." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try 
        {
            var userId = User.Identity?.Name ?? "Unknown";
            await _kioskService.CreateTemplateAsync(template, userId);
            return Ok(new { message = "Template creato con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio del template");
            return StatusCode(500, new { message = "Si è verificato un errore durante il salvataggio del template." });
        }
    }
    
    /// <summary>
    /// Elimina un template
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "Kiosk.Delete")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        try
        {
            await _kioskService.DeleteTemplateAsync(id);
            return Ok(new { message = "Template eliminato con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione del template {Id}", id);
            return BadRequest(new { message = "Impossibile eliminare il template." });
        }
    }

    /// <summary>
    /// Attiva/disattiva un template
    /// </summary>
    [HttpPost("{id}/toggle-status")]
    [Authorize(Policy = "Kiosk.Edit")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        try
        {
            await _kioskService.ToggleTemplateStatusAsync(id);
            return Ok(new { message = "Stato aggiornato con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il toggle dello stato del template {Id}", id);
            return BadRequest(new { message = "Impossibile aggiornare lo stato del template." });
        }
    }
}
