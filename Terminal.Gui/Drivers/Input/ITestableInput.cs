namespace Terminal.Gui.Drivers;

/// <summary>
///     Marker interface for IInput implementations that support test input injection.
/// </summary>
/// <typeparam name="TInputRecord">The input record type</typeparam>
public interface ITestableInput<TInputRecord> : IInput<TInputRecord>
    where TInputRecord : struct
{
    /// <summary>
    ///     Adds an input record that will be returned by Peek/Read for testing.
    /// </summary>
    void InjectInput (TInputRecord input);
}

