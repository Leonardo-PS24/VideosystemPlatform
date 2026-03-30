using Microsoft.AspNetCore.SignalR;

namespace Platform.Portal.Hubs;

public class KioskHub : Hub
{
    // Questo metodo può essere chiamato dai client per unirsi a un "gruppo"
    // specifico per una checklist, in modo da ricevere aggiornamenti solo per quella.
    public async Task JoinChecklistGroup(string instanceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"checklist_{instanceId}");
    }
    
    public async Task LeaveChecklistGroup(string instanceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"checklist_{instanceId}");
    }
}