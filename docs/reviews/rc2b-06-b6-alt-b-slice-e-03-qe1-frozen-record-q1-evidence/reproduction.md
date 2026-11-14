# Reproduce the independent checks

Run from the canonical Gagamba root using Python 3.14 or compatible
standard-library Python. Save the Python block to reviewer.py in temporary
space. The script reads the starting revision by Git identity and the twelve
named external Gagamba evidence files from their historical artifact root.
It uses no production project code, dependency installation, network or
runtime setup.

~~~powershell
$q = Join-Path ([System.IO.Path]::GetTempPath()) 'gagamba-qe1-q1-acd61fd'
New-Item -ItemType Directory -Force -Path $q | Out-Null
python (Join-Path $q 'reviewer.py') $q 'C:/Users/Laszlos/source/repos/Solo'
~~~

Observed exit 0. The probe regenerated the source hash table, 99-row
inventory, complete witness summary, 110 bootstrap outcomes (13 pass,
97 refuse), 46 binary outcomes (10 pass, one well-formed refusal, 35 other
refusals), and six field-only release outcomes (one agreement, five
refusals). The produced tables match the embedded evidence. The review
ran this script from scratch space, and the following listing provides
the exact reviewer source for reproducibility.

~~~python
import base64
import copy
import hashlib
import json
import re
import struct
import subprocess
import sys
from collections import Counter
from pathlib import Path
from uuid import UUID

q = Path(sys.argv[1])
P = 'rc2b-06-b6-alt-b-slice-e-03-'
D = 'docs/decisions/' + P + 'qe1-'
R = 'docs/reviews/' + P + 'qe1-'
C = 'docs/evidence/' + P + 'v3-pre-frozen-record-candidate-01.md'
BASE = 'acd61fd0af622f9e1d988845658c26a6835ddd62'
OWNER = '0fcd246812ffe1a8d9fa3813caccaf5863773eaf'
ANNEX = D + 'bootstrap-base-owner-01-evidence/'
ORIG = R + 'provenance-reconciliation-01-evidence/sources-and-verification.md'
binary = R + 'frozen-record-prep-01-evidence/binary-vectors.md'

def git(*args):
    return subprocess.check_output(['git', *args])

def raw(path, ref=BASE):
    return git('show', f'{ref}:{path}')

def txt(path, ref=BASE):
    return raw(path, ref).decode('utf-8')

def sha(data):
    return hashlib.sha256(data).hexdigest()

def lines(name, text):
    (q / (name + '.md')).write_bytes(text.encode('utf-8'))

roles = {role: [f'Gagamba.Execution.Linux.M1.{role}{suffix}'
                for suffix in ('.dll', '.deps.json', '.runtimeconfig.json')]
         + ['Gagamba.Execution.Linux.M1.dll', 'Gagamba.Execution.dll']
         for role in ('Recovery', 'Owner')}

def fields(spec):
    return {name: kind for rule in spec.split(';')
            for kind, names in [rule.split(':')] for name in names.split()}

common = fields('V3:Version;Guid:Execution Generation Session;Pid:ControllerPid;Principal:ControllerUid;Secret32:Secret;RecoveryPin:Recovery;OwnerPin:Owner;Path:EvidenceDirectory;PreMode:AuthorityMode;CapabilityHex:AuthorityHash')
inventory = {
    'B2': common | fields('Null:DelegatedParent Integrated;PreIdentity:B2InstallationIdentity;I64+:B2DeadlineNs'),
    'Integrated': common | fields('Path:DelegatedParent;IntegratedBody:Integrated'),
    'IntegratedBody': fields('Profile:Profile;Guid:Issuer StoreGeneration;I64+:AcceptedAtNs ExpiresAtNs;Controller:Controller;Runtime:Runtime;PreIdentity:InstallationIdentity'),
    'PreIdentity': fields('Sha:ArtifactSetSha256 OwnerClosureSha256 RecoveryClosureSha256 CoordinatorSha256 SeccompFilterSha256 RuntimeInventorySha256 Generation;One:ManifestSchema OwnerProtocol CoordinatorProtocol Bootstrap;ProfileId:ProfileId;ProfileVersion:ProfileVersion;False:Qualified;Null:qualificationId qualificationRecordSha256'),
    'Controller': fields('Pid:Pid;Principal:Uid Gid;PidNs:PidNamespace;UserNs:UserNamespace'),
    'Runtime': fields('Path:HostPath RuntimeDirectory HostFxrDirectory;Sha:HostSha256;UpperSha:RuntimeInventorySha256 HostFxrInventorySha256;RuntimeVersion:RuntimeVersion'),
    'Profile': fields('Path:OwnerDllPath DelegatedParentPath SourceRootPath StagingParentPath JournalDirectory;Sha:OwnerDllSha256 ToolSha256 InputSha256 CoordinatorSha256;Tool:ToolSourceName;Input:InputSourceName;Coordinator:CoordinatorSourceName;Filter:FilterSourceName;ValidId:ToolEnrollmentId InputEnrollmentId;Budget:TotalBudgetBytes;Four:MaxObjects;Lead:ExpiresLeadNanoseconds;Null:IntegratedRecovery'),
}
for role in roles:
    inventory[role + 'Pin'] = {'Directory': 'Path', 'Sha256': role + 'Hashes'}
    inventory[role + 'Hashes'] = dict.fromkeys(roles[role], 'Sha')

schema = txt(ANNEX + 'schema.md', OWNER)
candidate = txt(C)
assert raw(ANNEX+'schema.md') == raw(ANNEX+'schema.md', OWNER)
blob = git('rev-parse', f'{OWNER}:{ANNEX}schema.md').decode().strip()
assert blob == 'a7248152d09e19d8f3775db397f340949fa9ee57'
assert blob in candidate and 'Every listed member is required exactly once' in candidate
assert 'integrated forbids either' in candidate
owner_checks = txt(D + 'bootstrap-base-owner-01.md', OWNER)
a = candidate.split('### Cross-field checks and caller duties\n',1)[1].split('### Bootstrap verification fixtures')[0].strip()
b = owner_checks.split('### Cross-field checks and caller duties\n',1)[1].split('from the closed schema.',1)[0] + 'from the closed schema.'
assert a.startswith(b.lstrip()), 'Cross-field prose delta'
scope = {key: 'Both' for key in inventory} | {
    'B2':'B2', 'Integrated':'I', 'IntegratedBody':'I', 'Controller':'I',
    'Runtime':'I', 'Profile':'I'}
