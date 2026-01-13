using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller per la gestione dell'autenticazione
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Mostra la pagina di login
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Gestisce il login dell'utente
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByNameAsync(model.Username) 
                ?? await _userManager.FindByEmailAsync(model.Username);

            if (user != null)
            {
                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Utente disabilitato. Contattare l'amministratore.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!, 
                    model.Password, 
                    model.RememberMe, 
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Utente {user.UserName} ha effettuato il login");
                    
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    return RedirectToAction("Index", "Home");
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"Account {user.UserName} bloccato");
                    ModelState.AddModelError(string.Empty, "Account bloccato per troppi tentativi falliti.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Credenziali non valide.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Credenziali non valide.");
            }
        }

        return View(model);
    }

    /// <summary>
    /// Mostra la pagina per impostare la password per la prima volta
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult SetPassword(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return BadRequest("Link non valido o scaduto.");
        }

        var model = new SetPasswordViewModel { UserId = userId, Token = token };
        return View(model);
    }

    /// <summary>
    /// Imposta la password e fa il login
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Utente non trovato.");
                return View(model);
            }

            // Imposta la password
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Utente {user.UserName} ha impostato la sua password.");

                // Esegui il login automatico
                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["SuccessMessage"] = "Password impostata con successo. Benvenuto!";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    /// <summary>
    /// Gestisce il logout dell'utente
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Utente ha effettuato il logout");
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Pagina di accesso negato
    /// </summary>
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
