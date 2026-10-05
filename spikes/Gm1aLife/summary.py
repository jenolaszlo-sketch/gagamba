#!/usr/bin/env python3
"""One-line GL-1A summary for humans (evidence JSON is the record)."""
import json
import sys

with open(sys.argv[1]) as f:
    r = json.load(f)
print("legs:", " ".join(
    (("PASS" if leg["pass"] else "FAIL") + ":" + leg["id"])
    for leg in r["legs"]))
print("overall:", "PASS" if r["pass"] else "FAIL")