matrix = ['| Object | Member | Type | Presence | Nullability | v3-pre | Source | Candidate match |',
          '|---|---|---|---|---|---|---|---|']
annex_groups = {}
for section in re.split(r'^## ', schema, flags=re.M):
    title = section.split('\n',1)[0]
    if title not in inventory:
        continue
    rows = re.findall(r'^\| \x60([^\x60]+)\x60 \| ([^|]+) \| ([^|]+) \| ([^|]+) \| ([^|]+) \| [^|]+ \| ([^|]+) \|', section, re.M)
    observed = {}
    for name, kind, presence, nullable, omission, source in rows:
        kind, presence, nullable, omission, source = map(str.strip, (kind,presence,nullable,omission,source))
        assert name not in observed
        observed[name] = kind
        assert presence == 'Yes' and omission == 'No'
        assert nullable == ('Only null' if kind == 'Null' else 'No')
        matrix.append(f'| {title} | \x60{name}\x60 | {kind} | Once | {nullable} | {scope[title]} | BB §4 ({source}) | Exact annex |')
    assert observed == inventory[title], title
    annex_groups[title] = observed
assert annex_groups == inventory and len(inventory)==11 and len(matrix)==101
lines('schema-table', '\n'.join(matrix) + '\n')

folders = dict(zip(('AD','RV','F','FS'),(
    'gagamba-integrated-start-authority-owner-decision-01',
    'gagamba-integrated-start-authority-owner-decision-q1',
    'gagamba-f02-authority-mechanism-decision-01',
    'gagamba-f02-authority-mechanism-owner-01')))
source_root = Path(sys.argv[2])
sources = {}
source_names = {}
source_table = ['| ID | Bytes | Observed raw SHA-256 |', '|---|---:|---|']
manifest = txt(ORIG)
for ident, folder, name, size, digest in re.findall(
        r'^\| ([\w-]+) \| \x60(\w+)/([^\x60]+)\x60 \| (\d+) \| \x60([0-9a-f]{64})\x60',manifest, re.M):
    data = (source_root / folders[folder] / name).read_bytes()
    assert (len(data),sha(data))==(int(size),digest), ident
    sources[ident] = data.decode('utf-8')
    source_names[ident] = name
    source_table.append(f'| {ident} | {len(data)} | \x60{sha(data)}\x60 |')
assert len(sources)==12
for manifest_id, ids in {'AB-M':('AB','AB-H'),'AR-M':('AR',),'F-M':('F1','F2','F3')}.items():
    for ident in ids:
        digest_line = sha(sources[ident].encode()) + '  ' + source_names[ident]
        assert digest_line in sources[manifest_id].splitlines(), ident
sources_git = []
for ident, path, blob in re.findall(
        r'^\| ([\w-]+) \| \[[^\]]+\]\(([^)]+)\) \| \x60([0-9a-f]{40})\x60 \|',manifest,re.M):
    path = (Path(ORIG).parent / path).resolve().relative_to(Path.cwd()).as_posix()
    for ref in ('52b932aaebd52f16bd93170b95bb0f8e89241fd2',BASE):
        assert git('rev-parse',f'{ref}:{path}').decode().strip()==blob, (ident,ref)
    sources_git.append(f'| {ident} | \x60{blob}\x60 |')
assert len(sources_git)==13
assert sha(raw('docs/design/rc2b-06-b6-s3-b2-wire-delta-01.md')) == '88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6'
for path,ids in (
    ('docs/decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md',('AB-M','AR-M')),
    ('docs/decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md',('F-M','FR','FMX','FDB'))):
    for ident in ids:
        assert sha(sources[ident].encode()) in txt(path), ident
lines('sources-raw','\n'.join(source_table)+'\n\n| Git ID | Blob at entry baseline and Q1 baseline |\n|---|---|\n'+'\n'.join(sources_git)+'\n')

profile_id = 'ae4-linux-fixed-fixture-v1'
profile_version = '1'
def require(ok, label):
    if not ok:
        raise ValueError(label)

def matches(pattern, value):
    return isinstance(value,str) and re.fullmatch(pattern,value,re.ASCII) is not None

string_token = r'"(?:[^"\\\x00-\x1f]|\\(?:["\\/bfnrt]|u[0-9a-fA-F]{4}))*"'
member = re.compile(fr'({string_token})[ \r\n\t]*:[ \r\n\t]*({string_token})?')

def pairs(items):
    value = {}
    for key, item in items:
        require(not matches(r'[\s\S]*[\ud800-\udfff][\s\S]*',key), 'surrogate')
        require(key not in value,'duplicate')
        value[key] = item
    return value

def integer(text):
    require(matches(r'(?:0|[1-9][0-9]*)',text), 'integer token')
    require(len(text)<=20, 'integer range')
    return int(text)

def noninteger(_):
    raise ValueError('integer token')

def shape(value, kind, path=''):
    if isinstance(value,str):
        require(not matches(r'[\s\S]*[\ud800-\udfff][\s\S]*',value), 'surrogate')
    if kind in inventory:
        require(type(value) is dict,'object ' + path)
        expected = inventory[kind]
        extra = value.keys()-expected.keys()
        absent = expected.keys()-value.keys()
        require(not extra,'extra ' + path + '.'.join(sorted(extra)))
        require(not absent,'missing ' + path + '.'.join(sorted(absent)))
        for key, k in expected.items():
            shape(value[key],k,f'{path}{key}.')
        return

    fixed = {'ProfileId':profile_id, 'ProfileVersion':profile_version, 'PreMode':'validation-pre',
             'Tool':'tool','Input':'input','Coordinator':'coordinator','Filter':'filter'}
    bounds = {'V3':(3,3),'One':(1,1),'Four':(4,4),'Budget':(16777216,16777216),
              'I64+':(1,2**63-1),'Pid':(1,2**31-1),'Principal':(1,2**31-1),'Lead':(1,120000000000)}
    if kind in fixed:
        ok = value == fixed[kind]
    elif kind in bounds:
        lo,hi = bounds[kind]
        ok = type(value) is int and lo<=value<=hi
    elif kind == 'False':
        ok = value is False
    elif kind == 'Null':
        ok = value is None
    elif kind == 'Guid':
        ok = matches(r'[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}',value) and UUID(value).int!=0
    elif kind in ('Sha','UpperSha','CapabilityHex'):
        alphabet = '[0-9A-F]' if kind=='UpperSha' else '[0-9a-f]'
        ok = matches(alphabet+'{64}',value) and (kind=='CapabilityHex' or int(value,16)!=0)
    elif kind == 'Path':
        ok = isinstance(value,str) and 2<=len(value)<=1024 and value.startswith('/') and all(
            matches(r'[!-~]+',part) and part not in ('.','..') and chr(92) not in part for part in value[1:].split('/'))
    elif kind == 'Secret32':
        ok = matches(r'[A-Za-z0-9+/]{43}=',value) and base64.b64encode(base64.b64decode(value)).decode()==value
    elif kind == 'ValidId':
        ok = matches(r'[A-Za-z0-9._-]{1,128}',value) and not value.startswith('.') and not value.endswith('.')
    elif kind in ('PidNs','UserNs'):
        prefix = 'pid' if kind=='PidNs' else 'user'
        ok = matches(prefix+r':\[[1-9][0-9]{0,19}\]',value) and int(value.split('[')[1][:-1])<=2**64-1
    elif kind == 'RuntimeVersion':
        ok = matches(r'10\.0\.(?:0|[1-9][0-9]{0,9})',value) and int(value.split('.')[-1])<=2**31-1
    else:
        raise AssertionError(kind)
    require(ok, kind+' '+path.rstrip('.'))

