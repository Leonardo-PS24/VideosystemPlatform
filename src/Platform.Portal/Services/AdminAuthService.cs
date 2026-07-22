using Microsoft.AspNetCore.Identity;
using Platform.Portal.Models;
using Platform.Shared.Services;

namespace Platform.Portal.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminAuthService(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<bool> VerifyAdminCredentialsAsync(string username, string password)
    {
        var user = await _userManager.FindByNameAsync(username) ?? await _userManager.FindByEmailAsync(username);
        if (user == null || !user.IsActive) return false;

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded) return false;

        var roles = await _userManager.GetRolesAsync(user);
        return roles.Contains("Admin");
    }
}
