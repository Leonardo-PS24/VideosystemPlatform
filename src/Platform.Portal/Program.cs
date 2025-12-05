using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;
using Microsoft.FeatureManagement;
using Platform.Portal.Middleware;      
using Platform.Portal.Models;
using Platform.Portal.Services;        
using Platform.Shared.Services;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

// Configura porte HTTP e HTTPS
builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

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
builder.Services.AddFeatureManagement();

// Configura DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IPermissionService, PermissionService>();

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

// Seed del database - CREA AUTOMATICAMENTE IL DB
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Ricrea il database da zero (cancella e ricrea)
        // NOTA: Rimuovere in produzione!
        Log.Information("Verifico lo stato del database...");

        try
        {
            // Chiudi tutte le connessioni esistenti ed elimina il database
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = 'VideosystemPortal')
                BEGIN
                    ALTER DATABASE [VideosystemPortal] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [VideosystemPortal];
                END
            ");
            Log.Information("Database esistente eliminato");
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Nessun database da eliminare o errore durante l'eliminazione");
        }

        // Crea il database
        Log.Information("Creo il database...");
        context.Database.EnsureCreated();
        Log.Information("Database ricreato con successo");
        
        // Inizializza dati di default
        await DbInitializer.Initialize(context, userManager, roleManager);
        Log.Information("Inizializzazione database completata");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Errore durante l'inizializzazione del database");
        // NON FERMARE L'APP, continua comunque
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Log.Information("Platform Portal avviato");

app.Run();
