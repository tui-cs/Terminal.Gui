namespace ViewBaseTests.OrientationTests;

public class OrientationTests
{
    private class CustomView : View, IOrientation
    {
        private readonly OrientationHelper _orientationHelper;

        public CustomView ()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            _orientationHelper = new (this);
            Orientation = Orientation.Vertical;
            _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
            _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);
        }

        public Orientation Orientation
        {
            get => _orientationHelper.Orientation;
            set => _orientationHelper.Orientation = value;
        }

        public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;
        public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

        public bool CancelOnOrientationChanging { get; set; }

        public bool OnOrientationChangingCalled { get; private set; }
        public bool OnOrientationChangedCalled { get; private set; }

        public bool OnOrientationChanging (Orientation currentOrientation, Orientation newOrientation)
        {
            OnOrientationChangingCalled = true;
            // Custom logic before orientation changes
            return CancelOnOrientationChanging; // Return true to cancel the change
        }

        public void OnOrientationChanged (Orientation newOrientation)
        {
            OnOrientationChangedCalled = true;
            // Custom logic after orientation has changed
        }
    }

    [Fact]
    public void Orientation_Change_IsSuccessful ()
    {
        // Arrange
        var customView = new CustomView ();
        var orientationChanged = false;
        customView.OrientationChanged += (sender, e) => orientationChanged = true;

        // Act
        customView.Orientation = Orientation.Horizontal;

        // Assert
        Assert.True (orientationChanged, "OrientationChanged event was not invoked.");
        Assert.Equal (Orientation.Horizontal, customView.Orientation);
    }

    [Fact]
    public void Orientation_Change_OrientationChanging_Set_Cancel_IsCancelled ()
    {
        // Arrange
        var customView = new CustomView ();
        customView.OrientationChanging += (sender, e) => e.Cancel = true; // Cancel the orientation change
        var orientationChanged = false;
        customView.OrientationChanged += (sender, e) => orientationChanged = true;

        // Act
        customView.Orientation = Orientation.Horizontal;

        // Assert
        Assert.False (orientationChanged, "OrientationChanged event was invoked despite cancellation.");
        Assert.Equal (Orientation.Vertical, customView.Orientation); // Assuming Vertical is the default orientation
    }

    [Fact]
    public void Orientation_Change_OnOrientationChanging_Return_True_IsCancelled ()
    {
        // Arrange
        var customView = new CustomView ();
        customView.CancelOnOrientationChanging = true; // Cancel the orientation change

        var orientationChanged = false;
        customView.OrientationChanged += (sender, e) => orientationChanged = true;

        // Act
        customView.Orientation = Orientation.Horizontal;

        // Assert
        Assert.False (orientationChanged, "OrientationChanged event was invoked despite cancellation.");
        Assert.Equal (Orientation.Vertical, customView.Orientation); // Assuming Vertical is the default orientation
    }


    [Fact]
    public void OrientationChanging_VirtualMethodCalledBeforeEvent ()
    {
        // Arrange
        var optionSelector = new CustomView ();
        bool eventCalled = false;

        optionSelector.OrientationChanging += (sender, e) =>
                                          {
                                              eventCalled = true;
                                              Assert.True (optionSelector.OnOrientationChangingCalled, "OnOrientationChanging was not called before the event.");
                                          };

        // Act
        optionSelector.Orientation = Orientation.Horizontal;

        // Assert
        Assert.True (eventCalled, "OrientationChanging event was not called.");
    }

    [Fact]
    public void OrientationChanged_VirtualMethodCalledBeforeEvent ()
    {
        // Arrange
        var radioGroup = new CustomView ();
        bool eventCalled = false;

        radioGroup.OrientationChanged += (sender, e) =>
                                         {
                                             eventCalled = true;
                                             Assert.True (radioGroup.OnOrientationChangedCalled, "OnOrientationChanged was not called before the event.");
                                         };

        // Act
        radioGroup.Orientation = Orientation.Horizontal;

        // Assert
        Assert.True (eventCalled, "OrientationChanged event was not called.");
    }
}
