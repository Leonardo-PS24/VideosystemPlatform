using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data; // Aggiunto per accedere al DbContext
using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;
using Platform.Portal.Services;
using Platform.Shared.Constants;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller per le funzionalità amministrative
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager; // Aggiunto RoleManager
    private readonly ApplicationDbContext _context; // Aggiunto DbContext
    private readonly IEmailService _emailService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager, // Iniezione
        ApplicationDbContext context, // Iniezione
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
    /// Mostra la lista di tutti gli utenti in modo ottimizzato
    /// </summary>
    public async Task<IActionResult> Users()
    {
        // Query ottimizzata per evitare il problema N+1
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

        return View(userList);
    }
    
    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new UserViewModel());
    }
    
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
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

        return View(model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserViewModel model)
    {
        // Se la password non è stata inserita, non validarla
        if (string.IsNullOrEmpty(model.Password))
        {
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
        }

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.Id!);
            if (user == null)
            {
                return NotFound();
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
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }

                _logger.LogInformation($"Utente {user.UserName} modificato da {User.Identity!.Name}");
                TempData["SuccessMessage"] = "Utente modificato con successo";
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Non permettere di eliminare se stesso
        if (user.UserName == User.Identity!.Name)
        {
            TempData["ErrorMessage"] = "Non puoi eliminare il tuo account";
            return RedirectToAction(nameof(Users));
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Utente {user.UserName} eliminato da {User.Identity.Name}");
            TempData["SuccessMessage"] = "Utente eliminato con successo";
        }
        else
        {
            TempData["ErrorMessage"] = "Errore durante l'eliminazione dell'utente";
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            var status = user.IsActive ? "attivato" : "disattivato";
            _logger.LogInformation($"Utente {user.UserName} {status} da {User.Identity!.Name}");
            TempData["SuccessMessage"] = $"Utente {status} con successo";
        }
        else
        {
            TempData["ErrorMessage"] = "Errore durante il cambio di stato dell'utente";
        }

        return RedirectToAction(nameof(Users));
    }


    /// <summary>
    /// Crea un nuovo utente e invia un invito via email
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserViewModel model)
    {
        ModelState.Remove("Password");
        ModelState.Remove("ConfirmPassword");

        if (ModelState.IsValid)
        {
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

                // Invia l'email di invito
                var subject = "Benvenuto nella Piattaforma Videosystem";
                var body = $"<p>Ciao {user.FullName},</p>" +
                           "<p>Sei stato invitato a unirti alla piattaforma interna di Videosystem.</p>" +
                           $"<p>Per completare la registrazione e impostare la tua password, clicca sul link qui sotto:</p>" +
                           $"<a href='{callbackUrl}'>Imposta la tua password</a>" +
                           "<p>Grazie,<br>Il Team di Videosystem</p>";

                try
                {
                    await _emailService.SendEmailAsync(user.Email, subject, body);
                    TempData["SuccessMessage"] = $"Invito inviato con successo a {user.Email}.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossibile inviare l'email di invito a {Email}", user.Email);
                    TempData["ErrorMessage"] = "Utente creato, ma impossibile inviare l'email di invito. " +
                                               "Controlla i log per il link di attivazione.";
                    // Log del link come fallback
                    _logger.LogWarning("Link di attivazione per {Email}: {CallbackUrl}", user.Email, callbackUrl);
                }

                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }
}
