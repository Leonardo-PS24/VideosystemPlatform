using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;
using Platform.Shared.Constants;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller per le funzionalità amministrative
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Lista di tutti gli utenti
    /// </summary>
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = new List<UserListViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new UserListViewModel
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = user.CreatedAt
            });
        }

        return View(userList);
    }

    /// <summary>
    /// Form per creare un nuovo utente
    /// </summary>
    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new UserViewModel());
    }

    /// <summary>
    /// Crea un nuovo utente
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserViewModel model)
    {
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

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (result.Succeeded)
            {
                // Assegna il ruolo
                await _userManager.AddToRoleAsync(user, model.Role);

                _logger.LogInformation($"Utente {user.UserName} creato da {User.Identity!.Name}");
                TempData["SuccessMessage"] = "Utente creato con successo";
                return RedirectToAction(nameof(Users));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    /// <summary>
    /// Form per modificare un utente esistente
    /// </summary>
    [HttpGet]
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

    /// <summary>
    /// Modifica un utente esistente
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(UserViewModel model)
    {
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
                    await _userManager.ResetPasswordAsync(user, token, model.Password);
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

    /// <summary>
    /// Elimina un utente
    /// </summary>
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

    /// <summary>
    /// Attiva/Disattiva un utente
    /// </summary>
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
}
