using Microsoft.Extensions.Logging;

namespace AppTestHelpers;

internal class TextWriterLoggerProvider (TextWriter writer) : ILoggerProvider
{
    public ILogger CreateLogger (string category) { return new TextWriterLogger (writer); }

    public void Dispose () { writer.Dispose (); }
}
