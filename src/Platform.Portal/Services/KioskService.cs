using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;
using Platform.Shared.Models;
using Platform.Shared.Services;
using Microsoft.Extensions.Logging; // Aggiunto per logging

namespace Platform.Portal.Services;

public class KioskService : IKioskService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KioskService> _logger; // Logger

    public KioskService(ApplicationDbContext context, ILogger<KioskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<KioskChecklistTemplate>> GetActiveTemplatesAsync()
    {
        return await _context.KioskChecklistTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<KioskChecklistTemplate>> GetAllTemplatesAsync()
    {
        return await _context.KioskChecklistTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<KioskChecklistTemplate?> GetTemplateByIdAsync(int id)
    {
        return await _context.KioskChecklistTemplates.FindAsync(id);
    }

    public async Task<KioskChecklistInstance> CreateInstanceAsync(int templateId, string machineSerial, string userId)
    {
        try
        {
            var instance = new KioskChecklistInstance
            {
                TemplateId = templateId,
                MachineSerialNumber = machineSerial,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                Status = "InProgress",
                DataJson = "{}"
            };

            _context.KioskChecklistInstances.Add(instance);
            await _context.SaveChangesAsync();

            // Log creazione (Safe)
            await SafeLogHistoryAsync(instance.Id, "Created", "{}", "InProgress", userId, $"Creata nuova configurazione per {machineSerial}");

            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione dell'istanza Kiosk per TemplateId {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<KioskChecklistInstance?> GetInstanceByIdAsync(int id)
    {
        return await _context.KioskChecklistInstances
            .Include(i => i.Template)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task UpdateInstanceDataAsync(int instanceId, string dataJson, string status, string userId)
    {
        try
        {
            var instance = await _context.KioskChecklistInstances.FindAsync(instanceId);
            if (instance != null)
            {
                bool hasChanges = instance.DataJson != dataJson || instance.Status != status;
                
                if (hasChanges)
                {
                    instance.DataJson = dataJson;
                    instance.Status = status;
                    instance.UpdatedBy = userId;
                    instance.UpdatedAt = DateTime.UtcNow;
                    
                    if (status == "Completed" && instance.CompletedAt == null)
                    {
                        instance.CompletedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    // Log modifica (Safe)
                    string action = status == "Completed" ? "Completed" : "Updated";
                    await SafeLogHistoryAsync(instanceId, action, dataJson, status, userId, "Aggiornamento dati");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento dell'istanza {InstanceId}", instanceId);
            throw;
        }
    }

    public async Task<List<KioskChecklistInstance>> GetRecentInstancesAsync()
    {
        return await _context.KioskChecklistInstances
            .Include(i => i.Template)
            .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task DeleteInstanceAsync(int instanceId)
    {
        try
        {
            var instance = await _context.KioskChecklistInstances.FindAsync(instanceId);
            if (instance != null)
            {
                // Prova a rimuovere lo storico, ma se fallisce (es. tabella non esiste) ignora
                try 
                {
                    var history = await _context.KioskChecklistHistories.Where(h => h.InstanceId == instanceId).ToListAsync();
                    _context.KioskChecklistHistories.RemoveRange(history);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Impossibile eliminare lo storico per istanza {InstanceId} (tabella mancante?)", instanceId);
                }
                
                _context.KioskChecklistInstances.Remove(instance);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'eliminazione dell'istanza {InstanceId}", instanceId);
            throw;
        }
    }

    public async Task<List<KioskChecklistHistory>> GetInstanceHistoryAsync(int instanceId)
    {
        try
        {
            return await _context.KioskChecklistHistories
                .Where(h => h.InstanceId == instanceId)
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dello storico per istanza {InstanceId}", instanceId);
            // Ritorna lista vuota invece di crashare se la tabella non esiste
            return new List<KioskChecklistHistory>();
        }
    }

    // Metodi Admin Template
    public async Task CreateTemplateAsync(KioskChecklistTemplate template, string userId)
    {
        template.CreatedBy = userId;
        template.CreatedAt = DateTime.UtcNow;
        _context.KioskChecklistTemplates.Add(template);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTemplateAsync(KioskChecklistTemplate template, string userId)
    {
        var existing = await _context.KioskChecklistTemplates.FindAsync(template.Id);
        if (existing != null)
        {
            existing.Name = template.Name;
            existing.Description = template.Description;
            existing.StructureJson = template.StructureJson;
            existing.IsActive = template.IsActive;
            existing.UpdatedBy = userId;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _context.KioskChecklistTemplates.FindAsync(id);
        if (template != null)
        {
            var hasInstances = await _context.KioskChecklistInstances.AnyAsync(i => i.TemplateId == id);
            if (hasInstances)
            {
                template.IsActive = false;
            }
            else
            {
                _context.KioskChecklistTemplates.Remove(template);
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task ToggleTemplateStatusAsync(int id)
    {
        var template = await _context.KioskChecklistTemplates.FindAsync(id);
        if (template != null)
        {
            template.IsActive = !template.IsActive;
            await _context.SaveChangesAsync();
        }
    }

    // Helper privato per loggare in modo sicuro (senza crashare se manca la tabella)
    private async Task SafeLogHistoryAsync(int instanceId, string action, string dataJson, string status, string userId, string notes)
    {
        try
        {
            var history = new KioskChecklistHistory
            {
                InstanceId = instanceId,
                Action = action,
                DataJson = dataJson,
                Status = status,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                Notes = notes
            };
            _context.KioskChecklistHistories.Add(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Logga l'errore ma NON rilanciarlo, così l'operazione principale (Create/Update) non fallisce
            _logger.LogWarning(ex, "Impossibile salvare lo storico per istanza {InstanceId}. Probabilmente manca la migrazione del DB.", instanceId);
            
            // Rimuovi l'entità dal context per evitare che blocchi i futuri SaveChanges
            var entry = _context.ChangeTracker.Entries<KioskChecklistHistory>().FirstOrDefault();
            if (entry != null)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}