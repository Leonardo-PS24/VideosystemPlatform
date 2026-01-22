using Platform.Shared.Models;

namespace ConfigurationKiosk.Models;

public class KioskHistoryViewModel
{
    public KioskChecklistInstance Instance { get; set; } = null!;
    public List<KioskChecklistHistory> History { get; set; } = new();
}