#!/usr/bin/env bash
# Independent S2-Q1 trust-anchor / forged-authority probe (nonprivileged, read-only w.r.t. /opt).
set -e
rm -rf /tmp/q1
mkdir -p /tmp/q1/src /tmp/q1/cand
tar -xf /mnt/c/Users/Laszlos/source/repos/gagamba-b6-s2-q1-review/evidence/source-8b897e4.tar -C /tmp/q1/src
tar -xzf /mnt/c/Users/Laszlos/source/repos/Gagamba/docs/evidence/rc2b-06-b6-impl-01-s2/candidate-payload.tar.gz -C /tmp/q1/cand
cd /tmp/q1/src
dotnet build eng/InstallTool/InstallTool.csproj -c Release -p:NuGetAudit=false > /tmp/q1/toolbuild.log 2>&1 || { tail -20 /tmp/q1/toolbuild.log; exit 1; }
TOOL="dotnet /tmp/q1/src/eng/InstallTool/bin/Release/net10.0/InstallTool.dll"
CAND=/tmp/q1/cand/candidate
set +e

echo "=== check-payload (positive control, non-root candidate) ==="
$TOOL check-payload "$CAND"; echo "exit=$?"
echo "=== check-staged (user-owned /tmp ancestors) ==="
$TOOL check-staged "$CAND"; echo "exit=$?"
echo "=== verify-active (fixed root absent) ==="
$TOOL verify-active; echo "exit=$?"

python3 - <<"PY"
import json, hashlib
cand="/tmp/q1/cand/candidate"
raw=open(cand+"/manifest.json","rb").read()
m=json.loads(raw)
digest=hashlib.sha256(raw).hexdigest()
rec={"qualificationId":"Q1","artifactSetSha256":digest,"profileId":m["profileId"],"profileVersion":"1","rid":"linux-x64","scope":"WSL2-x86_64","host":"forged-user-owned","sourceCommit":m["source"]["commit"],"sourceTree":m["source"]["tree"],"evidenceSha256":"a"*64,"reviewCommit":"b"*40,"acceptedAtUtc":"2026-11-02T00:00:00Z","limitations":["forged"],"buildConfiguration":"Production"}
rb=json.dumps(rec,separators=(",",":")).encode()
open("/tmp/q1/forged-record.json","wb").write(rb)
receipt={"schemaVersion":1,"artifactSetSha256":digest,"manifestFileSha256":digest,"installedByUid":0,"installedAtUtc":"2026-11-02T00:00:00Z","candidatePath":cand,"installerVersion":"forged","expectedDigestSource":"qualification-record","qualificationId":"Q1","qualificationRecordSha256":hashlib.sha256(rb).hexdigest()}
open("/tmp/q1/forged-receipt.json","w").write(json.dumps(receipt,separators=(",",":")))
print("manifest digest:",digest)
open("/tmp/q1/forged-record-bad.json","wb").write(rb.replace(b'"rid":"linux-x64"',b'"rid":"linux-arm64"'))
PY

echo "=== check-association with fully self-consistent forged triple (pure logic) ==="
$TOOL check-association "$CAND/manifest.json" /tmp/q1/forged-receipt.json /tmp/q1/forged-record.json; echo "exit=$?"
echo "=== check-association with tampered record (RID linux-arm64) ==="
$TOOL check-association "$CAND/manifest.json" /tmp/q1/forged-receipt.json /tmp/q1/forged-record-bad.json; echo "exit=$?"
echo "=== forged metadata ownership (user-owned, not root) ==="
ls -l /tmp/q1/forged-receipt.json /tmp/q1/forged-record.json
echo "=== confirm no install root was created/changed ==="
ls -ld /opt/gagamba 2>&1 || true
