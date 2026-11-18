using System.Reflection;
using UnitTests;

namespace ViewsTests;

public class AllViewsTests (ITestOutputHelper output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Center_Properly (Type viewType)
    {
        IDriver driver = CreateTestDriver ();

        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view is null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        view.X = Pos.Center ();
        view.Y = Pos.Center ();

        // Ensure the view has positive dimensions
        view.Width = 10;
        view.Height = 10;

        var frame = new View { X = 0, Y = 0, Width = 50, Height = 50 };
        frame.Add (view);
        frame.LayoutSubViews ();
        frame.Dispose ();

        // What's the natural width/height?
        int expectedX = (frame.Frame.Width - view.Frame.Width) / 2;
        int expectedY = (frame.Frame.Height - view.Frame.Height) / 2;

        Assert.True (view.Frame.Left == expectedX, $"{view} did not center horizontally. Expected: {expectedX}. Actual: {view.Frame.Left}");

        Assert.True (view.Frame.Top == expectedY, $"{view} did not center vertically. Expected: {expectedY}. Actual: {view.Frame.Top}");
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Command_Accept_Raises_Accepting (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        // if (view is IDesignable designable)
        // {
        //    designable.EnableForDesign ();
        // }

        var acceptingCount = 0;
        view.Accepting += (s, e) => { acceptingCount++; };

        if (view.InvokeCommand (Command.Accept) == true)
        {
            Assert.Equal (1, acceptingCount);
        }
        view?.Dispose ();
    }

    // [Fact]
    // public void AllViews_HotKey_Works ()
    // {
    //	foreach (var type in GetAllViewClasses ()) {
    //		_output.WriteLine ($"Testing {type.Name}");
    //		var view = GetTypeInitializer (type, type.GetConstructor (Array.Empty<Type> ()));
    //		view.HotKeySpecifier = (Rune)'^';
    //		view.Text = "^text";
    //		Assert.Equal(Key.T, view.HotKey);
    //	}
    // }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Command_Activate_Raises_Activating (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var activatingCount = 0;
        view.Activating += (s, e) => activatingCount++;

        var acceptedCount = 0;
        view.Accepting += (s, e) => { acceptedCount++; };

        if (view.InvokeCommand (Command.Activate) == true)
        {
            Assert.Equal (1, activatingCount);
            Assert.Equal (0, acceptedCount);
        }
        view?.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Command_HotKey_Raises_HandlingHotKey (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }
        else
        {
            view.HotKey = Key.T;
        }

        var handlingHotKeyCount = 0;
        view.HandlingHotKey += (s, e) => { handlingHotKeyCount++; };

        if (view.InvokeCommand (Command.HotKey) == true)
        {
            Assert.Equal (1, handlingHotKeyCount);
        }
        view?.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Layout_Does_Not_Draw (Type viewType)
    {
        IDriver driver = CreateTestDriver ();

        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view is null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var drawContentCount = 0;
        view.DrawingContent += (s, e) => drawContentCount++;

        var layoutStartedCount = 0;
        view.SubViewLayout += (s, e) => layoutStartedCount++;

        var layoutCompleteCount = 0;
        view.SubViewsLaidOut += (s, e) => layoutCompleteCount++;

        view.SetNeedsLayout ();
        view.SetNeedsDraw ();
        view.Layout ();

        Assert.Equal (0, drawContentCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Tests_All_Constructors (Type viewType)
    {
        Assert.True (TestAllConstructorsOfType (viewType));

        return;

        bool TestAllConstructorsOfType (Type type)
        {
            foreach (ConstructorInfo ctor in type.GetConstructors ())
            {
                View? view = CreateViewFromType (type, ctor);

                if (view != null)
                {
                    Assert.True (type.FullName == view.GetType ().FullName);
                }

                view?.Dispose ();
            }

            return true;
        }
    }

    // [Theory]
    // [MemberData (nameof (AllViewTypes))]
    // public void AllViews_Disabled_Draws_Disabled_Or_Faint (Type viewType)
    // {
    //    var view = CreateInstanceIfNotGeneric (viewType);

    //    if (view == null)
    //    {
    //        output.WriteLine ($"Ignoring {viewType} - It's a Generic");

    //        return;
    //    }

    //    if (view is IDesignable designable)
    //    {
    //        designable.EnableForDesign ();
    //    }

    //    var driver = CreateTestDriver ();
    //    driver.AttributeSet += (_, args) =>
    //                           {
    //                               if (args != view.GetAttributeForRole (VisualRole.Disabled) && args.Style != TextStyle.Faint)
    //                               {
    //                                   Assert.Fail($"{viewType} with `Enabled == false` tried to SetAttribute to {args}");
    //                               }
    //                           };
    //    view.Driver = driver;
    //    view.Enabled = false;
    //    view.SetNeedsDraw ();
    //    view.Draw ();

    //    view?.Dispose ();
    // }
}
