using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Helpers for parsing string input into the value type exposed by <see cref="IValue{TValue}"/>.
/// </summary>
/// <remarks>
///     <para>
///         Used by the default implementation of <see cref="IValue.TrySetValueFromString"/> and by
///         derived classes that implement multiple <c>IValue&lt;T&gt;</c> interfaces and need to
///         disambiguate the diamond-inherited default implementation.
///     </para>
/// </remarks>
public static class IValueParser
{
    /// <summary>
    ///     Attempts to parse <paramref name="input"/> into a value of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The target value type. May be a reference type, value type, or <see cref="System.Nullable{T}"/>.</typeparam>
    /// <param name="input">The string representation to parse.</param>
    /// <param name="parsed">When this method returns <see langword="true"/>, contains the parsed value; otherwise <see langword="default"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if <paramref name="input"/> was parsed successfully; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>Supported types:</para>
    ///     <list type="bullet">
    ///         <item><description><see cref="string"/> (assigned directly).</description></item>
    ///         <item><description>Any type implementing <see cref="IParsable{TSelf}"/> via reflection on the static <c>TryParse(string, IFormatProvider?, out T)</c> method.</description></item>
    ///         <item><description><see cref="System.Nullable{T}"/> wrappers around any of the above.</description></item>
    ///         <item><description><see cref="System.Enum"/> types (case-insensitive).</description></item>
    ///     </list>
    /// </remarks>
    [UnconditionalSuppressMessage (
        "Trimming",
        "IL2090:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to target method.",
        Justification = "Reflective lookup of static TryParse(string, IFormatProvider?, out T) is intentional. Callers using AOT/trimming with custom IParsable types should preserve the TryParse method via DynamicDependency or equivalent.")]
    public static bool TryParseValue<TValue> (string input, out TValue? parsed)
    {
        parsed = default;

        if (input is null)
        {
            return false;
        }

        Type valueType = typeof (TValue);
        Type underlyingType = Nullable.GetUnderlyingType (valueType) ?? valueType;

        // string passthrough
        if (underlyingType == typeof (string))
        {
            parsed = (TValue?)(object?)input;

            return true;
        }

        // Enum support (case-insensitive)
        if (underlyingType.IsEnum)
        {
            if (Enum.TryParse (underlyingType, input, ignoreCase: true, out object? parsedEnum))
            {
                parsed = (TValue?)parsedEnum;

                return true;
            }

            return false;
        }

        // IParsable<T>.TryParse(string, IFormatProvider?, out T) via reflection
        MethodInfo? tryParse = underlyingType.GetMethod (
                                                         "TryParse",
                                                         BindingFlags.Public | BindingFlags.Static,
                                                         binder: null,
                                                         types: [typeof (string), typeof (IFormatProvider), underlyingType.MakeByRefType ()],
                                                         modifiers: null);

        if (tryParse is null)
        {
            return false;
        }

        object? [] args = [input, null, null];
        var success = (bool)tryParse.Invoke (null, args)!;

        if (!success)
        {
            return false;
        }

        parsed = (TValue?)args [2];

        return true;
    }
}
