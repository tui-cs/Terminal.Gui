#!/usr/bin/env bash
set -euo pipefail

repository="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
owner="${repository%%/*}"
allow_backmerge_ref="${1:-}"

open_release_pr="$(gh pr list \
    --repo "$repository" \
    --state open \
    --base main \
    --head "${owner}:develop" \
    --json number \
    --jq '.[0].number // empty')"

if [[ -n "$open_release_pr" ]]; then
    echo "::error::PR #${open_release_pr} is promoting develop to main. Develop merges are locked until its back-merge is complete."
    exit 1
fi

latest_release="$(gh pr list \
    --repo "$repository" \
    --state merged \
    --base main \
    --head "${owner}:develop" \
    --limit 100 \
    --json body,mergedAt,mergeCommit \
    --jq '[.[] | select((.body // "") | contains("<!-- terminal-gui-release:"))] | sort_by(.mergedAt) | last // empty')"

if [[ -z "$latest_release" ]]; then
    exit 0
fi

release_body="$(jq -r '.body // ""' <<< "$latest_release")"
release_version="$(grep -Eo '<!-- terminal-gui-release: version=[0-9]+\.[0-9]+\.[0-9]+; source-sha=[0-9a-fA-F]{40} -->' <<< "$release_body" | head -n 1 | sed -E 's/.*version=([0-9]+\.[0-9]+\.[0-9]+);.*/\1/' || true)"
release_merge_sha="$(jq -r '.mergeCommit.oid // ""' <<< "$latest_release")"

if [[ -z "$release_version" || ! "$release_merge_sha" =~ ^[0-9a-fA-F]{40}$ ]]; then
    echo "::error::The latest merged release PR has invalid release metadata. Maintainer intervention is required."
    exit 1
fi

backmerge_branch="backmerge/v${release_version}-${release_merge_sha:0:8}"

backmerge="$(gh pr list \
    --repo "$repository" \
    --state all \
    --base develop \
    --head "${owner}:${backmerge_branch}" \
    --json number,state,mergedAt \
    --jq '.[0] // empty')"

if [[ -z "$backmerge" ]]; then
    echo "::error::The back-merge PR for v${release_version} has not been created. Develop merges remain locked until it is merged."
    exit 1
fi

backmerge_state="$(jq -r '.state' <<< "$backmerge")"

if [[ "$allow_backmerge_ref" == "$backmerge_branch" && "$backmerge_state" == "OPEN" ]]; then
    echo "This is the active back-merge PR that clears the release lock."
    exit 0
fi

if [[ "$backmerge_state" != "MERGED" ]]; then
    backmerge_number="$(jq -r '.number' <<< "$backmerge")"
    echo "::error::Back-merge PR #${backmerge_number} for v${release_version} is not merged. Develop merges remain locked."
    exit 1
fi
