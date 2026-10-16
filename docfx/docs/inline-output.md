# Printing above an inline application

To append completed messages above a live text region, initialize an application in
<xref:Terminal.Gui.App.AppModel.Inline> mode using the ANSI driver, then await
`app.PrintAboveAsync(...)`. Keep a small fixed-height composer or status area live;
printed text moves down through available terminal space before scrolling into native history.
The terminal controls history retention, reflow, selection, and copying.

```csharp
using IApplication app = Application.Create ();
app.AppModel = AppModel.Inline;
app.Init (DriverRegistry.Names.ANSI);
```

To print styled lines from a background producer during a running session, supply
concrete attributes and await each batch:

```csharp
await app.PrintAboveAsync (
    owner,
    [new InlineOutputLine ([
        new InlineOutputSpan ("completed: ", new Terminal.Gui.Drawing.Attribute (Color.Green, Color.None, TextStyle.Bold)),
        new InlineOutputSpan (message)
    ])],
    cancellationToken).ConfigureAwait (false);
```

`owner` is the <xref:Terminal.Gui.App.SessionToken> returned by `Begin` or reported by
`SessionBegun`. Ending it cancels pending output, including an outer owner ended beneath
a modal. Unowned calls are canceled when the final session ends. Calls after Init and
before the first Begin wait for that session. Calls after final End cancel rather than
migrating to a later session. Calls before Init or during disposal throw.

