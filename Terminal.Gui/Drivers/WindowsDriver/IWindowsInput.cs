namespace Terminal.Gui.Drivers;

/// <summary>
///     Wraps IConsoleInput for Windows console input events (WindowsConsole.InputRecord). Needed to support Mocking in tests.
/// </summary>
public interface IWindowsInput : IInput<WindowsConsole.InputRecord>
{ }
