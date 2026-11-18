// Copilot

namespace ViewsTests;

public class TextViewMouseSelectionBindingsTests
{
    [Fact]
    public void TextView_SelectionStartMouseBindings_UsePlatformDefaults ()
    {
        TextView textView = new ();

        MouseFlags expectedStartSelection = OperatingSystem.IsWindows ()
                                                ? MouseFlags.LeftButtonPressed | MouseFlags.Alt
                                                : MouseFlags.LeftButtonPressed | MouseFlags.Shift;

        MouseFlags expectedStartRectangleSelection = OperatingSystem.IsWindows ()
                                                         ? MouseFlags.LeftButtonPressed | MouseFlags.Alt | MouseFlags.Ctrl
                                                         : MouseFlags.LeftButtonPressed | MouseFlags.Shift | MouseFlags.Ctrl;

        Assert.True (textView.MouseBindings.TryGet (expectedStartSelection, out MouseBinding startSelectionBinding));
        Assert.Contains (Command.StartSelection, startSelectionBinding.Commands);

        Assert.True (textView.MouseBindings.TryGet (expectedStartRectangleSelection, out MouseBinding startRectangleSelectionBinding));
        Assert.Contains (Command.StartRectangleSelection, startRectangleSelectionBinding.Commands);
    }
}
