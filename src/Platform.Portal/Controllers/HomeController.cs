using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.Portal.Models;
using Platform.Portal.Services;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Platform.Portal.Controllers;

/// <summary>
/// Controller API per la homepage e dashboard
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;
    private readonly IPermissionService _permissionService;

    public HomeController(
        IConfiguration configuration, 
        ILogger<HomeController> logger,
        IPermissionService permissionService)
    {
        _configuration = configuration;
        _logger = logger;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Restituisce la lista delle aziende e le loro applicazioni autorizzate per la dashboard React
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDashboardData()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var companies = _configuration.GetSection("Companies").Get<List<CompanyInfo>>() 
            ?? new List<CompanyInfo>();
        
        var filteredCompanies = new List<CompanyInfo>();
        foreach (var company in companies)
        {
            var filteredApps = new List<ApplicationInfo>();
            foreach (var app in company.Applications)
            {
                var hasView = await _permissionService.HasPermissionAsync(userId, app.AppId, PermissionType.View);
                if (hasView)
                {
                    filteredApps.Add(app);
                }
            }

            if (filteredApps.Any())
            {
                filteredCompanies.Add(new CompanyInfo
                {
                    Id = company.Id,
                    Name = company.Name,
                    PrimaryColor = company.PrimaryColor,
                    SecondaryColor = company.SecondaryColor,
                    Applications = filteredApps
                });
            }
        }
        
        return Ok(filteredCompanies);
    }
}
