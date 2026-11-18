
namespace Terminal.Gui.Drivers;

/// <summary>
///     Interface for subcomponent of a <see cref="InputProcessorImpl{T}"/> which
///     can translate the raw console input type T (which typically varies by
///     driver) to the shared Terminal.Gui <see cref="Key"/> class.
/// </summary>
/// <typeparam name="TInputRecord"></typeparam>
public interface IKeyConverter<TInputRecord>
{
    /// <summary>
    ///     Converts the native keyboard info type into
    ///     the <see cref="Key"/> class used by Terminal.Gui views.
    /// </summary>
    /// <param name="keyInfo"></param>
    /// <returns></returns>
    Key ToKey (TInputRecord keyInfo);

    /// <summary>
    ///     Converts a <see cref="Key"/> into the native keyboard info type. Should be used for simulating
    ///     key presses in unit tests.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    TInputRecord ToKeyInfo (Key key);
}
