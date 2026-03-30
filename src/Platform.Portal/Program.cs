using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;
using Microsoft.FeatureManagement;
using Platform.Portal.Middleware;
using Platform.Portal.Models;
using Platform.Portal.Services;
using Platform.Portal.Settings; // Aggiunto using per Settings
using Platform.Shared.Services;
using Platform.Portal.Hubs; // Aggiunto using per Hubs
using Serilog;

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
builder.Services.AddSignalR(); // Aggiunto SignalR
builder.Services.AddFeatureManagement();

// Configura DbContext (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // ... (impostazioni Identity) ...
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Registra i servizi
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IEmailService, EmailService>(); // Registra il servizio email
builder.Services.AddScoped<IKioskService, KioskService>(); // Registra il servizio Kiosk

// Configura le impostazioni
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings")); // Registra le impostazioni email

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
    options.HttpsPort = 5001;
});

var app = builder.Build();

// Inizializzazione Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        Log.Information("Verifica e inizializzazione database...");
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database pronto");
        
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

// Commentato per facilitare test in LAN senza certificati validi
// app.UseHttpsRedirection(); 

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UsePermissionMiddleware();
app.UseSerilogRequestLogging();

app.MapHub<KioskHub>("/kioskhub"); // Mappa l'hub

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Log.Information("Platform Portal avviato");

app.Run();
