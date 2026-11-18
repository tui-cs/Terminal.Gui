namespace AppTestHelpers;

/// <summary>
///     Thrown by <see cref="AppTestHelper.AssertAnsiSnapshot" /> when the rendered screen does
///     not match the recorded golden. Deliberately framework-agnostic (no xunit/nunit
///     dependency) — any test runner reports a thrown exception as a failure.
/// </summary>
public sealed class AnsiSnapshotException : Exception
{
    /// <inheritdoc cref="AnsiSnapshotException" />
    public AnsiSnapshotException (string message) : base (message) { }
}
