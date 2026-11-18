namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IHeld"/> for <see cref="AnsiResponseParser{TInputRecord}"/>
/// </summary>
/// <typeparam name="TInputRecord"></typeparam>
internal class GenericHeld<TInputRecord> : IHeld
{
    private readonly List<Tuple<char, TInputRecord>> _held = [];

    public void ClearHeld () => _held.Clear ();

    public string HeldToString () => new (_held.Select (h => h.Item1).ToArray ());

    public IEnumerable<object> HeldToObjects () => _held;

    public void AddToHeld (object o) => _held.Add ((Tuple<char, TInputRecord>)o);

    /// <inheritdoc/>
    public int Length => _held.Count;
}
