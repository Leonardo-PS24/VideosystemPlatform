using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;
using Platform.Shared.Models;
using Platform.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Platform.Portal.Hubs;

namespace Platform.Portal.Services;

public class KioskService : IKioskService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KioskService> _logger;
    private readonly IHubContext<KioskHub> _hubContext;

    public KioskService(ApplicationDbContext context, ILogger<KioskService> logger, IHubContext<KioskHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
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
                DataJson = "{}",
                Progress = 0,
                Revision = 0
            };

            _context.KioskChecklistInstances.Add(instance);
            await _context.SaveChangesAsync();

            await SafeLogHistoryAsync(instance.Id, "Created", "{}", "InProgress", userId, $"Creata nuova configurazione per {machineSerial}");
            await _hubContext.Clients.All.SendAsync("NewInstanceCreated", instance.Id, machineSerial);

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

    public async Task UpdateInstanceDataAsync(int instanceId, string dataJson, string userId)
    {
        try
        {
            var instance = await _context.KioskChecklistInstances.FindAsync(instanceId);
            if (instance != null)
            {
                try 
                {
                    using var doc = JsonDocument.Parse(dataJson);
                    if (doc.RootElement.TryGetProperty("_progressPercent", out var progressProp))
                    {
                        instance.Progress = progressProp.GetInt32();
                    }
                } 
                catch (Exception jsonEx) 
                {
                    _logger.LogWarning(jsonEx, "Impossibile estrarre _progressPercent dal JSON per istanza {Id}", instanceId);
                }

                if (instance.Status == "Completed") instance.Progress = 100;

                instance.DataJson = dataJson;
                instance.UpdatedBy = userId;
                instance.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("UpdateProgress", instanceId, instance.Progress);
                await _hubContext.Clients.Group($"checklist_{instanceId}").SendAsync("DataUpdated", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento dell'istanza {InstanceId}", instanceId);
            throw;
        }
    }

    public async Task CompleteInstanceAsync(int instanceId, string userId)
    {
        try
        {
            var instance = await _context.KioskChecklistInstances.FindAsync(instanceId);
            if (instance != null && instance.Status != "Completed")
            {
                instance.Status = "Completed";
                instance.Progress = 100;
                instance.CompletedAt = DateTime.UtcNow;
                instance.UpdatedBy = userId;
                instance.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await SafeLogHistoryAsync(instanceId, "Completed", instance.DataJson, "Completed", userId, "Configurazione terminata e bloccata.");
                await _hubContext.Clients.All.SendAsync("InstanceCompleted", instanceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il completamento dell'istanza {InstanceId}", instanceId);
            throw;
        }
    }

    public async Task StartRevisionAsync(int instanceId, string userId)
    {
        try
        {
            var instance = await _context.KioskChecklistInstances.FindAsync(instanceId);
            if (instance != null && instance.Status == "Completed")
            {
                instance.Status = "InRevision";
                instance.UpdatedBy = userId;
                instance.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                await _hubContext.Clients.All.SendAsync("UpdateStatus", instanceId, "InRevision");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante avvio revisione {InstanceId}", instanceId);
            throw;
        }
    }

    public async Task<bool> FinalizeRevisionAsync(int instanceId, string dataJson, string userId)
    {
        try
        {
            var instance = await _context.KioskChecklistInstances.FindAsync(instanceId);
            if (instance != null)
            {
                var lastCompleted = await _context.KioskChecklistHistories
                    .Where(h => h.InstanceId == instanceId && h.Status == "Completed")
                    .OrderByDescending(h => h.Timestamp)
                    .FirstOrDefaultAsync();

                string oldJson = lastCompleted?.DataJson ?? instance.DataJson;
                string diff = GenerateDiffNotes(oldJson, dataJson);

                if (diff == "Nessuna modifica rilevata")
                {
                    instance.Status = "Completed";
                    instance.UpdatedBy = userId;
                    instance.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("InstanceCompleted", instanceId);
                    return false; // Nessuna modifica
                }

                instance.Revision++;
                instance.DataJson = dataJson;
                instance.Status = "Completed";
                instance.Progress = 100;
                instance.UpdatedBy = userId;
                instance.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await SafeLogHistoryAsync(instanceId, $"Revision {instance.Revision}", dataJson, "Completed", userId, diff);

                await _hubContext.Clients.All.SendAsync("UpdateProgress", instanceId, 100);
                await _hubContext.Clients.All.SendAsync("InstanceCompleted", instanceId);
                await _hubContext.Clients.Group($"checklist_{instanceId}").SendAsync("RevisionFinalized", instance.Revision);
                return true; // Modifiche salvate
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la finalizzazione revisione {InstanceId}", instanceId);
            throw;
        }
        return false;
    }

    // ... (altri metodi invariati) ...
    public async Task<List<KioskChecklistInstance>> GetRecentInstancesAsync()
    {
        try
        {
            return await _context.KioskChecklistInstances
                .Include(i => i.Template)
                .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                .Take(20)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eccezione in GetRecentInstancesAsync.");
            throw;
        }
    }

    public async Task DeleteInstanceAsync(int instanceId)
    {
        try
        {
            var instance = await _context.KioskChecklistInstances.FindAsync(instanceId);
            if (instance != null)
            {
                try 
                {
                    var history = await _context.KioskChecklistHistories.Where(h => h.InstanceId == instanceId).ToListAsync();
                    _context.KioskChecklistHistories.RemoveRange(history);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Errore eliminazione storico"); }
                
                _context.KioskChecklistInstances.Remove(instance);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("InstanceDeleted", instanceId);
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Errore eliminazione istanza"); throw; }
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
        catch (Exception ex) { _logger.LogError(ex, "Errore storico"); return new List<KioskChecklistHistory>(); }
    }

    // Admin Template Methods
    public async Task CreateTemplateAsync(KioskChecklistTemplate template, string userId)
    {
        template.CreatedBy = userId; template.CreatedAt = DateTime.UtcNow;
        _context.KioskChecklistTemplates.Add(template); await _context.SaveChangesAsync();
    }
    public async Task UpdateTemplateAsync(KioskChecklistTemplate template, string userId)
    {
        var existing = await _context.KioskChecklistTemplates.FindAsync(template.Id);
        if (existing != null)
        {
            existing.Name = template.Name; existing.Description = template.Description; existing.StructureJson = template.StructureJson; existing.IsActive = template.IsActive; existing.UpdatedBy = userId; existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _context.KioskChecklistTemplates.FindAsync(id);
        if (template != null) { 
            var hasInstances = await _context.KioskChecklistInstances.AnyAsync(i => i.TemplateId == id);
            if (hasInstances) template.IsActive = false; else _context.KioskChecklistTemplates.Remove(template);
            await _context.SaveChangesAsync(); 
        }
    }
    public async Task ToggleTemplateStatusAsync(int id)
    {
        var template = await _context.KioskChecklistTemplates.FindAsync(id);
        if (template != null) { template.IsActive = !template.IsActive; await _context.SaveChangesAsync(); }
    }

    private async Task SafeLogHistoryAsync(int instanceId, string action, string dataJson, string status, string userId, string notes)
    {
        try
        {
            var history = new KioskChecklistHistory { InstanceId = instanceId, Action = action, DataJson = dataJson, Status = status, UserId = userId, Timestamp = DateTime.UtcNow, Notes = notes };
            _context.KioskChecklistHistories.Add(history); await _context.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Errore log storico"); if(_context.ChangeTracker.Entries<KioskChecklistHistory>().Any()) _context.ChangeTracker.Entries<KioskChecklistHistory>().First().State = EntityState.Detached; }
    }

    private string GenerateDiffNotes(string oldJson, string newJson)
    {
        try
        {
            var oldData = JsonSerializer.Deserialize<Dictionary<string, object>>(oldJson);
            var newData = JsonSerializer.Deserialize<Dictionary<string, object>>(newJson);
            if (oldData == null || newData == null) return "Dati non validi";
            var changes = new List<string>();
            foreach (var key in newData.Keys) {
                if (key.StartsWith("_")) continue;
                var oldVal = oldData.ContainsKey(key) ? oldData[key]?.ToString() : "";
                var newVal = newData[key]?.ToString();
                if (!string.Equals(oldVal, newVal, StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add($"'{key}': {oldVal} -> {newVal}");
                }
            }
            return changes.Any() ? "Modifiche: " + string.Join(", ", changes) : "Nessuna modifica rilevata";
        }
        catch
        {
            return "Impossibile calcolare le differenze";
        }
    }
}