#!/usr/bin/env bash
# Supported WP05 Linux M1 qualification execution profile.
#
# Process-global qualification (PR_SET_CHILD_SUBREAPER + waitpid(-1) in
# M1RecoveryScope) must not share a test-host process with unrelated
# child-process tests. The regular assembly runs in parallel without the tagged
# classes; each tagged class then runs in its own test-host process.
#
# Do not replace this with a bare parallel `dotnet test` of the full assembly:
# that profile can reproduce cross-test ECHILD/adopted-child interference and
# is not a supported qualification command.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj"
CONFIG="${CONFIGURATION:-Release}"
COMMON=(-c "$CONFIG" --nologo --logger "console;verbosity=minimal")

echo "== regular parallel phase (process-global reaping classes excluded) =="
dotnet test "$PROJECT" "${COMMON[@]}" --filter 'Category!=ProcessGlobalReaping'

# Each class owns process-global child state. Separate test hosts prevent one
# class's waitpid(-1) from reaping another class's children.
PROCESS_GLOBAL_CLASSES=(
  Gagamba.Execution.Linux.M1.Tests.Recovery.NonRootQualificationTests
  Gagamba.Execution.Linux.M1.Tests.Recovery.OwnerDeathQualificationTests
  Gagamba.Execution.Linux.M1.Tests.Recovery.IntegratedBarrierTests
  Gagamba.Execution.Linux.M1.Tests.Recovery.DomainAuthorityTests
  Gagamba.Execution.Linux.M1.Tests.Recovery.RecoveryHarness
  Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05TerminationObserverTests
)

for class_name in "${PROCESS_GLOBAL_CLASSES[@]}"; do
  echo "== process-global phase: $class_name (dedicated test host) =="
  dotnet test "$PROJECT" "${COMMON[@]}" --no-build \
    --filter "FullyQualifiedName~$class_name"
done



