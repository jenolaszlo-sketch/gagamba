#!/usr/bin/env bash
# GW-1A probe entrypoint (Linux/macOS and Windows-git-bash).
# The probe runs cross-platform; Windows-only legs report Unsupported off-Windows.
# Fails closed when the emitted CapabilityProbe aggregate is not Passed.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG="${1:-Release}"
ART="$ROOT/artifacts"
mkdir -p "$ART"
echo "probe: root=$ROOT config=$CONFIG"

dotnet build "$ROOT/spikes/Gw1aProbe/Gw1aProbe.csproj" -c "$CONFIG"
# Gw1aProbe returns non-zero when the aggregate is not Passed; set -e stops us.
dotnet run --no-build -c "$CONFIG" \
    --project "$ROOT/spikes/Gw1aProbe/Gw1aProbe.csproj" \
    -- --repo-root "$ROOT" --out "$ART"

latest="$(ls "$ART"/windows-probe-*.json | sort | tail -n 1)"
echo "probe: report=$latest"
if ! grep -q '"evidenceKind": "CapabilityProbe"' "$latest"; then
    echo "probe: unexpected evidence kind" >&2
    exit 1
fi
if ! grep -q '"aggregate": "Passed"' "$latest"; then
    echo "probe: aggregate is not Passed" >&2
    exit 1
fi
echo "probe: Passed"
