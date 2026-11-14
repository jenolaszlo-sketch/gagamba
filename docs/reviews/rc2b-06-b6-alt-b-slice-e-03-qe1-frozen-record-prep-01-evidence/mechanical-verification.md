# Mechanical verification of prepared bytes

Run from canonical Gagamba with Python. Copy the Python block below into
a PowerShell single-quoted here-string and pipe it to `python -`. Its input
documents are the adjacent witnesses. It reads Git's committed GOLD object
and checks preparation bytes without invoking production source. This is
producer-side verification, separate from independent Q1.

```python
from hashlib import sha256
from pathlib import Path
import json,re,struct,subprocess

git = lambda *args: subprocess.check_output(['git', *args])
def read(name):
    path = 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/' + name
    return Path(path).read_text(encoding='utf8')

J=re.findall(r'^(J[0-9]+) (\{.*)$',read('bootstrap-vectors.md'),re.M)
binary=read('binary-vectors.md')
hex_by_id=dict(re.findall(r'^((?:W[0-9]+|IE-V[0-9]+)) ([a-f0-9]+)$',binary,re.M))
metadata=re.findall(r'^\| ((?:W[0-9]+|IE-V[0-9]+)) \| ([0-9]+) \| ([^|]+) \| ([0-9]+) \|',binary,re.M)
B=[(vid,int(t),ctx.strip(),hex_by_id[vid]) for vid,t,ctx,n in metadata]
assert all(len(bytes.fromhex(hex_by_id[vid]))==int(n) for vid,t,ctx,n in metadata)
assert len(J)==18 and len(B)==26
expected=sha256(b'abc').digest()
clean=json.loads(J[0][1])
assert clean['AuthorityHash']==expected.hex()
assert json.loads(J[1][1])['Integrated']['InstallationIdentity']==clean['B2InstallationIdentity']
changes=[]
for vid, literal in J[2:]:
    if vid=='J18':
        try: json.loads(literal)
        except ValueError: continue
        raise AssertionError(vid)
    parsed=json.loads(literal)
    delta=[k for k in clean.keys()|parsed.keys() if k not in clean or k not in parsed or json.dumps(clean[k],sort_keys=True)!=json.dumps(parsed[k],sort_keys=True)]
    if vid=='J14':
        assert not delta and r'\u0062' in literal
    else:
        assert len(delta)==1
        if delta==['B2InstallationIdentity']:
            a=clean[delta[0]]
            b=parsed[delta[0]]
            assert len([k for k in a.keys()|b.keys() if k not in a or k not in b or type(a[k]) is not type(b[k]) or a[k]!=b[k]])==1
    changes.append(vid)
token=lambda s: re.search(r'"AuthorityHash":"([^"]*)"',s)
assert re.fullmatch('[0-9a-f]{64}',token(J[0][1])[1])
assert len(bytes.fromhex(token(J[0][1])[1]))==32
by_id=dict(J)
for vid in ('J11','J12','J13','J14'):
    assert not re.fullmatch('[0-9a-f]{64}',token(by_id[vid])[1])
assert len(bytes.fromhex(token(by_id['J15'])[1]))==32 and bytes.fromhex(token(by_id['J15'])[1])!=expected
print('JSON changed-field witnesses:',len(J),'literal inputs; two comparison-input controls')
for ident,n in [('J21',8192),('J22',8193)]:
    raw=J[0][1].encode()
    data=raw+b' '*(n-len(raw))
    assert len(data)==n and json.loads(data)==clean
    assert sha256(data).hexdigest() in read('bootstrap-vectors.md')
    print('SIZE',ident,n,sha256(data).hexdigest(),n<=8192)
gold=json.loads(git('cat-file','blob','HEAD:docs/evidence/rc2b-06-b6-impl-01-s3/golden-wire-vectors.json'))['vectors']
gold={row['id']:bytes.fromhex(row['payloadHex']) for row in gold}
seen={vid:bytes.fromhex(s) for vid,t,ctx,s in B}
for vid,t,ctx,s in B:
    raw=seen[vid]
    assert len(raw)*2==len(s)
    if t in (9,36) and len(raw)>=72:
        assert raw[70]==int(t==36) or vid=='W08'
        assert raw[71]==int(vid in ('W07','IE-V12'))
        assert struct.unpack_from('<HHH',raw,64)==(1,1,1)
for vid,t in [('W01',9),('W02',36),('W03',37)]:
    raw=seen[vid]
    offset=216 if t==37 else 71
    assert len(raw)==len(gold[t])
    assert [i for i,(a,b) in enumerate(zip(raw,gold[t])) if a!=b]==[offset]
    assert raw[offset]==0 and gold[t][offset]==1
profile=b'ae4-linux-fixed-fixture-v1'
tail=bytes([len(profile)])+profile+b'\x01\x31'
assert len(profile)==26 and len(tail)==29 and len(seen['W01'])==76
assert len(seen['IE-V02'])==105 and seen['IE-V02'][76:]==tail
assert seen['IE-V02'][76]==26 and seen['IE-V02'][103]==1 and seen['IE-V02'][104]==49
assert seen['W03'][217]==26 and seen['W03'][244]==1 and seen['W03'][245]==49
assert seen['W04']==gold[10] and len(seen['W04'])==36 and seen['W04'][:4]==b'\1\0\0\0'
assert seen['W05']==gold[12] and len(seen['W05'])==48
assert [i for i,(a,b) in enumerate(zip(seen['W10'],seen['W04'])) if a!=b]==[4]
assert [i for i,(a,b) in enumerate(zip(seen['W11'],seen['W05'])) if a!=b]==[16]
assert seen['W06']==seen['W01'][:-1]
assert seen['W09']==seen['IE-V03'] and seen['W12']==seen['W05']
assert seen['W13'][:4]==b'\0\1\0\0' and seen['W14'][:4]==b'\1\1\0\0'
print('Binary expectations:',len(B),'payload/context cases, v2 status-only controls and confirmation offsets checked')
pre=b'gagamba-validation-release-v1\0'+b''.join(bytes([a])*32 for a in (17,34,51,68))+struct.pack('<I',2)+b''.join(bytes([a])*16 for a in (85,102,119,136))+b'\x99'*32
assert len(pre)==258
release=sha256(pre).digest()
assert 'Preimage '+pre.hex() in binary and 'R01 '+release.hex() in binary
assert 'R02 '+(bytes([release[0]^1])+release[1:]).hex() in binary
print('PREIMAGE',pre.hex())
print('RELEASE',release.hex())
print('MISMATCH',(bytes([release[0]^1])+release[1:]).hex())
print('No project parser, build, tests or authenticated context used.')
```

