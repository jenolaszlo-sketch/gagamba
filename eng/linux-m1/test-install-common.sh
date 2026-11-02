#!/bin/sh
set -eu
exec /usr/bin/python3 -I "$(dirname -- "$0")/tests/test_install_common.py"
