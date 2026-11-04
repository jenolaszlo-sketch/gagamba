"""Author a planning manifest from observed inputs; no qualification approval."""
import json, hashlib
from pathlib import Path
root = Path(__file__).parent
platform = root / 'platform'
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
packages = read(platform/'selected-package-records.json')
chosen = {}
for package in packages:  # signed updates precede base; explicit candidate choice, not apt solving
    chosen.setdefault(package['Package'], package)
for name, package in chosen.items():
    local = platform / Path(package['Filename']).name
    package['verificationLevel'] = 'VERIFIED-BYTES' if local.exists() else 'VERIFIED-METADATA'
    package['downloaded'] = local.exists()
    package['installed'] = False
    package['sourceIndex'] = package['suite']+'-main-Packages.xz'
    package['officialUrl'] = 'https://archive.ubuntu.com/ubuntu/'+package['Filename']
    if local.exists(): assert digest(local) == package['SHA256']
observations = read(platform/'runtime-byte-observations.json')
bwrap = read(platform/'bubblewrap-byte-observations.json')[0]
probes = read(root/'kernel-probes-draft.json')
assert 'VALIDSIG 843938DF228D22F7B3742BC0D94AA3F0EFE21092' in (platform/'image-signature-verification.log').read_text(encoding='utf-8-sig')
for log in ('archive-signature-verification.log','base-signature-verification.log'):
    assert 'VALIDSIG F6ECB3762474EDA9D21B7022871920D1991BC93C' in (platform/log).read_text(encoding='utf-8-sig')
