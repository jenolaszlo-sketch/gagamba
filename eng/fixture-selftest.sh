#!/usr/bin/env bash
# GP-1A/GP-1B local entrypoint (Linux/macOS). Mirrors eng/fixture-selftest.ps1:
# build -> unit -> harness -> validate the emitted evidence, failing closed.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG="${1:-Release}"
ART="$ROOT/artifacts"
mkdir -p "$ART"
echo "fixture-selftest: root=$ROOT config=$CONFIG"

dotnet build "$ROOT/tests/Fixtures/Gagamba.Fixture.Worker/Gagamba.Fixture.Worker.csproj" -c "$CONFIG"
dotnet build "$ROOT/tests/Fixtures/Gagamba.Fixture.Harness/Gagamba.Fixture.Harness.csproj" -c "$CONFIG"
dotnet test "$ROOT/tests/Fixtures/Gagamba.Fixture.SelfTest/Gagamba.Fixture.SelfTest.csproj" -c "$CONFIG" --nologo

# The harness returns non-zero when the aggregate is not Passed; set -e stops us.
dotnet run --no-build -c "$CONFIG" \
    --project "$ROOT/tests/Fixtures/Gagamba.Fixture.Harness/Gagamba.Fixture.Harness.csproj" \
    -- --repo-root "$ROOT" --out "$ART"

# Run IDs are "yyyyMMdd-HHmmss-...", so lexical sort is chronological.
latest="$(ls "$ART"/fixture-selftest-*.json | sort | tail -n 1)"
echo "fixture-selftest: report=$latest"
if ! grep -q '"schemaVersion": 1' "$latest" \
    || ! grep -q '"evidenceKind": "FixtureSelfTest"' "$latest"; then
    echo "fixture-selftest: unexpected evidence envelope" >&2
    exit 1
fi
if ! grep -q '"aggregate": "Passed"' "$latest"; then
    echo "fixture-selftest: aggregate is not Passed" >&2
    exit 1
fi
echo "fixture-selftest: Passed"
