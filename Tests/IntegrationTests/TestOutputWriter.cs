using System.Text;

namespace IntegrationTests;

public class TestOutputWriter (ITestOutputHelper output) : TextWriter
{
    public override void WriteLine (string? value) { output.WriteLine (value ?? string.Empty); }

    public override Encoding Encoding => Encoding.UTF8;
}
