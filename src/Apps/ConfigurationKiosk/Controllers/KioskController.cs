using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Services;
using ConfigurationKiosk.Models;

namespace ConfigurationKiosk.Controllers;

[Authorize]
public class KioskController : Controller
{
    private readonly IKioskService _kioskService;

    public KioskController(IKioskService kioskService)
    {
        _kioskService = kioskService;
    }

    public async Task<IActionResult> Index()
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
        await _kioskService.UpdateInstanceDataAsync(request.InstanceId, request.DataJson, request.Status, userId);
        return Ok();
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