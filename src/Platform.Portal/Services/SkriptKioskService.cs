using Microsoft.EntityFrameworkCore;
using Platform.Portal.Data;
using Platform.Shared.Models;
using Platform.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Platform.Portal.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Platform.Portal.Services;

public class SkriptKioskService : ISkriptKioskService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SkriptKioskService> _logger;
    private readonly IHubContext<KioskHub> _hubContext;

    public SkriptKioskService(ApplicationDbContext context, ILogger<SkriptKioskService> logger, IHubContext<KioskHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<List<SkriptKioskChecklistTemplate>> GetActiveTemplatesAsync()
    {
        return await _context.SkriptKioskChecklistTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<SkriptKioskChecklistTemplate>> GetAllTemplatesAsync()
    {
        return await _context.SkriptKioskChecklistTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<SkriptKioskChecklistTemplate?> GetTemplateByIdAsync(int id)
    {
        return await _context.SkriptKioskChecklistTemplates.FindAsync(id);
    }

    public async Task<SkriptKioskChecklistInstance> CreateInstanceAsync(int templateId, string machineSerial, string userId)
    {
        try
        {
            var instance = new SkriptKioskChecklistInstance
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

            _context.SkriptKioskChecklistInstances.Add(instance);
            await _context.SaveChangesAsync();

            await SafeLogHistoryAsync(instance.Id, "Created", "{}", "InProgress", userId, $"Creata nuova configurazione per {machineSerial}");
            await _hubContext.Clients.All.SendAsync("NewInstanceCreated", instance.Id, machineSerial);

            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione dell'istanza SkriptKiosk per TemplateId {TemplateId}", templateId);
            throw;
        }
    }

    public async Task<SkriptKioskChecklistInstance?> GetInstanceByIdAsync(int id)
    {
        return await _context.SkriptKioskChecklistInstances
            .Include(i => i.Template)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task UpdateInstanceDataAsync(int instanceId, string dataJson, string userId)
    {
        try
        {
            var instance = await _context.SkriptKioskChecklistInstances.FindAsync(instanceId);
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
            var instance = await _context.SkriptKioskChecklistInstances.FindAsync(instanceId);
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
            var instance = await _context.SkriptKioskChecklistInstances.FindAsync(instanceId);
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
            var instance = await _context.SkriptKioskChecklistInstances.FindAsync(instanceId);
            if (instance != null)
            {
                var lastCompleted = await _context.SkriptKioskChecklistHistories
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

    public async Task SubmitApprovalAsync(int instanceId, string userId)
    {
        try
        {
            var instance = await _context.SkriptKioskChecklistInstances.FindAsync(instanceId);
            if (instance != null && instance.Status == "InRevision")
            {
                instance.Status = "PendingApproval";
                instance.UpdatedBy = userId;
                instance.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                await SafeLogHistoryAsync(instanceId, "SubmitApproval", instance.DataJson, "PendingApproval", userId, "Proposta inviata per approvazione all'Admin.");
                await _hubContext.Clients.All.SendAsync("UpdateStatus", instanceId, "PendingApproval");
                await _hubContext.Clients.Group($"checklist_{instanceId}").SendAsync("DataUpdated", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante submit-approval per {InstanceId}", instanceId);
            throw;
        }
    }

    public async Task RejectRevisionAsync(int instanceId, string userId)
    {
        try
        {
            var instance = await _context.SkriptKioskChecklistInstances.FindAsync(instanceId);
            if (instance != null && (instance.Status == "InRevision" || instance.Status == "PendingApproval"))
            {
                var lastCompleted = await _context.SkriptKioskChecklistHistories
                    .Where(h => h.InstanceId == instanceId && h.Status == "Completed")
                    .OrderByDescending(h => h.Timestamp)
                    .FirstOrDefaultAsync();

                instance.DataJson = lastCompleted?.DataJson ?? instance.DataJson;
                instance.Status = "Completed";
                instance.Progress = 100;
                instance.UpdatedBy = userId;
                instance.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await SafeLogHistoryAsync(instanceId, "RevisionRejected", instance.DataJson, "Completed", userId, "Revisione rifiutata, ripristinata versione precedente.");
                await _hubContext.Clients.All.SendAsync("UpdateStatus", instanceId, "Completed");
                await _hubContext.Clients.Group($"checklist_{instanceId}").SendAsync("DataUpdated", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il rifiuto della revisione {InstanceId}", instanceId);
            throw;
        }
    }

    public async Task<List<KioskChecklistDiff>> GetInstanceDiffAsync(int instanceId)
    {
        try
        {
            var instance = await _context.SkriptKioskChecklistInstances
                .Include(i => i.Template)
                .FirstOrDefaultAsync(i => i.Id == instanceId);
            if (instance == null) return new List<KioskChecklistDiff>();

            var lastCompleted = await _context.SkriptKioskChecklistHistories
                .Where(h => h.InstanceId == instanceId && h.Status == "Completed")
                .OrderByDescending(h => h.Timestamp)
                .FirstOrDefaultAsync();

            string oldJson = lastCompleted?.DataJson ?? "{}";
            string newJson = instance.DataJson;

            var oldData = JsonSerializer.Deserialize<Dictionary<string, object>>(oldJson) ?? new();
            var newData = JsonSerializer.Deserialize<Dictionary<string, object>>(newJson) ?? new();

            var structure = JsonSerializer.Deserialize<TemplateStructureDto>(instance.Template.StructureJson ?? "{\"sections\":[]}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var fieldMap = new Dictionary<string, TemplateFieldDto>();

            if (structure?.Sections != null)
            {
                foreach (var sec in structure.Sections)
                {
                    if (sec.Fields != null)
                    {
                        foreach (var field in sec.Fields)
                        {
                            fieldMap[field.Id] = field;
                        }
                    }
                }
            }

            var diffList = new List<KioskChecklistDiff>();
            foreach (var key in newData.Keys)
            {
                if (key.StartsWith("_")) continue;
                var oldValStr = oldData.ContainsKey(key) ? oldData[key]?.ToString() ?? "" : "";
                var newValStr = newData[key]?.ToString() ?? "";

                if (oldValStr != newValStr)
                {
                    fieldMap.TryGetValue(key, out var f);
                    diffList.Add(new KioskChecklistDiff
                    {
                        FieldId = key,
                        Label = f?.Label ?? key,
                        OldValue = oldValStr,
                        NewValue = newValStr
                    });
                }
            }

            return diffList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il calcolo del diff per {InstanceId}", instanceId);
            return new List<KioskChecklistDiff>();
        }
    }

    public async Task<List<SkriptKioskChecklistInstance>> GetRecentInstancesAsync()
    {
        try
        {
            return await _context.SkriptKioskChecklistInstances
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
            var instance = await _context.SkriptKioskChecklistInstances.FindAsync(instanceId);
            if (instance != null)
            {
                try 
                {
                    var history = await _context.SkriptKioskChecklistHistories.Where(h => h.InstanceId == instanceId).ToListAsync();
                    _context.SkriptKioskChecklistHistories.RemoveRange(history);
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Errore eliminazione storico"); }
                
                _context.SkriptKioskChecklistInstances.Remove(instance);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("InstanceDeleted", instanceId);
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Errore eliminazione istanza"); throw; }
    }

    public async Task<List<SkriptKioskChecklistHistory>> GetInstanceHistoryAsync(int instanceId)
    {
        try
        {
            return await _context.SkriptKioskChecklistHistories
                .Where(h => h.InstanceId == instanceId)
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Errore storico"); return new List<SkriptKioskChecklistHistory>(); }
    }

    public async Task CreateTemplateAsync(SkriptKioskChecklistTemplate template, string userId)
    {
        template.CreatedBy = userId; template.CreatedAt = DateTime.UtcNow;
        _context.SkriptKioskChecklistTemplates.Add(template); await _context.SaveChangesAsync();
    }

    public async Task UpdateTemplateAsync(SkriptKioskChecklistTemplate template, string userId)
    {
        var existing = await _context.SkriptKioskChecklistTemplates.FindAsync(template.Id);
        if (existing != null)
        {
            existing.Name = template.Name; existing.Description = template.Description; existing.StructureJson = template.StructureJson; existing.IsActive = template.IsActive; existing.UpdatedBy = userId; existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _context.SkriptKioskChecklistTemplates.FindAsync(id);
        if (template != null) { 
            var hasInstances = await _context.SkriptKioskChecklistInstances.AnyAsync(i => i.TemplateId == id);
            if (hasInstances) template.IsActive = false; else _context.SkriptKioskChecklistTemplates.Remove(template);
            await _context.SaveChangesAsync(); 
        }
    }

    public async Task ToggleTemplateStatusAsync(int id)
    {
        var template = await _context.SkriptKioskChecklistTemplates.FindAsync(id);
        if (template != null) { template.IsActive = !template.IsActive; await _context.SaveChangesAsync(); }
    }

    private async Task SafeLogHistoryAsync(int instanceId, string action, string dataJson, string status, string userId, string notes)
    {
        try
        {
            var history = new SkriptKioskChecklistHistory { InstanceId = instanceId, Action = action, DataJson = dataJson, Status = status, UserId = userId, Timestamp = DateTime.UtcNow, Notes = notes };
            _context.SkriptKioskChecklistHistories.Add(history); await _context.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Errore log storico"); if(_context.ChangeTracker.Entries<SkriptKioskChecklistHistory>().Any()) _context.ChangeTracker.Entries<SkriptKioskChecklistHistory>().First().State = EntityState.Detached; }
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
