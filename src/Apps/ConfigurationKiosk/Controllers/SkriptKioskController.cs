using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Shared.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ConfigurationKiosk.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SkriptKioskController : ControllerBase
{
    private readonly ISkriptKioskService _kioskService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IAdminAuthService _adminAuthService;
    private readonly ILogger<SkriptKioskController> _logger;

    public SkriptKioskController(
        ISkriptKioskService kioskService, 
        IAuthorizationService authorizationService, 
        IAdminAuthService adminAuthService,
        ILogger<SkriptKioskController> logger)
    {
        _kioskService = kioskService;
        _authorizationService = authorizationService;
        _adminAuthService = adminAuthService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var canCreate = (await _authorizationService.AuthorizeAsync(User, "SkriptKiosk.Create")).Succeeded;
        var canDelete = (await _authorizationService.AuthorizeAsync(User, "SkriptKiosk.Delete")).Succeeded;

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
            _logger.LogError(ex, "Errore nel caricamento della dashboard SkriptKiosk.");
            return StatusCode(500, new { message = "Impossibile caricare i dati." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "SkriptKiosk.Create")]
    public async Task<IActionResult> Create([FromBody] SkriptKioskCreateRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        var instance = await _kioskService.CreateInstanceAsync(request.TemplateId, request.MachineSerial, userId);
        return Ok(instance);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInstance(int id)
    {
        var canEdit = (await _authorizationService.AuthorizeAsync(User, "SkriptKiosk.Edit")).Succeeded;

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
    [Authorize(Policy = "SkriptKiosk.Edit")]
    public async Task<IActionResult> Save([FromBody] SkriptKioskSaveRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.UpdateInstanceDataAsync(request.InstanceId, request.DataJson, userId);
        return Ok();
    }

    [HttpPatch("complete")]
    [Authorize(Policy = "SkriptKiosk.Edit")]
    public async Task<IActionResult> Complete([FromBody] SkriptKioskCompleteRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.UpdateInstanceDataAsync(request.InstanceId, request.DataJson, userId);
        await _kioskService.CompleteInstanceAsync(request.InstanceId, userId);
        return Ok();
    }

    [HttpPatch("start-revision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StartRevision([FromBody] SkriptKioskInstanceRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        await _kioskService.StartRevisionAsync(request.InstanceId, userId);
        return Ok();
    }

    [HttpPatch("finalize-revision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> FinalizeRevision([FromBody] SkriptKioskFinalizeRequest request)
    {
        var userId = User.Identity?.Name ?? "Unknown";
        var result = await _kioskService.FinalizeRevisionAsync(request.InstanceId, request.DataJson, userId);
        return Ok(new { changesDetected = result });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "SkriptKiosk.Delete")]
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
    public async Task<IActionResult> UnlockWithAdmin(int id, [FromBody] SkriptKioskUnlockRequest request)
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

public class SkriptKioskUnlockRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class SkriptKioskCreateRequest
{
    public int TemplateId { get; set; }
    public string MachineSerial { get; set; } = "";
}

public class SkriptKioskSaveRequest 
{
    public int InstanceId { get; set; }
    public string DataJson { get; set; } = "";
    public string Status { get; set; } = "";
}

public class SkriptKioskCompleteRequest
{
    public int InstanceId { get; set; }
    public string DataJson { get; set; } = "";
}

public class SkriptKioskInstanceRequest
{
    public int InstanceId { get; set; }
}

public class SkriptKioskFinalizeRequest
{
    public int InstanceId { get; set; }
    public string DataJson { get; set; } = "";
}
