using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Platform.Portal.Data;
using Microsoft.FeatureManagement;
using Platform.Portal.Authorization;
using Platform.Portal.Middleware;
using Platform.Portal.Models;
using Platform.Portal.Services;
using Platform.Portal.Settings;
using Platform.Shared.Services;
using Platform.Portal.Hubs;
using Serilog;
using System;

var builder = WebApplication.CreateBuilder(args);

// Configura porte HTTP e HTTPS per ascoltare su tutti gli IP
builder.WebHost.UseUrls("http://*:5000", "https://*:5001");

// Configura Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/platform-portal-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Aggiungi servizi al container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddFeatureManagement();

// Configura DbContext (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // ... (impostazioni Identity) ...
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Registra i servizi
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IKioskService, KioskService>();

// Registra l'handler di autorizzazione
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

// Configura le policy di autorizzazione
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Kiosk.Create", policy => policy.Requirements.Add(new PermissionRequirement("ConfigurationKiosk.Create")));
    options.AddPolicy("Kiosk.Edit", policy => policy.Requirements.Add(new PermissionRequirement("ConfigurationKiosk.Edit")));
    options.AddPolicy("Kiosk.Delete", policy => policy.Requirements.Add(new PermissionRequirement("ConfigurationKiosk.Delete")));
});

// Configura le impostazioni
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Configura Forwarded Headers per il deploy dietro a un reverse proxy (IIS)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Configura cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Registra JWT Service
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey non configurata");
builder.Services.AddSingleton<IJwtService>(new JwtService(jwtSecretKey));

// Configura HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443; // Usa la porta standard HTTPS quando reindirizza
});

var app = builder.Build();

// Applica il middleware per i Forwarded Headers
app.UseForwardedHeaders();

// Inizializzazione Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        await DbInitializer.Initialize(context, userManager, roleManager);
        Log.Information("Inizializzazione dati completata");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Errore durante l'inizializzazione del database");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UsePermissionMiddleware();
app.UseSerilogRequestLogging();

app.MapHub<KioskHub>("/kioskhub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Log.Information("Platform Portal avviato");

app.Run();
