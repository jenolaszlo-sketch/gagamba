#!/bin/sh
set -eu
[ "$#" -eq 0 ] || { echo "verify-install takes no arguments; the anchor is /opt/gagamba" >&2; exit 2; }
exec /usr/bin/env -i /usr/lib/dotnet/dotnet /usr/local/libexec/gagamba-install/InstallTool.dll verify-active
