using Platform.Shared.Models;

namespace Platform.Shared.Services;

public interface IKioskService
{
    Task<List<KioskChecklistTemplate>> GetActiveTemplatesAsync();
    Task<List<KioskChecklistTemplate>> GetAllTemplatesAsync();
    Task<KioskChecklistTemplate?> GetTemplateByIdAsync(int id);
    
    Task<KioskChecklistInstance> CreateInstanceAsync(int templateId, string machineSerial, string userId);
    Task<KioskChecklistInstance?> GetInstanceByIdAsync(int id);
    Task UpdateInstanceDataAsync(int instanceId, string dataJson, string status, string userId);
    Task<List<KioskChecklistInstance>> GetRecentInstancesAsync();
    
    // Gestione Storico e Cancellazione
    Task DeleteInstanceAsync(int instanceId);
    Task<List<KioskChecklistHistory>> GetInstanceHistoryAsync(int instanceId);
    
    // Metodi Admin Template
    Task CreateTemplateAsync(KioskChecklistTemplate template, string userId);
    Task UpdateTemplateAsync(KioskChecklistTemplate template, string userId);
    Task DeleteTemplateAsync(int id);
    Task ToggleTemplateStatusAsync(int id);
}