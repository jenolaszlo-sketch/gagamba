# R1 bootstrap mechanical checks

The exact [owner witness annex](../../../docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/witnesses.md)
is blob `19886332f3fb1bcf3149f4e173642a8673fbb746` and its published
[mechanical check](../../../docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/mechanical-checks.md)
is blob `90e97bf480e336f7d17717f4484507161942aca7`. Both are bound
at `0fcd246812ffe1a8d9fa3813caccaf5863773eaf`.
The owner literals and expected inputs are reused exactly. P-B2 and P-I
exercise all required fields. P-LIMIT supplies the exact-size positive
and P-WS checks member ordering and whitespace. N01–N22 in that annex
are the original targeted negatives, with precise mutations and expected
failure rules. Their lengths, digests and outcomes are reproduced below.

All nine new cases are codec-level negative expectations. Each retains
the original structural comparison inputs. For object changes use compact
JSON in original member order, appending an inserted field to that
object. The script below gives the exact bytes and measures their SHA-256.

| ID | Parent | Exact mutation | Intended first failing invariant | Authority |
|---|---|---|---|---|
| R1-N01 | P-B2 | Remove root B2InstallationIdentity | B2 root required member | BB-01, schema B2 |
| R1-N02 | P-B2 | Remove root B2DeadlineNs | B2 root required member | BB-01, schema B2 |
| R1-N03 | P-I | Append root B2InstallationIdentity with the B2 identity object from P-B2 | Integrated root prohibits that name even with a valid B2 value | BB-01, schema Integrated |
| R1-N04 | P-I | Append root B2DeadlineNs with the positive i64 deadline from P-B2 | Integrated root prohibits that name even with a valid B2 value | BB-01, schema Integrated |
| R1-N05 | P-B2 | Remove Qualified from B2InstallationIdentity | Selected identity requires Boolean | BJ-01 |
| R1-N06 | P-B2 | Set B2InstallationIdentity.Qualified to Boolean true | Pre status is false only | BJ-01 |
| R1-N07 | P-I | Replace ASCII 1 in the first literal case-1 with raw octet FF | Invalid UTF-8 | Bootstrap owner §4 encoding |
| R1-N08 | P-I | Append Integrated.Runtime.Unknown with integer 0 | Closed nested Runtime member set | Bootstrap owner §4, schema Runtime |
| R1-N09 | P-I | Replace ASCII 1 in the first literal case-1 with C3 A9, then pad JSON with ASCII spaces to 8,193 bytes | Payload exceeds 8,192 bytes before decoding, despite 8,192 Unicode characters | Bootstrap owner §4 size |

R1-N09 would also fail the ASCII path rule if decoded. The size gate is
explicitly before decode. This control isolates counting bytes rather
than treating the multibyte path as a valid positive.

## Reproduce

Run in canonical Gagamba with `python -` and the following code on stdin,
for example using a single-quoted PowerShell here-string. The script
reads the exact source commit's documents through Git and runs the
documented producer check with those immutable inputs. It performs no
native, production-codec, protected I/O or independent Q1 exercise.

