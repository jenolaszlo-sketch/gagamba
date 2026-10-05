#!/usr/bin/env bash
# GW-1B launch spike is Windows-only (processmodel.dll). Refuse elsewhere:
# no evidence is emitted off-Windows.
echo "launch-spike: Windows-only spike (processmodel.dll); refusing on this host." >&2
exit 2
