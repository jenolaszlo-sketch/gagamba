#!/bin/sh
# WP02/WP04 native artifacts. Build only; no fixture or coordinator is run.
set -eu

if [ "$#" -gt 0 ]; then OUTDIR="$1"; else OUTDIR="$(dirname "$0")/../../tests/Fixtures/Gagamba.LinuxM1.Native/out"; fi
SRC="$(dirname "$0")/../../tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c"
NATIVE="$(dirname "$0")/../../native/linux-m1"
WP04_OUT="$(dirname "$0")/../../artifacts/linux-m1/wp04"

if [ "$(uname -s)" != "Linux" ]; then echo "build-native.sh: Linux required" >&2; exit 2; fi
for tool in gcc sha256sum tr xxd wc file; do command -v "$tool" >/dev/null 2>&1 || { echo "build-native.sh: $tool not found" >&2; exit 2; }; done
mkdir -p "$OUTDIR" "$WP04_OUT"
echo "CC: $(gcc --version | head -1)"
echo "host: $(uname -smr) libc: $(ldd --version | head -1)"

gcc -static -O2 -s -Wl,--build-id=none -o "$OUTDIR/fixture" "$SRC" || { echo "static fixture link failed" >&2; exit 3; }
sha256sum "$OUTDIR/fixture" | tee "$OUTDIR/fixture.sha256"

FILTER_BIN="$WP04_OUT/seccomp-filter.bin"
tr -d '[:space:]' < "$NATIVE/seccomp-filter.hex" | xxd -r -p > "$FILTER_BIN"
[ "$(wc -c < "$FILTER_BIN")" -eq 424 ] || { echo "filter length mismatch" >&2; exit 4; }
[ "$(sha256sum "$FILTER_BIN" | cut -d' ' -f1)" = "3649a083435cb7e7f1dad0157dfe6bd4c480ad635b7cecb88214a9332403395c" ] || { echo "filter digest mismatch" >&2; exit 4; }
gcc -static -O2 -s -fno-ident -frandom-seed=gagamba-wp04 -Wl,--build-id=none -I"$NATIVE" -o "$WP04_OUT/exec-coordinator" "$NATIVE/exec-coordinator.c" || { echo "static coordinator link failed" >&2; exit 5; }
sha256sum "$NATIVE/exec-coordinator.c" "$WP04_OUT/exec-coordinator" "$FILTER_BIN" | tee "$WP04_OUT/SHA256SUMS"
file "$OUTDIR/fixture" "$WP04_OUT/exec-coordinator" "$FILTER_BIN" || true
echo "build-native.sh: fixture and coordinator built, not executed"
