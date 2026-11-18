using System.ComponentModel;
using JetBrains.Annotations;

namespace ApplicationTests.Popover;

/// <summary>
///     Contains unit tests for the ToolTipManager class.
/// </summary>
[TestSubject (typeof (ApplicationToolTip))]
public class ApplicationToolTipTests
{
    [Fact]
    public void SetToolTip_NullTarget_ThrowsArgumentNullException ()
    {
        ApplicationToolTip manager = new ();
        ToolTipProvider provider = new (() => new Label { Text = "Test" });

        Assert.Throws<ArgumentNullException> (() => manager.SetToolTip (null!, provider));
    }

    [Fact]
    public void SetToolTip_NullProvider_ThrowsArgumentNullException ()
    {
        ApplicationToolTip manager = new ();
        View view = new ();

        Assert.Throws<ArgumentNullException> (() => manager.SetToolTip (view, (string?)null!));
    }

    [Fact]
    public void SetToolTip_ValidParameters_SetsRemovesToolTip ()
    {
        ApplicationToolTip manager = new ();
        View view = new ();
        ToolTipProvider provider = new (() => new Label { Text = "Test" });

        manager.SetToolTip (view, provider);

        Assert.True (manager._registrations.ContainsKey (view));

        manager.RemoveToolTip (view);

        Assert.False (manager._registrations.ContainsKey (view));
    }

    [Fact]
    public void EnterView_ShowsHidesToolTip ()
    {
        using IApplication app = Application.Create ().Init ();

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();
        window.BorderStyle = LineStyle.None;

        View view = new ();
        window.Add (view);
        View toolTipContent = new ();

        app.ToolTips!.SetToolTip (view, () => toolTipContent);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        // Simulate mouse enter event
        CancelEventArgs eventArgs = new ();
        _ = view.NewMouseEnterEvent (eventArgs);

        Assert.True (toolTipContent.Visible);

        // The tooltip host of the view should be visible
        Assert.True (toolTipContent.SuperView!.Visible);

        // Simulate mouse leave event
        view.NewMouseLeaveEvent ();

        Assert.False (toolTipContent.Visible);

        // The tooltip host of the view should be hidden
        Assert.False (toolTipContent.SuperView!.Visible);
    }
}
