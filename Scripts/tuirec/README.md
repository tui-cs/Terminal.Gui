# Recording Terminal.Gui Apps with `tuirec`

Use this guide when an issue or PR asks for a GIF/video capture of a Terminal.Gui
app or scenario. The recording tool is [`tui-cs/tuirec`](https://github.com/tui-cs/tuirec) —
a Go CLI that spawns the target app in a PTY, injects keystrokes, records terminal
output as an asciinema v2 cast, and renders an animated GIF via `agg`.

## Install

```powershell
# Requires Go 1.22+
go install github.com/tui-cs/tuirec/cmd/tuirec@latest
tuirec --version

# agg is auto-downloaded on first use — no separate install needed.
```

Verify: `tuirec --version`. If not on PATH, add Go's bin dir
(`$(go env GOPATH)\bin` on Windows, `$(go env GOPATH)/bin` on Linux/macOS) to PATH.

> **PowerShell vs. bash.** The snippets below are PowerShell (the project's
> default shell). The **raster recipes are Linux/macOS only** (Windows ConPTY
> can't capture Kitty/sixel), so where it matters this guide gives a bash version
> too. The mechanical translations: `Select-String -Pattern 'x'` →
> `grep -o 'x' | wc -l`, `Copy-Item a b` → `cp a b`, `$ks = '...'` →
> `ks='...'`, and the backtick line-continuation `` ` `` → `\`.

### Fresh container / clean clone

`tuirec` (Go) and `ScenarioRunner` (.NET) both need toolchains the Install
section assumes are present. On a clean Linux box:

```bash
# .NET SDK — match global.json (read the version from it; currently 10.0.100)
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 10.0.100 --install-dir ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"

# Go 1.22+ (if missing: distro package manager, or https://go.dev/dl)
# tuirec installs into GOPATH/bin, which is often off-PATH:
go install github.com/tui-cs/tuirec/cmd/tuirec@latest
export PATH="$(go env GOPATH)/bin:$PATH"

tuirec --version && dotnet --version
```

## Quick Start — Recording a UICatalog Scenario

```powershell
# 1. Build ScenarioRunner (do this ONCE before recording)
dotnet build Examples/ScenarioRunner/ScenarioRunner.csproj -c Release

# 2. Record (cross-platform: use dotnet to run the DLL)
$dll = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll"
$ks = 'wait:1200,Tab,Tab,wait:400,A,wait:1800,B,o,wait:1800,E,wait:1800,Tab,wait:400,CursorDown,CursorDown,CursorDown,wait:400,Shift+F10,wait:1500,Escape,wait:400,Escape'

tuirec record `
    --binary dotnet `
    --args "$dll,run,Character Map" `
    --name CharacterMap `
    --title "Character Map" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 1500 `
    --cols 120 --rows 30 `
    --open --copy
```

Output: `artifacts/CharacterMap.gif` and `artifacts/CharacterMap.cast`.

Copy the GIF to the scenario directory:
```powershell
Copy-Item artifacts/CharacterMap.gif Examples/UICatalog/Scenarios/CharacterMap/CharacterMap.gif
```

---

## Recording UICatalog Scenarios

### Prerequisites

1. **Build ScenarioRunner** — always build before recording to avoid startup noise:
   ```powershell
   dotnet build Examples/ScenarioRunner/ScenarioRunner.csproj -c Release
   ```
2. **Know the scenario name** — list available scenarios:
   ```powershell
   dotnet run --project Examples/ScenarioRunner -c Release --no-build -- list
   ```

### Finding the Right Keystrokes

Each scenario has a `GetDemoKeyStrokes()` method that defines a canonical
interaction sequence for benchmarking. **Use this as your starting point:**

```powershell
# Find the demo keystrokes for a scenario:
grep -n "GetDemoKeyStrokes" Examples/UICatalog/Scenarios/<ScenarioFile>.cs
```

The demo keystrokes show what keys the scenario expects and what UI flow is
interesting. Translate them to tuirec syntax:

| Terminal.Gui Key | tuirec Token |
|---|---|
| `Key.CursorDown` | `CursorDown` |
| `Key.CursorLeft` | `CursorLeft` |
| `Key.Tab` | `Tab` |
| `Key.Tab.WithShift` | `Shift+Tab` |
| `Key.Enter` | `Enter` |
| `Key.Esc` | `Esc` |
| `Key.B` | `B` (or `` `B` `` for literal) |

### Composing the Keystroke Script

**Principles for a great recording:**

1. **Start with `wait:1000`** — let the UI render fully after startup-delay.
2. **Add `wait:` between logical steps** — `wait:500` to `wait:1500` between
   groups of actions so viewers can follow what's happening.
3. **Keep it short** — 10–20 seconds of real-time interaction. Fewer keystrokes
   with generous waits beats many rapid keystrokes.
4. **Show variety** — demonstrate 2–3 features of the scenario, not just
   scrolling. Navigate between controls, trigger category changes, etc.
5. **End with `Escape`** — the default Terminal.Gui quit key.
6. **Avoid wide glyphs** — Emoji and CJK characters cause misaligned rendering
   in terminal recordings (agg renders each cell as monospace but wide glyphs
   consume 2 cells). Prefer categories with single-width characters (Arrows,
   Box Drawing, Block Elements, Mathematical Operators, etc.).

### Template Command

```powershell
$dll = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll"
$ks = '<your keystroke script here>'

tuirec record `
    --binary dotnet `
    --args "$dll,run,<Scenario Name>" `
    --name <ScenarioName> `
    --title "<Scenario Name>" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 2000 `
    --cols 120 --rows 30 `
    --verbosity high `
    --open --copy

# Copy GIF to scenario directory
Copy-Item artifacts/<ScenarioName>.gif Examples/UICatalog/Scenarios/<ScenarioDir>/<ScenarioName>.gif
```

### Output File Placement

GIFs live **alongside the `.cs` file they document**:

| What | Where |
|------|-------|
| Scenario in a subdirectory | `Examples/UICatalog/Scenarios/<ScenarioDir>/<ScenarioName>.gif` |
| Scenario directly in `Scenarios/` | `Examples/UICatalog/Scenarios/<ScenarioName>.gif` |
| View-derived class | `docfx/images/views/<ViewName>.gif` |

Use `--name <ScenarioName>` (PascalCase matching the class name) so the output
file is named correctly. The `--name` value determines the artifact filenames.

### Critical: `--kitty-keyboard` Decision

**Known bug ([tui-cs/tuirec#54](https://github.com/tui-cs/tuirec/issues/54)):**
tuirec currently encodes navigation keys (`CursorUp`, `CursorDown`, `CursorLeft`,
`CursorRight`, `PageUp`, `PageDown`, `Home`, `End`) incorrectly under
`--kitty-keyboard` — it sends fabricated CSI u codepoints that the Kitty spec
doesn't define. Terminal.Gui ignores or misinterprets these sequences.

**Workaround until fixed:**
- **Omit `--kitty-keyboard`** for demos that use navigation keys.
- **Add `--kitty-keyboard`** only when you need modifier disambiguation for
  non-navigation keys (`Ctrl+M` vs Enter, `Ctrl+I` vs Tab, `Ctrl+Q`, etc.)
  and the demo doesn't rely on arrow/page/home/end keys.

Once the bug is fixed, `--kitty-keyboard` should be the default for all
Terminal.Gui recordings (it provides cleaner modifier handling).

### `--args` for ScenarioRunner

The `--args` flag uses **comma-separated** values (not space-separated):

```powershell
--args "run,Character Map"      # Correct: two args ["run", "Character Map"]
--args "run Character Map"      # WRONG: one arg "run Character Map"
```

### PowerShell Quoting

Always assign keystrokes to a **single-quoted** `$ks` variable to preserve
backtick literals:

```powershell
# Correct — single quotes prevent PowerShell backtick interpolation:
$ks = 'wait:1000,`search text`,Enter,wait:500,Escape'

# WRONG — PowerShell eats the backticks:
--keystrokes "wait:1000,`search text`,Enter"
```

### Example: Character Map Scenario

```powershell
$dll = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll"

# Navigate to category list, browse Arrows → Box Drawing → Emoji, then context menu
$ks = 'wait:1200,Tab,Tab,wait:400,A,wait:1800,B,o,wait:1800,E,wait:1800,Tab,wait:400,CursorDown,CursorDown,CursorDown,wait:400,Shift+F10,wait:1500,Escape,wait:400,Escape'

tuirec record `
    --binary dotnet `
    --args "$dll,run,Character Map" `
    --name CharacterMap `
    --title "Character Map" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 1500 `
    --cols 120 --rows 30 `
    --open --copy
```

**Script breakdown:**

| Step | Tokens | What happens |
|------|--------|--------------|
| 1 | `wait:1200` | Let the CharMap UI fully render |
| 2 | `Tab,Tab` | Move focus to category list |
| 3 | `A` | CollectionNavigator jumps to "Arrows" |
| 4 | `wait:1800` | Pause so viewer sees arrow characters |
| 5 | `B,o` | Type "Bo" — jumps to "Box Drawing" |
| 6 | `wait:1800` | Pause so viewer sees box-drawing characters |
| 7 | `E` | Type "E" — jumps to "Emoji" |
| 8 | `wait:1800` | Pause so viewer sees emoji characters |
| 9 | `Tab` | Return focus to charmap grid |
| 10 | `CursorDown` ×3 | Navigate to a glyph |
| 11 | `Shift+F10` | Open context menu (Copy Glyph / Copy Code Point) |
| 12 | `wait:1500,Escape` | Let viewer see the menu, then dismiss |
| 13 | `Escape` | Quit |

**Key techniques demonstrated:**
- **CollectionNavigator typing** — type category name prefixes to jump directly
  (much better than scrolling through dozens of categories with arrow keys)
- **Context menu** — `Shift+F10` (the `PopoverMenu.DefaultKey`) shows the
  right-click menu on the selected glyph
- **Generous waits** — 1800ms between feature demonstrations so viewers
  can absorb each state change

---

## Verifying inline output and mouse reporting

To verify non-clearing resize on Windows, install `pywinpty` and `pyte`, build ScenarioRunner in Release,
then run `python Scripts/tuirec/verify-inline-resize.py`. This uses ConPTY directly because
tuirec's keystroke scripts do not resize the PTY. The check resizes during streaming and an
expanded preview, commits it, submits edited input after repeated resizes, and checks ordered,
exactly-once history and clean exit. It saves `artifacts/InlineResize.cast`, including resize
events, for replay. To select a different build, pass `--dll PATH`. A cast is also saved on
failure as diagnostic evidence; script exit status determines whether verification passed.
Old live frames may enter history during uncertain recovery; the live viewport must contain
one current frame.

To verify actual terminal reflow and cursor clamping on Linux or macOS, install `tmux`, build
ScenarioRunner in Release, and run `python3 Scripts/tuirec/verify-inline-resize-tmux.py`.
This creates an isolated tmux server with six shell sentinels and tests 30 height changes at
40 ms intervals during streaming, after output settles, and with a seven-row preview. The burst
loop injects no input/capture waits between changes. It checks all 24 messages and six shell lines
after each phase, input-driven repaint, preview commitment, clean exit, and actual tmux line-extent
behavior after CSI 2K. Snapshots include history/blank-row counts; evidence records the seed,
resize timestamps and history growth for each burst, with an explicit `passed` field.

```bash
python3 Scripts/tuirec/verify-inline-resize-tmux.py --seed 7
python3 Scripts/tuirec/verify-inline-resize-tmux.py --seed 7 --burst-mode width --burst-steps 40
python3 Scripts/tuirec/verify-inline-resize-tmux.py --seed 7 --burst-mode mixed --burst-ms 60
```

Evidence defaults to `artifacts/InlineResize-tmux.json`. Pass `--dll PATH` or `--evidence PATH`
to select a build or retain separate runs. Repeat seeds 0 through 9 to sweep different bursts.

To check the Inline Transcript scenario's history, keyboard editing, and F2 mouse toggle,
build ScenarioRunner in Release, install `pyte`, and run:

```bash
python3 Scripts/tuirec/verify-inline-output.py --verify-mouse
python3 Scripts/tuirec/verify-inline-output.py --columns 50 --rows 3 --verify-mouse
python3 Scripts/tuirec/verify-inline-output.py --verify-mouse --verify-shrink
```

The verifier asserts all 24 completed messages appear once and in order, editing submits
`hello` once, and mouse reporting is disabled at startup, toggled on/off, and disabled on
exit. To inspect a saved recording, pass `--cast path/to/InlineTranscript.cast --verify-mouse`.
Terminal-native selection and clipboard behavior still require checks in a real terminal.

The `--verify-shrink` check also expands the live preview with F5, commits it with F6,
and expands/collapses it again. It checks that the completed preview appears once, no
live preview enters history, and released rows below the composer are blank. A terminal
with at least seven rows also verifies that all four preview rows were visible.

## Recording Individual View Sub-classes with EnableForDesign

(Coming soon — will use a dedicated design-mode runner that instantiates
a single View with `EnableForDesign()` and records its interactions.)

---

## Recording Standalone Example Apps

For apps in `Examples/` that are not UICatalog scenarios:

```powershell
$dll = "./Examples/<AppName>/bin/Release/net10.0/<AppName>.dll"
$ks = 'wait:1000,<keystrokes>,Escape'

tuirec record `
    --binary dotnet `
    --args "$dll" `
    --name <app-id> `
    --title "<App Name> Demo" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 2000 `
    --cols 120 --rows 30 `
    --open --copy
```

---

## Raster graphics: Kitty (default) and sixel

Terminal.Gui's `ImageView` (with `UseRasterGraphics = true`) picks the best
raster protocol the terminal advertises: **Kitty graphics** when available,
otherwise **sixel**, otherwise cell rendering. Which one a recording captures
depends on what identity `tuirec` presents to the app.

- **`tuirec` ≥ v0.9.0 defaults to Kitty graphics.** It advertises a deterministic
  Kitty identity (a `KITTY_WINDOW_ID` marker) to the recorded app, so apps that
  prefer Kitty emit Kitty image escapes (`ESC _ G … ST`). The pinned
  `agg` (`v1.11.0-sixel`, built on a Kitty-capable `avt`) renders them in the GIF.
  This is the path the UICatalog **Mandelbrot** and **Images** scenarios take by
  default. Terminal.Gui detects Kitty support purely from the environment, so the
  app reports `Kitty … active` in its capability matrix with no extra flags.
- **Sixel** is still used when the app does not support Kitty, or you force the
  sixel path in-app (e.g. the Mandelbrot scenario's "Sixel" protocol option).
- **Both are Linux/macOS only.** Windows ConPTY strips both Kitty graphics APC
  strings and sixel DCS from the output stream, so neither is captured there.

**Confirm which protocol the cast captured** (the `.cast` is JSON, so the escape
introducer shows up as ``):

```powershell
# PowerShell
Select-String -Path artifacts/<name>.cast -Pattern 'u001b_G' | Measure-Object  # Kitty
Select-String -Path artifacts/<name>.cast -Pattern 'u001bPq' | Measure-Object  # sixel
```

```bash
# Linux/macOS — the raster recipes only run here
grep -o 'u001b_G' artifacts/<name>.cast | wc -l   # Kitty (expected by default)
grep -o 'u001bPq' artifacts/<name>.cast | wc -l   # sixel (only when forced)
```

The sixel cell-size verification below applies to the **sixel** path; the
[#84](https://github.com/tui-cs/tuirec/issues/84) cell-resolution mismatch is a
sixel concern and does not apply when the app renders via Kitty graphics.

> **The `adjusted agg font-size … to align the sixel cell grid (#84)` log line is
> expected, not an error.** `tuirec` ≥ v0.9.0 auto-calibrates agg's font size to
> close the #84 mismatch during recording. It's harmless for the Kitty path —
> don't chase it.

> **Smooth zoom/pan recordings.** Each keystroke pauses `--keystroke-delay` ms
> (default 200). For continuous-looking motion (e.g. zooming/panning an image),
> use a shorter delay (`--keystroke-delay 130`) and many small steps rather than
> a few large ones. Note that in-app *mouse-wheel* zoom may not work under
> `tuirec` (some views bind the wheel to pan); prefer the keyboard zoom keys.

### Exact recipe — `docfx/images/Mandelbrot.gif`

This reproduces the committed Mandelbrot hero GIF in one shot. Because raster
capture is **Linux/macOS only**, the recipe is shown in bash; build
`ScenarioRunner` first (see Prerequisites), then run from the repo root. (In
PowerShell on macOS, translate per the *PowerShell vs. bash* note above:
`$ks = '...'`, backtick line-continuations, `Copy-Item`.)

```bash
dll="./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll"
# Tour: full set → zoom into the seahorse valley → pan across it → zoom out → reset
ks='wait:1600,PageUp,wait:150,CursorLeft,CursorDown,CursorDown,wait:300,PageUp,wait:120,PageUp,wait:120,PageUp,wait:120,PageUp,wait:850,CursorRight,wait:150,CursorRight,wait:150,CursorUp,wait:180,CursorLeft,wait:150,CursorLeft,wait:150,CursorLeft,wait:150,CursorDown,wait:180,CursorRight,wait:150,CursorRight,wait:600,PageDown,wait:120,PageDown,wait:120,PageDown,wait:120,PageDown,wait:120,PageDown,wait:300,Home,wait:1100,Esc'

tuirec record --binary dotnet --args "$dll,run,Mandelbrot" --name Mandelbrot \
    --title "Mandelbrot" --keystrokes "$ks" \
    --startup-delay 2000 --drain 1200 --cols 120 --rows 30 --keystroke-delay 130

cp artifacts/Mandelbrot.gif docfx/images/Mandelbrot.gif
```

Validate (robust invariants — see the softened counts below):

```bash
grep -o 'u001b_G' artifacts/Mandelbrot.cast | wc -l   # Kitty: expect thousands
grep -o 'u001bPq' artifacts/Mandelbrot.cast | wc -l   # sixel: expect 0
# Extract a mid-zoom frame without ImageMagick/ffmpeg (needs python3 + Pillow):
python3 -c "from PIL import Image; im=Image.open('artifacts/Mandelbrot.gif'); im.seek(im.n_frames//2); im.convert('RGB').save('/tmp/mid.png')"
```

**Why each part matters (don't "improve" these blindly):**

- **The image view fills the display.** The scenario anchors `MandelbrotImageView`
  at `(0,0)` with `Dim.Fill()` so the fractal is large enough for motion to read.
  If you record a small centered image, the demo looks cramped.
- **Zoom is keyboard-only and center-anchored.** `PageUp`/`PageDown` zoom about
  the *center* (the in-app mouse wheel pans, it does not zoom). So you must pan
  the target to the center first, then zoom.
- **Target = the seahorse valley**, center ≈ `(-0.745, +0.11)` — the cusp where
  the main cardioid meets the period-2 bulb. The opening `PageUp` shrinks the pan
  step so `CursorLeft,CursorDown,CursorDown` lands at ≈ `(-0.74, +0.105)` instead
  of overshooting onto the (mostly black) antenna filament at `-0.8`.
- **Stop around span ≈ 1.0** (about 5 `PageUp`s total). At the scenario's default
  80 iterations the valley goes mostly black past ~span 0.5; span ≈ 1.0 keeps the
  colorful seahorse filaments. For a deeper dive you'd raise the iteration count
  first (the Iterations control, or `DEFAULT_ITERATIONS`).
- **Validate against invariants, not exact counts.** The cast holds **thousands**
  of `u001b_G` (Kitty) payloads and **zero** `u001bPq` (sixel), and the GIF is
  **~0.9 MB**. The precise payload count drifts (±a few hundred) with timing and
  the auto font-size adjust — don't treat it as a target. Then open `/tmp/mid.png`
  (extracted above) and confirm the in-app readout reads **Center X ≈ −0.74,
  Center Y ≈ 0.105, Span ≈ 1.0** over colorful seahorse structure — measuring the
  landmark, per this guide's "measure, don't eyeball" tenet, not eyeballing a vibe.

---

## Verifying Placement and Size (measure — don't eyeball)

**The recurring trap.** Confirming a sixel *appears* in the GIF — or that agg
rendered it faithfully at the cursor cell the app requested — does **not** prove
it is correct. A pixel-perfect render of a raster that was *built from the wrong
cell size* is still the wrong size on screen. "Looks present" and "pipeline is
faithful" are proxies. The invariant you must actually check is:

> **Does the rendered sixel cover the cells the app intended — in both position
> and size?**

Verify that with a measurement, not your eyes. A ~4% size error is invisible by
sight and obvious by arithmetic.

**Why this bites with tuirec specifically.** tuirec advertises a sixel cell
resolution (e.g. `8×17` px) that does **not** match agg's actual rendered font
cell (~`8.3×18.8` px at the default `--font-size 14`) — see
[#84](https://github.com/tui-cs/tuirec/issues/84). An app that *correctly* sizes
its raster as `cells × reportedResolution` (and fills exactly on a real sixel
terminal) therefore renders ~4% **undersized** under tuirec. Do not "fix" the app
for this; verify it and attribute it correctly.

**The check — calibrate agg's real cell, then reconcile:**

1. Extract a frame from the GIF (any decoder; e.g. ImageSharp
   `Image.Load(gif).Frames.CloneFrame(i)`).
2. Measure agg's **actual** cell size from a *known* grid reference — e.g. a
   border/box that spans a known number of columns:
   `cellPx = borderSpanPx / (spannedCells)`. (Don't trust `imageWidth / cols` —
   agg adds margins.)
3. Read the resolution the **app** used from the cast: the sixel DCS header
   `P…q"asp;asp;WIDTH;HEIGHT` gives raster pixel size; divide by the raster's
   cell count to get the app's px-per-cell.
4. **If agg's measured cell ≠ the app's px-per-cell, the sixel is mis-sized —
   and that is the tuirec mismatch ([#84](https://github.com/tui-cs/tuirec/issues/84)),
   not an app bug.** Then confirm the sixel's rendered bounding box actually spans
   the target region (the columns/rows it was meant to cover), not merely that it
   exists.

Run this whenever a sixel is sized or aligned to the text grid (bordered image
views, insets, bottom bands — anything grid-anchored). It turns "I think it looks
right" into a number, which is the only thing that catches sub-cell and
few-percent errors.

> **General principle (applies beyond sixel):** verify the *invariant the change
> was supposed to satisfy*, measured against the design intent — not that the tool
> ran, the file is non-empty, or the screenshot "has the thing in it." When you've
> just fixed one symptom, the next bug often hides in the dimension you didn't
> measure.

## Validation Checklist

After every recording, verify:

- [ ] `tuirec record` exited with code 0 and wrote both `.gif` and `.cast`.
- [ ] **Error check** — no errors in the cast:
  ```powershell
  Select-String -Path artifacts/<name>.cast -Pattern "error|unknown|not found|usage:" -CaseSensitive:$false
  ```
  ```bash
  grep -iE "error|unknown|not found|usage:" artifacts/<name>.cast
  ```
- [ ] **GIF is not blank** — file size > 100KB for a typical scenario recording.
      (A blank/static GIF is typically < 50KB.)
- [ ] **Visual check** — open the GIF (`--open` flag) and confirm:
  - The app content is visible (menu bar, controls, content).
  - The interaction sequence is visible (scrolling, focus changes, etc.).
  - The recording ends cleanly (no frozen frame or abrupt cutoff).
- [ ] **Output path is correct** — scenario GIFs go with their scenario code:
  ```
  Examples/UICatalog/Scenarios/<ScenarioDir>/<ScenarioName>.gif
  ```
- [ ] **Raster content recorded on Linux/macOS** — Kitty graphics APC and sixel
      DCS cannot be captured through Windows ConPTY. Confirm the expected protocol
      made it into the cast (Kitty is the default for raster apps; see *Raster
      graphics* above):
  ```powershell
  Select-String -Path artifacts/<name>.cast -Pattern 'u001b_G' | Measure-Object  # Kitty
  Select-String -Path artifacts/<name>.cast -Pattern 'u001bPq' | Measure-Object  # sixel
  ```
  ```bash
  grep -o 'u001b_G' artifacts/<name>.cast | wc -l   # Kitty
  grep -o 'u001bPq' artifacts/<name>.cast | wc -l   # sixel
  ```
- [ ] **Grid-anchored sixel measured, not eyeballed** — if the sixel is sized or
      aligned to the text grid, calibrate agg's real cell and confirm the rendered
      bbox covers the target columns/rows (see *Verifying Placement and Size*
      above). A ~4% undersize from [#84](https://github.com/tui-cs/tuirec/issues/84)
      is invisible by sight.

---

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| No raster output on Windows | **Windows ConPTY strips Kitty graphics APC and sixel DCS** and does not pass the DA1 sixel handshake — the app detects no raster support | Record raster content (Kitty or sixel) on Linux/macOS (see `tuirec agent-guide`). On Windows you can still verify the app's raster code path runs (e.g., via an app-level force flag) by checking redraw activity in the `.cast`, but image pixels will not appear |
| Image renders via sixel instead of Kitty (or vice versa) | The app picks its preferred protocol from what the terminal advertises; `tuirec` ≥ v0.9.0 advertises Kitty by default | Confirm the captured protocol in the cast (`u001b_G` for Kitty, `u001bPq` for sixel). To force sixel, use the app's own protocol control (e.g. the Mandelbrot scenario's "Sixel" option) |
| Sixel renders ~4% too small / short of a border | tuirec advertises a cell resolution that doesn't match agg's rendered font cell ([#84](https://github.com/tui-cs/tuirec/issues/84)) | App is correct (fills on a real terminal). Verify by measurement (see *Verifying Placement and Size*); attribute to tuirec, not the app. Until fixed, only a tuirec-specific over-render hack would close the gap |
| Wide glyphs misaligned in GIF | Emoji/CJK chars are 2-cell wide; agg renders per-cell | Avoid emoji/CJK categories; use single-width ranges (Arrows, Box Drawing, etc.) |
| Nav keys ignored with `--kitty-keyboard` | tuirec bug [#54](https://github.com/tui-cs/tuirec/issues/54) — sends wrong codepoints | Remove `--kitty-keyboard` |
| App doesn't quit | Wrong quit key or key not delivered | Use `Escape` (the default quit key); check `--kitty-keyboard` interaction |
| Blank frames at start/end | Pre/postroll not trimmed | `--trim` is on by default in v0.4.2+; ensure tuirec is up-to-date |
| GIF validation: 1 frame | `--trim` removes all frames for static views | Use `--trim=false` for views with no visual change during demo |
| Recording times out | App stuck / wrong keystrokes | Check with `--verbosity high`, fix script |
| `--binary` permission error | Relative path on Windows | Use `./` prefix or absolute path with forward slashes |
| Backtick text missing | PowerShell interpolation | Use single-quoted `$ks` variable |

---

## Agent Workflow Summary

When asked to record a scenario GIF:

1. **Build** — `dotnet build Examples/ScenarioRunner -c Release`
2. **Find scenario name** — `dotnet run --project Examples/ScenarioRunner -c Release --no-build -- list`
3. **Read `GetDemoKeyStrokes()`** — find it in the scenario source file
4. **Compose keystrokes** — translate to tuirec syntax, add waits, keep short
5. **Record** — `tuirec record --binary ... --args "run,<Name>" --keystrokes $ks ...`
6. **Validate** — error-grep the cast, check GIF file size, confirm the interaction
   played. For anything sized/aligned to the grid (sixels especially),
   **measure** placement and size against the design intent — do not stop at
   "the screenshot has the thing in it" (see *Verifying Placement and Size*).
7. **If nav keys fail** — remove `--kitty-keyboard` and retry
8. **Report** — share the output paths and exact command used

---

## Reference

- **tuirec repo:** https://github.com/tui-cs/tuirec
- **Full keystroke syntax:** `tuirec agent-guide` (embeds the complete reference)
- **CLI flags:** `tuirec record --help`
- **ScenarioRunner:** `Examples/ScenarioRunner/` — CLI that runs individual UICatalog scenarios
