using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Services;
using ConfigurationKiosk.Models;
using Microsoft.Extensions.Logging;

namespace ConfigurationKiosk.Controllers;

[Authorize]
public class KioskController : Controller
{
    private readonly IKioskService _kioskService;
    private readonly ILogger<KioskController> _logger;

    public KioskController(IKioskService kioskService, ILogger<KioskController> logger)
    {
        _kioskService = kioskService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var instances = await _kioskService.GetRecentInstancesAsync();
            var templates = await _kioskService.GetActiveTemplatesAsync();
            
            var model = new KioskDashboardViewModel
            {
                RecentInstances = instances,
                AvailableTemplates = templates
            };
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento della dashboard Kiosk.");
            TempData["ErrorMessage"] = "Impossibile caricare i dati. Verificare che il database sia aggiornato (migrazioni).";
            return View(new KioskDashboardViewModel());
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(int templateId, string machineSerial)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        var instance = await _kioskService.CreateInstanceAsync(templateId, machineSerial, userId);
        return RedirectToAction("Compile", new { id = instance.Id });
    }

    public async Task<IActionResult> Compile(int id)
    {
        var instance = await _kioskService.GetInstanceByIdAsync(id);
        if (instance == null) return NotFound();

        var model = new KioskCompileViewModel
        {
            Instance = instance,
            Template = instance.Template
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.UpdateInstanceDataAsync(request.InstanceId, request.DataJson, userId);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Complete([FromBody] CompleteRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.UpdateInstanceDataAsync(request.InstanceId, request.DataJson, userId);
        await _kioskService.CompleteInstanceAsync(request.InstanceId, userId);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> StartRevision([FromBody] InstanceRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.StartRevisionAsync(request.InstanceId, userId);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> FinalizeRevision([FromBody] FinalizeRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        var result = await _kioskService.FinalizeRevisionAsync(request.InstanceId, request.DataJson, userId);
        return Ok(new { changesDetected = result });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteInstance(int id)
    {
        await _kioskService.DeleteInstanceAsync(id);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History(int id)
    {
        var instance = await _kioskService.GetInstanceByIdAsync(id);
        if (instance == null) return NotFound();

        var history = await _kioskService.GetInstanceHistoryAsync(id);

        var model = new KioskHistoryViewModel
        {
            Instance = instance,
            History = history
        };

        return View(model);
    }
}

public class SaveRequest 
{
    public int InstanceId { get; set; }
    public string DataJson { get; set; } = "";
    public string Status { get; set; } = "";
}

public class CompleteRequest
{
    public int InstanceId { get; set; }
    public string DataJson { get; set; } = "";
}

public class InstanceRequest
{
    public int InstanceId { get; set; }
}

public class FinalizeRequest
{
    public int InstanceId { get; set; }
    public string DataJson { get; set; } = "";
}