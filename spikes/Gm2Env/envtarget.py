#!/usr/bin/env python3
"""GM-2 env probe target: runs UNDER launchd, dumps complete environment."""
import os
import sys

d = sys.argv[sys.argv.index("--dir") + 1]
with open(os.path.join(d, "env-dump.txt"), "w") as f:
    for k in sorted(os.environ):
        f.write(f"{k}={os.environ[k]}\n")
with open(os.path.join(d, "env-ready.txt"), "w") as f:
    f.write("ready")
import time
while True:
    time.sleep(60)
