# Reproducible documentation checks

From the canonical checkout, pipe the Python block below to `python -`
(for example, using a PowerShell single-quoted here-string). The check
reads the member inventory and exact structural fixtures. It verifies
the measured witnesses, targeted mutation failure reason, digest
comparison and representative cross-field relationships. It is a
documentation check, not a production decoder, exhaustive conformance
test or independent Q1. It performs no runtime or protected I/O.

```python
from pathlib import Path
import base64, hashlib, json, re
base = Path("docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence")
w = (base / "witnesses.md").read_text(encoding="utf8")
s = (base / "schema.md").read_text(encoding="utf8")
tables = {}
for name, body in re.findall(r"^## (\w+)\n([\s\S]*?)(?=^## |\Z)", s, re.M):
    rows = re.findall(r"^\| ~([^~]+)~ \| (\w+\+?) \| Yes \|".replace("~",chr(96)),body,re.M)
    if rows: tables[name]=dict(rows)
assert len(tables)==11
def pack(o): return json.dumps(o,separators=(",",":")).encode()
def pairs(rows):
    assert len(dict(rows))==len(rows),"duplicate"
    return dict(rows)
def read(b):
    assert 1<=len(b)<=8192,"size"
    assert not b.startswith(b"\xef\xbb\xbf"),"BOM"
    return json.loads(b.decode("utf8"),object_pairs_hook=pairs)
data={n: re.search(r"### "+n+r"\n\n~~~json\n([^\n]+)".replace("~",chr(96)),w)[1].encode() for n in ("P-B2","P-I")}
expected=hashlib.sha256(b"abc").digest()
values={"V3":3,"One":1,"Four":4,"Budget":16777216,"ProfileId":"ae4-linux-fixed-fixture-v1","ProfileVersion":"1","PreMode":"validation-pre","Tool":"tool","Input":"input","Coordinator":"coordinator","Filter":"filter","False":False,"Null":None}
bounds={"I64+":2**63-1,"Lead":120000000000,"Pid":2**31-1,"Principal":2**31-1}
regex={"Guid":r"[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}","Sha":r"[0-9a-f]{64}","UpperSha":r"[0-9A-F]{64}","PidNs":r"pid:\[[1-9][0-9]*\]","UserNs":r"user:\[[1-9][0-9]*\]","RuntimeVersion":r"10\.0\.(0|[1-9][0-9]*)","ValidId":r"[A-Za-z0-9._-]{1,128}","CapabilityHex":r"[0-9a-f]{64}"}
def fields(t,x):
    if t in tables:
        assert isinstance(x,dict),"object "+t
        miss=tables[t].keys()-x.keys()
        extra=x.keys()-tables[t].keys()
        assert not (miss or extra), (t,sorted(miss),sorted(extra))
        for k,kind in tables[t].items(): fields(kind,x[k])
    elif t in values:
        assert type(x) is type(values[t]) and x==values[t],t
    elif t in bounds:
        assert type(x) is int and 1<=x<=bounds[t],t
    elif t in regex:
        assert isinstance(x,str) and re.fullmatch(regex[t],x),t
        if t in {"Guid","Sha","UpperSha"}: assert x.strip("0-"),t
        if t=="ValidId": assert x[0]!="." and x[-1]!=".",t
        if t in {"PidNs","UserNs"}: assert int(x.split("[")[1][:-1])<=2**64-1,t
        if t=="RuntimeVersion": assert int(x.split(".")[-1])<=2**31-1,t
    elif t=="Secret32":
        y=base64.b64decode(x,validate=True)
        assert len(y)==32 and base64.b64encode(y).decode()==x,t
    elif t=="Path":
        assert isinstance(x,str) and 2<=len(x)<=1024 and x.startswith("/"),t
        assert all(y and y not in (".","..") and all("!"<=c<="~" and c!="\\" for c in y) for y in x[1:].split("/")),t
    else: raise AssertionError(t)
def check(b,form,cap):
    o=read(b)
    fields(form,o)
    raw=re.search(rb'"AuthorityHash"\s*:\s*"([^"]*)"',b)[1]
    assert re.fullmatch(rb"[0-9a-f]{64}",raw),"literal cap"
    assert len(cap)==32 and bytes.fromhex(raw.decode())==cap,"capability equality"
    i=o["B2InstallationIdentity"] if form=="B2" else o["Integrated"]["InstallationIdentity"]
    assert i["Generation"]==i["ArtifactSetSha256"],"generation"
    d="/opt/gagamba/installations/"+i["Generation"]
    assert o["Recovery"]["Directory"]==d+"/recovery" and o["Owner"]["Directory"]==d+"/owner","pins"
    if form=="Integrated":
        g=o[form]
        c=g["Controller"]
        assert c["Pid"]==o["ControllerPid"] and c["Uid"]==o["ControllerUid"],"controller equality"
        assert 0<g["ExpiresAtNs"]-g["AcceptedAtNs"]<=120000000000,"interval"
        r=g["Runtime"]
        p=g["Profile"]
        assert r["HostPath"]=="/usr/lib/dotnet/dotnet"
        assert r["RuntimeDirectory"]=="/usr/lib/dotnet/shared/Microsoft.NETCore.App/"+r["RuntimeVersion"]
        assert r["HostFxrDirectory"]=="/usr/lib/dotnet/host/fxr/"+r["RuntimeVersion"]
        assert bytes.fromhex(r["RuntimeInventorySha256"])==bytes.fromhex(i["RuntimeInventorySha256"])
        assert o["DelegatedParent"]==p["DelegatedParentPath"]
        name="Gagamba.Execution.Linux.M1.Owner.dll"
        assert p["OwnerDllPath"]==d+"/owner/"+name and p["OwnerDllSha256"]==o["Owner"]["Sha256"][name]
        assert p["CoordinatorSha256"]==i["CoordinatorSha256"]
        assert re.fullmatch(r"/opt/gagamba/validation-fixtures/[0-9a-f]{64}",p["SourceRootPath"])
        assert all(p[k].endswith("/va-"+o["Execution"].replace("-","")) for k in ("StagingParentPath","JournalDirectory"))
def out(n,b,result):
    print(n,len(b),hashlib.sha256(b).hexdigest(),result)
b=data["P-B2"]; i=data["P-I"]; I="Integrated"; B="B2"
lim=b+b" "*(8192-len(b))
ws=b"\t"+pack(dict(reversed(list(read(i).items()))))+b"\n"
for n,raw,kind in (("P-B2",b,B),("P-I",i,I),("P-LIMIT",lim,B),("P-WS",ws,I)):
    check(raw,kind,expected)
    out(n,raw,"PASS structural")
def mutate(raw,path,val):
    o=read(raw)
    t=o
    for k in path[:-1]: t=t[k]
    if val is Ellipsis: del t[path[-1]]
    else: t[path[-1]]=val
    return pack(o)
mutations=[
(i,I,("B2InstallationIdentity",),None,"B2InstallationIdentity"),
(i,I,("B2DeadlineNs",),None,"B2DeadlineNs"),
(b,B,("EvidenceDirectory",),Ellipsis,"EvidenceDirectory"),
(i,I,(I,"Runtime","HostSha256"),Ellipsis,"HostSha256"),
(i,I,(I,"Controller"),[],"object Controller"),
(b,B,("Integrated",),{},"Null"),
(i,I,(I,"InstallationIdentity","qualificationId"),Ellipsis,"qualificationId"),
(i,I,(I,"InstallationIdentity","qualificationRecordSha256"),"","Null"),
(b,B,("B2InstallationIdentity","Qualified"),0,"False"),
(i,I,("AuthorityHash",),"B"+expected.hex()[1:],"CapabilityHex"),
(b,B,("Other",),True,"Other"),
(i,I,(),i[:-1]+b',"Version":3}',"duplicate"),
(b,B,("Version",),4,"V3"),
(i,I,("Version",),2,"V3"),
(b,B,(),lim+b" ","size"),
(i,I,(I,"Controller","Pid"),124,"controller equality"),
(i,I,(I,"Profile","QualificationFault"),None,"QualificationFault"),
(b,B,(),b,"capability equality"),
(i,I,(I,"Runtime","RuntimeInventorySha256"),"a"*64,"UpperSha"),
(i,I,(),i[:-1]+br',"\u0056ersion":3}',"duplicate"),
(b,B,("B2DeadlineNs",),None,"I64+"),
(i,I,(),b"\xef\xbb\xbf"+i,"BOM")]
for j,(src,kind,path,val,why) in enumerate(mutations,1):
    raw=mutate(src,path,val) if path else val
    cap=bytes([0xaa])+expected[1:] if j==18 else expected
    try: check(raw,kind,cap)
    except AssertionError as error: assert why in str(error),(j,why,str(error))
    else: raise AssertionError(("unexpected acceptance",j))
    out(f"N{j:02}",raw,"REFUSE")
print("11 closed object inventories; 4 structural positives; 22 targeted refusals.")
```

