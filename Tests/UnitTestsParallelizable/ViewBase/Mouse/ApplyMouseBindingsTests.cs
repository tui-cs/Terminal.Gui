// Copilot

namespace ViewBaseTests.MouseTests;

/// <summary>
///     Tests for <see cref="View.ApplyMouseBindings"/> — validates layered binding behavior,
///     supported-command filtering, non-overwrite semantics, and generic-arity type-name override lookup.
/// </summary>
public class ApplyMouseBindingsTests
{
    /// <summary>
    ///     Verifies that only commands the view supports (via <see cref="View.GetSupportedCommands"/>)
    ///     are actually bound.
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Filters_Unsupported_Commands ()
    {
        // Base View supports Activate but not StartSelection (it's a TextView thing).
        View view = new () { Width = 10, Height = 1 };
        HashSet<Command> supported = new (view.GetSupportedCommands ());

        // Base View doesn't support StartSelection
        Assert.DoesNotContain (Command.StartSelection, supported);

        // So the mouse binding for StartSelection should NOT be in the view's MouseBindings
        IEnumerable<MouseFlags> selectionFlags = View.DefaultMouseBindings! [Command.StartSelection]
                                                     .GetCurrentPlatformMouseFlags ();

        foreach (MouseFlags mf in selectionFlags)
        {
            Assert.False (view.MouseBindings.TryGet (mf, out _),
                          $"MouseFlags {mf} should NOT be bound on base View since StartSelection is unsupported.");
        }
    }

    /// <summary>
    ///     Verifies that Activate (which base View supports) IS bound.
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Binds_Supported_Commands ()
    {
        View view = new () { Width = 10, Height = 1 };

        // Activate should be bound
        IEnumerable<MouseFlags> activateFlags = View.DefaultMouseBindings! [Command.Activate]
                                                    .GetCurrentPlatformMouseFlags ();

        foreach (MouseFlags mf in activateFlags)
        {
            if (mf == MouseFlags.None)
            {
                continue;
            }

            Assert.True (view.MouseBindings.TryGet (mf, out MouseBinding binding),
                         $"MouseFlags {mf} should be bound on View for Activate.");
            Assert.Contains (Command.Activate, binding.Commands);
        }
    }

    /// <summary>
    ///     Verifies "already bound" non-overwrite semantics — if a MouseFlags is already bound,
    ///     it is NOT overwritten by a later layer.
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Does_Not_Overwrite_Existing_Bindings ()
    {
        // ViewWithContext already has LeftButtonReleased -> Activate from SetupMouse.
        // Applying a conflicting layer with LeftButtonReleased -> Context should NOT overwrite.
        Dictionary<Command, PlatformMouseBinding> conflictingLayer = new ()
        {
            [Command.Context] = BindMouse.All (MouseFlags.LeftButtonReleased)
        };

        ViewWithContext viewWithContext = new ();

        // Verify Activate is bound to LeftButtonReleased already
        Assert.True (viewWithContext.MouseBindings.TryGet (MouseFlags.LeftButtonReleased, out MouseBinding existing));
        Assert.Contains (Command.Activate, existing.Commands);

        // Now apply the conflicting layer
        viewWithContext.TestApplyMouseBindings (conflictingLayer);

        // LeftButtonReleased should still be Activate, NOT Context
        Assert.True (viewWithContext.MouseBindings.TryGet (MouseFlags.LeftButtonReleased, out MouseBinding afterApply));
        Assert.Contains (Command.Activate, afterApply.Commands);
        Assert.DoesNotContain (Command.Context, afterApply.Commands);
    }

    /// <summary>
    ///     Verifies that all provided layers are applied in order.
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Applies_Multiple_Layers ()
    {
        ViewWithContext view = new ();

        Dictionary<Command, PlatformMouseBinding> firstLayer = new ()
        {
            [Command.Context] = BindMouse.All (MouseFlags.MiddleButtonReleased)
        };

        Dictionary<Command, PlatformMouseBinding> secondLayer = new ()
        {
            [Command.Activate] = BindMouse.All (MouseFlags.RightButtonReleased)
        };

        view.TestApplyMouseBindings (firstLayer, secondLayer);

        Assert.True (view.MouseBindings.TryGet (MouseFlags.MiddleButtonReleased, out MouseBinding contextBinding));
        Assert.Contains (Command.Context, contextBinding.Commands);

        Assert.True (view.MouseBindings.TryGet (MouseFlags.RightButtonReleased, out MouseBinding activateBinding));
        Assert.Contains (Command.Activate, activateBinding.Commands);
    }

