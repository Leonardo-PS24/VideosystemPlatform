using Platform.Shared.Models;

namespace ConfigurationKiosk.Models;

public class KioskDashboardViewModel
{
    public List<KioskChecklistInstance> RecentInstances { get; set; } = new();
    public List<KioskChecklistTemplate> AvailableTemplates { get; set; } = new();
}