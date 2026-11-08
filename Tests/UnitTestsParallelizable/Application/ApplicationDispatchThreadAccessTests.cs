// CoPilot - GPT-6.1 Sol
namespace ApplicationTests;

/// <summary>Runs the complete dispatch regression suite with per-application thread checks enabled.</summary>
[Collection ("Application Tests")]
public sealed class ApplicationDispatchThreadAccessTests : ApplicationDispatchTests
{
    protected override IApplication CreateApplication () => Application.Create ().EnableThreadAccessChecks ();
}
