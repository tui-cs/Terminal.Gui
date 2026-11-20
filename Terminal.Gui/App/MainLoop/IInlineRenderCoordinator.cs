namespace Terminal.Gui.App;

internal interface IInlineRenderCoordinator
{
    int InitialCursorRow { get; }
    void WithRenderGate (Action action);
}
