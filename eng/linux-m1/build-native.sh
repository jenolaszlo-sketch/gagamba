#!/bin/sh
# Build the WP02 native fixture reproducibly (static only). Fails closed
# when the compiler or static linking is unavailable; never executes the
# artifact. Usage: build-native.sh [outdir]
set -eu

OUTDIR="${1:-$(dirname "$0")/../../tests/Fixtures/Gagamba.LinuxM1.Native/out}"
SRC="$(dirname "$0")/../../tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c"

if [ "$(uname -s)" != "Linux" ]; then
    echo "build-native.sh: Linux required (WP02 fixture is Linux-only)" >&2
    exit 2
fi

if ! command -v gcc >/dev/null 2>&1; then
    echo "build-native.sh: gcc not found; failing closed" >&2
    exit 2
fi

if ! command -v sha256sum >/dev/null 2>&1; then
    echo "build-native.sh: sha256sum not found; failing closed" >&2
    exit 2
fi

mkdir -p "$OUTDIR"

echo "CC: $(gcc --version | head -1)"
echo "host: $(uname -smr) libc: $(ldd --version | head -1)"

# Deterministic flags: fixed optimization, no build ID, stripped.
if ! gcc -static -O2 -s -Wl,--build-id=none -o "$OUTDIR/fixture" "$SRC"; then
    echo "build-native.sh: static link failed; failing closed (no dynamic fallback)" >&2
    exit 3
fi

chmod 0755 "$OUTDIR/fixture"
echo "artifact: $OUTDIR/fixture"
sha256sum "$OUTDIR/fixture" | tee "$OUTDIR/fixture.sha256"
file "$OUTDIR/fixture" || true
echo "build-native.sh: built, not executed"
