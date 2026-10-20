#!/bin/sh
# Build the D5 test-only qualification coordinator. This is not the
# production coordinator and does not overwrite the historical D4 artifact.
set -eu
ROOT="$(dirname "$0")/../.."
NATIVE="$ROOT/native/linux-m1"
SRC="$ROOT/tests/Fixtures/wp05-qualification-coordinator.c"
OUT="$ROOT/artifacts/linux-m1/wp05-d5"
mkdir -p "$OUT"
gcc -static -O2 -s -fno-ident -frandom-seed=gagamba-wp05-d5 -Wl,--build-id=none \
    -I"$NATIVE" -o "$OUT/wp05-qualification-coordinator" "$SRC"
sha256sum "$SRC" "$OUT/wp05-qualification-coordinator" | tee "$OUT/SHA256SUMS"
file "$OUT/wp05-qualification-coordinator"
