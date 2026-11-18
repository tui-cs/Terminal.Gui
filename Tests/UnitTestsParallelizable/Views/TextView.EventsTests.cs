using UnitTests;

namespace ViewsTests.TextViewTests;

public class TextViewEventsTests (ITestOutputHelper output) : TestDriverBase
{
    // CoPilot - Test to prove bug #3990 exists
    [Fact]
    public void ContentsChanged_Should_Not_Fire_On_Initialization ()
    {
        using IApplication testApp = RunTestApplication (40, 10, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            var contentsChangedCount = 0;

            TextView tv = new ()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill (1),
                Height = Dim.Fill (2)
            };

            // Subscribe before adding to view hierarchy
            tv.ContentsChanged += (_, _) => contentsChangedCount++;

            (app.TopRunnable as View)!.Add (tv);

            app.LayoutAndDraw ();

            // Bug #3990: ContentsChanged fires during initialization with no actual content changes
            // Expected: 0, Actual: 1 (will fail, proving bug exists)
            Assert.Equal (0, contentsChangedCount);
        }
    }

    // CoPilot - Test to compare with TextField behavior
    [Fact]
    public void TextField_TextChanged_Should_Not_Fire_On_Initialization ()
    {
        using IApplication testApp = RunTestApplication (40, 10, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            var textChangedCount = 0;

            TextField tf = new ()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill (1)
            };

            // Subscribe before adding to view hierarchy
            tf.TextChanged += (_, _) => textChangedCount++;

            (app.TopRunnable as View)!.Add (tf);

            app.LayoutAndDraw ();

            // TextField works correctly - doesn't fire on initialization
            Assert.Equal (0, textChangedCount);
        }
    }

    // CoPilot - Test to verify ContentsChanged fires when content actually changes
    [Fact]
    public void ContentsChanged_Should_Fire_On_Actual_Content_Change ()
    {
        using IApplication testApp = RunTestApplication (40, 10, DoTest, true, output);

        return;

        void DoTest (object? _, EventArgs<IApplication?> args)
        {
            IApplication app = args.Value!;

            var contentsChangedCount = 0;

            TextView tv = new ()
            {
                Width = Dim.Fill (),
                Height = Dim.Fill ()
            };

            (app.TopRunnable as View)!.Add (tv);

            app.LayoutAndDraw ();

            // Subscribe after initialization
            tv.ContentsChanged += (_, _) => contentsChangedCount++;

            // Now make an actual change
            app.InjectKey (Key.A);

            Assert.Equal ("a", tv.Text);
            Assert.Equal (1, contentsChangedCount);
        }
    }
}
