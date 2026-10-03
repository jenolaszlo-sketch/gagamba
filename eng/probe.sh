#!/usr/bin/env bash
# GW-1A probe entrypoint (Linux/macOS and Windows-git-bash).
# The probe runs cross-platform; Windows-only legs report Unsupported off-Windows.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG="${1:-Release}"
ART="$ROOT/artifacts"
mkdir -p "$ART"
echo "probe: root=$ROOT config=$CONFIG"
dotnet build "$ROOT/spikes/Gw1aProbe/Gw1aProbe.csproj" -c "$CONFIG"
dotnet run --no-build -c "$CONFIG" --project "$ROOT/spikes/Gw1aProbe/Gw1aProbe.csproj" -- --repo-root "$ROOT" --out "$ART"
latest="$(ls -t "$ART"/windows-probe-*.json | head -n 1)"
echo "probe: report=$latest"
