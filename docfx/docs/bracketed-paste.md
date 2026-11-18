# Bracketed Paste

Terminal.Gui supports the ANSI [bracketed paste mode](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-Functions-using-CSI-_-ordered-by-the-final-character-s) (`DECSET 2004`). When the terminal has bracketed paste enabled, the terminal wraps clipboard pastes with the marker pair `ESC[200~` and `ESC[201~`. This lets the application distinguish a paste from typed input — useful for performance (a 10 KB paste arrives as one event instead of 10,000 keystrokes) and for security (the application does not have to interpret pasted text as commands or shortcuts).

Bracketed paste mode is enabled automatically by the driver when the application initializes and disabled on shutdown. Applications do not need to opt in.

## Architecture: bracketed paste shares the `Command.Paste` pipeline

Bracketed paste and keyboard-driven paste (`Ctrl+V`) route through the same command pipeline:

```
driver bytes  ──►  AnsiResponseParser ──►  IApplication.Paste (event, raw payload)
                                       │
                                       ▼
                              focused view.InvokeCommand (Command.Paste, ctx.WithValue (PastePayload (payload)))
                                       │
                                       ▼
              ┌─────────────────  default Command.Paste handler  ─────────────────┐
              │  payload = ctx.Value is PastePayload p ? p.Text : clipboard        │
              │  sanitized = OnSanitizingPaste (payload)                            │
              │  raise  Pasting  (cancellable, mutable Text)                        │
              │  if not cancelled:  consumed = OnPaste (sanitized)                  │
              │  if inserted:  raise  Pasted                                        │
              └─────────────────────────────────────────────────────────────────────┘
```

The same handler serves both bracketed paste (payload travels in a dedicated command-context paste payload) and keyboard `Ctrl+V` (no payload → clipboard fallback). There is no parallel paste dispatch path.

## Receiving paste events

There are three places to hook in, in order of dispatch:

### 1. `Application.Paste` — raw, app-wide

```csharp
app.Paste += (_, args) =>
              {
                  myLog.AppendLine ($"Pasted {args.Text.Length} characters");
                  // Set args.Handled = true to stop further dispatch.
              };
```

Subscribers see the raw terminal-delivered payload before any sanitization. Cancelling here prevents the paste from reaching any view.

### 2. `View.Pasting` — sanitized, cancellable, mutable

```csharp
field.Pasting += (_, args) =>
                  {
                      // args.Text is already sanitized. Rewrite it, or cancel.
                      args.Text = args.Text.Trim ();
                      // args.Handled = true   // cancel insertion
                  };
```

Raised after the default handler resolves the payload and calls `OnSanitizingPaste`. Subscribers may rewrite `args.Text` to alter what gets inserted, or set `Handled` to cancel.

### 3. `View.Pasted` — observation only

```csharp
field.Pasted += (_, args) => log.AppendLine ($"Inserted: {args.Text}");
```

Raised after `OnPaste` has consumed the paste.

## Customizing paste behavior in a custom view

The default `View` declines pastes (its `OnPaste` returns `false`). Text-input views override two virtual methods:

```csharp
protected override string OnSanitizingPaste (string raw)
{
    // Return the text you want inserted. Strip controls, normalize line
    // endings, etc. Default implementation strips C0/C1 controls except \t \n \r.
}

protected override bool OnPaste (string sanitized)
{
    // Insert the sanitized text. Return true if you consumed the paste.
}
```

## Default behavior in TextField / TextView

| View        | `OnSanitizingPaste`                                                          | `OnPaste`                                  |
|-------------|------------------------------------------------------------------------------|--------------------------------------------|
| `TextField` | First line only, strip C0/C1 controls (including tab).                      | Insert at cursor; respects `ReadOnly`.     |
| `TextView`  | Normalize `\r` and `\r\n` to `\n`; strip C0/C1 controls except tab/newline. | Insert (multi-line aware); respects `ReadOnly`. |

`TextField`'s "first line only" matches the legacy clipboard `Paste` command. `TextView`'s line-ending normalization mirrors [Windows Terminal's `FilterStringForPaste`](https://github.com/microsoft/terminal/blob/main/src/types/utils.cpp).

## Terminals without bracketed paste support

On terminals that do not support bracketed paste mode (older versions of `xterm` without the feature compiled in, or terminals where the user has explicitly disabled it), the markers never arrive. The paste flows through as ordinary key events and the `Paste` event never fires. No probing or fallback is needed.

## Stranded pastes

If the terminal sends `ESC[200~` but the matching `ESC[201~` is dropped (broken connection, terminated remote shell), the parser would otherwise hold the buffered paste content forever. Terminal.Gui flushes the partial buffer as a `Paste` event after a 5-second idle timeout measured from the most recent paste byte so an active slow paste is not cut off prematurely. There is also a hard cap of 1 MiB on the paste buffer size; payloads exceeding it are truncated, and the remaining bytes are discarded until the matching end marker arrives so tail bytes do not leak into normal input processing.

## Security considerations

Terminal.Gui trusts the *terminal* to sanitize the paste payload. Terminals such as xterm, Windows Terminal, Alacritty, and kitty already strip dangerous control sequences from the clipboard before bracketing — see [`xterm` `allowPasteControls`](https://invisible-island.net/xterm/xterm-paste64.html), [Windows Terminal `FilterStringForPaste`](https://github.com/microsoft/terminal/blob/main/src/types/utils.cpp), and [Alacritty's pre-bracket filter](https://github.com/alacritty/alacritty/blob/master/alacritty/src/event.rs).

For defense in depth, the default `View.OnSanitizingPaste` strips C0/C1 control characters before inserting. `TextField` and `TextView` apply additional view-specific sanitization. Applications that consume the raw `Application.Paste` event are responsible for their own sanitization.

## Disabling bracketed paste

To opt out, intercept the driver setup and skip the enable sequence. The relevant constant is <xref:Terminal.Gui.Drivers.EscSeqUtils.CSI_EnableBracketedPaste>. There is no built-in application-level toggle; if you have a need for one, file a feature request.

## Related

- <xref:Terminal.Gui.App.IApplication.Paste> — application-level event (raw payload)
- <xref:Terminal.Gui.ViewBase.View.Pasting> — cancellable view event, mutable `Text`
- <xref:Terminal.Gui.ViewBase.View.Pasted> — view event after insertion
- <xref:Terminal.Gui.ViewBase.View.OnSanitizingPaste(System.String)> — view virtual hook for filtering
- <xref:Terminal.Gui.ViewBase.View.OnPaste(System.String)> — view virtual hook for insertion
- <xref:Terminal.Gui.Input.Command.Paste> — the canonical paste command
- <xref:Terminal.Gui.Input.PasteEventArgs> — `IApplication.Paste` arguments
- <xref:Terminal.Gui.Input.PastingEventArgs> — `View.Pasting` arguments
- <xref:Terminal.Gui.Input.PastedEventArgs> — `View.Pasted` arguments
- <xref:Terminal.Gui.Drivers.EscSeqUtils.CSI_EnableBracketedPaste> / <xref:Terminal.Gui.Drivers.EscSeqUtils.CSI_DisableBracketedPaste> — driver-level constants
