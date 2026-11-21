namespace Terminal.Gui.Drivers;

// Journals cleared flags so a failed frame can be retried without dirtying untouched inline cells.
// Output buffers are owned by the UI thread for the duration of a frame.
internal struct OutputDirtyState
{
    private readonly Cell [,]? _contents;
    private readonly bool [] _dirtyLines;
    private readonly int [] _changes;
    private readonly int _stride;
    private int _count;

    public OutputDirtyState (IOutputBuffer buffer, ref int []? reusableChanges)
    {
        _contents = buffer.Contents;
        _dirtyLines = buffer.DirtyLines;
        _stride = checked (buffer.Cols + 1);
        int length = checked (buffer.Rows * _stride);
        _count = 0;

        if (length > Utf8Buffer.RetainedCapacityLimit / sizeof (int))
        {
            _changes = new int [length];
            return;
        }

        if (reusableChanges is null || reusableChanges.Length < length)
        {
            reusableChanges = new int [length];
        }

        _changes = reusableChanges;
    }

    public void ClearCell (int row, int col)
    {
        if (!_contents! [row, col].IsDirty)
        {
            return;
        }

        _changes [_count] = row * _stride + col;
        _count++;
        _contents [row, col].IsDirty = false;
    }

    public void ClearLine (int row)
    {
        if (!_dirtyLines [row])
        {
            return;
        }

        _changes [_count] = row * _stride + _stride - 1;
        _count++;
        _dirtyLines [row] = false;
    }

    public readonly void Restore ()
    {
        for (int index = 0; index < _count; index++)
        {
            int row = _changes [index] / _stride;
            int col = _changes [index] % _stride;

            if (col == _stride - 1)
            {
                _dirtyLines [row] = true;
                continue;
            }

            _contents! [row, col].IsDirty = true;
        }
    }
}
