namespace Terminal.Gui.Views;

/// <summary>Provides data for the <see cref="Markdown.LinkClicked"/> event.</summary>
public class MarkdownLinkEventArgs : EventArgs
{
    /// <summary>Initializes a new <see cref="MarkdownLinkEventArgs"/>.</summary>
    /// <param name="url">The URL of the link that was clicked.</param>
    public MarkdownLinkEventArgs (string url) => Url = url;

    /// <summary>Gets the URL of the clicked link (may be absolute, relative, or an anchor like <c>#section</c>).</summary>
    public string Url { get; }

    /// <summary>Gets or sets whether the event has been handled. Set to <see langword="true"/> to prevent default navigation.</summary>
    public bool Handled { get; set; }
}
