namespace Terminal.Gui.Views;

internal interface IColorBar
{
    int Value { get; set; }

    /// <summary>
    ///     Update the value of <see cref="Value"/> and reflect
    ///     changes in UI state but do not raise a value changed
    ///     event (to avoid circular events).
    /// </summary>
    /// <param name="v"></param>
    internal void SetValueWithoutRaisingEvent (int v);
}
