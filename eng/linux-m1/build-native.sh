#!/bin/sh
# WP02/WP04 native artifacts. Build only; no fixture or coordinator is run.
set -eu

if [ "$#" -gt 0 ]; then OUTDIR="$1"; else OUTDIR="$(dirname "$0")/../../tests/Fixtures/Gagamba.LinuxM1.Native/out"; fi
SRC="$(dirname "$0")/../../tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c"
NATIVE="$(dirname "$0")/../../native/linux-m1"
WP04_OUT="$(dirname "$0")/../../artifacts/linux-m1/wp04"
WP05_OUT="$(dirname "$0")/../../artifacts/linux-m1/wp05"
WP05_QUALIFICATION_SRC="$(dirname "$0")/../../tests/Fixtures/wp05-qualification-coordinator.c"

if [ "$(uname -s)" != "Linux" ]; then echo "build-native.sh: Linux required" >&2; exit 2; fi
for tool in gcc sha256sum python3 wc file; do command -v "$tool" >/dev/null 2>&1 || { echo "build-native.sh: $tool not found" >&2; exit 2; }; done
[ "$(uname -m)" = "x86_64" ] || { echo "build-native.sh: x86_64 required for the pinned filter" >&2; exit 2; }
mkdir -p "$OUTDIR" "$WP04_OUT" "$WP05_OUT"
echo "CC: $(gcc --version | head -1)"
echo "host: $(uname -smr) libc: $(ldd --version | head -1)"

gcc -static -O2 -s -Wl,--build-id=none -o "$OUTDIR/fixture" "$SRC" || { echo "static fixture link failed" >&2; exit 3; }
sha256sum "$OUTDIR/fixture" | tee "$OUTDIR/fixture.sha256"

FILTER_BIN="$WP04_OUT/seccomp-filter.bin"
python3 "$NATIVE/seccomp_filter_policy.py" --check || { echo "filter source/output mismatch" >&2; exit 4; }
python3 "$NATIVE/seccomp_filter_policy.py" --binary > "$FILTER_BIN"
[ "$(wc -c < "$FILTER_BIN")" -eq 672 ] || { echo "filter length mismatch" >&2; exit 4; }
[ "$(sha256sum "$FILTER_BIN" | cut -d' ' -f1)" = "4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc" ] || { echo "filter digest mismatch" >&2; exit 4; }
gcc -static -O2 -s -fno-ident -frandom-seed=gagamba-wp04 -Wl,--build-id=none -I"$NATIVE" -o "$WP04_OUT/exec-coordinator" "$NATIVE/exec-coordinator.c" || { echo "static coordinator link failed" >&2; exit 5; }
gcc -static -O2 -s -fno-ident -frandom-seed=gagamba-wp05-qualification -Wl,--build-id=none -I"$NATIVE" -o "$WP05_OUT/wp05-qualification-coordinator" "$WP05_QUALIFICATION_SRC" || { echo "WP05 qualification coordinator link failed" >&2; exit 6; }
sha256sum "$NATIVE/exec-coordinator.c" "$WP04_OUT/exec-coordinator" "$FILTER_BIN" "$WP05_QUALIFICATION_SRC" "$WP05_OUT/wp05-qualification-coordinator" | tee "$WP04_OUT/SHA256SUMS"
file "$OUTDIR/fixture" "$WP04_OUT/exec-coordinator" "$WP05_OUT/wp05-qualification-coordinator" "$FILTER_BIN" || true
echo "build-native.sh: fixture and coordinators built, not executed"
