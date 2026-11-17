"""Inspect every outgoing commit, additive scope and credential-pattern hits."""
import gzip
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

REPO=Path(r"C:/Users/Laszlos/source/repos/Gagamba")
BASE="1116c66d7858e6000f0001a0abe39319e4e694be"
Q1="b6631367d39531b2c5af8a1917760d7c807f8e73"
IMPL="28895d55de98eea4bc402373b653f7216e092843"
STEM="rc2b-06-b6-alt-b-slice-e-05"
CHAIN=[
 ("b309a9ec1a9a0f160ea0c2dd513001c3561a36d9","docs/decisions/",STEM+"-entry-owner-01"),
 ("1ff4d6e6d1f756646072790478921d9a64f15654","docs/decisions/",STEM+"-design-owner-01"),
 ("02ba7ad3c81e72de885349c2bd54b0b7ab16bc12","docs/decisions/",STEM+"-entry-owner-02"),
 ("7a30d0a05a7c898c4e90cc2ca01280db166ed6f2","docs/decisions/",STEM+"-entry-auth-01"),
 (IMPL,"",""),
 ("0a77422c1349b99e518292d46314606b383527e4","docs/evidence/",STEM+"-custody-implementation-01"),
 (Q1,"docs/reviews/",STEM+"-custody-q1"),
]
RULES={
 "private_key":rb"-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----",
 "github_token":rb"\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,})\b",
 "aws_access_key":rb"\b(?:AKIA|ASIA)[A-Z0-9]{16}\b",
 "slack_token":rb"\bxox[baprs]-[A-Za-z0-9-]{20,}\b",
 "credential_url":rb"https?://[^\s/\"'<>:@]{1,64}:[^\s/\"'<>@]{8,}@",
}
def git(*args,data=None):
 return subprocess.run(["git","--no-optional-locks","-C",str(REPO),*args],input=data,capture_output=True,check=True).stdout

def scan(target):
 assert git("remote","get-url","origin").decode().strip()==git("remote","get-url","--push","origin").decode().strip()=="https://github.com/jenolaszlo-sketch/gagamba.git"
 git("merge-base","--is-ancestor",BASE,target)
 actual=git("rev-list","--reverse",BASE+".."+target).decode().splitlines()
 chain=CHAIN if target==Q1 else CHAIN+[(target,"docs/decisions/",STEM+"-owner-01")]
 assert actual==[s[0] for s in chain]
 previous=BASE
 unique={}
 commits=[]
 for rev,prefix,name in chain:
  assert git("show","-s","--format=%P",rev).decode().strip()==previous
  changes=git("diff-tree","--no-commit-id","--name-status","-r",rev).decode().splitlines()
  paths=[]
  for line in changes:
   status,path=line.split("\t")
   assert status=="A",line
   if rev==IMPL:
    approved={f"src/Gagamba.Execution.Linux.M1/Bootstrap/{s}.cs" for s in ["ValidationCustodyModelData","ValidationCustodyModel"]}
    approved|={f"tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModel{s}.cs" for s in ["Tests","LifecycleTests","CompositionTests","Vectors"]}
    assert path in approved,path
   else:
    assert path==prefix+name+".md" or path.startswith(prefix+name+"-evidence/"),path
   paths.append(path)
   oid=git("rev-parse",rev+":"+path).decode().strip()
   unique[oid]=path
  assert len(paths)==6 if rev==IMPL else True
  commits.append(dict(commit=rev,parent=previous,subject=git("show","-s","--format=%s",rev).decode().strip(),added_paths=len(paths)))
  previous=rev
 requests="".join(oid+"\n" for oid in unique).encode()
 raw=git("cat-file","--batch",data=requests)
 offset=0
 compressed=0
 findings=[]
 for oid,path in unique.items():
  end=raw.index(b"\n",offset)
  returned,kind,size=raw[offset:end].decode().split()
  assert returned==oid and kind=="blob"
  body=raw[end+1:end+1+int(size)]
  offset=end+2+int(size)
  if path.endswith(".gz"):
   body=gzip.decompress(body)
   compressed+=1
  for rule,pattern in RULES.items():
   if re.search(pattern,body):
    findings.append(dict(path=path,rule=rule))
 assert offset==len(raw)
 assert not findings,findings
 assert not git("diff","--name-only",IMPL,target,"--","src","tests","eng",".github","native","Gagamba.sln","Directory.Build.props","Directory.Build.targets")
 return dict(base=BASE,target=target,normal_fast_forward=True,commits=commits,unique_added_blobs=len(unique),
             expanded_gzip_blobs=compressed,scope="Only the authorized E-5 decision, implementation, producer, Q1 and owner paths",
             credential_patterns=list(RULES),credential_pattern_hits=findings,
             privacy_review="Local host paths and reviewer session identifiers are intended provenance. Embedded test identities and secrets are synthetic fixtures.",
             artifacts_review="Qualified evidence and source procedures are documented. No build outputs, package binaries or unrelated local files are included.")

if __name__=="__main__":
 target=sys.argv[1]
 result=scan(target)
 Path(sys.argv[2]).write_text(json.dumps(result,indent=2)+"\n",encoding="utf-8")
 print(f"Outgoing range PASS: {len(result['commits'])} authorized commits, {result['unique_added_blobs']} blobs, zero credential-pattern hits.")
