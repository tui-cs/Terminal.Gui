#!/usr/bin/env python3
"""Verify Inline Transcript across Windows ConPTY resizes; requires pywinpty and pyte."""

import argparse
from collections import defaultdict
import json
import os
from pathlib import Path
import re
import threading
import time

import pyte
import winpty


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    root = Path(__file__).resolve().parents[2]
    parser.add_argument("--dll", type=Path, default=root / "Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll")
    parser.add_argument("--cast", type=Path, default=root / "artifacts/InlineResize.cast")
    args = parser.parse_args()
    if not args.dll.is_file():
        parser.error("build Examples/ScenarioRunner first")

    environment = os.environ.copy()
    environment.update(TERM="xterm-256color", COLORTERM="truecolor")
    environment.pop("NO_COLOR", None)
    environment.pop("DisableRealDriverIO", None)
    process = winpty.PtyProcess.spawn(["dotnet", str(args.dll.resolve()), "run", "Inline Transcript"],
                                     dimensions=(18, 90), env=environment, backend=winpty.Backend.ConPTY)
    lock = threading.RLock()
    events = []
    failures = []
    started = time.monotonic()

    class Terminal(pyte.HistoryScreen):
        def write_process_input(self, data):
            process.write(data)

    screen = Terminal(90, 18, history=10000)
    stream = pyte.Stream(screen)

    def read():
        try:
            while True:
                chunk = process.read(65536)
                with lock:
                    events.append([time.monotonic() - started, "o", chunk])
                    stream.feed(chunk)
        except EOFError:
            return
        except Exception as error:
            failures.append(error)

    reader = threading.Thread(target=read, daemon=True)
    reader.start()

    def wait_for(predicate, description, timeout=8):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            with lock:
                if failures:
                    raise failures[0]
                if predicate():
                    return
            time.sleep(0.05)
        raise AssertionError(f"timed out waiting for {description}")

    def visible():
        return "\n".join(screen.display)

    def resize(columns, rows):
        with lock:
            # Stress tmux's policy: discard rows below the cursor first, including nonblank
            # live rows. pyte otherwise always removes top rows and misses cursor clamping.
            removed = min(max(0, screen.lines - rows), max(0, screen.cursor.y + 1 - rows))
            content = {row: defaultdict(lambda: screen.default_char, screen.buffer[row + removed])
                       for row in range(rows) if row + removed < screen.lines}
            for row in range(removed):
                screen.history.top.append(screen.buffer[row].copy())
            cursor_row = max(0, screen.cursor.y - removed)
            screen.resize(lines=rows, columns=columns)
            screen.buffer.clear()
            for row, line in content.items():
                for column in list(line):
                    if column >= columns:
                        del line[column]
                screen.buffer[row] = line
            screen.cursor.y = min(cursor_row, rows - 1)
            screen.cursor.x = min(screen.cursor.x, columns - 1)
            events.append([time.monotonic() - started, "r", f"{columns}x{rows}"])
            process.setwinsize(rows, columns)
        # Allow size polling, the scheduled cursor request, and the following repaint.
        time.sleep(0.4)
        wait_for(lambda: "Inline transcript" in visible(), "composer after resize")

    try:
        wait_for(lambda: "message 004:" in visible(), "streaming output", timeout=30)
        resize(50, 18)
        wait_for(lambda: "message 023:" in visible(), "all completed messages")
        process.write("\x1b[15~")  # F5
        wait_for(lambda: "LIVE PREVIEW row 4" in visible(), "expanded preview")
        resize(90, 12)
        process.write("\x1b[17~")  # F6
        wait_for(lambda: "preview: completed preview" in visible() and "LIVE PREVIEW" not in visible(), "preview commitment")
        for columns, rows in ((50, 18), (90, 18), (50, 12), (90, 18)):
            resize(columns, rows)
        process.write("after resize")
        process.write("\r")
        wait_for(lambda: "you: after resize" in visible(), "submission after resize")
        process.write("\x1b")
        wait_for(lambda: not process.isalive(), "clean Escape exit")
        reader.join(timeout=2)
        if process.exitstatus != 0:
            raise AssertionError(f"scenario exited with status {process.exitstatus}")
        with lock:
            lines = ["".join(cell.data for _, cell in sorted(row.items())) for row in screen.history.top] + screen.display
            transcript = "\n".join(lines)
            messages = re.findall(r"message (\d{3}):", transcript)
            if messages != [f"{number:03d}" for number in range(24)]:
                raise AssertionError(f"missing, duplicated, or reordered messages: {messages}")
            for completed in ("preview: completed preview", "you: after resize"):
                if transcript.count(completed) != 1:
                    raise AssertionError(f"expected exactly one {completed!r}")
            if "LIVE PREVIEW" in visible() or visible().count("Inline transcript") != 1:
                raise AssertionError("stale live content remains in the viewport")
        print("PASS: six resizes during streaming/preview; all 24 messages preserved in order")
        print("PASS: preview committed once; post-resize submission and clean exit; no stale live rows in viewport")
    finally:
        args.cast.parent.mkdir(parents=True, exist_ok=True)
        with args.cast.open("w", encoding="utf-8") as destination:
            destination.write(json.dumps({"version": 2, "width": 90, "height": 18}) + "\n")
            with lock:
                for event in events:
                    destination.write(json.dumps(event) + "\n")
        if process.isalive():
            process.terminate(force=True)
        process.close()


if __name__ == "__main__":
    main()