def selected(data, topology):
    return data['B2InstallationIdentity'] if topology=='B2' else data['Integrated']['InstallationIdentity']

def structural(data, topology, expected, expected_digest):
    require(1 <= len(data) <= 8192,'size')
    text = data.decode('utf-8',errors='strict')
    doc = json.loads(text, object_pairs_hook=pairs, parse_int=integer,parse_float=noninteger,parse_constant=noninteger)
    shape(doc,topology)
    tokens = [m.group(2) for m in member.finditer(text) if json.loads(m.group(1))=='AuthorityHash']
    require(len(tokens)==1 and matches('"[0-9a-f]{64}"',tokens[0]),'AuthorityHash literal token')
    require(isinstance(expected_digest,bytes) and len(expected_digest)==32,'expected digest width')
    require(bytes.fromhex(doc['AuthorityHash'])==expected_digest,'expected digest equality')
    identity = selected(doc,topology)
    require(identity['ArtifactSetSha256']==identity['Generation'],'installation generation equality')
    for role in roles:
        directory = f"/opt/gagamba/installations/{identity['Generation']}/{role.lower()}"
        require(doc[role]['Directory']==directory,'role directory')
    if topology=='Integrated':
        body=doc['Integrated']
        controller, runtime, profile = (body[key] for key in ('Controller','Runtime','Profile'))
        require((controller['Pid'],controller['Uid'])==(doc['ControllerPid'],doc['ControllerUid']),'controller equality')
        require(0<body['ExpiresAtNs']-body['AcceptedAtNs']<=120000000000,'integrated interval')
        require(runtime['HostPath']=='/usr/lib/dotnet/dotnet','host path')
        for key, prefix in (('RuntimeDirectory','shared/Microsoft.NETCore.App'),('HostFxrDirectory','host/fxr')):
            require(runtime[key]==f"/usr/lib/dotnet/{prefix}/{runtime['RuntimeVersion']}",'runtime paths')
        require(bytes.fromhex(runtime['RuntimeInventorySha256'])==bytes.fromhex(identity['RuntimeInventorySha256']),
                'runtime inventory bytes')
        require(doc['DelegatedParent']==profile['DelegatedParentPath'],'delegated equality')
        owner_dll=roles['Owner'][0]
        require(profile['OwnerDllPath']==doc['Owner']['Directory']+'/'+owner_dll,'owner DLL path')
        require(profile['OwnerDllSha256']==doc['Owner']['Sha256'][owner_dll],'owner DLL digest')
        require(profile['CoordinatorSha256']==identity['CoordinatorSha256'],'coordinator equality')
        require(matches(r'/opt/gagamba/validation-fixtures/[0-9a-f]{64}',profile['SourceRootPath']),'fixture root')
        suffix='/va-'+doc['Execution'].replace('-','')
        require(all(profile[key].endswith(suffix) for key in ('StagingParentPath','JournalDirectory')),'execution directory')
    require(expected is not None,'original data missing')
    require(all(doc[key]==expected[key] for key in expected),'original projection')
    return 'pass'

