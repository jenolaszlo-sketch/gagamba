#!/usr/bin/env bash
# GL-1A Linux lifecycle probe (Windows host, Ubuntu WSL2 guest).
# Copies the spike into the distro's Linux filesystem (reads come from
# /mnt/c but execution never does), runs it as root, copies the evidence
# JSON back to artifacts/. No .NET in Ubuntu: shell + python3 only.
set -euo pipefail
# Called from Git Bash: disable MSYS path mangling for wsl.exe arguments
# (it concatenates guest paths with the Windows CWD otherwise).
export MSYS_NO_PATHCONV=1
# Repo root from this script's location (Git Bash /c/... form), mapped to
# the guest's /mnt/c/... view. GAGAMBA_ROOT overrides (Windows C:/... form).
if [ -n "${GAGAMBA_ROOT:-}" ]; then
  case "$GAGAMBA_ROOT" in
    [Cc]:/*) REPO_MNT="/mnt/c/${GAGAMBA_ROOT:3}" ;;
    *) echo "gl1a-life: GAGAMBA_ROOT must be a Windows C:/... path" >&2; exit 2 ;;
  esac
  ART="$GAGAMBA_ROOT/artifacts"
else
  REPO_BASH="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
  REPO_MNT="$REPO_BASH"
  case "$REPO_MNT" in
    /c/*) REPO_MNT="/mnt/c/${REPO_MNT:3}" ;;
    /d/*) REPO_MNT="/mnt/d/${REPO_MNT:3}" ;;
  esac
  ART="$REPO_BASH/artifacts"
fi
DISTRO="${GL1A_DISTRO:-Ubuntu}"
GUEST_SRC="/root/gl1a-src"
mkdir -p "$ART"

echo "gl1a-life: distro=$DISTRO guest=$GUEST_SRC"
wsl -d "$DISTRO" -u root bash -c "rm -rf '$GUEST_SRC' '$GUEST_SRC'-out-* && mkdir -p '$GUEST_SRC' && cp '$REPO_MNT/spikes/Gl1aLife/fixture.py' '$REPO_MNT/spikes/Gl1aLife/run.py' '$REPO_MNT/spikes/Gl1aLife/summary.py' '$GUEST_SRC/' && python3 -m py_compile '$GUEST_SRC/fixture.py' '$GUEST_SRC/run.py' '$GUEST_SRC/summary.py' && echo compile-ok"

STAMP="$(date +%Y%m%d-%H%M%S)"
wsl -d "$DISTRO" -u root python3 "$GUEST_SRC/run.py" "$GUEST_SRC/out-$STAMP"
wsl -d "$DISTRO" -u root python3 "$GUEST_SRC/summary.py" "$GUEST_SRC/out-$STAMP/gl1a-life.json"
wsl -d "$DISTRO" -u root bash -c "cat '$GUEST_SRC/out-$STAMP/gl1a-life.json'" > "$ART/gl1a-life-$STAMP.json"
echo "gl1a-life: evidence=$ART/gl1a-life-$STAMP.json"
