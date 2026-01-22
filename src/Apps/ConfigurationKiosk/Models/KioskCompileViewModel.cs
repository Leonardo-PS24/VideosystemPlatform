using Platform.Shared.Models;

namespace ConfigurationKiosk.Models;

public class KioskCompileViewModel
{
    public KioskChecklistInstance Instance { get; set; } = null!;
    public KioskChecklistTemplate Template { get; set; } = null!;
}