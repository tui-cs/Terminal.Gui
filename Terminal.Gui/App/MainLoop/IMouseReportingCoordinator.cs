namespace Terminal.Gui.App;

/// <summary>Coordinates terminal mouse mode changes with rendering and shutdown.</summary>
internal interface IMouseReportingCoordinator
{
    void SetMouseDisabled (bool disabled);
}