The [Inline Transcript UICatalog scenario](https://github.com/tui-cs/Terminal.Gui/blob/develop/Examples/UICatalog/Scenarios/InlineTranscript.cs)
provides an editable composer, a collapsible live preview, and an awaited background producer.
To run it directly, run `dotnet run --project Examples/ScenarioRunner -- run "Inline Transcript"`.

## Native text selection

To select and copy completed output with the terminal's normal mouse gestures, disable
mouse reporting before initialization:

```csharp
using IApplication app = Application.Create ();
app.AppModel = AppModel.Inline;
app.Mouse.IsMouseDisabled = true;
app.Init (DriverRegistry.Names.ANSI);
```

The built-in ANSI driver honors this setting at startup and during a session, in both
inline and fullscreen modes. Mouse reporting remains enabled by default. To change the
setting while running, use the UI thread or `app.Invoke(...)`. Disabling releases an
active mouse grab, stops hold repetition, and clears hover state. Keyboard editing,
bracketed paste, and `PrintAboveAsync` continue to work. Other drivers suppress application
mouse events but do not configure native terminal selection.

On Windows, the ANSI driver also disables console mouse input and enables Quick Edit
while mouse reporting is disabled; the original console mode is restored on disposal.
Classic conhost may pause application activity while a Quick Edit selection is active.
The terminal controls selection and copying, including whether a selection persists
while new output arrives.

The Inline Transcript scenario starts with mouse reporting disabled. To toggle between
terminal selection and application mouse interaction, press **F2**.

## Text and ordering

Each <xref:Terminal.Gui.Drawing.InlineOutputLine> is a logical line, wrapped using the
execution width and existing grapheme width rules. Spans are immutable snapshots;
null attributes reset to terminal defaults. A grapheme crossing span boundaries uses
the first scalar's style. A glyph wider than a one-column terminal becomes a replacement
glyph. A leading isolated zero-width grapheme gets a dotted-circle base.

The string overload normalizes CRLF and CR to LF. One final LF terminates the last
line: `"a\n"` prints one line; `"a\n\n"` prints a line and a blank line. Empty text or
`"\n"` prints one blank line; an empty structured list emits nothing. Tabs expand to
eight-column stops. Structured spans reject line breaks, invalid UTF-16 and terminal
controls, including ESC and C0/C1 controls. Raw ANSI, OSC hyperlinks, Markdown and
raster output are not accepted. Literal URL text is supported.

All nonempty calls enqueue, including calls on the UI thread. FIFO means queue admission
order, with no ordering guarantee relative to unrelated Invoke calls. Only one batch is
printed at each eligible render pass. To preserve a producer's order, await batches or
submit one batch. The queue retains at most 256 requests and 4 Mi UTF-16 units, including
metadata and executing work. Overflow throws; it never drops old output. Native writes
are synchronous, so keep batches small for responsiveness.

To submit a completed live preview, mutate the view and capture a print task in one
synchronous owned dispatch, then await the print outside that dispatch:

```csharp
Task print = Task.CompletedTask;
await app.InvokeAsync (owner, () =>
{
    preview.Text = string.Empty;
    print = app.PrintAboveAsync (owner, completedText, cancellationToken);
}, cancellationToken);
await print;
```

To await from a UI callback, yield to the event loop. Blocking with `.Wait()` or `.Result`
prevents the rendering boundary from executing. Manually calling `Begin` establishes a
session but does not pump iterations continuously. Startup waits for the existing cursor
and size readiness gate; optional-probe completion alone cannot force an early insertion.

## Automatic live-region shrink

To let an inline composer shrink after removing a live preview, use a content-sized root:

```csharp
using Runnable composer = new ()
{
    Width = Dim.Fill (),
    Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 3)
};
```

The built-in ANSI driver reduces the reservation after layout reports a smaller height.
Explicit height changes and content-based `Dim.Func` sizes also work. Shrink keeps the
top row fixed, erases only the old live region, and repaints the compact view. It leaves
blank space below the region until growth or new output uses it. It does not scroll,
replay completed output, or erase shell output above the region. The next `PrintAboveAsync`
uses the smaller reservation, including when removing a preview and queuing its completed
text in one UI callback or owned dispatch.

Active runnable frames set a lower bound on the reserved height. Visible popovers and
active, pending, or retained raster graphics defer shrink. Space-dependent root positions
and heights (`Pos.Center`, `Dim.Fill`, `Dim.Percent`, and percentage/fill auto-size bounds)
retain their reservation to avoid layout feedback. To use `Dim.Func` for a shrinking root,
calculate its height from content rather than from `app.Screen.Height`.

A root can shrink to zero; queued printing waits until a positive live height returns.
A failed physical erase retains the published geometry and suppresses further inline
placement under the same failure policy as printing. Shrink publishes its new geometry
and schedules repaint before raising `ScreenChanged`.

To exercise this in Inline Transcript, press **F5** to expand/collapse the four-row preview.
To remove it and print its completed text in one callback, press **F6** while it is visible.

## Completion, failures and supported terminals

A successful task means insertion bytes and the final live-area erase were accepted by
the output sink, the new origin was published, and the live frame was invalidated. It
does not promise terminal acknowledgment, durable history, or successful delivery of the
subsequent frame. Frame retries repaint live content without repeating completed text.

Cancellation wins before claim. After claim, insertion finishes or faults, even if the
token becomes canceled. Invalid input or geometry and unsupported graphics fail before
insertion. An I/O failure can leave some bytes printed: the operation faults, is never
replayed, and pending/new output faults for that initialized lifetime. Further inline
frame and cursor placement is suppressed. Input and ordinary dispatch remain available
so the application can stop; disposal restores modes and styling without using a stale
origin. To resume printing, create a fresh initialized lifetime. Never blindly retry a
failed message. A failed task does not roll back application data or preview changes.

The built-in ANSI driver supports this operation on Windows, macOS and Linux using an
attached terminal, including a controlling terminal with redirected stdout. Headless
output, the `dotnet`/`windows` drivers, fullscreen or mismatched modes, and partial-width
Screen rectangles are rejected. Zero terminal or live dimensions defer until valid;
teardown and cancellation still settle pending requests.

Active, queued or incompletely erased Sixel/Kitty placements prevent insertion. Legacy
queued Sixels lack cell dimensions; once emitted, printing requires a fresh output
lifetime because cleanup cannot be proven. Exhausting the bounded Sixel placement tracker also
requires a fresh output lifetime. Ordinary raster rendering remains available
for applications that do not use this API.

Resize preservation remains separate work: the current resize handler still clears visible
rows and resets origin. Terminal reflow during a physical write, external Console writes,
raw cursor/mode changes, or another application sharing the terminal are outside the
serialization contract. Terminal width disagreement can affect wrapping.

The portable insertion geometry follows Ratatui's
[insert_before design](https://github.com/ratatui/ratatui/blob/d1ef782b48ddf0cb825c111d4d8b9adef572c0e1/ratatui-core/src/terminal/inline.rs),
and the renderer-owned queue follows Bubble Tea's
[print-message design](https://github.com/charmbracelet/bubbletea/blob/v1.3.10/standard_renderer.go).
