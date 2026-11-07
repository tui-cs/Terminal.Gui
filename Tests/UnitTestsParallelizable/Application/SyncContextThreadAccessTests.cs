// CoPilot - GPT-6.1 Sol
namespace ApplicationTests;

/// <summary>Runs the synchronization-context lifecycle regression suite with thread checks enabled.</summary>
[Collection ("Application Tests")]
public sealed class SyncContextThreadAccessTests : SyncContextLifecycleTests
{
    protected override IApplication CreateApplication () => Application.Create ().EnableThreadAccessChecks ();
}
