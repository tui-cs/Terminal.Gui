using System.Text;
using UnitTests;

namespace ViewsTests;

/// <summary>
///     Tests for the Cancellable Workflow Pattern (CWP) properties on <see cref="LinearRangeViewBase{TOption,TValue}"/>.
/// </summary>
public class LinearRangeCWPTests : TestDriverBase
{
    [Fact]
    public void LegendsOrientation_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = Orientation.Horizontal;
        var newValue = Orientation.Vertical;

        sel.LegendsOrientationChanging += (sender, args) =>
                                          {
                                              changingRaised = true;
                                              Assert.Equal (oldValue, args.CurrentValue);
                                              Assert.Equal (newValue, args.NewValue);
                                          };

        sel.LegendsOrientationChanged += (sender, args) =>
                                         {
                                             changedRaised = true;
                                             Assert.Equal (oldValue, args.OldValue);
                                             Assert.Equal (newValue, args.NewValue);
                                         };

        sel.LegendsOrientation = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.LegendsOrientation);
    }

    [Fact]
    public void MinimumInnerSpacing_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = 1;
        var newValue = 5;

        sel.MinimumInnerSpacingChanging += (sender, args) =>
                                           {
                                               changingRaised = true;
                                               Assert.Equal (oldValue, args.CurrentValue);
                                               Assert.Equal (newValue, args.NewValue);
                                           };

        sel.MinimumInnerSpacingChanged += (sender, args) =>
                                          {
                                              changedRaised = true;
                                              Assert.Equal (oldValue, args.OldValue);
                                              Assert.Equal (newValue, args.NewValue);
                                          };

        sel.MinimumInnerSpacing = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.MinimumInnerSpacing);
    }

    [Fact]
    public void ShowEndSpacing_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = false;
        var newValue = true;

        sel.ShowEndSpacingChanging += (sender, args) =>
                                      {
                                          changingRaised = true;
                                          Assert.Equal (oldValue, args.CurrentValue);
                                          Assert.Equal (newValue, args.NewValue);
                                      };

        sel.ShowEndSpacingChanged += (sender, args) =>
                                     {
                                         changedRaised = true;
                                         Assert.Equal (oldValue, args.OldValue);
                                         Assert.Equal (newValue, args.NewValue);
                                     };

        sel.ShowEndSpacing = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.ShowEndSpacing);
    }

    [Fact]
    public void ShowLegends_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = true;
        var newValue = false;

        sel.ShowLegendsChanging += (sender, args) =>
                                   {
                                       changingRaised = true;
                                       Assert.Equal (oldValue, args.CurrentValue);
                                       Assert.Equal (newValue, args.NewValue);
                                   };

        sel.ShowLegendsChanged += (sender, args) =>
                                  {
                                      changedRaised = true;
                                      Assert.Equal (oldValue, args.OldValue);
                                      Assert.Equal (newValue, args.NewValue);
                                  };

        sel.ShowLegends = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.ShowLegends);
    }

    [Fact]
    public void UseMinimumSize_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = false;
        var newValue = true;

        sel.UseMinimumSizeChanging += (sender, args) =>
                                      {
                                          changingRaised = true;
                                          Assert.Equal (oldValue, args.CurrentValue);
                                          Assert.Equal (newValue, args.NewValue);
                                      };

        sel.UseMinimumSizeChanged += (sender, args) =>
                                     {
                                         changedRaised = true;
                                         Assert.Equal (oldValue, args.OldValue);
                                         Assert.Equal (newValue, args.NewValue);
                                     };

        sel.UseMinimumSize = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.UseMinimumSize);
    }

    // Copilot
    [Fact]
    public void Command_Activate_Calls_SetFocusedOption ()
    {
        LinearSelector<int> sel = new ();

        sel.Options =
        [
            new LinearRangeOption<int> ("A", new Rune ('a'), 1),
            new LinearRangeOption<int> ("B", new Rune ('b'), 2),
            new LinearRangeOption<int> ("C", new Rune ('c'), 3)
        ];

        sel.FocusedOption = 1;

        bool? result = sel.InvokeCommand (Command.Activate);

        Assert.False (result);
        Assert.Contains (1, sel.SelectedIndices);

        sel.Dispose ();
    }

    // Copilot
    [Fact]
    public void Command_Accept_Calls_SetFocusedOption ()
    {
        LinearSelector<int> sel = new ();

        sel.Options =
        [
            new LinearRangeOption<int> ("A", new Rune ('a'), 1),
            new LinearRangeOption<int> ("B", new Rune ('b'), 2),
            new LinearRangeOption<int> ("C", new Rune ('c'), 3)
        ];

        sel.FocusedOption = 2;

        sel.InvokeCommand (Command.Accept);

        Assert.Contains (2, sel.SelectedIndices);

        sel.Dispose ();
    }
}
