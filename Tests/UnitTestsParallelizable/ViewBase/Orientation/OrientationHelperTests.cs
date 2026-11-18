using Moq;

namespace ViewBaseTests.OrientationTests;

public class OrientationHelperTests
{
    [Fact]
    public void Orientation_Set_NewValue_InvokesChangingAndChangedEvents ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var orientationHelper = new OrientationHelper (mockIOrientation.Object);
        var changingEventInvoked = 0;
        var changedEventInvoked = 0;

        orientationHelper.OrientationChanging += (sender, e) => { changingEventInvoked++; };
        orientationHelper.OrientationChanged += (sender, e) => { changedEventInvoked++; };

        // Act
        orientationHelper.Orientation = Orientation.Vertical;

        // Assert
        Assert.Equal (1, changingEventInvoked);
        Assert.Equal(1, changedEventInvoked);
    }

    [Fact]
    public void Orientation_Set_NewValue_InvokesOnChangingAndOnChangedOverrides ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var onChangingOverrideCalled = 0;
        var onChangedOverrideCalled = 0;

        mockIOrientation.Setup (x => x.OnOrientationChanging (It.IsAny<Orientation> (), It.IsAny<Orientation> ()))
                        .Callback (() => onChangingOverrideCalled++)
                        .Returns (false); // Ensure it doesn't cancel the change

        mockIOrientation.Setup (x => x.OnOrientationChanged (It.IsAny<Orientation> ()))
                        .Callback (() => onChangedOverrideCalled++);

        var orientationHelper = new OrientationHelper (mockIOrientation.Object);

        // Act
        orientationHelper.Orientation = Orientation.Vertical;

        // Assert
        Assert.Equal (1, onChangingOverrideCalled);
        Assert.Equal (1, onChangedOverrideCalled);
    }

    [Fact]
    public void Orientation_Set_SameValue_DoesNotInvokeChangingOrChangedEvents ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var orientationHelper = new OrientationHelper (mockIOrientation.Object);
        orientationHelper.Orientation = Orientation.Horizontal; // Set initial orientation
        var changingEventInvoked = 0;
        var changedEventInvoked = 0;

        orientationHelper.OrientationChanging += (sender, e) => { changingEventInvoked++; };
        orientationHelper.OrientationChanged += (sender, e) => { changedEventInvoked++; };

        // Act
        orientationHelper.Orientation = Orientation.Horizontal; // Set to the same value

        // Assert
        Assert.Equal (0, changingEventInvoked);
        Assert.Equal (0, changedEventInvoked);
    }

    [Fact]
    public void Orientation_Set_NewValue_OrientationChanging_CancellationPreventsChange ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();
        var orientationHelper = new OrientationHelper (mockIOrientation.Object);
        orientationHelper.OrientationChanging += (sender, e) => { e.Cancel = true; }; // Cancel the change

        // Act
        orientationHelper.Orientation = Orientation.Vertical;

        // Assert
        Assert.Equal (Orientation.Horizontal, orientationHelper.Orientation); // Initial orientation is Horizontal
    }

    [Fact]
    public void Orientation_Set_NewValue_OnOrientationChanging_CancelsChange ()
    {
        // Arrange
        Mock<IOrientation> mockIOrientation = new Mock<IOrientation> ();

        mockIOrientation.Setup (x => x.OnOrientationChanging (It.IsAny<Orientation> (), It.IsAny<Orientation> ()))
                        .Returns (true); // Override to return true, cancelling the change

        var orientationHelper = new OrientationHelper (mockIOrientation.Object);

        // Act
        orientationHelper.Orientation = Orientation.Vertical;

        // Assert
        Assert.Equal (
                      Orientation.Horizontal,
                      orientationHelper.Orientation); // Initial orientation is Horizontal, and it should remain unchanged due to cancellation
    }
}
