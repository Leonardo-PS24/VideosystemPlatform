using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Services;
using ConfigurationKiosk.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ConfigurationKiosk.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KioskController : ControllerBase
{
    private readonly IKioskService _kioskService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly ILogger<KioskController> _logger;

    public KioskController(
        IKioskService kioskService, 
        IAuthorizationService authorizationService, 
        IAdminAuthService adminAuthService,
        ILogger<KioskController> logger)
    {
        _kioskService = kioskService;
        _authorizationService = authorizationService;
        _adminAuthService = adminAuthService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var canCreate = (await _authorizationService.AuthorizeAsync(User, "Kiosk.Create")).Succeeded;
        var canDelete = (await _authorizationService.AuthorizeAsync(User, "Kiosk.Delete")).Succeeded;

        try
        {
            var instances = await _kioskService.GetRecentInstancesAsync();
            var templates = await _kioskService.GetActiveTemplatesAsync();
            
            return Ok(new
            {
                canCreate = canCreate,
                canDelete = canDelete,
                recentInstances = instances,
                availableTemplates = templates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel caricamento della dashboard Kiosk.");
            return StatusCode(500, new { message = "Impossibile caricare i dati." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Kiosk.Create")]
    public async Task<IActionResult> Create([FromBody] CreateRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        var instance = await _kioskService.CreateInstanceAsync(request.TemplateId, request.MachineSerial, userId);
        return Ok(instance);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInstance(int id)
    {
        var canEdit = (await _authorizationService.AuthorizeAsync(User, "Kiosk.Edit")).Succeeded;

        var instance = await _kioskService.GetInstanceByIdAsync(id);
        if (instance == null) return NotFound(new { message = "Istanza non trovata" });

        return Ok(new
        {
            canEdit = canEdit,
            instance = instance,
            template = instance.Template
        });
    }

    [HttpPatch("save")]
    [Authorize(Policy = "Kiosk.Edit")]
    public async Task<IActionResult> Save([FromBody] SaveRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.UpdateInstanceDataAsync(request.InstanceId, request.DataJson, userId);
        return Ok();
    }

    [HttpPatch("complete")]
    [Authorize(Policy = "Kiosk.Edit")]
    public async Task<IActionResult> Complete([FromBody] CompleteRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.UpdateInstanceDataAsync(request.InstanceId, request.DataJson, userId);
        await _kioskService.CompleteInstanceAsync(request.InstanceId, userId);
        return Ok();
    }

    [HttpPatch("start-revision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StartRevision([FromBody] InstanceRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.StartRevisionAsync(request.InstanceId, userId);
        return Ok();
    }

    [HttpPatch("finalize-revision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FinalizeRevision([FromBody] FinalizeRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        var result = await _kioskService.FinalizeRevisionAsync(request.InstanceId, request.DataJson, userId);
        return Ok(new { changesDetected = result });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Kiosk.Delete")]
    public async Task<IActionResult> DeleteInstance(int id)
    {
        await _kioskService.DeleteInstanceAsync(id);
        return Ok();
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var instance = await _kioskService.GetInstanceByIdAsync(id);
        if (instance == null) return NotFound(new { message = "Istanza non trovata" });

        var history = await _kioskService.GetInstanceHistoryAsync(id);

        return Ok(new
        {
            instance = instance,
            history = history
        });
    }

    [HttpPost("{id}/unlock-with-admin")]
    public async Task<IActionResult> UnlockWithAdmin(int id, [FromBody] UnlockRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username e password dell'amministratore sono richiesti." });
        }

        var isAuthorized = await _adminAuthService.VerifyAdminCredentialsAsync(request.Username, request.Password);
        if (!isAuthorized)
        {
            return BadRequest(new { message = "Credenziali non valide o l'utente non è un amministratore." });
        }

        await _kioskService.StartRevisionAsync(id, request.Username);
        return Ok(new { message = "Checklist sbloccata con successo." });
    }

    [HttpGet("{id}/diff")]
    public async Task<IActionResult> GetDiff(int id)
    {
        var diff = await _kioskService.GetInstanceDiffAsync(id);
        return Ok(diff);
    }

    [HttpPost("{id}/submit-approval")]
    public async Task<IActionResult> SubmitApproval(int id)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.SubmitApprovalAsync(id, userId);
        return Ok();
    }

    [HttpPost("{id}/reject-revision")]
    public async Task<IActionResult> RejectRevision(int id)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.RejectRevisionAsync(id, userId);
        return Ok();
    }

    [HttpPost("{id}/propose-revision")]
    public async Task<IActionResult> ProposeRevision(int id)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.StartRevisionAsync(id, userId);
        return Ok();
    }
}

public class UnlockRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class CreateRequest
{
    public int TemplateId { get; set; }
    public string MachineSerial { get; set; } = "";
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
