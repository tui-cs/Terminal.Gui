using System.Text;

namespace AppTestHelpers;

class ThreadSafeStringWriter : StringWriter
{
    private readonly object _lock;

    public ThreadSafeStringWriter (StringBuilder sb, object syncLock) : base (sb)
    {
        _lock = syncLock;
    }

    public override void Write (char value)
    {
        lock (_lock)
        {
            base.Write (value);
        }
    }

    public override void Write (string? value)
    {
        lock (_lock)
        {
            base.Write (value);
        }
    }

    // (override other Write* methods as needed)
}
