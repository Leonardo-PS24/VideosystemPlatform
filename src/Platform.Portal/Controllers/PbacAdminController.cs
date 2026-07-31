using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Platform.Portal.Models.PBAC;
using Platform.Portal.Services.PBAC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Portal.Controllers;

[Authorize(Roles = "Admin,Developer")]
[ApiController]
[Route("api/[controller]")]
public class PbacAdminController : ControllerBase
{
    private readonly IPbacService _pbacService;
    private readonly ILogger<PbacAdminController> _logger;

    public PbacAdminController(IPbacService pbacService, ILogger<PbacAdminController> logger)
    {
        _pbacService = pbacService;
        _logger = logger;
    }

    /// <summary>
    /// Restituisce tutto il catalogo dei permessi atomici disponibili
    /// </summary>
    [HttpGet("Catalog")]
    public async Task<IActionResult> GetCatalog()
    {
        var catalog = await _pbacService.GetPermissionCatalogAsync();
        return Ok(catalog);
    }

    /// <summary>
    /// Registra un nuovo permesso atomico nel catalogo
    /// </summary>
    [HttpPost("RegisterPermission")]
    public async Task<IActionResult> RegisterPermission([FromBody] RegisterPermissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key)) return BadRequest(new { message = "La chiave del permesso è obbligatoria." });

        await _pbacService.RegisterPermissionAsync(
            request.Key,
            request.Area,
            request.Module,
            request.Action,
            request.Description);

        return Ok(new { message = $"Permesso '{request.Key}' salvato nel catalogo con successo." });
    }

    /// <summary>
    /// Restituisce tutte le Policy di sistema e personalizzate
    /// </summary>
    [HttpGet("Policies")]
    public async Task<IActionResult> GetPolicies()
    {
        var policies = await _pbacService.GetAllPoliciesAsync();
        return Ok(policies);
    }

    /// <summary>
    /// Restituisce i dettagli di una singola Policy
    /// </summary>
    [HttpGet("Policies/{id}")]
    public async Task<IActionResult> GetPolicyById(int id)
    {
        var policy = await _pbacService.GetPolicyByIdAsync(id);
        if (policy == null) return NotFound(new { message = "Policy non trovata." });
        return Ok(policy);
    }

    /// <summary>
    /// Crea o aggiorna una Policy
    /// </summary>
    [HttpPost("Policies")]
    public async Task<IActionResult> SavePolicy([FromBody] SavePolicyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Il nome della Policy è obbligatorio." });
        }

        try
        {
            var result = await _pbacService.SavePolicyAsync(
                request.Id,
                request.Name.Trim(),
                request.Description?.Trim(),
                request.PermissionCatalogIds ?? new List<int>());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio della policy");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una Policy personalizzata
    /// </summary>
    [HttpDelete("Policies/{id}")]
    public async Task<IActionResult> DeletePolicy(int id)
    {
        try
        {
            var deleted = await _pbacService.DeletePolicyAsync(id);
            if (!deleted) return NotFound(new { message = "Policy non trovata." });
            return Ok(new { message = "Policy eliminata con successo." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore eliminazione policy");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Restituisce le assegnazioni Policy di un Utente
    /// </summary>
    [HttpGet("UserAssignments/{userId}")]
    public async Task<IActionResult> GetUserAssignments(string userId)
    {
        var assignments = await _pbacService.GetUserPolicyAssignmentsAsync(userId);
        return Ok(assignments);
    }

    /// <summary>
    /// Assegna una Policy a un Utente con Scope Sottoazienda e Reparto
    /// </summary>
    [HttpPost("UserAssignments")]
    public async Task<IActionResult> AssignUserPolicy([FromBody] AssignUserPolicyRequest request)
    {
        if (string.IsNullOrEmpty(request.UserId) || request.PolicyId <= 0)
        {
            return BadRequest(new { message = "UserId e PolicyId sono obbligatori." });
        }

        await _pbacService.AssignPolicyToUserAsync(
            request.UserId,
            request.PolicyId,
            request.CompanyScope ?? "ALL",
            request.DepartmentScope ?? "ALL");

        return Ok(new { message = "Policy assegnata all'utente con successo." });
    }

    /// <summary>
    /// Rimuove un'assegnazione Policy da un Utente
    /// </summary>
    [HttpDelete("UserAssignments/{id}")]
    public async Task<IActionResult> RemoveUserPolicy(int id)
    {
        await _pbacService.RemovePolicyFromUserAsync(id);
        return Ok(new { message = "Assegnazione policy rimossa dall'utente." });
    }

    /// <summary>
    /// Restituisce le assegnazioni Policy dei Ruoli
    /// </summary>
    [HttpGet("RoleAssignments")]
    public async Task<IActionResult> GetRoleAssignments([FromQuery] string? roleName = null)
    {
        var assignments = await _pbacService.GetRolePolicyAssignmentsAsync(roleName);
        return Ok(assignments);
    }

    /// <summary>
    /// Assegna una Policy a un Ruolo con Scope Sottoazienda e Reparto
    /// </summary>
    [HttpPost("RoleAssignments")]
    public async Task<IActionResult> AssignRolePolicy([FromBody] AssignRolePolicyRequest request)
    {
        if (string.IsNullOrEmpty(request.RoleName) || request.PolicyId <= 0)
        {
            return BadRequest(new { message = "RoleName e PolicyId sono obbligatori." });
        }

        await _pbacService.AssignPolicyToRoleAsync(
            request.RoleName,
            request.PolicyId,
            request.CompanyScope ?? "ALL",
            request.DepartmentScope ?? "ALL");

        return Ok(new { message = "Policy assegnata al ruolo con successo." });
    }

    /// <summary>
    /// Rimuove un'assegnazione Policy da un Ruolo
    /// </summary>
    [HttpDelete("RoleAssignments/{id}")]
    public async Task<IActionResult> RemoveRolePolicy(int id)
    {
        await _pbacService.RemovePolicyFromRoleAsync(id);
        return Ok(new { message = "Assegnazione policy rimossa dal ruolo." });
    }
}

public class RegisterPermissionRequest
{
    public string Key { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class SavePolicyRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<int> PermissionCatalogIds { get; set; } = new();
}

public class AssignUserPolicyRequest
{
    public string UserId { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public string? CompanyScope { get; set; } = "ALL";
    public string? DepartmentScope { get; set; } = "ALL";
}

public class AssignRolePolicyRequest
{
    public string RoleName { get; set; } = string.Empty;
    public int PolicyId { get; set; }
    public string? CompanyScope { get; set; } = "ALL";
    public string? DepartmentScope { get; set; } = "ALL";
}