    /// <summary>
    ///     Verifies that <see cref="View.ViewMouseBindings"/> per-type overrides are applied
    ///     using the short type name (stripping generic arity backtick suffix).
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Uses_TypeName_Without_GenericArity ()
    {
        // Set up ViewMouseBindings with a key matching our test view's name.
        // Use Command.Activate which is supported by all views so it can be bound during SetupMouse.
        Dictionary<string, Dictionary<Command, PlatformMouseBinding>>? original = View.ViewMouseBindings;

        try
        {
            View.ViewMouseBindings = new ()
            {
                ["ViewWithContext"] = new ()
                {
                    // Bind Activate to RightButtonReleased for this type
                    [Command.Activate] = BindMouse.All (MouseFlags.RightButtonReleased)
                }
            };

            ViewWithContext view = new ();

            // The ViewMouseBindings override should have been applied
            Assert.True (view.MouseBindings.TryGet (MouseFlags.RightButtonReleased, out MouseBinding binding));
            Assert.Contains (Command.Activate, binding.Commands);
        }
        finally
        {
            View.ViewMouseBindings = original;
        }
    }

    /// <summary>
    ///     Verifies that generic type arity is stripped when looking up <see cref="View.ViewMouseBindings"/> overrides.
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Uses_Generic_TypeName_Without_GenericArity ()
    {
        Dictionary<string, Dictionary<Command, PlatformMouseBinding>>? original = View.ViewMouseBindings;

        try
        {
            View.ViewMouseBindings = new ()
            {
                ["GenericViewWithContext"] = new ()
                {
                    [Command.Context] = BindMouse.All (MouseFlags.MiddleButtonReleased)
                }
            };

            GenericViewWithContext<string> view = new ();

            Assert.True (view.MouseBindings.TryGet (MouseFlags.MiddleButtonReleased, out MouseBinding binding));
            Assert.Contains (Command.Context, binding.Commands);
        }
        finally
        {
            View.ViewMouseBindings = original;
        }
    }

    /// <summary>
    ///     Verifies that null layers are safely skipped.
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Null_Layer_Is_Skipped ()
    {
        ViewWithContext view = new ();
        int countBefore = 0;

        foreach (MouseFlags mf in Enum.GetValues<MouseFlags> ())
        {
            if (view.MouseBindings.TryGet (mf, out _))
            {
                countBefore++;
            }
        }

        // Applying a null layer should not change anything
        view.TestApplyMouseBindings ((Dictionary<Command, PlatformMouseBinding>?)null);

        int countAfter = 0;

        foreach (MouseFlags mf in Enum.GetValues<MouseFlags> ())
        {
            if (view.MouseBindings.TryGet (mf, out _))
            {
                countAfter++;
            }
        }

        Assert.Equal (countBefore, countAfter);
    }

    /// <summary>
    ///     Verifies that MouseFlags.None entries in a PlatformMouseBinding are skipped.
    /// </summary>
    [Fact]
    public void ApplyMouseBindings_Skips_MouseFlags_None ()
    {
        ViewWithContext view = new ();

        Dictionary<Command, PlatformMouseBinding> layerWithNone = new ()
        {
            [Command.Context] = BindMouse.All (MouseFlags.None)
        };

        view.TestApplyMouseBindings (layerWithNone);

        // MouseFlags.None should never be bound
        Assert.False (view.MouseBindings.TryGet (MouseFlags.None, out _));
    }

    /// <summary>Helper view that supports Command.Context for testing.</summary>
    private class ViewWithContext : View
    {
        public ViewWithContext ()
        {
            Width = 10;
            Height = 1;
            AddCommand (Command.Context, _ => true);
        }

        public void TestApplyMouseBindings (params Dictionary<Command, PlatformMouseBinding>? [] layers)
        {
            ApplyMouseBindings (layers);
        }
    }

    /// <summary>Generic helper view that supports Command.Context for testing.</summary>
    private class GenericViewWithContext<T> : View
    {
        public GenericViewWithContext ()
        {
            Width = 10;
            Height = 1;
            AddCommand (Command.Context, _ => true);
        }
    }
}
