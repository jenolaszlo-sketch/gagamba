#!/usr/bin/env bash
# RC2A-02B WSL2 qualification runs (labeled wsl2, never native Linux).
cd /tmp/rc2a02b-repo || exit 90
ART=/tmp/rc2a02b-artifacts
OUT=/tmp/rc2a02b-out
mkdir -p "$OUT"
FILTER='FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.'
dotnet build tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --artifacts-path "$ART" > "$OUT/wsl2-build-public.log" 2>&1; echo $? > "$OUT/wsl2-build-public.exit"
dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --artifacts-path "$ART" > "$OUT/wsl2-build-internal.log" 2>&1; echo $? > "$OUT/wsl2-build-internal.exit"
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-build --artifacts-path "$ART" --logger "trx;LogFileName=wsl2-public.trx" --results-directory "$OUT" --blame-hang-timeout 60s > "$OUT/wsl2-public.log" 2>&1; echo $? > "$OUT/wsl2-public.exit"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --artifacts-path "$ART" --filter "$FILTER" --logger "trx;LogFileName=wsl2-required-set.trx" --results-directory "$OUT" --blame-hang-timeout 60s > "$OUT/wsl2-required-set.log" 2>&1; echo $? > "$OUT/wsl2-required-set.exit"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --artifacts-path "$ART" --filter "$FILTER" --logger "trx;LogFileName=wsl2-required-set-rerun.trx" --results-directory "$OUT" --blame-hang-timeout 60s > "$OUT/wsl2-required-set-rerun.log" 2>&1; echo $? > "$OUT/wsl2-required-set-rerun.exit"
for f in "$OUT"/*.exit; do echo "$(basename "$f")=$(cat "$f")"; done