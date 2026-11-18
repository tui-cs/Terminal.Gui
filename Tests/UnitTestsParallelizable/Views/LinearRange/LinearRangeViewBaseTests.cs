using UnitTests;

namespace ViewsTests;

/// <summary>
///     Shared base behavior tests, exercised through <see cref="LinearSelector{T}"/>
///     (the simplest concrete subclass).
/// </summary>
public class LinearRangeViewBaseTests : TestDriverBase
{
    [Fact]
    public void MovePlus_Should_MoveFocusRight_When_OptionIsAvailable ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        bool result = sel.MovePlus ();

        Assert.True (result);
        Assert.Equal (1, sel.FocusedOption);
    }

    [Fact]
    public void MovePlus_Should_NotMoveFocusRight_When_AtEnd ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        sel.FocusedOption = 3;

        bool result = sel.MovePlus ();

        Assert.False (result);
        Assert.Equal (3, sel.FocusedOption);
    }

    [Fact]
    public void OnOptionFocused_Event_Cancelled ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        var eventRaised = false;
        sel.OptionFocused += (sender, args) => eventRaised = true;
        var newFocusedOption = 1;

        LinearRangeEventArgs<int> args = new (new Dictionary<int, LinearRangeOption<int>> (), newFocusedOption) { Cancel = false };
        Assert.Equal (0, sel.FocusedOption);

        sel.OnOptionFocused (newFocusedOption, args);

        Assert.True (eventRaised);
        Assert.Equal (newFocusedOption, sel.FocusedOption);

        args = new LinearRangeEventArgs<int> (new Dictionary<int, LinearRangeOption<int>> (), newFocusedOption) { Cancel = true };

        sel.OnOptionFocused (2, args);

        Assert.True (eventRaised);
        Assert.Equal (newFocusedOption, sel.FocusedOption);
    }

    [Fact]
    public void OnOptionFocused_Event_Raised ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        var eventRaised = false;
        sel.OptionFocused += (sender, args) => eventRaised = true;
        var newFocusedOption = 1;
        LinearRangeEventArgs<int> args = new (new Dictionary<int, LinearRangeOption<int>> (), newFocusedOption);

        sel.OnOptionFocused (newFocusedOption, args);

        Assert.True (eventRaised);
    }

    [Fact]
    public void Set_Should_Not_Clear_When_EmptyNotAllowed ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]) { AllowEmpty = false };

        Assert.NotEmpty (sel.SelectedIndices);

        // Re-activating the same focused option must not clear it when AllowEmpty=false.
        sel.InvokeCommand (Command.Activate);

        Assert.NotEmpty (sel.SelectedIndices);
    }

    [Fact]
    public void Set_Should_SetFocusedOption ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        sel.FocusedOption = 2;
        bool result = sel.InvokeCommand (Command.Activate) ?? false;

        Assert.Equal (2, sel.FocusedOption);
        Assert.Single (sel.SelectedIndices);
    }

    [Fact]
    public void TryGetOptionByPosition_InvalidPosition_Failure ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        var x = 10;
        var y = 10;
        var threshold = 2;
        int expectedOption = -1;

        bool result = sel.TryGetOptionByPosition (x, y, threshold, out int option);

        Assert.False (result);
        Assert.Equal (expectedOption, option);
    }

    [Theory]
    [InlineData (0, 0, 0, 1)]
    [InlineData (3, 0, 0, 2)]
    [InlineData (9, 0, 0, 4)]
    [InlineData (0, 0, 1, 1)]
    [InlineData (3, 0, 1, 2)]
    [InlineData (9, 0, 1, 4)]
    public void TryGetOptionByPosition_ValidPositionHorizontal_Success (int x, int y, int threshold, int expectedData)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetOptionByPosition (x, y, threshold, out int option);

        Assert.True (result);
        Assert.Equal (expectedData, sel.Options [option].Data);
    }

    [Theory]
    [InlineData (0, 0, 0, 1)]
    [InlineData (0, 3, 0, 2)]
    [InlineData (0, 9, 0, 4)]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 3, 1, 2)]
    [InlineData (0, 9, 1, 4)]
    public void TryGetOptionByPosition_ValidPositionVertical_Success (int x, int y, int threshold, int expectedData)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);
        sel.Orientation = Orientation.Vertical;
        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetOptionByPosition (x, y, threshold, out int option);

        Assert.True (result);
        Assert.Equal (expectedData, sel.Options [option].Data);
    }

    [Fact]
    public void TryGetPositionByOption_InvalidOption_Failure ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        int option = -1;
        (int, int) expectedPosition = (-1, -1);

        bool result = sel.TryGetPositionByOption (option, out (int x, int y) position);

        Assert.False (result);
        Assert.Equal (expectedPosition, position);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 3, 0)]
    [InlineData (3, 9, 0)]
    public void TryGetPositionByOption_ValidOptionHorizontal_Success (int option, int expectedX, int expectedY)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);
        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetPositionByOption (option, out (int x, int y) position);

        Assert.True (result);
        Assert.Equal (expectedX, position.x);
        Assert.Equal (expectedY, position.y);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 0, 3)]
    [InlineData (3, 0, 9)]
    public void TryGetPositionByOption_ValidOptionVertical_Success (int option, int expectedX, int expectedY)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);
        sel.Orientation = Orientation.Vertical;
        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetPositionByOption (option, out (int x, int y) position);

        Assert.True (result);
        Assert.Equal (expectedX, position.x);
        Assert.Equal (expectedY, position.y);
    }

    [Fact]
    private void DimAuto_Both_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        List<object> options = ["01234", "01234"];

        LinearMultiSelector<object> ms = new (options) { Orientation = Orientation.Vertical };
        view.Add (ms);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = ms.Frame.Size;

        Assert.Equal (new Size (6, 3), expectedSize);

        view.SetContentSize (new Size (1, 1));

        view.LayoutSubViews ();
        ms.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, ms.Frame.Size);
    }

    [Fact]
    private void DimAuto_Height_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = 10, Height = Dim.Fill () };

        List<object> options = ["01234", "01234"];

        LinearMultiSelector<object> ms = new (options) { Orientation = Orientation.Vertical, Width = 10 };
        view.Add (ms);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = ms.Frame.Size;

        Assert.Equal (new Size (10, 3), expectedSize);

        view.SetContentSize (new Size (1, 1));

        view.LayoutSubViews ();
        ms.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, ms.Frame.Size);
    }

    [Fact]
    private void DimAuto_Width_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = Dim.Fill (), Height = 10 };

        List<object> options = ["01234", "01234"];

        LinearMultiSelector<object> ms = new (options) { Orientation = Orientation.Vertical, Height = 10 };
        view.Add (ms);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = ms.Frame.Size;

        Assert.Equal (new Size (6, 10), expectedSize);

        view.SetContentSize (new Size (1, 1));

        view.LayoutSubViews ();
        ms.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, ms.Frame.Size);
    }

    // https://github.com/tui-cs/Terminal.Gui/issues/3099
    [Fact]
    private void One_Option_Does_Not_Throw ()
    {
        LinearSelector<int> sel = new ();
        sel.BeginInit ();
        sel.EndInit ();

        sel.Options = [new LinearRangeOption<int> ()];
    }
}
