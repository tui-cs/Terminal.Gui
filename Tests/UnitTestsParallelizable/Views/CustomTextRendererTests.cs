using UnitTests;

namespace ViewsTests;

public class CustomTextRendererTests : TestDriverBase
{
    // CoPilot - GPT-5.6
    [Fact]
    public void ColorPicker_SuppressesGenericTextRendering ()
    {
        ColorPicker colorPicker = new () { Width = 32, Height = 5, Value = Color.BrightRed };

        AssertGenericTextRenderingIsSuppressed (colorPicker);
    }

    // CoPilot - GPT-5.6
    [Fact]
    public void LinearRangeViewBase_SuppressesGenericTextRendering ()
    {
        LinearSelector<string> linearSelector = new () { Width = 20, Height = 3, Text = "One,Two,Three" };

        AssertGenericTextRenderingIsSuppressed (linearSelector);
    }

    // CoPilot - GPT-5.6
    [Fact]
    public void ProgressBar_SuppressesGenericTextRendering ()
    {
        ProgressBar progressBar = new () { Width = 10, Height = 3, Fraction = 0.5F };

        AssertGenericTextRenderingIsSuppressed (progressBar);
    }

    // CoPilot - GPT-5.6
    [Fact]
    public void TextView_SuppressesGenericTextRendering ()
    {
        TextView textView = new () { Width = 10, Height = 3, Text = "custom text" };

        AssertGenericTextRenderingIsSuppressed (textView);
    }

    // CoPilot - GPT-5.6
    [Fact]
    public void Code_SuppressesGenericTextRendering ()
    {
        Code code = new () { Width = 10, Height = 3, Language = "cs", Text = "int answer = 42;" };

        AssertGenericTextRenderingIsSuppressed (code);
    }

    private void AssertGenericTextRenderingIsSuppressed (View view)
    {
        using (view)
        using (IDriver driver = CreateTestDriver (40, 8))
        {
            driver.Clip = new (driver.Screen);
            view.Driver = driver;

            bool drawingTextRaised = false;
            bool drewTextRaised = false;
            view.DrawingText += (_, _) => drawingTextRaised = true;
            view.DrewText += (_, _) => drewTextRaised = true;

            view.BeginInit ();
            view.EndInit ();
            view.LayoutSubViews ();

            Assert.NotEmpty (view.Text);

            view.Draw ();

            Assert.False (drawingTextRaised);
            Assert.False (drewTextRaised);
        }
    }
}
