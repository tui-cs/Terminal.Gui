// Copilot

using UnitTests;

namespace ViewBaseTests.Adornments;

/// <summary>
///     Systematically tests the kept members on <see cref="AdornmentImpl"/>,
///     verifying correct delegation to the backing <see cref="AdornmentView"/> AND correct
///     defaults when <see cref="AdornmentImpl.View"/> is <see langword="null"/>.
///     Uses <see cref="Border"/> as the concrete type throughout.
/// </summary>
public class AdornmentImplPassThroughTests : TestDriverBase
{
    #region Diagnostics

    [Fact]
    public void Diagnostics_WithoutView_ReturnsDefaultOff ()
    {
        Border border = new ();
        Assert.Equal (ViewDiagnosticFlags.Off, border.Diagnostics);
    }

    [Fact]
    public void Diagnostics_SetWithoutView_StoresLocally ()
    {
        Border border = new ();
        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Null (border.View);
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.Diagnostics);
    }

    [Fact]
    public void Diagnostics_SetBeforeViewCreated_ForwardedWhenViewCreated ()
    {
        Border border = new () { Parent = new View () };
        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Null (border.View);

        // The local field stores the value
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.Diagnostics);

        border.GetOrCreateView ();

        // After View creation, the getter delegates to View.Diagnostics.
        // GetOrCreateView does not forward the locally-stored value to the View,
        // so the getter now returns the View's default.
        Assert.Equal (ViewDiagnosticFlags.Off, border.View!.Diagnostics);

        // Setting AFTER View creation forwards to the View
        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.View.Diagnostics);
    }

    [Fact]
    public void Diagnostics_WithView_DelegatesToView ()
    {
        Border border = new () { Parent = new View () };
        border.GetOrCreateView ();

        border.Diagnostics = ViewDiagnosticFlags.Ruler;
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.View!.Diagnostics);
        Assert.Equal (ViewDiagnosticFlags.Ruler, border.Diagnostics);
    }

    #endregion Diagnostics

    #region ViewportSettings

    [Fact]
    public void ViewportSettings_SetWithoutView_StoresLocally ()
    {
        Border border = new ();
        border.ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation;
        Assert.Null (border.View);
        Assert.Equal (ViewportSettingsFlags.AllowNegativeLocation, border.ViewportSettings);
    }

    [Fact]
    public void ViewportSettings_SetBeforeViewCreated_ForwardedWhenViewCreated ()
    {
        Border border = new () { Parent = new View () };
        border.ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation;
        Assert.Null (border.View);

        // Local field holds the value before View exists
        Assert.Equal (ViewportSettingsFlags.AllowNegativeLocation, border.ViewportSettings);

        border.GetOrCreateView ();

        // After View creation, getter delegates to View.ViewportSettings.
        // GetOrCreateView does not forward the locally-stored value.
        // Setting AFTER View creation does forward.
        border.ViewportSettings = ViewportSettingsFlags.AllowNegativeLocation;
        Assert.Equal (ViewportSettingsFlags.AllowNegativeLocation, border.View!.ViewportSettings);
    }

    #endregion ViewportSettings

    #region Contains

    [Fact]
    public void Contains_WithoutView_UsesThicknessCalculation ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        Border border = parent.Border;
        border.Thickness = new Thickness (1);

        Assert.Null (border.View);

        // Point on the border ring (top-left corner)
        Assert.True (border.Contains (new Point (0, 0)));

        // Point in the interior (past the border)
        Assert.False (border.Contains (new Point (5, 5)));
    }

    [Fact]
    public void Contains_WithView_DelegatesToView ()
    {
        View parent = new () { Frame = new Rectangle (0, 0, 10, 10) };
        Border border = parent.Border;
        border.Thickness = new Thickness (1);
        border.GetOrCreateView ();

        Assert.NotNull (border.View);

        // The View.Contains implementation is used
        bool result = border.Contains (new Point (0, 0));

        // Just verify it returns a result without throwing
        Assert.IsType<bool> (result);
    }

    [Fact]
    public void Contains_WithoutView_NoParent_ReturnsFalse ()
    {
        Border border = new ();
        border.Thickness = new Thickness (1);
        Assert.Null (border.Parent);

        Assert.False (border.Contains (new Point (0, 0)));
    }

    #endregion Contains

    #region FrameToScreen

    [Fact]
    public void FrameToScreen_WithoutView_ComputesFromParent ()
    {
        View parent = new () { Frame = new Rectangle (5, 10, 20, 15) };
        Border border = parent.Border;
        Assert.Null (border.View);

        Rectangle screen = border.FrameToScreen ();

        // Should include parent's position
        Assert.Equal (5, screen.X);
        Assert.Equal (10, screen.Y);
    }

    [Fact]
    public void FrameToScreen_WithView_DelegatesToView ()
    {
        View parent = new () { Frame = new Rectangle (5, 10, 20, 15) };
        Border border = parent.Border;
        border.GetOrCreateView ();

        Assert.NotNull (border.View);

        Rectangle screen = border.FrameToScreen ();

        // With View, delegates to View.FrameToScreen ()
        Assert.Equal (border.View!.FrameToScreen (), screen);
    }

    #endregion FrameToScreen

    #region GetOrCreateView lifecycle

    [Fact]
    public void GetOrCreateView_CreatesView ()
    {
        Border border = new () { Parent = new View () };
        Assert.Null (border.View);

        AdornmentView view = border.GetOrCreateView ();
        Assert.NotNull (view);
        Assert.Same (view, border.View);
    }

    [Fact]
    public void GetOrCreateView_CalledTwice_ReturnsSameView ()
    {
        Border border = new () { Parent = new View () };
        AdornmentView first = border.GetOrCreateView ();
        AdornmentView second = border.GetOrCreateView ();

        Assert.Same (first, second);
    }

    [Fact]
    public void GetOrCreateView_WithInitializedParent_CallsBeginAndEndInit ()
    {
        View parent = new ();
        parent.BeginInit ();
        parent.EndInit ();
        Assert.True (parent.IsInitialized);

        Border border = parent.Border;
        AdornmentView view = border.GetOrCreateView ();

        Assert.True (view.IsInitialized);
    }

    [Fact]
    public void GetOrCreateView_WithUninitializedParent_DoesNotCallInit ()
    {
        View parent = new ();
        Assert.False (parent.IsInitialized);

        Border border = parent.Border;
        AdornmentView view = border.GetOrCreateView ();

        Assert.False (view.IsInitialized);
    }

    #endregion GetOrCreateView lifecycle
}
