namespace Terminal.Gui.ViewBase;

public partial class View
{
    /// <summary>
    ///     Gets the current Clip region.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         There is a single clip region for the entire application.
    ///     </para>
    ///     <para>
    ///         This method returns the current clip region, not a clone. If there is a need to modify the clip region, clone
    ///         it first.
    ///     </para>
    /// </remarks>
    /// <returns>The current Clip.</returns>
    public Region? GetClip () => Driver?.Clip;

    /// <summary>
    ///     Sets the Clip to the specified region.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         There is a single clip region for the entire application. This method sets the clip region to the specified
    ///         region.
    ///     </para>
    /// </remarks>
    /// <param name="region"></param>
    public void SetClip (Region? region) => Driver?.Clip = region;

    /// <summary>
    ///     Sets the Clip to be the rectangle of the screen.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         There is a single clip region for the entire application. This method sets the clip region to the screen.
    ///     </para>
    ///     <para>
    ///         This method returns the current clip region, not a clone. If there is a need to modify the clip region, it is
    ///         recommended to clone it first.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The current Clip, which can be then re-applied <see cref="View.SetClip"/>
    /// </returns>
    public Region? SetClipToScreen ()
    {
        Region? previous = GetClip ();

        Driver?.Clip = new Region (Driver!.Screen);

        return previous;
    }

    /// <summary>
    ///     Removes the specified rectangle from the Clip.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         There is a single clip region for the entire application.
    ///     </para>
    /// </remarks>
    /// <param name="rectangle"></param>
    public void ExcludeFromClip (Rectangle rectangle) => Driver?.Clip?.Exclude (rectangle);

    /// <summary>
    ///     Removes the specified rectangle from the Clip.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         There is a single clip region for the entire application.
    ///     </para>
    /// </remarks>
    /// <param name="region"></param>
    public void ExcludeFromClip (Region? region) => Driver?.Clip?.Exclude (region);

    /// <summary>
    ///     Changes the Clip to the intersection of the current Clip and the <see cref="Frame"/> of this View.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method returns the current clip region, not a clone. If there is a need to modify the clip region, it is
    ///         recommended to clone it first.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The current Clip, which can be then re-applied <see cref="View.SetClip"/>
    /// </returns>
    internal Region? AddFrameToClip ()
    {
        if (Driver is null)
        {
            return null;
        }

        Region previous = GetClip () ?? new Region (Driver.Screen);

        Region frameRegion = previous.Clone ();

        // Translate viewportRegion to screen-relative coords
        Rectangle screenRect = FrameToScreen ();
        frameRegion.Intersect (screenRect);

        if (this is AdornmentView { Adornment: { } } adornment && adornment.Adornment.Thickness != Thickness.Empty)
        {
            // Ensure adornments can't draw outside their thickness
            frameRegion.Exclude (adornment.Adornment.Thickness.GetInside (FrameToScreen ()));
        }

        SetClip (frameRegion);

        return previous;
    }

    /// <summary>Changes the Clip to the intersection of the current Clip and the <see cref="Viewport"/> of this View.</summary>
    /// <remarks>
    ///     <para>
    ///         By default, sets the Clip to the intersection of the current clip region and the
    ///         <see cref="Viewport"/>. This ensures that drawing is constrained to the viewport, but allows
    ///         content to be drawn beyond the viewport.
    ///     </para>
    ///     <para>
    ///         If <see cref="ViewportSettings"/> has <see cref="ViewBase.ViewportSettingsFlags.ClipContentOnly"/> set,
    ///         clipping will be
    ///         applied to just the visible content area.
    ///     </para>
    ///     <remarks>
    ///         <para>
    ///             This method returns the current clip region, not a clone. If there is a need to modify the clip region, it
    ///             is recommended to clone it first.
    ///         </para>
    ///     </remarks>
    /// </remarks>
    /// <returns>
    ///     The current Clip, which can be then re-applied <see cref="View.SetClip"/>
    /// </returns>
    public Region? AddViewportToClip ()
    {
        if (Driver is null)
        {
            return null;
        }

        Region previous = GetClip () ?? new Region (App!.Screen);

        Region viewportRegion = previous.Clone ();

        Rectangle viewport = ViewportToScreen (new Rectangle (Point.Empty, Viewport.Size));
        viewportRegion.Intersect (viewport);

        if (ViewportSettings.FastHasFlags (ViewportSettingsFlags.ClipContentOnly))
        {
            // Clamp the Clip to the just content area that is within the viewport
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new Point (-Viewport.X, -Viewport.Y), GetContentSize ()));
            viewportRegion.Intersect (visibleContent);
        }

        if (this is AdornmentView { Adornment: { } } adornment && adornment.Adornment.Thickness != Thickness.Empty)
        {
            // Ensure adornments can't draw outside their thickness
            viewportRegion.Exclude (adornment.Adornment.Thickness.GetInside (viewport));
        }

        SetClip (viewportRegion);

        return previous;
    }
}
