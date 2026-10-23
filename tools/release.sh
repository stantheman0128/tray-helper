#!/usr/bin/env bash
# Publish a GitHub release from the box. Put the PC's dist\TrayHelper.exe and dist\TrayHelper-Setup.exe in /workspace/release/ first.
# Usage: tools/release.sh 0.8.0      (notes are taken from that version's CHANGELOG section)
set -euo pipefail
v="$1"; d="${RELEASE_DIR:-/workspace/release}"; cd "$(dirname "$0")/.."
for f in TrayHelper.exe TrayHelper-Setup.exe; do test -f "$d/$f"; (cd "$d" && sha256sum "$f" > "$f.sha256"); done
awk -v v="## $v" '$0==v{on=1;next} /^## /{on=0} on' CHANGELOG.md > "$d/notes.md"
gh release create "v$v" --title "TrayHelper $v" --notes-file "$d/notes.md" "$d/TrayHelper.exe" "$d/TrayHelper.exe.sha256" "$d/TrayHelper-Setup.exe" "$d/TrayHelper-Setup.exe.sha256"
