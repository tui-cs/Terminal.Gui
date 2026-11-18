namespace Terminal.Gui.Drivers;

/// <summary>
///     Wraps IConsoleInput for .NET console input events (ConsoleKeyInfo). Needed to support Mocking in tests.
/// </summary>
internal interface INetInput : IInput<ConsoleKeyInfo>
{ }