def encode(doc):
    return json.dumps(doc,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()

def fixture(topology):
    def digest(label):
        return sha(('Q1 synthetic '+label).encode())
    def ident(n):
        return f'397812ef-4b30-8014-9120-{n:012x}'
    artifact=digest('artifact')
    identity = {
        'ArtifactSetSha256':artifact, 'Generation':artifact, 'CoordinatorSha256':digest('coordinator'),
        'SeccompFilterSha256':digest('filter'), 'RuntimeInventorySha256':digest('runtime'),
        'ManifestSchema':1, 'OwnerProtocol':1, 'CoordinatorProtocol':1, 'Bootstrap':1,
        'ProfileId':profile_id, 'ProfileVersion':profile_version, 'Qualified':False,
        'qualificationId':None,'qualificationRecordSha256':None,
    }
    doc=dict(Version=3,Execution=ident(71),Generation=ident(72),Session=ident(73),ControllerPid=2804,
             ControllerUid=2052,Secret=base64.b64encode(bytes(reversed(range(32)))).decode(),
             EvidenceDirectory='/srv/review/evidence/q1',AuthorityMode='validation-pre',
             AuthorityHash=digest(topology+' capability'))
    for idx, role in enumerate(roles):
        hashes={name:digest(role+name) for name in roles[role]}
        ordered=[roles[role][i] for i in (4,1,0,3,2)]
        identity[role+'ClosureSha256']=sha(b'gagamba-b6-role-v1\0'+bytes([idx])+b''.join(bytes.fromhex(hashes[n]) for n in ordered))
        doc[role]=dict(Directory=f'/opt/gagamba/installations/{artifact}/{role.lower()}',Sha256=hashes)
    if topology=='B2':
        return doc | dict(DelegatedParent=None,Integrated=None,B2DeadlineNs=22998000000,B2InstallationIdentity=identity)
    delegated='/sys/fs/cgroup/review'
    subdir='/va-'+doc['Execution'].replace('-','')
    controller=dict(Pid=2804,Uid=2052,Gid=2070,PidNamespace='pid:[4026532456]',UserNamespace='user:[4026532457]')
    runtime=dict(HostPath='/usr/lib/dotnet/dotnet',HostSha256=digest('host'),
                 RuntimeDirectory='/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.21',
                 RuntimeInventorySha256=digest('runtime').upper(),HostFxrDirectory='/usr/lib/dotnet/host/fxr/10.0.21',
                 HostFxrInventorySha256=digest('fxr').upper(),RuntimeVersion='10.0.21')
    profile=dict(OwnerDllPath=doc['Owner']['Directory']+'/'+roles['Owner'][0],DelegatedParentPath=delegated,
                 SourceRootPath='/opt/gagamba/validation-fixtures/'+digest('fixture manifest'),
                 StagingParentPath='/srv/review/staging'+subdir,JournalDirectory='/srv/review/journal'+subdir,
                 OwnerDllSha256=doc['Owner']['Sha256'][roles['Owner'][0]],ToolSha256=digest('tool'),
                 InputSha256=digest('input'),CoordinatorSha256=digest('coordinator'),ToolSourceName='tool',
                 InputSourceName='input',CoordinatorSourceName='coordinator',FilterSourceName='filter',
                 ToolEnrollmentId='Q1-tool.7',InputEnrollmentId='Q1-input.8',TotalBudgetBytes=16777216,
                 MaxObjects=4,ExpiresLeadNanoseconds=2000000000,IntegratedRecovery=None)
    return doc | dict(DelegatedParent=delegated,Integrated=dict(Profile=profile,Issuer=ident(74),StoreGeneration=ident(75),
                      AcceptedAtNs=20400000000,ExpiresAtNs=22998000000,Controller=controller,Runtime=runtime,InstallationIdentity=identity))

def projection(doc,topology):
    fields = ('Execution','Generation','Session','ControllerPid','ControllerUid','Secret','Recovery','Owner','EvidenceDirectory')
    fields += ('B2InstallationIdentity','B2DeadlineNs') if topology=='B2' else ('DelegatedParent','Integrated')
    return {key:doc[key] for key in fields}

originals = re.findall(rb'\x60\x60\x60json\r?\n([^\r\n]+)\r?\n\x60\x60\x60',raw(ANNEX+'witnesses.md',OWNER))
assert len(originals)==2
b2, integrated = (fixture(kind) for kind in ('B2','Integrated'))
parents={'B':('B2',b2),'I':('Integrated',integrated)}
complete = []
bootstrap_table = ['| ID | Category / source | Parent | Exact mutation or construction | Bytes | Expected | Observed invariant / result |',
                   '|---|---|---|---|---:|---|---|']
totals=Counter()

def bootstrap(ident, category, parent, desc, payload, expect, invariant='', data=None, digest=None, use_base=True):
    topology,base=parents[parent]
    if use_base:
        data=projection(base,topology)
        digest=bytes.fromhex(base['AuthorityHash'])
    try:
        observed=structural(payload,topology,data,digest)
    except UnicodeDecodeError:
        observed='invalid UTF-8'
    except json.JSONDecodeError:
        observed='JSON syntax'
    except ValueError as e:
        observed=str(e)
    assert (observed=='pass')==expect, (ident,observed)
    assert not invariant or observed.startswith(invariant),(ident,invariant,observed)
    bootstrap_table.append(f'| {ident} | {category} | {parent} | {desc} | {len(payload)} | {"Pass" if expect else "Refuse"} | {observed} |')
    totals[category]+=1

for parent, raw_witness, declared_size, declared_hash in zip(('B','I'), originals,(2693,4519),(
    '52626efe08bac30435ffde51036413380a31cf17bdbfff8f2fc01841455ddfbf',
    '46ed4d54b119baacb4cb737dc70d1924cfca0a3241298c524081b6f691de069c')):
    assert (len(raw_witness),sha(raw_witness))==(declared_size,declared_hash)
    topology,base=parents[parent]
    doc=json.loads(raw_witness)
    bootstrap('L-'+parent,'Owner literals',parent,'Committed literal, using documented synthetic expectations',
              raw_witness,True,data=projection(doc,topology),digest=hashlib.sha256(b'abc').digest(),use_base=False)
    complete.append((f'Owner {parent}',len(raw_witness),sha(raw_witness)))
    payload=encode(base)
    bootstrap('P-'+parent,'Independent positive',parent,'Reviewer fixture, sorted members',payload,True)
    complete.append((f'Reviewer {parent}',len(payload),sha(payload)))
lines('complete', '| Witness | UTF-8 bytes | Raw SHA-256 |\n|---|---:|---|\n'+'\n'.join(
    f'| {name} | {size} | \x60{digest}\x60 |' for name,size,digest in complete)+'\n')
owner_i=json.loads(originals[1])
owner_limit=originals[0].ljust(8192,b' ')
owner_ws=b'\t'+json.dumps(dict(reversed(list(owner_i.items()))),separators=(',',':')).encode()+b'\n'
for parent, payload, digest_text in (
    ('B',owner_limit,'70de1b51f0e1efc9cab3e2e83bf9e0e04f9a54e37bcb749d6d0e7c88571f4c27'),
    ('I',owner_ws,'d474bb76f6ee9362cd69ed60426965dd6c31c40fdc5c2b0e28b8459ef61a987d')):
    assert sha(payload)==digest_text
    t=parents[parent][0]
    bootstrap('L-'+parent+'-2','Owner literals',parent,'Documented exact owner limit or whitespace transform',payload,True,
              data=projection(json.loads(payload),t),digest=hashlib.sha256(b'abc').digest(),use_base=False)

def mutation(ident,category,parent,path,value,invariant,delete=False):
    doc=copy.deepcopy(parents[parent][1])
    container=doc
    for key in path[:-1]:
        container=container[key]
    if delete:
        del container[path[-1]]
    else:
        container[path[-1]]=value
    description=' / '.join(path)+(' omitted' if delete else ' = '+json.dumps(value,separators=(',',':')))
    if path[-1]=='B2InstallationIdentity' and value is not None:
        description='Insert reviewer B2 identity object at integrated root'
    bootstrap(ident,category,parent,description,encode(doc),False,invariant)

for index,key in enumerate(('B2InstallationIdentity','B2DeadlineNs'),1):
    mutation(f'M{index:02}','BB-01','B',[key],None,'missing',True)
    mutation(f'M{index+2:02}','BB-01','B',[key],None,'object' if index==1 else 'I64+')
    mutation(f'M{index+4:02}','BB-01','I',[key],None,'extra')
    mutation(f'M{index+6:02}','BB-01','I',[key],b2[key],'extra')

for index,(parent,key,value,label,delete) in enumerate([
    ('B','Qualified',None,'missing',True), ('I','Qualified',True,'False',False),
    ('B','Qualified','false','False',False), ('I','Qualified',0,'False',False),
    ('B','qualificationId',None,'missing',True), ('I','qualificationRecordSha256',None,'missing',True),
    ('B','qualificationRecordSha256','','Null',False), ('I','qualificationId',{},'Null',False),
    ('B','qualificationId','accepted-record-84','Null',False),
    ('I','qualificationRecordSha256',sha(b'synthetic record'),'Null',False),
    ('I','qualificationId','', 'Null',False), ('B','qualificationRecordSha256',{},'Null',False)
],9):
    path=['B2InstallationIdentity'] if parent=='B' else ['Integrated','InstallationIdentity']
    mutation(f'M{index:02}','BJ-01/02',parent,path+[key],value,label,delete)

mutation('M21','BB §4','I',['Integrated','Profile','IntegratedRecovery'],{},'Null')
mutation('M22','BB §4','I',['Integrated','Runtime'],[],'object')
mutation('M23','BB §4','B',['DelegatedParent'],'/test','Null')
mutation('M24','BB §4','I',['Integrated','Controller','Gid'],None,'missing',True)

B,I=map(encode,(b2,integrated))
for index,(parent, desc, payload, expected, invariant) in enumerate([
    ('B','Add UTF-8 BOM',b'\xef\xbb\xbf'+B,False,'JSON syntax'),
    ('I','Insert invalid UTF-8 C0 AF in evidence path',I.replace(b'/srv/review/evidence/q1',b'/srv/\xc0\xaf/q1'),False,'invalid UTF-8'),
    ('B',r'Insert root duplicate as \\u0056ersion',br'{"\u0056ersion":3,'+B[1:],False,'duplicate'),
    ('I',r'Insert nested duplicate as P\\u0069d',I.replace(b'"Controller":{',br'"Controller":{"P\u0069d":2804,'),False,'duplicate'),
    ('I','Append trailing comma',I[:-1]+b',}',False,'JSON syntax'),
    ('B','Append second object',B+b' {}',False,'JSON syntax'),
    ('B',r'Replace final path component with \\uD800',B.replace(b'/q1"',br'/\uD800"'),False,'surrogate'),
    ('I','Prepend JSON comment',b'/*q*/'+I,False,'JSON syntax'),
    ('B',r'Replace final path component with \\q',B.replace(b'/q1"',br'/\q"'),False,'JSON syntax'),
    ('B','Prefix non-JSON Unicode NBSP',b'\xc2\xa0'+B,False,'JSON syntax'),
    ('I','Add ASCII tab/CR/linefeed, reverse root order',b'\t'+json.dumps(dict(reversed(list(integrated.items()))),separators=(',',':')).encode()+b'\r\n',True,''),
    ('I',r'Spell key as \\u0041uthorityHash, value unchanged',I.replace(b'"AuthorityHash"',br'"\u0041uthorityHash"'),True,''),
    ('B',r'Spell evidence path slash as \\/',B.replace(b'/srv/review/evidence/q1',br'\/srv/review/evidence/q1'),True,''),
    ('B','Replace evidence ASCII q with U+00E9',B.replace(b'/q1"',b'/\xc3\xa91"'),False,'Path'),
],1):
    bootstrap(f'J{index:02}','BB §4 JSON',parent,desc,payload,expected,invariant)
escaped_key=I.replace(b'"AuthorityHash"',br'"\u0041uthorityHash"')
assert re.search(rb'"AuthorityHash"\s*:\s*"([^"]*)"',escaped_key) is None
mutation('J15','BB §4 JSON','B',['Extra'],False,'extra')
mutation('J16','BB §4 JSON','I',['Integrated','Runtime','Extra'],False,'extra')
mutation('J17','BB §4 JSON','I',['Integrated','Profile','QualificationFault'],None,'extra')
mutation('J18','BB §4 JSON','I',['Integrated','Profile','QualifiedInstallation'],None,'extra')

h=b2['AuthorityHash']
for index,value in enumerate([h.upper(),h[:8]+h[8:].upper(),'g'+h[1:],h[:63],h+'a',' '+h,h+' ','0x'+h,'',None,
                             base64.b64encode(bytes.fromhex(h)).decode(),[1,2]],1):
    mutation(f'H{index:02}','BJ-03','B',['AuthorityHash'],value,'CapabilityHex')
mutation('H13','BJ-03','B',['AuthorityHash'],None,'missing',True)
bootstrap('H14','BJ-03','B','Escape first hex digit as its JSON Unicode escape',B.replace(
    h.encode(),fr'\u{ord(h[0]):04x}'.encode()+h[1:].encode()),False,'AuthorityHash literal token')
bproj=projection(b2,'B2')
digest=bytes.fromhex(h)
for index,(desc,expectation,ok,invariant) in enumerate([
    ('Digest of separate reviewer synthetic capability label',hashlib.sha256(b'Q1 synthetic B2 capability').digest(),True,''),
    ('Flip high bit of expected last octet',digest[:-1]+bytes([digest[-1]^128]),False,'expected digest equality'),
    ('Expected digest omitted',None,False,'expected digest width'),
    ('Keep first 31 expected octets',digest[:-1],False,'expected digest width'),
    ('Append an expected octet',digest+b'\x00',False,'expected digest width'),
    ('Matching bytes supplied as caller-controlled arbitrary bytes',bytes.fromhex(h),True,''),
    ('Different synthetic capability within unchanged projection/catalog context',hashlib.sha256(b'Q1 other capability same catalog').digest(),False,'expected digest equality'),
],1):
    bootstrap(f'C{index:02}','BJ-04','B',desc,B,ok,invariant,data=bproj,digest=expectation,use_base=False)

for size in (8191,8192,8193):
    payload = b'\t' + I + b'\r\n' + b' '*(size-len(I)-3)
    bootstrap(f'S{size}','BB §4 size','I',f'Ordinary JSON whitespace to {size} bytes',payload,size<=8192,'size' if size>8192 else '')
for size in (8192,8193):
    body=B.replace(b'/q1"',b'/\xc3\xa91"')
    payload=body.ljust(size,b' ')
    assert len(payload.decode())==size-1
    bootstrap(f'U{size}','BB §4 size','B',f'U+00E9 in path, padded to {size} bytes and {size-1} code points',payload,False,'size' if size>8192 else 'Path')
bootstrap('S00','BB §4 size','B','Empty payload',b'',False,'size')
for index,value in enumerate((1,2,4,'3',True),1):
    mutation(f'V{index:02}','BB-03 version','I',['Version'],value,'V3')
for index,token in enumerate((b'3.0',b'3e0',b'03'),6):
    bootstrap(f'V{index:02}','BB-03 version','B',f'Version token {token.decode()}',B.replace(b'"Version":3',b'"Version":'+token),False)
mutation('V09','BB-03 version','I',['AuthorityMode'],'validation-post','PreMode')
bootstrap('V10','BB-03 version','I','Route B2 bytes to integrated selection',B,False,'extra')
bootstrap('V11','BB §4','B','Original projection unavailable',B,False,'original data missing',
          data=None,digest=digest,use_base=False)
for index,(parent,path,value,invariant) in enumerate([
    ('B',['B2InstallationIdentity','Generation'],sha(b'other generation'),'installation generation'),
    ('I',['Integrated','Controller','Uid'],2071,'controller equality'),
    ('I',['Integrated','ExpiresAtNs'],20400000000,'integrated interval'),
    ('I',['Integrated','Runtime','RuntimeInventorySha256'],'AA'*32,'runtime inventory bytes'),
    ('I',['Integrated','Profile','OwnerDllSha256'],'ab'*32,'owner DLL digest'),
    ('I',['Integrated','Profile','JournalDirectory'],'/srv/review/journal/va-2','execution directory'),
    ('I',['EvidenceDirectory'],'/srv/review/evidence/other','original projection'),
    ('I',['Integrated','Profile','CoordinatorSha256'],'ab'*32,'coordinator equality'),
    ('I',['Integrated','Runtime','HostFxrDirectory'],'/usr/lib/dotnet/host/fxr/10.0.22','runtime paths'),
    ('B',['Recovery','Directory'],'/opt/unrelated/recovery','role directory'),
    ('I',['Integrated','Profile','DelegatedParentPath'],'/sys/fs/cgroup/other','delegated equality'),
],1):
    mutation(f'X{index:02}','BB §4 relationships',parent,path,value,invariant)
for index,(parent,path,value,invariant) in enumerate([
    ('B',['B2DeadlineNs'],2**63,'I64+'),
    ('B',['B2DeadlineNs'],0,'I64+'),
    ('I',['ControllerUid'],True,'Principal'),
    ('I',['Integrated','Controller','PidNamespace'],'pid:[18446744073709551616]','PidNs'),
    ('I',['Integrated','Runtime','RuntimeVersion'],'10.0.021','RuntimeVersion'),
    ('I',['Integrated','Profile','ToolEnrollmentId'],'.Q1-tool','ValidId'),
    ('B',['Secret'],b2['Secret'][:-2]+'9=','Secret32'),
    ('I',['Integrated','Runtime','RuntimeInventorySha256'],integrated['Integrated']['Runtime']['RuntimeInventorySha256'].lower(),'UpperSha'),
    ('B',['B2InstallationIdentity','RecoveryClosureSha256'],'0'*64,'Sha'),
    ('B',['Session'],'00000000-0000-0000-0000-000000000000','Guid'),
    ('I',['EvidenceDirectory'],'/srv/review/../evidence','Path'),
    ('I',['Integrated','Profile','TotalBudgetBytes'],16777215,'Budget'),
    ('I',['Integrated','Profile','ExpiresLeadNanoseconds'],120000000001,'Lead'),
],1):
    mutation(f'D{index:02}','BB §4 scalar table',parent,path,value,invariant)
lines('bootstrap-table', '\n'.join(bootstrap_table)+'\n')
print('bootstrap outcomes',Counter('pass' if '| Pass |' in row else 'refuse' for row in bootstrap_table[2:]))
print('bootstrap',dict(totals))

tail_id=profile_id.encode('utf-8')
tail_version=profile_version.encode('utf-8')
tail=bytes([len(tail_id)])+tail_id+bytes([len(tail_version)])+tail_version
assert len(tail_id)==26 and len(tail_version)==1 and len(tail)==29
a,o,r,c,s,t=[hashlib.sha256(('Q1 binary '+label).encode()).digest() for label in
           ('artifact','owner','recovery','coordinator','seccomp','runtime')]
session=UUID('7980ab12-3423-87ba-9ce8-123456def987').bytes_le

def echo(artefact,closure,role,status=0,extension=b''):
    return struct.pack('<32s32sHHHBB4s',artefact,closure,1,1,1,role,status,b'\0'*4)+extension

def expectation(guid,hashes,status):
    return struct.pack('<16s32s32s32s32s32s32sHHHHB',guid,*hashes,1,1,1,1,status)+tail

def response(artefact, reason=0):
    return struct.pack('<BBH32s',int(reason==0),reason,0,artefact)

def confirmation(guid,artefact):
    return struct.pack('<16s32s',guid,artefact)

def slice_at(data,offset,value):
    return data[:offset]+value+data[offset+len(value):]

def echo_verdict(data,topology,role,context=3,profile=(tail_id,tail_version)):
    if context not in (2,3):
        return 'context'
    if len(data)<76 or len(data)>256 or (topology=='B2' and len(data)!=76):
        return 'length'
    if data[0:32]!=a or data[32:64]!=(r if role==0 else o):
        return 'digest'
    if data[64:70]!=b'\x01\x00'*3:
        return 'protocol'
    if data[70]!=role:
        return 'role'
    if data[71]!=(0 if context==3 else 1):
        return 'status'
    if data[72:76]!=b'\0'*4:
        return 'reserved'
    if profile!=(tail_id,tail_version):
        return 'expected profile'
    if len(data)==76:
        return 'pass'
    count=data[76]
    if not 1<=count<=64 or 77+count>=len(data):
        return 'prefix 1'
    version_index=77+count
    width=data[version_index]
    if not 1<=width<=64 or version_index+1+width!=len(data):
        return 'prefix 2 or tail length'
    if (data[77:version_index],data[version_index+1:])!=profile:
        return 'profile bytes'
    return 'pass'

def expectation_verdict(data,topology,context,expected_profile=tail):
    if topology!='B2' or context!=3:
        return 'context'
    if len(data)!=219+len(tail_id)+len(tail_version):
        return 'length'
    if data[:16]!=session:
        return 'session'
    if data[16:208]!=b''.join((a,o,r,c,s,t)):
        return 'digest'
    if data[208:216]!=b'\x01\x00'*4:
        return 'protocol'
    if data[216]!=0:
        return 'status'
    return 'pass' if data[217:]==expected_profile else 'profile bytes'

def conf_verdict(data,msg,topology,context,prior):
    if context!=3 or (msg==12 and topology!='B2'):
        return 'context'
    if not prior:
        return 'sequence'
    if len(data)!=(36 if msg==10 else 48):
        return 'length'
    if msg==12:
        if data[:16]!=session:
            return 'session'
        return 'pass' if data[16:]==a else 'digest'
    if data[2:4]!=b'\0\0':
        return 'reserved'
    ok,reason=data[:2]
    if (ok,reason)!=(1,0) and not (ok==0 and 1<=reason<=5):
        return 'pair'
    if data[4:]!=a:
        return 'digest'
    return 'pass' if ok else 'well-formed refusal'

g=json.loads(raw('docs/evidence/rc2b-06-b6-impl-01-s3/golden-wire-vectors.json'))
gold={entry['id']:bytes.fromhex(entry['payloadHex']) for entry in g['vectors']}
p={int(k,16):bytes([int(k,16)])*32 for k in ('11','22','33','44','55','66')}
ga,go,gr,gc,gs,gt=p.values()
gsession=UUID('ffeeddcc-bbaa-9988-7766-554433221100').bytes_le
assert gold[9]==echo(ga,gr,0,1) and gold[36]==echo(ga,go,1,1)
assert gold[37]==expectation(gsession,(ga,go,gr,gc,gs,gt),1)
assert gold[10]==response(ga) and gold[12]==confirmation(gsession,ga)
prep= {label:bytes.fromhex(hexes) for label,hexes in re.findall(
       r'^(W\d\d|IE-V\d\d|R0[12]|Preimage) ([0-9a-f]+)$',txt(binary),re.M)}
normative = {label:bytes.fromhex(hexes) for label,hexes in re.findall(
       r'^(IE-V\d\d) ([0-9a-f ]+)$',txt(D+'normative-owner-01.md'),re.M)}
assert len(normative)==12 and all(prep[key]==value for key,value in normative.items())
rebuilt = {
    'W01':echo(ga,gr,0),'W02':echo(ga,go,1),'W03':expectation(gsession,(ga,go,gr,gc,gs,gt),0),
    'W04':response(ga),'W05':confirmation(gsession,ga),'IE-V01':echo(ga,gr,0),
    'IE-V02':echo(ga,go,1,extension=tail),'IE-V03':echo(ga,gr,0,extension=tail)}
assert all(prep[key]==data for key,data in rebuilt.items())
for key,msg,offset in (('W01',9,71),('W02',36,71),('W03',37,216)):
    assert [i for i,(old,new) in enumerate(zip(gold[msg],rebuilt[key])) if old!=new]==[offset]
    assert (gold[msg][offset],rebuilt[key][offset])==(1,0)
assert len(rebuilt['W03'])==246

binary_table=['| ID | Source | Parent | Bytes | Change / construction | Expected | Observed |',
              '|---|---|---|---:|---|---|---|']
binary_totals=Counter()
def result(ident,source,parent,data,desc,expected,actual):
    assert expected==actual,(ident,actual)
    binary_table.append(f'| {ident} | {source} | {parent} | {len(data)} | {desc} | {expected} | {actual} |')
    binary_totals[source]+=1

b9=echo(a,r,0)
b36=echo(a,o,1)
i36=echo(a,o,1,extension=tail)
i9=echo(a,r,0,extension=tail)
echo_parents={'E01':('B2',0,b9),'E02':('B2',1,b36),
              'E03':('Integrated',0,b9),'E04':('Integrated',1,i36),'E05':('Integrated',0,i9),
              'E06':('Integrated',1,b36)}
for ident,(topology,role,data) in echo_parents.items():
    result(ident,'B2 §3 / IE-01–03',topology,data,f'Build role {role} from reviewer hash labels','pass',echo_verdict(data,topology,role))
for number,(parent, desc, data, exp, context, expected_pair) in enumerate([
    ('E04','Replace whole tail with zero prefix only',i36[:76]+b'\0','prefix 1',3,(tail_id,tail_version)),
    ('E05','Keep complete ID and omit second prefix',i9[:103],'prefix 1',3,(tail_id,tail_version)),
    ('E04','First prefix 65',slice_at(i36,76,b'A'),'prefix 1',3,(tail_id,tail_version)),
    ('E05','Second prefix 3',slice_at(i9,103,b'\3'),'prefix 2 or tail length',3,(tail_id,tail_version)),
    ('E04','Change ID initial a to A',slice_at(i36,77,b'A'),'profile bytes',3,(tail_id,tail_version)),
    ('E05','Change ASCII version 1 to 0',slice_at(i9,104,b'0'),'profile bytes',3,(tail_id,tail_version)),
    ('E04','Delete final version byte',i36[:-1],'prefix 2 or tail length',3,(tail_id,tail_version)),
    ('E05','Append extra tail byte 7f',i9+b'\x7f','prefix 2 or tail length',3,(tail_id,tail_version)),
    ('E03','Truncate within reserved bytes',b9[:74],'length',3,(tail_id,tail_version)),
    ('E01','Append otherwise valid integrated tail',i9,'length',3,(tail_id,tail_version)),
    ('E04','Route unchanged pre status 0 to v2 selection',i36,'status',2,(tail_id,tail_version)),
    ('E03','Change pre status to legacy 1',slice_at(b9,71,b'\1'),'status',3,(tail_id,tail_version)),
    ('E02','Replace pre status with 255',slice_at(b36,71,b'\xff'),'status',3,(tail_id,tail_version)),
    ('E01','Set final reserved byte to 3',slice_at(b9,75,b'\3'),'reserved',3,(tail_id,tail_version)),
    ('E04','Put recovery role in owner message',slice_at(i36,70,b'\0'),'role',3,(tail_id,tail_version)),
    ('E03','Substitute valid another artifact digest',slice_at(b9,0,o),'digest',3,(tail_id,tail_version)),
    ('E04','Substitute wrong closure in tail-present form',slice_at(i36,32,r),'digest',3,(tail_id,tail_version)),
    ('E02','Encode owner protocol big-endian',slice_at(b36,64,b'\0\1'),'protocol',3,(tail_id,tail_version)),
    ('E03','Retain base but supply different expected profile',b9,'expected profile',3,(tail_id,b'2')),
    ('E06','Select unsupported legacy v1 evidence path',b36,'context',1,(tail_id,tail_version)),
    ('E04','Append second complete pair',i36+tail,'prefix 2 or tail length',3,(tail_id,tail_version)),
],7):
    topology,role,_=echo_parents[parent]
    result(f'E{number:02}','B2 §3 / IE-01–03',parent,data,desc,exp,echo_verdict(data,topology,role,context,expected_pair))

w37=expectation(session,(a,o,r,c,s,t),0)
for n,(label,data,topo,expected) in enumerate([
    ('Independent complete 37',w37,'B2','pass'),('Legacy 1 at 216',slice_at(w37,216,b'\1'),'B2','status'),
    ('Use integrated selection',w37,'Integrated','context'),('Change first session byte',slice_at(w37,0,b'\xff'),'B2','session'),
    ('Change profile version',slice_at(w37,245,b'3'),'B2','profile bytes')],1):
    result(f'F{n:02}','B2 37 / F1','F01',data,label,expected,expectation_verdict(data,topo,3))
msg10=response(a)
msg12=confirmation(session,a)
for n,(msg,topology,label,data,ctx,prior,expected) in enumerate([
    (10,'B2','Valid response after matching 9',msg10,3,True,'pass'),
    (10,'Integrated','Valid response after matching 9',msg10,3,True,'pass'),
    (12,'B2','Valid confirmation after verified 36',msg12,3,True,'pass'),
    (10,'Integrated','Other artifact digest',response(t),3,True,'digest'),
    (10,'B2','Drop final digest octet',msg10[:-1],3,True,'length'),
    (10,'B2','Valid negative with legacy reason 5',response(a,5),3,True,'well-formed refusal'),
    (10,'B2','Success with reason 3',slice_at(msg10,1,b'\3'),3,True,'pair'),
    (10,'B2','Select unsupported v1 evidence context',msg10,1,True,'context'),
    (12,'B2','Other artifact digest',confirmation(session,o),3,True,'digest'),
    (12,'B2','Prefix session octet wrong',slice_at(msg12,0,b'\xff'),3,True,'session'),
    (12,'B2','Drop final digest octet',msg12[:-1],3,True,'length'),
    (12,'Integrated','Route B2 12 to integrated context',msg12,3,True,'context'),
    (12,'B2','No preceding verified 36',msg12,3,False,'sequence'),
    (10,'B2','No preceding matching 9',msg10,3,False,'sequence'),
],1):
    result(f'K{n:02}','SPEC §6.2.4 / B2 §3 / F1',f'{topology} {msg}',data,label,
           expected,conf_verdict(data,msg,topology,ctx,prior))
lines('binary-table','\n'.join(binary_table)+'\n')
lines('binary-positive','\n'.join(f'{name} {data.hex()}' for name,data in
    [('E01',b9),('E02',b36),('E03',b9),('E04',i36),('E05',i9),('E06',b36),('F01',w37),('K01/K02',msg10),('K03',msg12)])+'\n')
print('binary',dict(binary_totals),'tail',tail.hex())
print('binary outcomes',Counter(row.rsplit(' | ',2)[-2] for row in binary_table[2:]))

formula=lambda src: next(line for line in src.splitlines() if line.startswith('\x60SHA256(ASCII("gagamba-validation-release-v1")'))
assert formula(sources['F2'])==formula(sources['AB'])
domain=b'gagamba-validation-release-v1'
prefix=domain+b'\0'
rr_digests = [hashlib.sha256(('Q1 release '+label).encode()).digest() for label in
              ('capability','catalog','script','action','preparation manifest')]
rr_ids = [UUID(f'a1b2c3d{i}-e5f6-4718-a92a-3204567890ab') for i in range(1,5)]
def preimage(hashes,guids,action=2, order='bytes_le'):
    return prefix+b''.join(hashes[:4])+struct.pack('<I',action)+b''.join(getattr(value,order) for value in guids)+hashes[4]
original=prefix+b''.join(bytes([i])*32 for i in (0x11,0x22,0x33,0x44))+struct.pack('<I',2)+b''.join(
    bytes([i])*16 for i in (0x55,0x66,0x77,0x88))+bytes([0x99])*32
assert original==prep['Preimage'] and hashlib.sha256(original).digest()==prep['R01']
assert len(original)==258 and prep['R01']!=prep['R02']
reviewer=preimage(rr_digests,rr_ids)
expected=hashlib.sha256(reviewer).digest()
assert len(reviewer)==258 and len(expected)==32 and len(prefix)==30
assert rr_ids[0].bytes_le.hex()=='d1c3b2a1f6e51847a92a3204567890ab'
release_table=['| ID | Mutation / context | Expected | Observed |','|---|---|---|---|']
for ident,label,claim,approval,want in [
    ('R01','Exact reviewer digest, flag true',expected,True,'field agreement'),
    ('R02','GUIDs use network byte order',hashlib.sha256(preimage(rr_digests,rr_ids,order='bytes')).digest(),True,'refuse'),
    ('R03','Action index 1 instead of 2',hashlib.sha256(preimage(rr_digests,rr_ids,action=1)).digest(),True,'refuse'),
    ('R04','Digest matches but approval flag is false',expected,False,'refuse'),
    ('R05','Use the capability SHA-256 itself',rr_digests[0],True,'refuse'),
    ('R06','Omit correlation',None,True,'refuse'),
]:
    got='field agreement' if approval and claim==expected else 'refuse'
    assert got==want
    release_table.append(f'| {ident} | {label} | {want} | {got} |')
lines('release-table', '\n'.join(release_table)+f'\n\nReviewer preimage (258 bytes) and digest (32 bytes)\n\n~~~text\n{reviewer.hex()}\n{expected.hex()}\n~~~\n')
print('release agreement, 5 refusals,',expected.hex())
~~~
