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
            // Prova a trovare l'utente per username o email
            var user = await _userManager.FindByNameAsync(model.Username) 
                ?? await _userManager.FindByEmailAsync(model.Username);

            if (user != null)
            {
                // Verifica se l'utente è attivo
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
