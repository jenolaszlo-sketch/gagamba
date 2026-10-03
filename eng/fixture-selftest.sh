#!/usr/bin/env bash
# GP-1A local entrypoint (Linux/macOS). Mirrors eng/fixture-selftest.ps1.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG="${1:-Release}"
ART="$ROOT/artifacts"
mkdir -p "$ART"
echo "fixture-selftest: root=$ROOT config=$CONFIG"
dotnet build "$ROOT/tests/Fixtures/Gagamba.Fixture.Worker/Gagamba.Fixture.Worker.csproj" -c "$CONFIG"
dotnet build "$ROOT/tests/Fixtures/Gagamba.Fixture.Harness/Gagamba.Fixture.Harness.csproj" -c "$CONFIG"
dotnet test "$ROOT/tests/Fixtures/Gagamba.Fixture.SelfTest/Gagamba.Fixture.SelfTest.csproj" -c "$CONFIG" --nologo
dotnet run --no-build -c "$CONFIG" --project "$ROOT/tests/Fixtures/Gagamba.Fixture.Harness/Gagamba.Fixture.Harness.csproj" -- --repo-root "$ROOT" --out "$ART"
latest="$(ls -t "$ART"/fixture-selftest-*.json | head -n 1)"
echo "fixture-selftest: report=$latest"
python3 -c "import json,sys; r=json.load(open('$latest'.replace(chr(39),''))); assert r['summary']['aggregate']=='Passed', r['summary']; print('fixture-selftest: Passed', r['summary'])"
