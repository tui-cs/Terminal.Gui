
/// <summary>
/// 
/// </summary>
public class KeyEqualityComparer : IEqualityComparer<Key>
{
    /// <inheritdoc />
    public bool Equals (Key? x, Key? y)
    {
        if (ReferenceEquals (x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.KeyCode == y.KeyCode;
    }

    /// <inheritdoc />
    public int GetHashCode (Key? obj)
    {
        if (obj is null)
        {
            return 0;
        }

        return obj.KeyCode.GetHashCode ();
    }
}
