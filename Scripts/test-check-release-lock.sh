#!/usr/bin/env bash
set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
lock_script="${script_directory}/check-release-lock.sh"
temporary_directory="$(mktemp -d)"
trap 'rm -rf "$temporary_directory"' EXIT

cat > "$temporary_directory/gh" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

scenario="${FAKE_GH_SCENARIO:?}"

if [ "$scenario" = "gh-failure" ]; then
    exit 1
fi

if [[ "$*" == *" --base main "* && "$*" == *" --state open "* && "$*" == *" --head develop "* ]]; then
    if [ "$scenario" = "locked-open" ]; then
        printf '%s\n' '100'
    elif [ "$scenario" = "old-open" ]; then
        printf '\n'
    else
        printf '\n'
    fi
    exit 0
fi

if [[ "$*" == *" --base main "* && "$*" == *" --state open "* ]]; then
    if [ "$scenario" = "old-open" ]; then
        printf '%s\n' '101'
    else
        printf '\n'
    fi
    exit 0
fi

if [[ "$*" == *" --base main "* && "$*" == *" --state merged "* ]]; then
    if [ "$scenario" = "locked-missing" ] || [ "$scenario" = "locked-backmerge" ] || [ "$scenario" = "active-backmerge" ] || [ "$scenario" = "merged-backmerge" ] || [ "$scenario" = "fork-backmerge" ] || [ "$scenario" = "closed-backmerge" ]; then
        printf '%s\n' '{"number":100,"body":"<!-- terminal-gui-release: tag=v2.6.0; source-sha=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa -->","headRefName":"develop","mergeCommit":{"oid":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}}'
    elif [ "$scenario" = "old-merged" ]; then
        printf '%s\n' '{"number":90,"body":"","headRefName":"release/v2.5.0","mergeCommit":{"oid":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"}}'
    else
        printf '\n'
    fi
    exit 0
fi

if [[ "$*" == *" --base develop "* && "$*" == *" --state all "* ]]; then
    MARKER="<!-- terminal-gui-release-backmerge: release-pr=100; tag=v2.6.0 -->"

    case "$scenario" in
        active-backmerge|locked-backmerge)
            printf '{"number":200,"state":"OPEN","body":"%s"}\n' "$MARKER"
            ;;
        merged-backmerge)
            printf '{"number":200,"state":"MERGED","body":"%s"}\n' "$MARKER"
            ;;
        closed-backmerge)
            printf '{"number":200,"state":"CLOSED","body":"%s"}\n' "$MARKER"
            ;;
        fork-backmerge|locked-missing)
            printf '\n'
            ;;
        *)
            printf '\n'
            ;;
    esac
    exit 0
fi

echo "Unexpected gh invocation: $*" >&2
exit 1
EOF

chmod +x "$temporary_directory/gh"

if ! command -v jq >/dev/null 2>&1; then
    cat > "$temporary_directory/jq" <<'EOF'
#!/usr/bin/env python3
import json
import re
import sys

expression = sys.argv[-1]
value = json.load(sys.stdin)
match = re.match(r"\.([A-Za-z0-9_.]+)", expression)

for part in match.group(1).split("."):
    if value == "":
        break

    value = value.get(part, "") if isinstance(value, dict) else ""

if isinstance(value, bool):
    print(str(value).lower())
elif value == "" or value is None:
    print("")
else:
    print(value)
EOF
    chmod +x "$temporary_directory/jq"
fi

export PATH="$temporary_directory:$PATH"
export GITHUB_REPOSITORY="tui-cs/Terminal.Gui"
export REPOSITORY_OWNER="tui-cs"

ExpectSuccess() {
    local scenario="$1"
    shift

    if FAKE_GH_SCENARIO="$scenario" "$@"; then
        return
    fi

    echo "Expected success for scenario: $scenario" >&2
    exit 1
}

ExpectFailure() {
    local scenario="$1"
    shift

    if FAKE_GH_SCENARIO="$scenario" "$@" >/dev/null 2>&1; then
        echo "Expected failure for scenario: $scenario" >&2
        exit 1
    fi
}

ExpectSuccess unlocked bash "$lock_script"
ExpectFailure locked-open bash "$lock_script"
ExpectFailure locked-missing bash "$lock_script"
ExpectFailure locked-backmerge bash "$lock_script"
ExpectSuccess active-backmerge bash "$lock_script" 200
ExpectFailure active-backmerge bash "$lock_script" 201
ExpectSuccess merged-backmerge bash "$lock_script"
ExpectFailure fork-backmerge bash "$lock_script" 200
ExpectFailure closed-backmerge bash "$lock_script" 200
ExpectSuccess old-merged bash "$lock_script"
ExpectFailure old-open bash "$lock_script"
ExpectFailure gh-failure bash "$lock_script"
