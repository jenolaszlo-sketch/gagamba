#!/usr/bin/env bash
set -Eeuo pipefail
export DOTNET_CLI_TELEMETRY_OPTOUT=1
cd /home/gaga/qualification/gagamba-r12
O=/mnt/c/Users/Laszlos/source/repos/gagamba-review-d1d3/docs/reviews/gagamba-linux-m1-wp05-d1d3-review-artifacts

{
  date -u +%FT%TZ
  uname -a
  cat /proc/sys/kernel/random/boot_id
  id
  grep -E "Cap|NoNewPrivs" /proc/self/status
  cat /proc/self/cgroup
  dotnet --version
  gcc --version | head -n 1
  bwrap --version 2>/dev/null | head -n 1
  sha256sum artifacts/linux-m1/wp04/seccomp-filter.bin
  git rev-parse HEAD
  git status --porcelain | wc -l
} > "$O/reviewer-environment.txt" 2>&1

{
  echo "independent rerun start $(date -u +%FT%TZ)"
  echo "checkout $(git rev-parse HEAD) clean=$(git status --porcelain | wc -l) modified"
} > "$O/reviewer-run-header.txt"

echo "building fixture"
dotnet build tests/Fixtures/Wp05LeaseFixture/Wp05LeaseFixture.csproj -c Release --nologo \
  | tee "$O/reviewer-fixture-build.log"

echo "running contract probes"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --nologo --filter FullyQualifiedName~Wp05ReleaseContractProbes \
  | tee "$O/reviewer-contract-probes.log"

echo "running lease process probe"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --nologo --filter FullyQualifiedName~Wp05LeaseProcessProbes \
  | tee "$O/reviewer-lease-process.log"

echo "running native release protocol tests"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --nologo --filter FullyQualifiedName~ReleaseProtocolTests \
  | tee "$O/reviewer-native-release-protocol.log"

echo "running focused existing classes"
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj \
  -c Release --nologo \
  --filter "FullyQualifiedName~ReleaseArbiterTests|FullyQualifiedName~Wp05ReleasePathIndependentQualificationTests|FullyQualifiedName~CoordinatorStatusReaderTests|FullyQualifiedName~Native.ReleaseProtocolTests|FullyQualifiedName~ReconcilerTests" \
  | tee "$O/reviewer-focused-existing.log"

{
  echo "independent rerun done $(date -u +%FT%TZ)"
  git status --porcelain | wc -l
} > "$O/reviewer-run-footer.txt"
