#!/usr/bin/env bash
set -euo pipefail
umask 077
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
report="$ROOT/artifacts/rc2b-int01/wsl2"
build="$(mktemp -d /tmp/gagamba-int01-build.XXXXXXXX)"
mkdir -p "$report"
export GAGAMBA_INT01_NATIVE_PRIMITIVES=1
export GAGAMBA_INT01_EVIDENCE="$report"
trap 'tar -czf "$report/primitive-build.tar.gz" -C "$build" . 2>/dev/null || true' EXIT
{
 date -u; id; uname -a; cat /proc/sys/kernel/random/boot_id; cat /proc/self/cgroup
 sed -n '/^Cap/p' /proc/self/status; dotnet --info; gcc --version | head -1; ldd --version | head -1
} > "$report/environment.txt"
dotnet build tests/Fixtures/B2Controller/B2Controller.csproj -c Release --artifacts-path "$build" --nologo > "$report/probe-build.log" 2>&1
dotnet build src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj -c Release --artifacts-path "$build" --nologo > "$report/recovery-build.log" 2>&1
dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --artifacts-path "$build" --nologo > "$report/tests-build.log" 2>&1
find "$build" -type d -exec chmod 700 {} +
find "$build" -type f -exec chmod 700 {} +
export GAGAMBA_B2_CONTROLLER_DLL="$build/bin/B2Controller/release/Gagamba.Execution.Linux.M1.B2Controller.dll"
export GAGAMBA_OWNER_DLL="$build/bin/Gagamba.Execution.Linux.M1.Owner/release/Gagamba.Execution.Linux.M1.Owner.dll"
find "$build/bin" -type f -print0 | sort -z | xargs -0 sha256sum > "$report/managed-artifact-sha256.txt"
dotnet test "$build/bin/Gagamba.Execution.Linux.M1.Tests/release/Gagamba.Execution.Linux.M1.Tests.dll" --filter 'FullyQualifiedName~DirectAuthenticationTests' --logger 'trx;LogFileName=direct-auth-final.trx' --results-directory "$report" > "$report/direct-auth-final.log" 2>&1
dotnet test "$build/bin/Gagamba.Execution.Linux.M1.Tests/release/Gagamba.Execution.Linux.M1.Tests.dll" --filter 'FullyQualifiedName~OwnerSafetyMonitorTests|FullyQualifiedName~ReleaseAdmissionTests|FullyQualifiedName~OwnerProtocolTests|FullyQualifiedName~RecoveryB2Tests|FullyQualifiedName~OwnerHeldWireCodecTests|FullyQualifiedName~JournalFencingTests' --logger 'trx;LogFileName=integration-contracts.trx' --results-directory "$report" > "$report/integration-contracts.log" 2>&1
gcc -std=c11 -O2 -Wall -Wextra -Werror -I tests/Fixtures tests/Fixtures/b3-exec-oracle-probe.c -o "$build/exec-oracle-probe"
gcc -std=c11 -O2 -Wall -Wextra -Werror -static -I native/linux-m1 -I tests/Fixtures tests/Fixtures/b3-release-coordinator.c -o "$build/b3-release-coordinator"
gcc -std=c11 -O2 -Wall -Wextra -Werror -static tests/Fixtures/b3-exec-target-probe.c -o "$build/exec-target-probe"
gcc -std=gnu11 -O2 -Wall -Wextra -Werror -static tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c -o "$build/b3-fixed-fixture"
"$build/b3-fixed-fixture" > "$report/fixed-fixture.stdout" 2> "$report/fixed-fixture.stderr"
sha256sum "$build/b3-fixed-fixture" "$build/exec-target-probe" "$build/exec-oracle-probe" "$build/b3-release-coordinator" > "$report/native-artifact-sha256.txt"
python3 eng/test-b3-exec-oracle.py "$build/exec-oracle-probe" "$build/exec-target-probe" > "$report/exec-oracle.log" 2>&1
cat "$report/direct-auth-final.log" "$report/integration-contracts.log" "$report/exec-oracle.log"
