#!/usr/bin/env python3
"""Verify inline resize in a real, isolated tmux server on Linux/macOS (no Python packages)."""

import argparse
import json
from pathlib import Path
import re
import random
import shlex
import subprocess
import time
import uuid


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    root = Path(__file__).resolve().parents[2]
    parser.add_argument("--dll", type=Path, default=root / "Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.dll")
    parser.add_argument("--evidence", type=Path, default=root / "artifacts/InlineResize-tmux.json")
    parser.add_argument("--seed", type=int, default=7)
    parser.add_argument("--burst-steps", type=int, default=30)
    parser.add_argument("--burst-ms", type=float, default=40)
    parser.add_argument("--burst-mode", choices=("height", "width", "mixed"), default="height")
    args = parser.parse_args()
    if not args.dll.is_file():
        parser.error("build ScenarioRunner in Release or pass --dll")
    if args.burst_steps < 1 or args.burst_ms <= 0:
        parser.error("burst steps and interval must be positive")
    server = "codex-inline-" + uuid.uuid4().hex[:12]
    snapshots = []
    succeeded = False
    generator = random.Random(args.seed)
    bursts = []
    shell_lines = [f"SHELL CONTEXT {index}" for index in range(6)]

    def tmux(*arguments):
        return subprocess.check_output(["tmux", "-L", server, *arguments], text=True)

    def capture(history=False):
        return tmux("capture-pane", "-p", "-S", "-10000" if history else "0")

    def alive():
        return tmux("display-message", "-p", "#{pane_dead}").strip() == "0"

    def wait_for(predicate, description, timeout=30, allow_dead=False):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            if predicate():
                return
            if not allow_dead and not alive():
                raise AssertionError(f"scenario exited while waiting for {description}:\n{capture(True)}")
            time.sleep(0.05)
        raise AssertionError(f"timed out waiting for {description}:\n{capture(True)}")

    def snapshot(description):
        history = capture(True)
        snapshots.append({"description": description,
                          "geometry": tmux("display-message", "-p", "#{pane_width}x#{pane_height}").strip(),
                          "viewport": capture(), "history_and_viewport": history,
                          "history_rows": int(tmux("display-message", "-p", "#{history_size}")),
                          "blank_rows": sum(not line.strip() for line in history.splitlines()),
                          "live_titles": history.count("Inline transcript")})

    def resize(columns, rows):
        tmux("resize-window", "-x", str(columns), "-y", str(rows))
        # A key-driven change proves that input, dispatch and repaint still run after resize.
        time.sleep(0.3)
        tmux("send-keys", "F2")
        wait_for(lambda: "Mouse on:" in capture(), "mouse toggle after resize")
        tmux("send-keys", "F2")
        wait_for(lambda: "Mouse off:" in capture(), "second mouse toggle after resize")
        text = capture()
        if text.count("Inline transcript") != 1:
            raise AssertionError(f"stale live title remains in viewport:\n{text}")
        snapshot(f"resize {columns}x{rows}")

    def assert_preserved():
        transcript = capture(True)
        messages = re.findall(r"message (\d{3}):", transcript)
        if messages != [f"{number:03d}" for number in range(24)]:
            raise AssertionError(f"missing, duplicated or reordered messages: {messages}")
        for line in shell_lines:
            if transcript.splitlines().count(line) != 1:
                raise AssertionError(f"lost or duplicated shell context: {line!r}")

    def burst(description):
        before = int(tmux("display-message", "-p", "#{history_size}"))
        deadline = time.monotonic()
        changes = []
        for _ in range(args.burst_steps):
            columns = 90 if args.burst_mode == "height" else generator.randint(40, 100)
            rows = 18 if args.burst_mode == "width" else generator.randint(6, 30)
            tmux("resize-window", "-x", str(columns), "-y", str(rows))
            changes.append({"size": [columns, rows], "time": time.monotonic()})
            deadline += args.burst_ms / 1000
            time.sleep(max(0, deadline - time.monotonic()))
        # No captures, key-driven repaints or settling waits occur between changes.
        resize(90, 18)
        bursts.append({"description": description, "changes": changes,
                       "history_growth": int(tmux("display-message", "-p", "#{history_size}")) - before})
        snapshot(description)

    def verify_line_extent():
        # A real terminal retains line extent after EL0 over previously written cells.
        # EL2 before replacement must leave ROW NEXT directly beneath the short line.
        text = "L" * 90 + "\x1b[1;1H\x1b[2KMESSAGE PROBE\x1b[2;1HROW NEXT\x1b[1;1H"
        tmux("set-option", "-g", "default-size", "90x18")
        tmux("new-window", "-d", "-n", "extent",
             "printf '%s' " + shlex.quote(text) + "; sleep 30")
        geometry = tmux("display-message", "-p", "-t", "inline:extent", "#{pane_width}x#{pane_height}").strip()
        if geometry != "90x18":
            raise AssertionError(f"unexpected line-probe geometry: {geometry}")
        wait_for(lambda: "ROW NEXT" in tmux("capture-pane", "-p", "-t", "inline:extent"), "line extent probe")
        tmux("resize-window", "-t", "inline:extent", "-x", "50", "-y", "18")
        lines = tmux("capture-pane", "-p", "-t", "inline:extent").splitlines()
        if lines[:2] != ["MESSAGE PROBE", "ROW NEXT"]:
            raise AssertionError(f"blank continuation after narrowing: {lines}")
        tmux("kill-window", "-t", "inline:extent")

    try:
        # Start a server before the session so remain-on-exit also catches startup failures.
        tmux("-f", "/dev/null", "start-server", ";", "set-option", "-g", "remain-on-exit", "on",
             ";", "set-option", "-g", "history-limit", "10000", ";", "new-session", "-d",
             "-x", "90", "-y", "18", "-s", "inline",
             "printf '%s\\n' " + shlex.join(shell_lines) + "; exec "
             + "env -u DisableRealDriverIO -u NO_COLOR TERM=xterm-256color COLORTERM=truecolor "
             + shlex.join(["dotnet", str(args.dll.resolve()), "run", "Inline Transcript"]))
        wait_for(lambda: "message 004:" in capture(True), "streaming output")
        burst("streaming burst")
        wait_for(lambda: "message 023:" in capture(True), "all completed messages")
        assert_preserved()
        burst("settled transcript burst")
        assert_preserved()
        resize(90, 17)
        resize(90, 16)
        tmux("send-keys", "F5")
        wait_for(lambda: "LIVE PREVIEW row 4" in capture(), "expanded preview")
        burst("expanded preview burst")
        assert_preserved()
        for columns, rows in ((50, 16), (40, 16), (90, 18), (90, 12), (50, 18), (90, 18)):
            resize(columns, rows)
        tmux("send-keys", "F6")
        wait_for(lambda: "preview: completed preview" in capture(True) and "LIVE PREVIEW" not in capture(), "preview commitment")
        tmux("send-keys", "-l", "after resize")
        tmux("send-keys", "Enter")
        wait_for(lambda: "you: after resize" in capture(True), "submission after resize")
        snapshot("before exit")
        transcript = capture(True)
        assert_preserved()
        for completed in ("preview: completed preview", "you: after resize"):
            if transcript.count(completed) != 1:
                raise AssertionError(f"expected exactly one {completed!r}")
        verify_line_extent()
        tmux("send-keys", "Escape")
        wait_for(lambda: not alive(), "clean Escape exit", allow_dead=True)
        wait_for(lambda: tmux("display-message", "-p", "#{pane_dead_status}#{pane_dead_signal}").strip() != "",
                 "exit status", allow_dead=True)
        status = tmux("display-message", "-p", "#{pane_dead_status}").strip()
        snapshot("after exit")
        if status != "0":
            signal = tmux("display-message", "-p", "#{pane_dead_signal}").strip()
            raise AssertionError(f"scenario exited with status {status!r}, signal {signal!r}:\n{capture()}")
        succeeded = True
        print(f"PASS: {args.burst_mode} bursts, seed {args.seed}, {args.burst_steps} steps at {args.burst_ms:g} ms in three phases")
        print("PASS: 24 ordered unique messages and six shell lines; preview commitment, input/repaint and clean exit")
    finally:
        args.evidence.parent.mkdir(parents=True, exist_ok=True)
        args.evidence.write_text(json.dumps({"passed": succeeded, "tmux": subprocess.check_output(["tmux", "-V"], text=True).strip(),
                                             "seed": args.seed, "mode": args.burst_mode, "bursts": bursts,
                                             "snapshots": snapshots}, indent=2), encoding="utf-8")
        subprocess.run(["tmux", "-L", server, "kill-server"], check=False, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


if __name__ == "__main__":
    main()
