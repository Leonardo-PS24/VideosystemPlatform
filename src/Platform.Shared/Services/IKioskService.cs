using Platform.Shared.Models;

namespace Platform.Shared.Services;

public interface IKioskService
{
    Task<List<KioskChecklistTemplate>> GetActiveTemplatesAsync();
    Task<List<KioskChecklistTemplate>> GetAllTemplatesAsync();
    Task<KioskChecklistTemplate?> GetTemplateByIdAsync(int id);
    
    Task<KioskChecklistInstance> CreateInstanceAsync(int templateId, string machineSerial, string userId);
    Task<KioskChecklistInstance?> GetInstanceByIdAsync(int id);
    
    // Salva i dati
    Task UpdateInstanceDataAsync(int instanceId, string dataJson, string userId);
    
    // Completa la prima volta
    Task CompleteInstanceAsync(int instanceId, string userId);
    
    // Avvia una revisione (Admin) - Cambia stato a InRevision
    Task StartRevisionAsync(int instanceId, string userId);
    
    // Finalizza una revisione (Admin) - Ritorna true se ci sono modifiche
    Task<bool> FinalizeRevisionAsync(int instanceId, string dataJson, string userId);

    // Invia la proposta di revisione per l'approvazione (cambia stato a PendingApproval)
    Task SubmitApprovalAsync(int instanceId, string userId);

    // Rifiuta la proposta di revisione (ripristina la versione precedente)
    Task RejectRevisionAsync(int instanceId, string userId);

    // Ottiene il confronto (Diff) tra la bozza attuale e l'ultima versione completata
    Task<List<KioskChecklistDiff>> GetInstanceDiffAsync(int instanceId);

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