Actual output:

```text
P-B2 2693 52626efe08bac30435ffde51036413380a31cf17bdbfff8f2fc01841455ddfbf PASS structural
P-I 4519 46ed4d54b119baacb4cb737dc70d1924cfca0a3241298c524081b6f691de069c PASS structural
P-LIMIT 8192 70de1b51f0e1efc9cab3e2e83bf9e0e04f9a54e37bcb749d6d0e7c88571f4c27 PASS structural
P-WS 4521 d474bb76f6ee9362cd69ed60426965dd6c31c40fdc5c2b0e28b8459ef61a987d PASS structural
N01 4549 4188f4d58edd44ee25d42e6c403c8f6e363cdf01731d656a43c41349b3905587 REFUSE
N02 4539 acb2b1c9f15e7c30a2beb59d4b294b07da7364c3eff3938b3b37aa3293f5a49e REFUSE
N03 2638 f77fc4082170686edbd5c078782b4b859d36b8c1e45e1f5be0640061c1eead19 REFUSE
N04 4439 5c3a1301768dbe0c1fb6c3c411d370b9f1d1d22c0d763bb7071e43094d4880f3 REFUSE
N05 4418 c8def438548cc730fcd6f28d0bb8e0e76bd6fb0564515dad4eb540a3b2716d3e REFUSE
N06 2691 f384d5db30538fc0fc862c0ee863ea1204e32753602476dd1645b519de1407f1 REFUSE
N07 4496 a25c611792c3381b33aa680d7988f6c4b4ea2eff8b92e591040587656b336ca9 REFUSE
N08 4517 1164dc5f3e824b765bcc16f6e3cfa5bb5fac7f829af6208d2d04597e89056f6b REFUSE
N09 2689 3d7208351015df699dca4baba2319ef5b9bdabe684a3d5cf3db7c083118c842f REFUSE
N10 4519 bb5ca7b036e611004a295c7e1387adcb4e5cacf671bd1e7dffa65d35df74694c REFUSE
N11 2706 f860822e5fbe1f1d13334e357a9bc368822f09308f4450abb266b6de13b148d7 REFUSE
N12 4531 06b3113fb273370b603ee4700868f11fca5896d71993f41a9d46a0336b6d2e81 REFUSE
N13 2693 5d3299d7c59bc467528cd6edfad2ddac9c35a3488594648285f9fd155bcdb29a REFUSE
N14 4519 8a240e21231ed1190805b8e9be6b393bacbaede873697fd90823e96706f5ccf4 REFUSE
N15 8193 be4c76739ad1b146ecfb7fc922eb20149bd108be5ca75a6cedf9a2de121659f4 REFUSE
N16 4519 eef2ab83cd6c3945ba1fafed6d165c256309dc7fa5832dd0a646b31835eeae52 REFUSE
N17 4545 033ade4b02e3a043556e5c842f205bdf807e40183aaa691f8f5e240b8c7ef587 REFUSE
N18 2693 52626efe08bac30435ffde51036413380a31cf17bdbfff8f2fc01841455ddfbf REFUSE
N19 4519 c3b7885251b4024df7b624893f053f5e5c1f70fba42383798cacc8f72f17d363 REFUSE
N20 4536 236714dcd2aa69cfbadd105070bf164e21636071329fb485af4b60e832dd593f REFUSE
N21 2687 c3e6298b8eff10fd018b8eeb565e221b9384c996b77aabaf779a976dda61def2 REFUSE
N22 4522 42e0385166b0726706b1a9e4174640737014181e9ecc2b0677107da3b4aa52cb REFUSE
11 closed object inventories; 4 structural positives; 22 targeted refusals.
```
