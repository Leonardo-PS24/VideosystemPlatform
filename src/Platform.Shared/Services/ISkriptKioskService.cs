using Platform.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Platform.Shared.Services;

public interface ISkriptKioskService
{
    Task<List<SkriptKioskChecklistTemplate>> GetActiveTemplatesAsync();
    Task<List<SkriptKioskChecklistTemplate>> GetAllTemplatesAsync();
    Task<SkriptKioskChecklistTemplate?> GetTemplateByIdAsync(int id);
    
    Task<SkriptKioskChecklistInstance> CreateInstanceAsync(int templateId, string machineSerial, string userId);
    Task<SkriptKioskChecklistInstance?> GetInstanceByIdAsync(int id);
    
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

    Task<List<SkriptKioskChecklistInstance>> GetRecentInstancesAsync();
    
    // Gestione Storico e Cancellazione
    Task DeleteInstanceAsync(int instanceId);
    Task<List<SkriptKioskChecklistHistory>> GetInstanceHistoryAsync(int instanceId);
    
    // Metodi Admin Template
    Task CreateTemplateAsync(SkriptKioskChecklistTemplate template, string userId);
    Task UpdateTemplateAsync(SkriptKioskChecklistTemplate template, string userId);
    Task DeleteTemplateAsync(int id);
    Task ToggleTemplateStatusAsync(int id);
}