Executed output:

```text
JSON changed-field witnesses: 18 literal inputs; two comparison-input controls
SIZE J21 8192 8c667f6fe288f1c16586c321d22451f0ad2f6812dd7dfea94d9c716ae8ed421e True
SIZE J22 8193 a510d96b830105eb55658677e7581ec454a1669bf94f17e0b0188b3b2cf01d85 False
Binary expectations: 26 payload/context cases, v2 status-only controls and confirmation offsets checked
PREIMAGE 676167616d62612d76616c69646174696f6e2d72656c656173652d763100111111111111111111111111111111111111111111111111111111111111111122222222222222222222222222222222222222222222222222222222222222223333333333333333333333333333333333333333333333333333333333333333444444444444444444444444444444444444444444444444444444444444444402000000555555555555555555555555555555556666666666666666666666666666666677777777777777777777777777777777888888888888888888888888888888889999999999999999999999999999999999999999999999999999999999999999
RELEASE 3737572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e
MISMATCH 3637572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e
No project parser, build, tests or authenticated context used.
```

The source identity pass was separate: raw SHA-256, byte length and UTF-8
properties matched the committed 12-file inventory. It also verified the
committed B2 amendment SHA-256 and literal equality of the formula lines
in F2 and AB. No accepted files or external packages were rewritten.
