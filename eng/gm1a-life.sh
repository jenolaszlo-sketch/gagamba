#!/usr/bin/env bash
# GM-1A macOS lifecycle probe. Runs ON macOS (native shell + python3):
# no WSL, no copies, no .NET.
# Usage: eng/gm1a-life.sh [--only M1,M2,...]
set -euo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ART="$REPO/artifacts"
mkdir -p "$ART"

STAMP="$(date +%Y%m%d-%H%M%S)"
python3 "$REPO/spikes/Gm1aLife/run.py" --out "$ART/gm1a-$STAMP" "$@"
python3 "$REPO/spikes/Gm1aLife/summary.py" "$ART/gm1a-$STAMP/gm1a-life.json"
cp "$ART/gm1a-$STAMP/gm1a-life.json" "$ART/gm1a-life-$STAMP.json"
echo "gm1a-life: evidence=$ART/gm1a-life-$STAMP.json"
