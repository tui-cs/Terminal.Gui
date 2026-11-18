using UnitTests;

namespace ViewBaseTests.Navigation;


public class HasFocusChangeEventTests () : TestsAllViews
{
    #region HasFocusChanging_NewValue_True

    [Fact]
    public void HasFocusChanging_SetFocus_Raises ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);
    }


    [Fact]
    public void HasFocusChanging_SetFocus_SubView_SetFocus_Raises ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        view.SetFocus ();

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);
    }


    [Fact]
    public void HasFocusChanging_SetFocus_On_SubView_SubView_SetFocus_Raises ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        subview.SetFocus ();

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);
    }

    [Fact]
    public void HasFocusChanging_SetFocus_CompoundSubView_Raises ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subViewEnterCount = 0;
        var subViewLeaveCount = 0;

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        subView.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subViewEnterCount++;
                                        }
                                        else
                                        {
                                            subViewLeaveCount++;
                                        }
                                    };

        var subviewSubView1EnterCount = 0;
        var subviewSubView1LeaveCount = 0;

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };

        subViewSubView1.HasFocusChanging += (s, e) =>
                                            {
                                                if (e.NewValue)
                                                {
                                                    subviewSubView1EnterCount++;
                                                }
                                                else
                                                {
                                                    subviewSubView1LeaveCount++;
                                                }
                                            };

        var subviewSubView2EnterCount = 0;
        var subviewSubView2LeaveCount = 0;

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true
        };

        subViewSubView2.HasFocusChanging += (s, e) =>
                                            {
                                                if (e.NewValue)
                                                {
                                                    subviewSubView2EnterCount++;
                                                }
                                                else
                                                {
                                                    subviewSubView2EnterCount++;
                                                }
                                            };

        var subviewSubView3EnterCount = 0;
        var subviewSubView3LeaveCount = 0;

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = false
        };

        subViewSubView3.HasFocusChanging += (s, e) =>
                                            {
                                                if (e.NewValue)
                                                {
                                                    subviewSubView3EnterCount++;
                                                }
                                                else
                                                {
                                                    subviewSubView3LeaveCount++;
                                                }
                                            };

        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.True (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subViewEnterCount);
        Assert.Equal (0, subViewLeaveCount);

        Assert.Equal (0, subviewSubView1EnterCount);
        Assert.Equal (0, subviewSubView1LeaveCount);

        Assert.Equal (1, subviewSubView2EnterCount);
        Assert.Equal (0, subviewSubView2LeaveCount);

        Assert.Equal (0, subviewSubView3EnterCount);
        Assert.Equal (0, subviewSubView3LeaveCount);
    }

    [Fact]
    public void HasFocusChanging_Can_Cancel ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                         e.Cancel = true;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        view.SetFocus ();

        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (0, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);
    }

    [Fact]
    public void HasFocusChanging_SetFocus_On_SubView_Can_Cancel ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                         e.Cancel = true;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        subview.SetFocus ();

        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);
    }

    [Fact]
    public void HasFocusChanging_SubView_Can_Cancel ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                            e.Cancel = true;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        view.SetFocus ();

        Assert.True (view.HasFocus);
        Assert.False (subview.HasFocus);

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);
    }


    [Fact]
    public void HasFocusChanging_SetFocus_On_SubView_If_SubView_Cancels ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                            e.Cancel = true;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        subview.SetFocus ();

        Assert.False (view.HasFocus); // Never had focus
        Assert.False (subview.HasFocus); // Cancelled

        Assert.Equal (0, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);

        // Now set focus on the view
        view.SetFocus ();

        Assert.True (view.HasFocus);
        Assert.False (subview.HasFocus); // Cancelled

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (2, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);
    }

    #endregion HasFocusChanging_NewValue_True

    #region HasFocusChanging_NewValue_False

    [Fact]
    public void HasFocusChanging_RemoveFocus_Raises ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (1, hasFocusFalseCount);
    }


    [Fact]
    public void HasFocusChanging_RemoveFocus_SubView_SetFocus_Raises ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        view.SetFocus ();

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subview.HasFocus);
        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (1, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (1, subviewHasFocusFalseCount);
    }



    [Fact]
    public void HasFocusChanging_RemoveFocus_On_SubView_SubView_SetFocus_Raises ()
    {
        var hasFocusTrueCount = 0;
        var hasFocusFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanging += (s, e) =>
                                 {
                                     if (e.NewValue)
                                     {
                                         hasFocusTrueCount++;
                                     }
                                     else
                                     {
                                         hasFocusFalseCount++;
                                     }
                                 };

        var subviewHasFocusTrueCount = 0;
        var subviewHasFocusFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanging += (s, e) =>
                                    {
                                        if (e.NewValue)
                                        {
                                            subviewHasFocusTrueCount++;
                                        }
                                        else
                                        {
                                            subviewHasFocusFalseCount++;
                                        }
                                    };

        view.Add (subview);

        subview.SetFocus ();

        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (0, subviewHasFocusFalseCount);

        subview.HasFocus = false;
        Assert.False (subview.HasFocus);
        Assert.Equal (1, hasFocusTrueCount);
        Assert.Equal (0, hasFocusFalseCount);

        Assert.Equal (1, subviewHasFocusTrueCount);
        Assert.Equal (1, subviewHasFocusFalseCount);

    }

    #endregion HasFocusChanging_NewValue_False

    #region HasFocusChanged

    [Fact]
    public void HasFocusChanged_RemoveFocus_Raises ()
    {
        var newValueTrueCount = 0;
        var newValueFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanged += (s, e) =>
                                {
                                    if (e.NewValue)
                                    {
                                        newValueTrueCount++;
                                    }
                                    else
                                    {
                                        newValueFalseCount++;
                                    }
                                };

        Assert.True (view.CanFocus);
        Assert.False (view.HasFocus);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Equal (1, newValueTrueCount);
        Assert.Equal (0, newValueFalseCount);

        view.HasFocus = false;
        Assert.Equal (1, newValueTrueCount);
        Assert.Equal (1, newValueFalseCount);
    }


    [Fact]
    public void HasFocusChanged_With_SubView_Raises ()
    {
        var newValueTrueCount = 0;
        var newValueFalseCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanged += (s, e) =>
                                {
                                    if (e.NewValue)
                                    {
                                        newValueTrueCount++;
                                    }
                                    else
                                    {
                                        newValueFalseCount++;
                                    }
                                };

        var subviewNewValueTrueCount = 0;
        var subviewNewValueFalseCount = 0;

        var subview = new View
        {
            Id = "subview",
            CanFocus = true
        };

        subview.HasFocusChanged += (s, e) =>
                                   {
                                       if (e.NewValue)
                                       {
                                           subviewNewValueTrueCount++;
                                       }
                                       else
                                       {
                                           subviewNewValueFalseCount++;
                                       }
                                   };

        view.Add (subview);

        view.SetFocus ();
        Assert.Equal (1, newValueTrueCount);
        Assert.Equal (0, newValueFalseCount);

        Assert.Equal (1, subviewNewValueTrueCount);
        Assert.Equal (0, subviewNewValueFalseCount);

        view.HasFocus = false;

        Assert.Equal (1, newValueTrueCount);
        Assert.Equal (1, newValueFalseCount);

        Assert.Equal (1, subviewNewValueTrueCount);
        Assert.Equal (1, subviewNewValueFalseCount);

        view.SetFocus ();

        Assert.Equal (2, newValueTrueCount);
        Assert.Equal (1, newValueFalseCount);

        Assert.Equal (2, subviewNewValueTrueCount);
        Assert.Equal (1, subviewNewValueFalseCount);

        subview.HasFocus = false;

        Assert.Equal (2, newValueTrueCount);
        Assert.Equal (1, newValueFalseCount);

        Assert.Equal (2, subviewNewValueTrueCount);
        Assert.Equal (2, subviewNewValueFalseCount);
    }


    [Fact]
    public void HasFocusChanged_CompoundSubView_Raises ()
    {
        var viewEnterCount = 0;
        var viewLeaveCount = 0;

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanged += (s, e) =>
                                {
                                    if (e.NewValue)
                                    {
                                        viewEnterCount++;
                                    }
                                    else
                                    {
                                        viewLeaveCount++;
                                    }
                                };

        var subViewEnterCount = 0;
        var subViewLeaveCount = 0;

        var subView = new View
        {
            Id = "subView",
            CanFocus = true
        };

        subView.HasFocusChanged += (s, e) =>
                                   {
                                       if (e.NewValue)
                                       {
                                           subViewEnterCount++;
                                       }
                                       else
                                       {
                                           subViewLeaveCount++;
                                       }
                                   };

        var subviewSubView1EnterCount = 0;
        var subviewSubView1LeaveCount = 0;

        var subViewSubView1 = new View
        {
            Id = "subViewSubView1",
            CanFocus = false
        };

        subViewSubView1.HasFocusChanged += (s, e) =>
                                           {
                                               if (e.NewValue)
                                               {
                                                   subviewSubView1EnterCount++;
                                               }
                                               else
                                               {
                                                   subviewSubView1LeaveCount++;
                                               }
                                           };

        var subviewSubView2EnterCount = 0;
        var subviewSubView2LeaveCount = 0;

        var subViewSubView2 = new View
        {
            Id = "subViewSubView2",
            CanFocus = true
        };

        subViewSubView2.HasFocusChanged += (s, e) =>
                                           {
                                               if (e.NewValue)
                                               {
                                                   subviewSubView2EnterCount++;
                                               }
                                               else
                                               {
                                                   subviewSubView2LeaveCount++;
                                               }
                                           };
        var subviewSubView3EnterCount = 0;
        var subviewSubView3LeaveCount = 0;

        var subViewSubView3 = new View
        {
            Id = "subViewSubView3",
            CanFocus = false
        };

        subViewSubView3.HasFocusChanged += (s, e) =>
                                           {
                                               if (e.NewValue)
                                               {
                                                   subviewSubView3EnterCount++;
                                               }
                                               else
                                               {
                                                   subviewSubView3LeaveCount++;
                                               }
                                           };

        subView.Add (subViewSubView1, subViewSubView2, subViewSubView3);

        view.Add (subView);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.True (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subView.HasFocus);
        Assert.False (subViewSubView1.HasFocus);
        Assert.False (subViewSubView2.HasFocus);
        Assert.False (subViewSubView3.HasFocus);

        Assert.Equal (1, viewEnterCount);
        Assert.Equal (1, viewLeaveCount);

        Assert.Equal (1, subViewEnterCount);
        Assert.Equal (1, subViewLeaveCount);

        Assert.Equal (0, subviewSubView1EnterCount);
        Assert.Equal (0, subviewSubView1LeaveCount);

        Assert.Equal (1, subviewSubView2EnterCount);
        Assert.Equal (1, subviewSubView2LeaveCount);

        Assert.Equal (0, subviewSubView3EnterCount);
        Assert.Equal (0, subviewSubView3LeaveCount);
    }


    [Fact]
    public void HasFocusChanged_NewValue_False_Hide_SubView ()
    {
        var subView1 = new View
        {
            Id = $"subView1",
            CanFocus = true
        };

        var subView2 = new View
        {
            Id = $"subView2",
            CanFocus = true
        };

        var view = new View
        {
            Id = "view",
            CanFocus = true
        };

        view.HasFocusChanged += (s, e) =>
                                {
                                    if (e.NewValue)
                                    {
                                        Assert.True (view.HasFocus);
                                        Assert.True (subView1.HasFocus);
                                        Assert.False (subView2.HasFocus);

                                        subView1.Visible = true;
                                        subView2.Visible = false;

                                        Assert.True (view.HasFocus);
                                        Assert.True (subView1.HasFocus);
                                        Assert.False (subView2.HasFocus);

                                    }
                                    else
                                    {
                                        Assert.False (view.HasFocus);
                                        Assert.False (subView1.HasFocus);
                                        Assert.False (subView2.HasFocus);

                                        subView1.Visible = false;
                                        subView2.Visible = true;
                                    }
                                };

        view.Add (subView1, subView2);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subView1.HasFocus);
        Assert.False (subView2.HasFocus);

        view.HasFocus = false;
        Assert.False (view.HasFocus);
        Assert.False (subView1.HasFocus);
        Assert.False (subView2.HasFocus);
    }

    #endregion HasFocusChanged
}
