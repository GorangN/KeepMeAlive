using KeepMeAlive.Services.Interfaces;
using static KeepMeAlive.Helpers.Interop;

namespace KeepMeAlive.Services;

public class KeepAwakeService : IKeepAwakeService
{
    public bool IsEnabled { get; private set; }

    public void Enable()
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        IsEnabled = true;
    }

    public void Refresh() => Enable();

    public void Disable()
    {
        SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        IsEnabled = false;
    }
}
