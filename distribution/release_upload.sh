#!/usr/bin/env bash
# Adds files to the full release $TAG, for the Build workflow's version tag builds: each platform's job
# runs it as soon as its files are built, so the faster platforms are up before the slower ones.
# The first one creates the release if it doesn't exist yet, with $NOTES (the hidden peggle-build line)
# as its notes. A release that already exists keeps its title and notes, so notes written by hand stay
# as they are; only the hidden line is added to their end if it isn't there. Needs GH_TOKEN and GH_REPO.
#
#   release_upload.sh FILE...
set -euo pipefail
: "${TAG:?TAG must be set}"

if ! gh release view "$TAG" > /dev/null 2>&1; then
  # Another job can create it at the same moment; then theirs is used.
  gh release create "$TAG" --verify-tag --title "$TAG" --notes "${NOTES:-}" --latest \
    || gh release view "$TAG" > /dev/null
fi
# A release made by hand gets the hidden line at the end of its notes (the only change to them), so
# make_source.py lists it in the AltStore / SideStore source.
BODY="$(gh release view "$TAG" --json body --jq .body)"
if [ -n "${NOTES:-}" ] && [[ "$BODY" != *"<!-- peggle-build "* ]]; then
  gh release edit "$TAG" --notes "$(printf '%s\n\n%s' "$BODY" "$NOTES")" > /dev/null
fi
gh release upload "$TAG" "$@" --clobber
ls -lh "$@"
