// Claude - Opus 4.8
// All-views render-fingerprint harness for issue #4522 verification. Draws every concrete View type
// (via EnableForDesign) onto a test driver and emits a hash of the rendered screen per view. Used to
// diff rendering before/after the LayoutSubViews content-size change: identical fingerprints prove the
// change is render-neutral across all views. Kept as a coarse all-views smoke test.

using System.Security.Cryptography;
using System.Text;
using UnitTests;

namespace ViewBaseTests.Layout;

public class AllViewsRenderFingerprintTests (ITestOutputHelper output) : TestsAllViews
{
    private static string Hash (string s)
    {
        byte [] bytes = SHA256.HashData (Encoding.UTF8.GetBytes (s));

        return Convert.ToHexString (bytes) [..16];
    }

    [Fact]
    public void AllViews_Render_Fingerprint ()
    {
        List<Type> types = GetAllViewClasses ()
                           .Where (t => !t.IsGenericType)
                           .OrderBy (t => t.FullName, StringComparer.Ordinal)
                           .ToList ();

        StringBuilder combined = new ();
        List<string> threw = [];
        int successful = 0;

        foreach (Type type in types)
        {
            string line = FingerprintOne (type);
            output.WriteLine (line);
            combined.AppendLine (line);

            if (line.Contains ("|EX:"))
            {
                threw.Add (line);
            }
            else if (!line.Contains ("|GENERIC") && !line.Contains ("|ENV:"))
            {
                successful++;
            }
        }

        output.WriteLine ($"--- {successful} view types successfully rendered ---");
        output.WriteLine ($"COMBINED={Hash (combined.ToString ())}");

        // Guarantee we're actually exercising a meaningful number of concrete view types.
        Assert.True (successful > 30, $"Expected to render many view types, got {successful}.");

        // No concrete view may throw while being laid out and drawn in design mode.
        Assert.True (threw.Count == 0, $"View(s) threw during layout/draw:\n{string.Join ("\n", threw)}");
    }

    private static string FingerprintOne (Type type)
    {
        IDriver driver = CreateTestDriver (60, 20);

        View? view = CreateInstanceIfNotGeneric (type);

        if (view is null)
        {
            driver.Dispose ();

            return $"{type.FullName}|GENERIC";
        }

        view.Driver = driver;

        // EnableForDesign for filesystem-interactive views (FileDialog family) attempts to list
        // directories during design setup or the later initialization/layout pass. That is an
        // environment constraint, not a layout bug — separate it from layout/draw exceptions.
        try
        {
            if (view is IDesignable designable)
            {
                designable.EnableForDesign ();
            }
        }
        catch (Exception ex) when (IsFileDialogEnvironmentException (view, ex))
        {
            view.Dispose ();
            driver.Dispose ();

            return $"{type.FullName}|ENV:{ex.GetType ().Name}";
        }

        try
        {
            view.BeginInit ();
            view.EndInit ();
            view.Layout ();

            string fingerprint = $"frame={view.Frame};needsLayout={view.NeedsLayout}";

            if (view.Visible)
            {
                view.SetNeedsDraw ();
                view.Draw ();
                fingerprint += $";screen={Hash (driver.ToString () ?? string.Empty)}";
            }

            view.Dispose ();
            driver.Dispose ();

            return $"{type.FullName}|{fingerprint}";
        }
        catch (Exception ex) when (IsFileDialogEnvironmentException (view, ex))
        {
            view.Dispose ();
            driver.Dispose ();

            return $"{type.FullName}|ENV:{ex.GetType ().Name}";
        }
        catch (Exception ex)
        {
            view.Dispose ();
            driver.Dispose ();

            return $"{type.FullName}|EX:{ex.GetType ().Name}";
        }
    }

    private static bool IsFileDialogEnvironmentException (View view, Exception ex) =>
        view is FileDialog && ex is UnauthorizedAccessException or IOException;
}
