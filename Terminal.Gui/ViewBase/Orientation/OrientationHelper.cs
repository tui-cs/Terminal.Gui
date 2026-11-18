
namespace Terminal.Gui.ViewBase;



/// <summary>
///     Helper class for implementing <see cref="IOrientation"/>.
/// </summary>
/// <remarks>
///     <para>
///         Implements the standard pattern for changing/changed events.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// private class OrientedView : View, IOrientation
/// {
///     private readonly OrientationHelper _orientationHelper;
/// 
///     public OrientedView ()
///     {
///         // ReSharper disable once UseObjectOrCollectionInitializer
///         _orientationHelper = new (this);
///         Orientation = Orientation.Vertical;
///         _orientationHelper.OrientationChanging += (sender, e) =&gt; OrientationChanging?.Invoke (this, e);
///         _orientationHelper.OrientationChanged += (sender, e) =&gt; OrientationChanged?.Invoke (this, e);
///     }
/// 
///     public Orientation Orientation
///     {
///         get =&gt; _orientationHelper.Orientation;
///         set =&gt; _orientationHelper.Orientation = value;
///     }
/// 
///     public event EventHandler&lt;CancelEventArgs&lt;Orientation&gt;&gt; OrientationChanging;
///     public event EventHandler&lt;EventArgs&lt;Orientation&gt;&gt; OrientationChanged;
/// 
///     public bool OnOrientationChanging (Orientation currentOrientation, Orientation newOrientation)
///     {
///        // Custom logic before orientation changes
///        return false; // Return true to cancel the change
///     }
/// 
///     public void OnOrientationChanged (Orientation newOrientation)
///     {
///         // Custom logic after orientation has changed
///     }
/// }
/// </code>
/// </example>
public class OrientationHelper
{
    private Orientation _orientation;
    private readonly IOrientation _owner;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OrientationHelper"/> class.
    /// </summary>
    /// <param name="owner">Specifies the object that owns this helper instance and implements <see cref="IOrientation"/>.</param>
    public OrientationHelper (IOrientation owner) { _owner = owner; }

    /// <summary>
    ///     Gets or sets the orientation of the View.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation == value)
            {
                return;
            }

            // Best practice is to call the virtual method first.
            // This allows derived classes to handle the event and potentially cancel it.
            if (_owner?.OnOrientationChanging (value, _orientation) ?? false)
            {
                return;
            }

            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            CancelEventArgs<Orientation> args = new (in _orientation, ref value);
            OrientationChanging?.Invoke (_owner, args);

            if (args.Cancel)
            {
                return;
            }

            // If the event is not canceled, update the value.
            Orientation old = _orientation;

            if (_orientation != value)
            {
                _orientation = value;

                if (_owner is { })
                {
                    _owner.Orientation = value;
                }
            }

            // Best practice is to call the virtual method first, then raise the event.
            _owner?.OnOrientationChanged (_orientation);
            OrientationChanged?.Invoke (_owner, new (in _orientation));
        }
    }

    /// <summary>
    ///     Raised when the orientation is changing. This is cancelable.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Views that implement <see cref="IOrientation"/> should raise <see cref="IOrientation.OrientationChanging"/>
    ///         after the orientation has changed
    ///         (<code>_orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);</code>).
    ///     </para>
    ///     <para>
    ///         This event will be raised after the <see cref="IOrientation.OnOrientationChanging"/> method is called (assuming
    ///         it was not canceled).
    ///     </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <summary>
    ///     Raised when the orientation has changed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Views that implement <see cref="IOrientation"/> should raise <see cref="IOrientation.OrientationChanged"/>
    ///         after the orientation has changed
    ///         (<code>_orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);</code>).
    ///     </para>
    ///     <para>
    ///         This event will be raised after the <see cref="IOrientation.OnOrientationChanged"/> method is called.
    ///     </para>
    /// </remarks>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
}


//public class OrientationHelper
//{
//    private Orientation _orientation;

//    public Orientation Orientation
//    {
//        get => _orientation;
//        set
//        {
//            CWPPropertyHelper.ChangeProperty (
//                                              currentValue: _orientation,
//                                              newValue: ref value,
//                                              onChanging: args => OnOrientationChanging (args),
//                                              changingEvent: OrientationChanging,
//                                              onChanged: args => OnOrientationChanged (args),
//                                              changedEvent: OrientationChanged
//                                             );
//            _orientation = value;
//        }
//    }

//    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;
//    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

//    protected virtual bool OnOrientationChanging (CancelEventArgs<Orientation> args) => false;
//    protected virtual void OnOrientationChanged (EventArgs<Orientation> args) { }
//}

