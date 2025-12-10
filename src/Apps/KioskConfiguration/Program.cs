using Microsoft.EntityFrameworkCore;
using Platform.Apps.KioskConfiguration.Data;
using Platform.Apps.KioskConfiguration.Models;
using Platform.Apps.KioskConfiguration.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configura porte HTTP
builder.WebHost.UseUrls("http://localhost:5003");

// Configurazione Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/kiosk-configuration-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllersWithViews();

// Configura antiforgery per HTTP in development
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Permetti su HTTP
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Application Services
builder.Services.AddScoped<ITemplateService, TemplateService>();

// Session per form multi-step (opzionale)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Permetti cookie su HTTP in development
});

// CORS per permettere iframe dal Platform.Portal
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPortal", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Inizializza il database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Ricrea il database da zero (cancella e ricrea)
        // NOTA: Rimuovere in produzione!
        Log.Information("Verifico lo stato del database...");

        try
        {
            // Chiudi tutte le connessioni esistenti ed elimina il database
            await context.Database.ExecuteSqlRawAsync(@"
                IF EXISTS (SELECT name FROM sys.databases WHERE name = 'KioskConfigDB')
                BEGIN
                    ALTER DATABASE [KioskConfigDB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [KioskConfigDB];
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

        // Seed templates se non esistono
        ConfigurationTemplateEntity? standardTemplate = null;
        ConfigurationTemplateEntity? advancedTemplate = null;

        if (!context.ConfigurationTemplates.Any())
        {
            standardTemplate = new ConfigurationTemplateEntity
            {
                Id = Guid.NewGuid(),
                TemplateId = "kiosk-standard-v1",
                TemplateName = "Kiosk Standard",
                Version = "1.0",
                JsonContent = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            advancedTemplate = new ConfigurationTemplateEntity
            {
                Id = Guid.NewGuid(),
                TemplateId = "kiosk-advanced-v1",
                TemplateName = "Kiosk Avanzato",
                Version = "1.0",
                JsonContent = "{}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.ConfigurationTemplates.AddRange(standardTemplate, advancedTemplate);
            context.SaveChanges();

            Log.Information("Template iniziali creati con successo");
        }
        else
        {
            // Recupera i template esistenti per le configurazioni di test
            standardTemplate = context.ConfigurationTemplates.FirstOrDefault(t => t.TemplateId == "kiosk-standard-v1");
            advancedTemplate = context.ConfigurationTemplates.FirstOrDefault(t => t.TemplateId == "kiosk-advanced-v1");
        }

        // Seed configurazioni di test se non esistono
        if (!context.KioskConfigurations.Any() && standardTemplate != null && advancedTemplate != null)
        {
            var testConfigs = new List<KioskConfigurationEntity>
            {
                new KioskConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    TemplateId = standardTemplate.Id,
                    SerialNumber = "KIOSK-2024-001",
                    Status = "Completed",
                    CompletedBy = "Admin",
                    CompletedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new KioskConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    TemplateId = advancedTemplate.Id,
                    SerialNumber = "KIOSK-2024-002",
                    Status = "InProgress",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new KioskConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    TemplateId = standardTemplate.Id,
                    SerialNumber = "KIOSK-2024-003",
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new KioskConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    TemplateId = standardTemplate.Id,
                    SerialNumber = "KIOSK-2024-004",
                    Status = "Approved",
                    CompletedBy = "Technician",
                    CompletedAt = DateTime.UtcNow.AddDays(-7),
                    ApprovedBy = "Manager",
                    ApprovedAt = DateTime.UtcNow.AddDays(-6),
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new KioskConfigurationEntity
                {
                    Id = Guid.NewGuid(),
                    TemplateId = advancedTemplate.Id,
                    SerialNumber = "KIOSK-2024-005",
                    Status = "Rejected",
                    CompletedBy = "Technician",
                    CompletedAt = DateTime.UtcNow.AddDays(-2),
                    RejectionReason = "Configurazione non conforme agli standard",
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            context.KioskConfigurations.AddRange(testConfigs);
            context.SaveChanges();

            Log.Information("Configurazioni di test create con successo: {Count}", testConfigs.Count);
        }
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

// Rimuovi HTTPS redirect per permettere HTTP su 5003
// app.UseHttpsRedirection();

app.UseStaticFiles();

// Abilita CORS prima del routing
app.UseCors("AllowPortal");

// Header per permettere iframe dal Platform.Portal
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("X-Frame-Options");
    context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'self' http://localhost:5000 https://localhost:5001");
    await next();
});

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Configuration}/{action=Index}/{id?}");

// Log startup
Log.Information("KioskConfiguration Application started");

app.Run();