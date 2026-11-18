
namespace AppTestHelpers;

/// <summary>
///     Entry point to fluent assertions.
/// </summary>
public static class With
{
    /// <summary>
    ///     Entrypoint to fluent assertions
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="driverName"></param>
    /// <param name="logWriter"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public static AppTestHelper A<T> (
        int width,
        int height,
        string driverName,
        TextWriter? logWriter = null,
        TimeSpan? timeout = null
    ) where T : IRunnable, new ()
    {
        return new (
                    () => new T ()
                    {
                        //Id = $"{typeof (T).Name}"
                    },
                    width,
                    height,
                    driverName,
                    logWriter,
                    timeout ?? Timeout);
    }

    /// <summary>
    /// Overload that takes a function to create instance <paramref name="runnableFactory"/> after application is initialized.
    /// </summary>
    /// <param name="runnableFactory"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="driverName"></param>
    /// <param name="logWriter"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    public static AppTestHelper A (
        Func<IRunnable> runnableFactory,
        int width,
        int height,
        string driverName,
        TextWriter? logWriter = null,
        TimeSpan? timeout = null
    )
    {
        return new (runnableFactory, width, height, driverName, logWriter, timeout ?? Timeout);
    }

    /// <summary>
    ///     The global timeout to allow for any given application to run for before shutting down.
    /// </summary>
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds (30);


}
