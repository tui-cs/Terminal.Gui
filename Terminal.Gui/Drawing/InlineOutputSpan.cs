namespace Terminal.Gui.Drawing;

/// <summary>A literal text span and its optional terminal colors and style.</summary>
/// <param name="Text">Text without terminal controls or line breaks. Tabs are expanded at eight-column stops.</param>
/// <param name="Attribute">Concrete styling, or null to use terminal defaults.</param>
public sealed record InlineOutputSpan (string Text, Attribute? Attribute = null);
