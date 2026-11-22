#!/usr/bin/env bash
set -euo pipefail

repository="${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is required}"
owner="${repository%%/*}"
allowed_backmerge_number="${1:-}"
export REPOSITORY_OWNER="$owner"

open_release_pr="$(gh pr list \
    --repo "$repository" \
    --state open \
    --base main \
    --limit 100 \
    --head develop \
    --json number,headRefName,headRepositoryOwner,isCrossRepository \
    --jq '[.[] | select((.isCrossRepository // false) | not) | select(.headRepositoryOwner.login == env.REPOSITORY_OWNER)] | sort_by(.number) | first | .number // empty')"

if [[ -z "$open_release_pr" ]]; then
    open_release_pr="$(gh pr list \
        --repo "$repository" \
        --state open \
        --base main \
        --limit 100 \
        --json number,headRefName,headRepositoryOwner,isCrossRepository \
        --jq '[.[] | select((.isCrossRepository // false) | not) | select(.headRepositoryOwner.login == env.REPOSITORY_OWNER) | select(.headRefName | startswith("release/"))] | sort_by(.number) | first | .number // empty')"
fi

if [[ -n "$open_release_pr" ]]; then
    echo "::error::PR #${open_release_pr} is promoting a release to main. Develop merges are locked until its back-merge is complete."
    exit 1
fi

latest_release="$(gh pr list \
    --repo "$repository" \
    --state merged \
    --base main \
    --limit 100 \
    --json number,body,headRefName,headRepositoryOwner,isCrossRepository,mergedAt,mergeCommit \
    --jq '[.[] | select((.isCrossRepository // false) | not) | select(.headRepositoryOwner.login == env.REPOSITORY_OWNER) | select(.headRefName == "develop" or (.headRefName | startswith("release/")))] | sort_by(.mergedAt) | last // empty')"

if [[ -z "$latest_release" ]]; then
    exit 0
fi

release_body="$(jq -r '.body // ""' <<< "$latest_release")"
release_pr_number="$(jq -r '.number // ""' <<< "$latest_release")"
release_head_ref="$(jq -r '.headRefName // ""' <<< "$latest_release")"
release_tag="$(grep -Eo '<!-- terminal-gui-release: tag=v[0-9]+\.[0-9]+\.[0-9]+(-(beta|rc)\.[0-9]+)?; source-sha=[0-9a-fA-F]{40} -->' <<< "$release_body" | head -n 1 | sed -E 's/.*tag=(v[^;]+).*/\1/' || true)"

if [[ -z "$release_tag" ]]; then
    if [[ "$release_head_ref" == release/* ]]; then
        exit 0
    fi

    echo "::error::The latest merged release PR has invalid release metadata. Maintainer intervention is required."
    exit 1
fi

release_merge_sha="$(jq -r '.mergeCommit.oid // ""' <<< "$latest_release")"

if [[ ! "$release_merge_sha" =~ ^[0-9a-fA-F]{40}$ ]]; then
    echo "::error::The latest merged release PR is missing its merge commit."
    exit 1
fi

backmerge_branch="backmerge/${release_tag}-${release_merge_sha:0:8}"
backmerge_marker="<!-- terminal-gui-release-backmerge: release-pr=${release_pr_number}; tag=${release_tag} -->"
backmerge="$(gh pr list \
    --repo "$repository" \
    --state all \
    --base develop \
    --head "$backmerge_branch" \
    --limit 100 \
    --json number,state,body,headRepositoryOwner,isCrossRepository \
    --jq '[.[] | select((.isCrossRepository // false) | not) | select(.headRepositoryOwner.login == env.REPOSITORY_OWNER)] | sort_by(.number) | last // empty')"

if [[ -z "$backmerge" ]]; then
    echo "::error::The back-merge PR for ${release_tag} has not been created. Develop merges remain locked until it is merged."
    exit 1
fi

backmerge_body="$(jq -r '.body // ""' <<< "$backmerge")"
backmerge_number="$(jq -r '.number // ""' <<< "$backmerge")"
backmerge_state="$(jq -r '.state // ""' <<< "$backmerge")"

if [[ "$backmerge_body" != *"$backmerge_marker"* ]]; then
    echo "::error::PR #${backmerge_number} does not contain the marker for release PR #${release_pr_number}."
    exit 1
fi

if [[ "$allowed_backmerge_number" == "$backmerge_number" && "$backmerge_state" == "OPEN" ]]; then
    echo "PR #${backmerge_number} is the active back-merge and clears the release lock."
    exit 0
fi

if [[ "$backmerge_state" != "MERGED" ]]; then
    echo "::error::Back-merge PR #${backmerge_number} for ${release_tag} is ${backmerge_state,,}, not merged. Develop merges remain locked."
    exit 1
fi

exit 0