```python
from pathlib import Path
import hashlib,json,re,subprocess
g=lambda *a:subprocess.check_output(['git',*a])
base='docs/decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01'
def committed(p): return g('cat-file','blob','0fcd246812ffe1a8d9fa3813caccaf5863773eaf:'+p)
def sha(b): return hashlib.sha256(b).hexdigest()
def pack(x): return json.dumps(x,separators=(',',':')).encode()
owner=committed(base+'-evidence/mechanical-checks.md').decode()
code=re.search(r'\x60{3}python\n(.*?)\n\x60{3}',owner,re.S)[1]
# Use exact Git annex bytes as inputs to the immutable owner check.
source=code.replace('w = (base / "witnesses.md").read_text(encoding="utf8")','w = committed(str(base / "witnesses.md").replace(chr(92),"/")).decode()')
source=source.replace('s = (base / "schema.md").read_text(encoding="utf8")','s = committed(str(base / "schema.md").replace(chr(92),"/")).decode()')
ns={'committed':committed}
exec(compile(source,'bootstrap owner evidence','exec'),ns)
tables=ns['tables']
assert len(tables)==11 and sum(map(len,tables.values()))==99
print('Schema rows', ' / '.join(f'{key}:{len(rows)}' for key,rows in tables.items()))
b,i=ns['b'],ns['i']
assert (len(b),len(i))==(2693,4519)
assert sha(b)=='52626efe08bac30435ffde51036413380a31cf17bdbfff8f2fc01841455ddfbf'
assert sha(i)=='46ed4d54b119baacb4cb737dc70d1924cfca0a3241298c524081b6f691de069c'
e=ns['expected']
def field_change(parent,path,value):
    x=json.loads(parent)
    node=x
    for name in path[:-1]: node=node[name]
    if value is Ellipsis: del node[path[-1]]
    else: node[path[-1]]=value
    return pack(x)
cases=[
(b,'B2',['B2InstallationIdentity'],Ellipsis,'B2InstallationIdentity'),
(b,'B2',['B2DeadlineNs'],Ellipsis,'B2DeadlineNs'),
(i,'Integrated',['B2InstallationIdentity'],json.loads(b)['B2InstallationIdentity'],'B2InstallationIdentity'),
(i,'Integrated',['B2DeadlineNs'],json.loads(b)['B2DeadlineNs'],'B2DeadlineNs'),
(b,'B2',['B2InstallationIdentity','Qualified'],Ellipsis,'Qualified'),
(b,'B2',['B2InstallationIdentity','Qualified'],True,'False'),
(i,'Integrated',[],i.replace(b'case-1',b'case-\xff',1),'utf8'),
(i,'Integrated',['Integrated','Runtime','Unknown'],0,'Unknown'),
(i,'Integrated',[],i.replace(b'case-1',b'case-\xc3\xa9',1),'byte size'),
]
for ix,(parent,form,path,value,want) in enumerate(cases,1):
    raw=field_change(parent,path,value) if path else value
    if ix==9:
        raw+=b' '*(8193-len(raw))
        # The string is non-ASCII and fails Path at a later layer.
        # This control demonstrates that the size check must come first.
        assert len(raw)==8193 and len(raw.decode('utf8'))==8192
        want='size'
    try: ns['check'](raw,form,e)
    except UnicodeDecodeError: assert ix==7
    except AssertionError as err: assert want in str(err),(ix,want,str(err))
    else: raise AssertionError(ix)
    print(f'R1-N{ix:02}',len(raw),sha(raw),'REFUSE',want)
print('4 positive and 31 negative bootstrap controls, including 9 R1-only. No codec run.')
```

## Measured output

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
Schema rows B2:16 / Integrated:14 / IntegratedBody:8 / RecoveryPin:2 / OwnerPin:2 / RecoveryHashes:5 / OwnerHashes:5 / PreIdentity:16 / Controller:5 / Runtime:7 / Profile:19
R1-N01 1841 f4d1263392379f3d328408d58e4002fd2b0935aeaa42f8e81c65bb087c277a9a REFUSE B2InstallationIdentity
R1-N02 2667 5e0335e63d4d7f0c4e2ff9f2a5f8fd615fdcf42ff88e52b6051cce9e4e95917e REFUSE B2DeadlineNs
R1-N03 5371 6e1a438f8e4a1330330c30edb51a73b87ca0b72157c509b10649177eea1a10f8 REFUSE B2InstallationIdentity
R1-N04 4545 a9d9029c4663b98ba035d9a491298d8a1e81558e526fe125c84fd0a2595be594 REFUSE B2DeadlineNs
R1-N05 2675 b0a27f0b5fab0596ec1078e31de71a1593e87fc9b7f687e69bcaee3a030b2d5f REFUSE Qualified
R1-N06 2692 b5efff9d786da087969950294ba4e75b4d34a956ab5b3d3baf16fefcaf3e36fe REFUSE False
R1-N07 4519 db4d3b41dde07dad26a3d941544ab0bb71db2095849b397cef6cb02831b11928 REFUSE utf8
R1-N08 4531 631019ea75c252beee6b51865d5956ad93d64f2a0ad4f3ef6f9174b13a200f4f REFUSE Unknown
R1-N09 8193 de42804776bac010d3cceaee5f91f72bdfa661a1558dbff5044a5da274eb410b REFUSE size
4 positive and 31 negative bootstrap controls, including 9 R1-only. No codec run.
```
