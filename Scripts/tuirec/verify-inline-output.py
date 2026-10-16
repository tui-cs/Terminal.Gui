#!/usr/bin/env python3
"""Record and verify UICatalog's Inline Transcript scenario; requires tuirec and pyte."""

import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile

import pyte


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    root = Path(__file__).resolve().parents[2]
    parser.add_argument("--dll", type=Path, default=root / "Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll")
    parser.add_argument("--tuirec", default="tuirec")
    parser.add_argument("--columns", type=int, default=90)
    parser.add_argument("--rows", type=int, default=18)
    parser.add_argument("--cast", type=Path, help="Verify an existing cast instead of recording")
    parser.add_argument("--verify-mouse", action="store_true", help="Toggle F2 twice and verify native mouse reporting modes")
    args = parser.parse_args()
    if args.columns < 1 or args.rows < 1:
        parser.error("terminal dimensions must be positive")
    if args.cast:
        verify(args.cast, args.verify_mouse)
        return
    if not args.dll.is_file():
        parser.error("build Examples/ScenarioRunner in Release first")

    environment = os.environ.copy()
    environment.update(TERM="xterm-256color", COLORTERM="truecolor")
    environment.pop("NO_COLOR", None)
    environment.pop("DisableRealDriverIO", None)
    with tempfile.TemporaryDirectory(prefix="inline-verify-") as directory:
        cast = Path(directory) / "InlineTranscript.cast"
        subprocess.run(
            [args.tuirec, "record", "--binary", "dotnet",
             "--args", f"{args.dll.resolve()},run,Inline Transcript",
             "--inline", "--cols", str(args.columns), "--rows", str(args.rows),
             "--startup-delay", "1000", "--keystrokes",
             "wait:500,h,e,l,o,CursorLeft,l,Enter,wait:5000,"
             + ("F2,wait:300,F2,wait:300," if args.verify_mouse else "") + "Esc",
             "--drain", "1500", "--max-duration", "20",
             "--output", str(Path(directory) / "InlineTranscript.gif"),
             "--cast-output", str(cast)],
            env=environment, check=True, timeout=40,
        )
        verify(cast, args.verify_mouse)


def verify(cast, verify_mouse=False):
    output = []
    with cast.open(encoding="utf-8") as source:
        header = json.loads(next(source))
        screen = pyte.HistoryScreen(header["width"], header["height"], history=10000)
        stream = pyte.Stream(screen)
        for line in source:
            event = json.loads(line)
            if event[1] == "o":
                output.append(event[2])
                stream.feed(event[2])

    history = ["".join(cell.data for _, cell in sorted(line.items())) for line in screen.history.top]
    text = "\n".join(line.rstrip() for line in history + screen.display)
    comparison = text if header["width"] >= 30 else text.replace("\n", "")
    messages = re.findall(r"message (\d{3}):", comparison)
    if messages != [f"{number:03d}" for number in range(24)]:
        raise AssertionError(f"completed messages missing, duplicated, or reordered: {messages}")
    if header["width"] >= 30 and header["height"] >= 3:
        if comparison.count("you: hello") != 1:
            raise AssertionError("caret editing and Enter must submit 'hello' exactly once")
        if comparison.count("Inline transcript") > 1:
            raise AssertionError("live composer was replayed into history")
    if verify_mouse:
        raw = "".join(output)
        for mode in (1003, 1015, 1006):
            transitions = re.findall(rf"\x1b\[\?{mode}([hl])", raw)
            if transitions != ["l", "h", "l", "l"]:
                raise AssertionError(f"mouse mode {mode}: expected startup off, F2 on/off, exit off; got {transitions}")
        if header["width"] >= 30 and raw.index("\x1b[?1003h") < raw.index("message 023:"):
            raise AssertionError("mouse reporting enabled before the scripted F2 toggle")
    print(f"PASS: {header['width']}x{header['height']}; all 24 messages preserved in order")
    if verify_mouse:
        print("PASS: mouse reporting disabled at startup, toggled with F2, and disabled on exit")


if __name__ == "__main__":
    main()
