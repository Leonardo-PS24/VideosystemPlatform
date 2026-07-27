using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;
using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;
using Platform.Portal.Services;
using Platform.Shared.Constants;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller API per le funzionalità amministrative
/// </summary>
[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Restituisce la lista di tutti gli utenti
    /// </summary>
    [HttpGet("Users")]
    public async Task<IActionResult> GetUsers()
    {
        var userList = await (from user in _context.Users
            join userRole in _context.UserRoles on user.Id equals userRole.UserId into ur
            from subUserRole in ur.DefaultIfEmpty()
            join role in _context.Roles on subUserRole.RoleId equals role.Id into r
            from subRole in r.DefaultIfEmpty()
            select new UserListViewModel
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Role = subRole.Name ?? "Nessun Ruolo",
                CreatedAt = user.CreatedAt
            }).ToListAsync();

        return Ok(userList);
    }
    
    /// <summary>
    /// Restituisce i dettagli di un singolo utente
    /// </summary>
    [HttpGet("Users/{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Utente non trovato" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var model = new UserViewModel
        {
            Id = user.Id,
            Username = user.UserName!,
            Email = user.Email!,
            FullName = user.FullName,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? PlatformConstants.Roles.User,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Ok(model);
    }
    
    /// <summary>
    /// Modifica i dati di un utente
    /// </summary>
    [HttpPut("Users/{id}")]
    public async Task<IActionResult> EditUser(string id, [FromBody] UserViewModel model)
    {
        if (string.IsNullOrEmpty(model.Password))
        {
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Utente non trovato" });
        }

        user.UserName = model.Username;
        user.Email = model.Email;
        user.FullName = model.FullName;
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            // Aggiorna il ruolo
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            // Aggiorna la password se fornita
            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = errors });
                }
            }

            _logger.LogInformation($"Utente {user.UserName} modificato da {User.Identity!.Name}");
            return Ok(new { message = "Utente modificato con successo" });
        }

        var updateErrors = string.Join(", ", result.Errors.Select(e => e.Description));
        return BadRequest(new { message = updateErrors });
    }
    
    /// <summary>
    /// Elimina un utente
    /// </summary>
    [HttpDelete("Users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Utente non trovato" });
        }

        // Non permettere di eliminare se stesso
        if (user.UserName == User.Identity!.Name)
        {
            return BadRequest(new { message = "Non puoi eliminare il tuo account" });
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Utente {user.UserName} eliminato da {User.Identity.Name}");
            return Ok(new { message = "Utente eliminato con successo" });
        }

        return BadRequest(new { message = "Errore durante l'eliminazione dell'utente" });
    }

    /// <summary>
    /// Cambia lo stato attivo/disattivo di un utente
    /// </summary>
    [HttpPost("Users/{id}/toggle-status")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "Utente non trovato" });
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var status = user.IsActive ? "attivato" : "disattivato";
            _logger.LogInformation($"Utente {user.UserName} {status} da {User.Identity!.Name}");
            return Ok(new { message = $"Utente {status} con successo", isActive = user.IsActive });
        }

        return BadRequest(new { message = "Errore durante il cambio di stato dell'utente" });
    }

    /// <summary>
    /// Crea un nuovo utente ed invia l'invito via email
    /// </summary>
    [HttpPost("Users")]
    public async Task<IActionResult> CreateUser([FromBody] UserViewModel model)
    {
        ModelState.Remove("Password");
        ModelState.Remove("ConfirmPassword");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = model.IsActive,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("SetPassword", "Account",
                new { userId = user.Id, token },
                protocol: Request.Scheme);

            var subject = "Benvenuto nella Piattaforma Videosystem";
            var body = $"<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; padding: 40px 0; width: 100%;\">" +
                       $"  <div style=\"max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05); border: 1px solid #eef2f5;\">" +
                       $"    <div style=\"background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 30px; text-align: center;\">" +
                       $"      <h2 style=\"color: #ffffff; margin: 0; font-size: 24px; font-weight: bold; letter-spacing: 0.5px;\">Benvenuto in Videosystem</h2>" +
                       $"    </div>" +
                       $"    <div style=\"padding: 40px; color: #333333; line-height: 1.6;\">" +
                       $"      <p style=\"font-size: 16px; margin-top: 0;\">Ciao <strong>{user.FullName}</strong>,</p>" +
                       $"      <p style=\"font-size: 15px;\">Sei stato invitato ad unirti alla piattaforma interna di <strong>Videosystem S.r.l.</strong></p>" +
                       $"      <div style=\"background-color: #f8fafc; border-left: 4px solid #1e3c72; padding: 15px; margin: 25px 0; border-radius: 4px;\">" +
                       $"        <span style=\"font-size: 12px; color: #64748b; display: block; margin-bottom: 5px; text-transform: uppercase;\">Questo è il tuo nome utente:</span>" +
                       $"        <strong style=\"font-size: 18px; color: #0f172a; letter-spacing: 0.5px;\">{user.UserName}</strong>" +
                       $"      </div>" +
                       $"      <p style=\"font-size: 15px; margin-bottom: 30px;\">Clicca sul pulsante sottostante per creare la tua password e accedere alla piattaforma:</p>" +
                       $"      <div style=\"text-align: center; margin: 35px 0;\">" +
                       $"        <a href=\"{callbackUrl}\" style=\"background-color: #1e3c72; color: #ffffff; padding: 14px 28px; font-weight: bold; font-size: 15px; text-decoration: none; border-radius: 8px; display: inline-block; box-shadow: 0 4px 6px rgba(30, 60, 114, 0.15);\">Crea la tua password</a>" +
                       $"      </div>" +
                       $"      <p style=\"font-size: 13px; color: #94a3b8; text-align: center; margin-top: 40px; border-top: 1px solid #f1f5f9; padding-top: 20px;\">" +
                       $"        Se il pulsante non funziona, copia e incolla il seguente link nel browser:<br>" +
                       $"        <a href=\"{callbackUrl}\" style=\"color: #2a5298; word-break: break-all;\">{callbackUrl}</a>" +
                       $"      </p>" +
                       $"    </div>" +
                       $"    <div style=\"background-color: #f8fafc; padding: 20px; text-align: center; color: #64748b; font-size: 12px; border-top: 1px solid #f1f5f9;\">" +
                       $"      © {DateTime.UtcNow.Year} Videosystem S.r.l. • Piattaforma Interna" +
                       $"    </div>" +
                       $"  </div>" +
                       $"</div>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, body);
                return Ok(new { message = $"Invito inviato con successo a {user.Email}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Impossibile inviare l'email di invito a {Email}", user.Email);
                _logger.LogWarning("Link di attivazione per {Email}: {CallbackUrl}", user.Email, callbackUrl);
                return Ok(new { 
                    message = "Utente creato con successo, ma è stato impossibile inviare l'email di invito. Controllare i log del server per il link di attivazione.",
                    activationUrl = callbackUrl // Restituiamo il link anche per comodità in fase di test dev
                });
            }
        }

        var createErrors = string.Join(", ", result.Errors.Select(e => e.Description));
        return BadRequest(new { message = createErrors });
    }
}