out = {
 'schemaVersion': 1, 'commission':'GAGAMBA-RC2B-06-B6-NATIVE-INPUT-FREEZE-01',
 'date':'2026-11-04 Asia/Manila','disposition':'INPUT FREEZE READY WITH QUALIFICATIONS',
 'authorization':{'provisioning':False,'privilegedExecution':False,'candidateBuild':False,
                  'driverImplementation':False,'qualificationPublication':False,'activation':False,'push':False},
 'source':read(root/'production-source-binding.json'),
 'planningOwnerDecision':{'state':'PENDING','recommendedDisposition':'ACCEPTED WITH QUALIFICATIONS — PLANNING MILESTONE ONLY','approvalReference':None},
 'image':{'distribution':'Ubuntu','release':'26.04.1 LTS','codename':'resolute','architecture':'amd64',
  'publisher':'Canonical','filename':'ubuntu-26.04.1-wsl-amd64.wsl','format':'WSL tar-based .wsl',
  'url':'https://releases.ubuntu.com/26.04.1/ubuntu-26.04.1-wsl-amd64.wsl',
  'publisherListingTimestamp':'2026-08-27 14:17 (server listing; not an independently established build ID)',
  'buildId':None,'expectedSha256':'48d56724b5c8e60f24893e83e73bbb58c60b3ca22fba3da977075420acd54104',
  'actualSha256':None,'downloaded':False,'verificationLevel':'VERIFIED-METADATA',
  'checksumUrl':'https://releases.ubuntu.com/26.04.1/SHA256SUMS',
  'signatureUrl':'https://releases.ubuntu.com/26.04.1/SHA256SUMS.gpg',
  'checksumFileSha256':digest(platform/'image-SHA256SUMS'),
  'signatureFileSha256':digest(platform/'image-SHA256SUMS.gpg'),
  'signatureValid':True,'signingFingerprint':'843938DF228D22F7B3742BC0D94AA3F0EFE21092',
  'keyProvenance':'Official Ubuntu HTTPS keyserver, full fingerprint compared with official verification tutorial; no user keyring trust modification',
  'imagePackageList':{'file':'platform/ubuntu-26.04.1-wsl-amd64.manifest','sha256':digest(platform/'ubuntu-26.04.1-wsl-amd64.manifest'),
    'status':'VERIFIED-METADATA','signedBindingToImageEstablished':False},
  'futureVerification':['explicit owner-authorized image acquisition','hash full image against frozen expectedSha256',
      'inspect archive without executing; validate WSL metadata, paths, root entry and ownership/modes',
      'authorize exact named import only after bytes match','inspect guest ext4 root/ancestry before installation'],
  'storage':{'windowsVhdxDirectory':'D:\\GagambaQualification\\B6-Native-01','guestInstallRoot':'/opt/gagamba',
       'expectedFilesystem':'ext4 inside VHDX','actualFilesystem':None,'forbiddenTrustedPath':'/mnt/d',
       'readiness':'PROBE-REQUIRED'},'updates':'No latest/current alias or replacement digest; any changed bytes require re-review and owner authorization'},
 'signedRepositoryMetadata':read(platform/'metadata-chain.json'),
 'packageCandidates':list(chosen.values()),'packageTransaction':{'state':'UNRESOLVED','lock':None,
   'reason':'Candidate exact metadata is frozen, but complete dependency/alternative transaction and all payload bytes are not validated. No apt install recipe is authorized.'},
 'bubblewrap':{'verificationLevel':'VERIFIED-BYTES','readiness':'VERIFIED','observation':bwrap,
   'remaining':['installed root ancestry/metadata/capability xattr','recursive actual linked-library closure','non-root namespace/seccomp behavior']},
 'runtimeByteObservations':observations,
 'runtime':{'rid':'linux-x64','sdk':'10.0.114','framework':'Microsoft.NETCore.App','version':'10.0.14',
    'hostPath':'/usr/lib/dotnet/dotnet','fxrDirectory':'/usr/lib/dotnet/host/fxr/10.0.14',
    'runtimeDirectory':'/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.14','rollForward':'Disable',
    'actualRuntimeInventorySha256':None,'actualHostfxrInventorySha256':None,
    'globalization':'Current default non-invariant; ICU78 package required; alternative invariant mode requires HD-5 proof/decision',
    'dynamicDependencyCoverage':['ELF interpreter','glibc','libstdc++','libgcc_s','libm','ICU','OpenSSL','zlib',
       'libbrotli','liblttng-ust','libunwind','all other DT_NEEDED/dlopen inputs actually used'],
    'loadedClosureReadiness':'PROBE-REQUIRED'},
 'toolchain':{'gccRequiredVersion':'15.2.0','gccPackageCandidate':chosen.get('gcc-15'),
    'binutilsPackageCandidate':chosen.get('binutils'),'staticLibcPackageCandidate':chosen.get('libc6-dev'),
    'gccSelection':'Builder calls gcc; explicitly reviewed isolated build PATH must resolve to gcc-15, not distro default; do not mutate global alternatives.',
    'compilerBinarySha256':None,'linkerBinarySha256':None,'staticArchivesSha256':None,
    'nativeBuildReproducibility':'UNRESOLVED until authorized build equals hardcoded coordinator hash; version matching alone insufficient'},
 'kernelProbeSpecification':probes,
 'delegation':{'state':'PROBE-REQUIRED','environment':'Gagamba-B6-Native-01',
    'service':'gagamba-b6-native.service','actualControlGroup':None,'runtimeUid':None,'runtimeGid':None,
    'supervisor':'P/supervisor','delegatedDomainParent':'P/domains','controllers':['memory','pids'],
    'placement':'Supported DelegateSubgroup=supervisor (systemd254+) or separately reviewed operator mechanism; empty interior nodes',
    'targetLimits':{'memory.max':'134217728','memory.swap.max':'0','pids.max':'48'},
    'outerLimits':None,'authority':'Operator pre-enables controllers; runtime creates/removes owned execution children, reads identity/events/procs, writes child limits and cgroup.kill; kernel requires target/common-ancestor cgroup.procs write permission. No unrelated host cgroup authority or added threads/subtree-control grant.'},
 'candidate':{'sourceCommit':'af4300f5dff7e4e49fca98233911a44a15894a43','sourceTree':'2d0ece92e20247d3d61ac84e9215837a713e9921',
   'configuration':'Release','buildConfigurationIdentity':'Production','rid':'linux-x64','artifactCount':14,
   'artifactSetSha256':None,'candidateArchiveSha256':None,'qualificationId':None,'qualificationRecordSha256':None,
   'coordinatorSha256':'5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b',
   'filterSha256':'2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108','filterBytes':672,
   'digestDerivation':'SHA256 of actual canonical manifest bytes binding reviewed actual artifact bytes, lengths, metadata and platform/source inputs',
   'builder':'eng/linux-m1/build-install-candidate.sh <canonical-main-real-git-directory> <empty-output-directory>',
   'readiness':'PROBE-REQUIRED'},
 'publication':{'ownerAcceptance':None,'administrativeApproval':None,'record':None,'published':False,
   'adminStore':'/etc/gagamba/accepted-qualifications','runtimeStore':'/opt/gagamba/qualifications',
   'readiness':'OWNER-DECISION-REQUIRED','stagedPublicationProtocol':None},
 'driver':{'commission':'GAGAMBA-RC2B-06-B6-NATIVE-INSTALLED-DRIVER-01','assignedSeparatelyTo':'Luna',
   'readiness':'IMPLEMENTATION-REQUIRED','implementedHere':False},
 'unresolvedInputs':['full image bytes/archive verification','complete package transaction and payload acquisition',
   'compiler/linker/static-archive identities and coordinator reproduction','actual runtime/fxr inventories and loaded closure',
   'actual kernel release/feature permissions','numeric runtime identity and actual delegated parent/outer budget',
   'new S3 candidate digest','owner planning decision and scoped execution authorizations','independent qualification record/staged publication decision',
   'installed-profile driver and reviewed exact test recipes','dedicated 4096-byte evidence-reader MSG_CTRUNC coverage','measured resource/cleanup evidence']
}
out['signedRepositoryMetadata'].update({'signatureValid':True,'signingFingerprint':'F6ECB3762474EDA9D21B7022871920D1991BC93C',
    'keyProvenance':'Official archive.ubuntu.com HTTPS public keyring retained; no user trust settings changed',
    'baseSignatureVerification':'base-signature-verification.log'})
