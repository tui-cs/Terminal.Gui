namespace ViewBaseTests.Navigation;

public class AdvanceFocusTests ()
{
    [Fact]
    public void AdvanceFocus_CanFocus_Mixed ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = false, TabStop = TabBehavior.TabStop };
        var v3 = new View { CanFocus = false, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Theory]
    [CombinatorialData]
    public void AdvanceFocus_Change_CanFocus_Works ([CombinatorialValues (TabBehavior.NoStop, TabBehavior.TabStop, TabBehavior.TabGroup)] TabBehavior behavior)
    {
        var r = new View { CanFocus = true };
        var v1 = new View ();
        var v2 = new View ();
        var v3 = new View ();
        Assert.True (r.CanFocus);
        Assert.False (v1.CanFocus);
        Assert.False (v2.CanFocus);
        Assert.False (v3.CanFocus);

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v1.CanFocus = true;
        v1.TabStop = behavior;
        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v2.CanFocus = true;
        v2.TabStop = behavior;
        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v3.CanFocus = true;
        v3.TabStop = behavior;
        r.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        r.Dispose ();
    }


    [Fact]
    public void AdvanceFocus_SubViews_TabStop ()
    {
        TabBehavior behavior = TabBehavior.TabStop;
        var top = new View { Id = "top", CanFocus = true };

        var v1 = new View { Id = "v1", CanFocus = true, TabStop = behavior };
        var v2 = new View { Id = "v2", CanFocus = true, TabStop = behavior };
        var v3 = new View { Id = "v3", CanFocus = false, TabStop = behavior };

        top.Add (v1, v2, v3);

        // Cycle through v1 & v2
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v1.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v2.HasFocus);

        // Should cycle back to v1
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v1.HasFocus);

        // Go backwards
        top.AdvanceFocus (NavigationDirection.Backward, behavior);
        Assert.True (v2.HasFocus);
        top.AdvanceFocus (NavigationDirection.Backward, behavior);
        Assert.True (v1.HasFocus);

        top.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_Compound_SubView_TabStop ()
    {
        TabBehavior behavior = TabBehavior.TabStop;
        var top = new View { Id = "top", CanFocus = true };

        var compoundSubView = new View
        {
            CanFocus = true,
            Id = "compoundSubView",
            TabStop = behavior
        };
        var v1 = new View { Id = "v1", CanFocus = true, TabStop = behavior };
        var v2 = new View { Id = "v2", CanFocus = true, TabStop = behavior };
        var v3 = new View { Id = "v3", CanFocus = false, TabStop = behavior };

        compoundSubView.Add (v1, v2, v3);

        top.Add (compoundSubView);

        // Cycle through v1 & v2
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.Equal (v1, compoundSubView.Focused);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.Equal (v2, compoundSubView.Focused);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.Equal (v1, compoundSubView.Focused);

        // Add another subview
        View otherSubView = new ()
        {
            CanFocus = true,
            TabStop = behavior,
            Id = "otherSubView"
        };

        top.Add (otherSubView);

        // Adding a focusable subview causes advancefocus
        Assert.True (otherSubView.HasFocus);
        Assert.False (v1.HasFocus);

        // Cycle through v1 & v2
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        Assert.True (otherSubView.HasFocus);

        // v2 was previously focused down the compoundSubView focus chain
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);

        top.Dispose ();
    }


    [Fact]
    public void AdvanceFocus_CompoundCompound_SubView_TabStop ()
    {
        //top
        //    ├── topv1, topv2, topv3 (only topv1 and topv2 are focusable)
        //    └── compoundSubView
        //        ├── v1, v2, v3 (v3 is not focusable)
        //        └── compoundCompoundSubView
        //            └── v4, v5, v6 (v6 is not focusable)
        TabBehavior behavior = TabBehavior.TabStop;
        var top = new View { Id = "top", CanFocus = true };
        var topv1 = new View { Id = "topv1", CanFocus = true, TabStop = behavior };
        var topv2 = new View { Id = "topv2", CanFocus = true, TabStop = behavior };
        var topv3 = new View { Id = "topv3", CanFocus = false, TabStop = behavior };
        top.Add (topv1, topv2, topv3);

        var compoundSubView = new View
        {
            CanFocus = true,
            Id = "compoundSubView",
            TabStop = behavior
        };
        var v1 = new View { Id = "v1", CanFocus = true, TabStop = behavior };
        var v2 = new View { Id = "v2", CanFocus = true, TabStop = behavior };
        var v3 = new View { Id = "v3", CanFocus = false, TabStop = behavior };

        compoundSubView.Add (v1, v2, v3);


        var compoundCompoundSubView = new View
        {
            CanFocus = true,
            Id = "compoundCompoundSubView",
            TabStop = behavior
        };
        var v4 = new View { Id = "v4", CanFocus = true, TabStop = behavior };
        var v5 = new View { Id = "v5", CanFocus = true, TabStop = behavior };
        var v6 = new View { Id = "v6", CanFocus = false, TabStop = behavior };

        compoundCompoundSubView.Add (v4, v5, v6);

        compoundSubView.Add (compoundCompoundSubView);

        top.Add (compoundSubView);

        top.SetFocus ();
        Assert.True (topv1.HasFocus);

        // Cycle through topv2
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (topv2.HasFocus);

        // Cycle v1, v2, v4, v5
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v1.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v2.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v4.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (v5.HasFocus);

        // Should cycle back to topv1
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.Equal (topv1, top.Focused);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.Equal (topv2, top.Focused);

        // Add another top subview. Should cycle to it after v5
        View otherSubView = new ()
        {
            CanFocus = true,
            TabStop = behavior,
            Id = "otherSubView"
        };

        top.Add (otherSubView);

        // Adding a focusable subview causes advancefocus
        Assert.True (otherSubView.HasFocus);

        // Cycle through topv1, topv2, v1, v2, v4, v5
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (topv1.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (topv2.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, behavior);

        // the above should have cycled to v5 since it was the previously most focused subview of compoundSubView
        Assert.True (v5.HasFocus);

        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (otherSubView.HasFocus);

        // Should cycle back to topv1
        top.AdvanceFocus (NavigationDirection.Forward, behavior);
        Assert.True (topv1.HasFocus);

        top.Dispose ();
    }

    [Fact]
    public void FocusNavigation_Should_Cycle_Back_To_Top_Level_Views ()
    {
        // Create a simplified version of the hierarchy from CompoundCompound test
        // top
        //  ├── topLeftButton, topMiddleButton
        //  └── nestedContainer
        //       └── innerButton

        // Create top-level view
        View top = new ()
        {
            Id = "top",
            CanFocus = true
        };

        // Create top-level buttons
        View topLeftButton = new ()
        {
            Id = "topLeftButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        View topMiddleButton = new ()
        {
            Id = "topMiddleButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        // Create nested container with a button inside
        View nestedContainer = new ()
        {
            Id = "nestedContainer",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        View innerButton = new ()
        {
            Id = "innerButton",
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        // Build the view hierarchy
        nestedContainer.Add (innerButton);
        top.Add (topLeftButton, topMiddleButton, nestedContainer);

        // Initial focus on topLeftButton
        topLeftButton.SetFocus ();
        Assert.Equal (topLeftButton, top.Focused);

        // Advance focus to topMiddleButton
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (topMiddleButton, top.Focused);

        // Advance focus to innerButton
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (innerButton, nestedContainer.Focused);
        Assert.Equal (nestedContainer, top.Focused);

        // THIS IS WHERE THE BUG OCCURS
        // Advancing focus from innerButton should go back to topLeftButton
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

        // This assertion will fail with current implementation
        Assert.Equal (topLeftButton, top.Focused);

        top.Dispose ();
    }


    [Fact]
    public void AdvanceFocus_Compound_SubView_TabGroup ()
    {
        var top = new View { Id = "top", CanFocus = true, TabStop = TabBehavior.TabGroup };

        var compoundSubView = new View
        {
            CanFocus = true,
            Id = "compoundSubView",
            TabStop = TabBehavior.TabGroup
        };
        var tabStopView = new View { Id = "tabStop", CanFocus = true, TabStop = TabBehavior.TabStop };
        var tabGroupView1 = new View { Id = "tabGroup1", CanFocus = true, TabStop = TabBehavior.TabGroup };
        var tabGroupView2 = new View { Id = "tabGroup2", CanFocus = true, TabStop = TabBehavior.TabGroup };

        compoundSubView.Add (tabStopView, tabGroupView1, tabGroupView2);

        top.Add (compoundSubView);
        top.SetFocus ();
        Assert.True (tabStopView.HasFocus);

        // TabGroup should cycle to tabGroup1 then tabGroup2
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        Assert.False (tabStopView.HasFocus);
        Assert.True (tabGroupView1.HasFocus);
        Assert.False (tabGroupView2.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        Assert.False (tabStopView.HasFocus);
        Assert.False (tabGroupView1.HasFocus);
        Assert.True (tabGroupView2.HasFocus);

        // Add another TabGroup subview
        View otherTabGroupSubView = new ()
        {
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            Id = "otherTabGroupSubView"
        };

        top.Add (otherTabGroupSubView);

        // Adding a focusable subview causes advancefocus
        Assert.True (otherTabGroupSubView.HasFocus);
        Assert.False (tabStopView.HasFocus);

        // TabGroup navs to the other subview
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        Assert.Equal (compoundSubView, top.Focused);
        Assert.True (tabStopView.HasFocus);

        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        Assert.Equal (compoundSubView, top.Focused);
        Assert.True (tabGroupView1.HasFocus);

        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
        Assert.Equal (compoundSubView, top.Focused);
        Assert.True (tabGroupView2.HasFocus);

        // Now go backwards
        top.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup);
        Assert.Equal (compoundSubView, top.Focused);
        Assert.True (tabGroupView1.HasFocus);

        top.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup);
        Assert.Equal (otherTabGroupSubView, top.Focused);
        Assert.True (otherTabGroupSubView.HasFocus);

        top.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_NoStop_And_CanFocus_True_No_Focus ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v3 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_NoStop_Change_Enables_Stop ()
    {
        var r = new View { CanFocus = true };
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v3 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        v1.TabStop = TabBehavior.TabStop;
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v2.TabStop = TabBehavior.TabStop;
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);

        v3.TabStop = TabBehavior.TabStop;
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_NoStop_Prevents_Stop ()
    {
        var r = new View ();
        var v1 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v2 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };
        var v3 = new View { CanFocus = true, TabStop = TabBehavior.NoStop };

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
    }

    [Fact]
    public void AdvanceFocus_Null_And_CanFocus_False_No_Advance ()
    {
        var r = new View ();
        var v1 = new View ();
        var v2 = new View ();
        var v3 = new View ();
        Assert.False (v1.CanFocus);
        Assert.Null (v1.TabStop);

        r.Add (v1, v2, v3);

        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        r.Dispose ();
    }

    [Fact]
    public void AdvanceFocus_SubViews_Raises_HasFocusChanged ()
    {
        var top = new View
        {
            Id = "top",
            CanFocus = true
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = true
        };
        top.Add (subView1, subView2);

        var subView1HasFocusChangedTrueCount = 0;
        var subView1HasFocusChangedFalseCount = 0;

        subView1.HasFocusChanged += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subView1HasFocusChangedTrueCount++;
                                        }
                                        else
                                        {
                                            subView1HasFocusChangedFalseCount++;
                                        }
                                    };

        var subView2HasFocusChangedTrueCount = 0;
        var subView2HasFocusChangedFalseCount = 0;

        subView2.HasFocusChanged += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subView2HasFocusChangedTrueCount++;
                                        }
                                        else
                                        {
                                            subView2HasFocusChangedFalseCount++;
                                        }
                                    };

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangedTrueCount);
        Assert.Equal (0, subView1HasFocusChangedFalseCount);

        Assert.Equal (0, subView2HasFocusChangedTrueCount);
        Assert.Equal (0, subView2HasFocusChangedFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.False (subView1.HasFocus);
        Assert.True (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangedTrueCount);
        Assert.Equal (1, subView1HasFocusChangedFalseCount);

        Assert.Equal (1, subView2HasFocusChangedTrueCount);
        Assert.Equal (0, subView2HasFocusChangedFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (2, subView1HasFocusChangedTrueCount);
        Assert.Equal (1, subView1HasFocusChangedFalseCount);

        Assert.Equal (1, subView2HasFocusChangedTrueCount);
        Assert.Equal (1, subView2HasFocusChangedFalseCount);
    }

    [Fact]
    public void AdvanceFocus_SubViews_Raises_HasFocusChanging ()
    {
        var top = new View
        {
            Id = "top",
            CanFocus = true
        };

        var subView1 = new View
        {
            Id = "subView1",
            CanFocus = true
        };

        var subView2 = new View
        {
            Id = "subView2",
            CanFocus = true
        };
        top.Add (subView1, subView2);

        var subView1HasFocusChangingTrueCount = 0;
        var subView1HasFocusChangingFalseCount = 0;

        subView1.HasFocusChanging += (s, e) =>
                                     {
                                         if (e.NewValue)
                                         {
                                             subView1HasFocusChangingTrueCount++;
                                         }
                                         else
                                         {
                                             subView1HasFocusChangingFalseCount++;
                                         }
                                     };

        var subView2HasFocusChangingTrueCount = 0;
        var subView2HasFocusChangingFalseCount = 0;

        subView2.HasFocusChanging += (s, e) =>
                                     {
                                         if (e.NewValue)
                                         {
                                             subView2HasFocusChangingTrueCount++;
                                         }
                                         else
                                         {
                                             subView2HasFocusChangingFalseCount++;
                                         }
                                     };

        top.SetFocus ();
        Assert.True (top.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangingTrueCount);
        Assert.Equal (0, subView1HasFocusChangingFalseCount);

        Assert.Equal (0, subView2HasFocusChangingTrueCount);
        Assert.Equal (0, subView2HasFocusChangingFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.False (subView1.HasFocus);
        Assert.True (subView2.HasFocus);

        Assert.Equal (1, subView1HasFocusChangingTrueCount);
        Assert.Equal (1, subView1HasFocusChangingFalseCount);

        Assert.Equal (1, subView2HasFocusChangingTrueCount);
        Assert.Equal (0, subView2HasFocusChangingFalseCount);

        top.AdvanceFocus (NavigationDirection.Forward, null);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        Assert.Equal (2, subView1HasFocusChangingTrueCount);
        Assert.Equal (1, subView1HasFocusChangingFalseCount);

        Assert.Equal (1, subView2HasFocusChangingTrueCount);
        Assert.Equal (1, subView2HasFocusChangingFalseCount);
    }

    [Fact]
    public void AdvanceFocus_With_CanFocus_Are_All_True ()
    {
        var top = new View { Id = "top", CanFocus = true };
        var v1 = new View { Id = "v1", CanFocus = true };
        var v2 = new View { Id = "v2", CanFocus = true };
        var v3 = new View { Id = "v3", CanFocus = true };

        top.Add (v1, v2, v3);

        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.True (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.True (v2.HasFocus);
        Assert.False (v3.HasFocus);
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.False (v1.HasFocus);
        Assert.False (v2.HasFocus);
        Assert.True (v3.HasFocus);
        top.Dispose ();
    }

    [Theory]
    [CombinatorialData]
    public void TabStop_And_CanFocus_Are_Decoupled (bool canFocus, TabBehavior tabStop)
    {
        var view = new View { CanFocus = canFocus, TabStop = tabStop };

        Assert.Equal (canFocus, view.CanFocus);
        Assert.Equal (tabStop, view.TabStop);
    }

    [Fact]
    public void AdvanceFocus_Cycles_Through_Peers_And_All_Nested_SubViews_When_Multiple ()
    {
        var top = new View { Id = "top", CanFocus = true };

        View peer1 = new View
        {
            CanFocus = true,
            Id = "peer1",
        };

        var peer2 = new View
        {
            CanFocus = true,
            Id = "peer2",
        };
        var peer2SubView = new View
        {
            Id = "peer2SubView", CanFocus = true
        };
        var v1 = new View { Id = "v1", CanFocus = true };
        var v2 = new View { Id = "v2", CanFocus = true };
        peer2SubView.Add (v1, v2);

        peer2.Add (peer2SubView);

        top.Add (peer1, peer2);
        top.SetFocus ();

        Assert.Equal (peer1, top.MostFocused);

        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (v1, top.MostFocused);

        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (v2, top.MostFocused);

        // This should cycle to peer1 - previously it incorrectly cycled to v1
        top.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        Assert.Equal (peer1, top.MostFocused);

        top.Dispose ();
    }
}
