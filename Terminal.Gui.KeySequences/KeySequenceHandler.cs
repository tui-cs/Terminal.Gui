namespace Terminal.Gui.KeySequences;

/// <summary>Handles a completed key sequence.</summary>
/// <param name="context">The completed sequence context.</param>
/// <returns><see langword="true"/> if the sequence was consumed.</returns>
public delegate bool KeySequenceHandler (KeySequenceContext context);