artifacts=[]
for role in ('owner','recovery','provider'):
    entries=[('contract','Gagamba.Execution.dll','managed'),('m1','Gagamba.Execution.Linux.M1.dll','managed')]
    if role!='provider':
        entry='Gagamba.Execution.Linux.M1.'+role.capitalize()
        entries += [('entry',entry+'.dll','managed'),('deps',entry+'.deps.json','config'),('runtimeconfig',entry+'.runtimeconfig.json','config')]
    for suffix,name,kind in entries:
        artifacts.append({'role':role+'-'+suffix,'path':role+'/'+name,'kind':kind,'mode':'0444','uid':0,'gid':0,'nlink':1,'sha256':None,'length':None})
artifacts += [dict(role='coordinator',path='native/exec-coordinator',kind='native',mode='0555',uid=0,gid=0,nlink=1,
    expectedSha256=out['candidate']['coordinatorSha256'],actualSha256=None,length=None),
    dict(role='seccomp-filter',path='native/seccomp-filter.bin',kind='policy',mode='0444',uid=0,gid=0,nlink=1,
    expectedSha256=out['candidate']['filterSha256'],actualSha256=None,length=672)]
out['candidate'].update({'profileId':'ae4-linux-fixed-fixture-v1','profileVersion':'1','scope':'WSL2-x86_64',
    'payload':artifacts,'manifestSchema':1,'ownerProtocol':1,'coordinatorProtocol':1,'manifestBootstrapIdentity':1,
    'recoveryConfigurationVersionForQualifiedLaunch':2,'kernelRelease':None,
    'sourceDeliveryReadiness':'UNRESOLVED: builder requires canonical-main real .git; main advances for docs. Authorize exact frozen-revision delivery/rebind before build; no checkout/reset/branch/worktree here.'})
(root/'rc2b-06-b6-native-platform-input-manifest.json').write_text(json.dumps(out,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Manifest authored: image metadata, '+str(len(chosen))+' exact package candidates, 4 verified package payloads; no approvals generated.')
