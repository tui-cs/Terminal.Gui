namespace Terminal.Gui.App;

/// <summary>
///     Thrown when user code attempts to access a property or perform a method
///     Exception type thrown when trying to use a property or method
///     that is only supported after initialization, e.g. of an <see cref="IApplicationMainLoop{T}"/>
/// </summary>
public class NotInitializedException : InvalidOperationException
{
    /// <summary>
    ///     Creates a new instance of the exception indicating that the class
    ///     <paramref name="memberName"/> cannot be used until owner is initialized.
    /// </summary>
    /// <param name="memberName">Property or method name</param>
    public NotInitializedException (string memberName) : base ($"{memberName} cannot be accessed before Initialization") { }

    /// <summary>
    ///     Creates a new instance of the exception with the full message/inner exception.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="innerException"></param>
    public NotInitializedException (string msg, Exception innerException) : base (msg, innerException) { }
}
