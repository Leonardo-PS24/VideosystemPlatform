using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Platform.Portal.Models;
using Platform.Portal.Models.ViewModels;
using System.Security.Claims;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller API per la gestione dell'autenticazione
/// </summary>
[ApiController]
[Route("[controller]")]
public class AccountController : ControllerBase
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
    /// Restituisce le informazioni sull'utente corrente se autenticato
    /// </summary>
    [HttpGet("User")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new { message = "Non autenticato" });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound(new { message = "Utente non trovato" });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            username = user.UserName,
            email = user.Email,
            fullName = user.FullName,
            roles = roles
        });
    }

    /// <summary>
    /// Gestisce il login dell'utente tramite API
    /// </summary>
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByNameAsync(model.Username) 
            ?? await _userManager.FindByEmailAsync(model.Username);

        if (user != null)
        {
            if (!user.IsActive)
            {
                return BadRequest(new { message = "Utente disabilitato. Contattare l'amministratore." });
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Utente {user.UserName} ha effettuato il login");
                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new
                {
                    message = "Login eseguito con successo",
                    user = new
                    {
                        username = user.UserName,
                        email = user.Email,
                        fullName = user.FullName,
                        roles = roles
                    }
                });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning($"Account {user.UserName} bloccato");
                return BadRequest(new { message = "Account bloccato per troppi tentativi falliti." });
            }
        }

        return BadRequest(new { message = "Credenziali non valide." });
    }

    /// <summary>
    /// Reindirizza alla rotta React per l'impostazione password
    /// </summary>
    [HttpGet("SetPassword")]
    [AllowAnonymous]
    public IActionResult SetPassword(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return Redirect("/login?error=LinkNonValido");
        }

        // Reindirizza al frontend React
        return Redirect($"/set-password?userId={userId}&token={System.Net.WebUtility.UrlEncode(token)}");
    }

    /// <summary>
    /// Imposta la password tramite API
    /// </summary>
    [HttpPost("SetPassword")]
    [AllowAnonymous]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return BadRequest(new { message = "Utente non trovato." });
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Utente {user.UserName} ha impostato la sua password.");
            return Ok(new { message = "Password impostata con successo." });
        }

        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return BadRequest(new { message = errors });
    }

    /// <summary>
    /// Gestisce il logout tramite API
    /// </summary>
    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Utente ha effettuato il logout");
        return Ok(new { message = "Logout eseguito con successo" });
    }

    /// <summary>
    /// Pagina di accesso negato (reindirizzamento client-side)
    /// </summary>
    [HttpGet("AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return Redirect("/access-denied");
    }

    /// <summary>
    /// Avvia la richiesta di login esterno (es. Google/Microsoft)
    /// </summary>
    [HttpGet("ExternalLogin")]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    /// <summary>
    /// Callback gestito dal provider esterno
    /// </summary>
    [HttpGet("ExternalLoginCallback")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= "/";

        if (remoteError != null)
        {
            _logger.LogError($"Errore durante il login con provider esterno: {remoteError}");
            return Redirect($"/login?error={Uri.EscapeDataString(remoteError)}");
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("Impossibile caricare le informazioni dal provider esterno");
            return Redirect("/login?error=InfoEsternaNonTrovata");
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            _logger.LogInformation($"Utente ha effettuato l'accesso con {info.LoginProvider}.");
            return Redirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return Redirect("/access-denied");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email != null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];

                user = new ApplicationUser
                {
                    UserName = email.Split('@')[0],
                    Email = email,
                    FullName = fullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Platform.Shared.Constants.PlatformConstants.Roles.User);
                    _logger.LogInformation($"Creato nuovo utente {user.UserName} tramite {info.LoginProvider}.");
                }
                else
                {
                    var errors = Uri.EscapeDataString(string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Redirect($"/login?error={errors}");
                }
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                _logger.LogInformation($"Associato ed effettuato l'accesso per {user.UserName} con {info.LoginProvider}.");
                return Redirect(returnUrl);
            }
        }

        return Redirect("/login?error=ImpossibileRecuperareEmail");
    }
}
