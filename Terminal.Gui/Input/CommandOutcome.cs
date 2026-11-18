namespace Terminal.Gui.Input;

/// <summary>
///     Describes the outcome of a <see cref="Command"/> invocation, replacing the three-valued <c>bool?</c>
///     (<see langword="null"/> = not found, <see langword="false"/> = not handled, <see langword="true"/> = handled).
/// </summary>
public enum CommandOutcome
{
    /// <summary>
    ///     The command was not handled; routing continues.
    /// </summary>
    NotHandled,

    /// <summary>
    ///     The command was handled; routing stops.
    /// </summary>
    HandledStop,

    /// <summary>
    ///     The command was handled but routing may continue (notification semantics).
    /// </summary>
    HandledContinue,
}

/// <summary>
///     Provides extension methods for converting between <see cref="CommandOutcome"/> and <c>bool?</c>.
/// </summary>
public static partial class CommandOutcomeExtensions
{
    /// <summary>
    ///     Converts a <see cref="CommandOutcome"/> to a nullable boolean for backward compatibility.
    /// </summary>
    /// <param name="outcome">The outcome to convert.</param>
    /// <returns>
    ///     <see langword="true"/> for <see cref="CommandOutcome.HandledStop"/>,
    ///     <see langword="false"/> for <see cref="CommandOutcome.HandledContinue"/>,
    ///     <see langword="null"/> for <see cref="CommandOutcome.NotHandled"/>.
    /// </returns>
    public static bool? ToBool (this CommandOutcome outcome) => outcome switch
    {
        CommandOutcome.HandledStop => true,
        CommandOutcome.HandledContinue => false,
        _ => null
    };

    /// <summary>
    ///     Converts a nullable boolean to a <see cref="CommandOutcome"/>.
    /// </summary>
    /// <param name="result">The nullable boolean to convert.</param>
    /// <returns>
    ///     <see cref="CommandOutcome.HandledStop"/> for <see langword="true"/>,
    ///     <see cref="CommandOutcome.HandledContinue"/> for <see langword="false"/>,
    ///     <see cref="CommandOutcome.NotHandled"/> for <see langword="null"/>.
    /// </returns>
    public static CommandOutcome ToOutcome (this bool? result) => result switch
    {
        true => CommandOutcome.HandledStop,
        false => CommandOutcome.HandledContinue,
        null => CommandOutcome.NotHandled
    };
}
