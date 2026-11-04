"""Offline metadata and ELF inspection only. Never executes package content."""
import hashlib, io, json, lzma, re, struct, tarfile
from pathlib import Path

base = Path(__file__).parent / 'platform'
sha = lambda b: hashlib.sha256(b).hexdigest()
release = (base / 'resolute-updates-InRelease').read_text()
match = re.findall(r'^ ([0-9a-f]{64})\s+(\d+) main/binary-amd64/Packages.xz$', release, re.M)
assert len(match) == 1, match
index = (base / 'resolute-updates-main-Packages.xz').read_bytes()
assert sha(index) == match[0][0] and len(index) == int(match[0][1]), 'Signed index mismatch'
selected = {'bubblewrap','libc6','libc6-dev','libc-dev-bin','gcc-15','gcc-15-base','libgcc-15-dev',
            'binutils','binutils-x86-64-linux-gnu','libstdc++6','libgcc-s1','dotnet-sdk-10.0',
            'dotnet-runtime-10.0','dotnet-host-10.0','dotnet-hostfxr-10.0','dotnet-runtime-deps-10.0',
            'libicu78','libicu76','libssl3t64','zlib1g','systemd','libcap2','libselinux1','linux-libc-dev',
            'python3','bash','tar','coreutils'}
selected.update({'gcc-15-x86-64-linux-gnu','cpp-15','cpp-15-x86-64-linux-gnu','gcc-16-base',
                 'libbrotli1','liblttng-ust1t64','libunwind8','libpcre2-8-0','libzstd1',
                 'libbinutils','binutils-common','ca-certificates','python3-minimal','git','libc-bin','make'})
records = []
for stanza in lzma.decompress(index).decode().split('\n\n'):
    fields = {}
    key = None
    for line in stanza.splitlines():
        if line.startswith(' ') and key:
            fields[key] += '\n' + line
        elif ': ' in line:
            key, value = line.split(': ', 1)
            fields[key] = value
    if fields.get('Package') in selected:
        fields['suite'] = 'resolute-updates'
        records.append({k: fields.get(k) for k in ('Package','Version','Architecture','Source','Depends','Pre-Depends','Filename','Size','SHA256','suite')})
base_release = (base / 'resolute-InRelease').read_text()
base_match = re.findall(r'^ ([0-9a-f]{64})\s+(\d+) main/binary-amd64/Packages.xz$', base_release, re.M)
assert len(base_match) == 1
base_index = (base / 'resolute-main-Packages.xz').read_bytes()
assert sha(base_index) == base_match[0][0] and len(base_index) == int(base_match[0][1])
for stanza in lzma.decompress(base_index).decode().split('\n\n'):
    fields = {}
    key = None
    for line in stanza.splitlines():
        if line.startswith(' ') and key:
            fields[key] += '\n' + line
        elif ': ' in line:
            key, value = line.split(': ', 1)
            fields[key] = value
    if fields.get('Package') in selected:
        fields['suite'] = 'resolute'
        records.append({k: fields.get(k) for k in ('Package','Version','Architecture','Source','Depends','Pre-Depends','Filename','Size','SHA256','suite')})
(base / 'selected-package-records.json').write_text(json.dumps(records, indent=2)+'\n')

def ar_members(raw):
    assert raw[:8] == b'!<arch>\n'
    off = 8
    members = {}
    while off < len(raw):
        header = raw[off:off+60]
        assert len(header) == 60 and header[-2:] == b'`\n'
        name = header[:16].decode().strip().rstrip('/')
        size = int(header[48:58])
        members[name] = raw[off+60:off+60+size]
        off += 60 + size + size % 2
    return members

def elf_info(raw):
    assert raw[:6] == b'\x7fELF\x02\x01', 'Expected ELF64 little endian'
    hdr = struct.unpack_from('<16sHHIQQQIHHHHHH', raw)
    sections = [struct.unpack_from('<IIQQQQIIQQ', raw, hdr[6]+i*hdr[11]) for i in range(hdr[12])]
    dyn = next(s for s in sections if s[1] == 6)
    strs = sections[dyn[6]]
    string_data = raw[strs[4]:strs[4]+strs[5]]
    needed = []
    for offset in range(dyn[4], dyn[4]+dyn[5], dyn[9]):
        tag, value = struct.unpack_from('<qQ', raw, offset)
        if tag == 1:
            needed.append(string_data[value:].split(b'\0',1)[0].decode())
    return {'class':'ELF64','endian':'little','machine':hdr[2], 'dtNeeded':needed,
            'glibcStrings': sorted(set(x.decode() for x in re.findall(rb'GLIBC_[0-9.]+', raw)))}

def exported_versions(raw, wanted):
    hdr = struct.unpack_from('<16sHHIQQQIHHHHHH', raw)
    sections = [struct.unpack_from('<IIQQQQIIQQ', raw, hdr[6]+i*hdr[11]) for i in range(hdr[12])]
    symbols = next(s for s in sections if s[1] == 11)
    strings = sections[symbols[6]]
    string_data = raw[strings[4]:strings[4]+strings[5]]
    versions = next(s for s in sections if s[1] == 0x6fffffff)
    definitions = next(s for s in sections if s[1] == 0x6ffffffd)
    tags = {}
    off = definitions[4]
    while True:
        ver, flags, index, count, hash_value, aux, nxt = struct.unpack_from('<HHHHIII', raw, off)
        name_offset, _ = struct.unpack_from('<II', raw, off+aux)
        tags[index] = string_data[name_offset:].split(b'\0',1)[0].decode()
        if not nxt: break
        off += nxt
    found = []
    for i, off in enumerate(range(symbols[4], symbols[4]+symbols[5], symbols[9])):
        name, info, other, section, value, size = struct.unpack_from('<IBBHQQ', raw, off)
        text = string_data[name:].split(b'\0',1)[0].decode()
        if text in wanted and section:
            tag = struct.unpack_from('<H', raw, versions[4]+2*i)[0] & 0x7fff
            found.append({'name':text,'version':tags.get(tag),'size':size,'defined':True})
    return found

observations = []
for path in base.glob('bubblewrap_*.deb'):
    raw = path.read_bytes()
    record = next((r for r in records if Path(r['Filename']).name == path.name), None)
    if record:
        assert sha(raw) == record['SHA256'] and len(raw) == int(record['Size'])
    members = ar_members(raw)
    control_bytes = next(v for k,v in members.items() if k.startswith('control.tar'))
    with tarfile.open(fileobj=io.BytesIO(control_bytes), mode='r:*') as control:
        control_file = next(m for m in control if m.name.lstrip('./') == 'control')
        control_text = control.extractfile(control_file).read().decode()
    data_bytes = next(v for k,v in members.items() if k.startswith('data.tar'))
    with tarfile.open(fileobj=io.BytesIO(data_bytes), mode='r:*') as data:
        matches = [m for m in data if m.name.lstrip('./') == 'usr/bin/bwrap']
        assert len(matches) == 1 and matches[0].isfile()
        item = matches[0]
        binary = data.extractfile(item).read()
        output = base / (path.stem + '.bwrap.elf')
        output.write_bytes(binary)
        observation = {'package':path.name,'packageBytes':len(raw),'packageSha256':sha(raw),
            'signedIndexMatch':bool(record), 'control':control_text,
            'memberPath':item.name,'archiveUid':item.uid,'archiveGid':item.gid,'archiveMode':oct(item.mode),
            'archivePaxHeaders':item.pax_headers,'binaryBytes':len(binary),'binarySha256':sha(binary),
            'requiredBinarySha256':'523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6',
            'matchesRequiredBinary':sha(binary)=='523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6',
            'elf':elf_info(binary),'installedMetadataVerified':False,'executed':False}
        observations.append(observation)
(base / 'bubblewrap-byte-observations.json').write_text(json.dumps(observations,indent=2)+'\n')
(base / 'metadata-chain.json').write_text(json.dumps({'inReleaseSha256':sha((base/'resolute-updates-InRelease').read_bytes()),
    'indexSha256':sha(index),'indexBytes':len(index),'indexMatchesSignedRelease':True,
    'baseInReleaseSha256':sha((base/'resolute-InRelease').read_bytes()),'baseIndexSha256':sha(base_index),
    'baseIndexBytes':len(base_index),'baseIndexMatchesSignedRelease':True,
    'signatureVerification':'archive-signature-verification.log','selectedRecords':len(records)},indent=2)+'\n')
runtime_observations = []
for prefix in ('libc6_', 'dotnet-host-10.0_', 'dotnet-hostfxr-10.0_'):
    for path in base.glob(prefix+'*.deb'):
        raw = path.read_bytes()
        record = next(r for r in records if Path(r['Filename']).name == path.name)
        assert sha(raw) == record['SHA256'] and len(raw) == int(record['Size'])
        members = ar_members(raw)
        data_bytes = next(v for k,v in members.items() if k.startswith('data.tar'))
        entry = {'package':path.name,'packageSha256':sha(raw),'signedIndexMatch':True,'files':[],'executed':False}
        with tarfile.open(fileobj=io.BytesIO(data_bytes), mode='r:*') as data:
            for item in data:
                if not item.isfile(): continue
                name = item.name.lstrip('./')
                if name in ('usr/lib/x86_64-linux-gnu/libc.so.6','usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2','usr/lib/dotnet/dotnet') or name.endswith('/libhostfxr.so'):
                    blob = data.extractfile(item).read()
                    details = {'path':name,'sha256':sha(blob),'bytes':len(blob),'uid':item.uid,'gid':item.gid,
                               'archiveMode':oct(item.mode),'elf':elf_info(blob)}
                    if name.endswith('/libc.so.6'):
                        details['exports'] = exported_versions(blob, {'posix_spawnattr_setcgroup_np','posix_spawnattr_getcgroup_np'})
                    entry['files'].append(details)
        runtime_observations.append(entry)
(base / 'runtime-byte-observations.json').write_text(json.dumps(runtime_observations,indent=2)+'\n')
print(json.dumps({'indexesVerified':True,'selectedPackageRecords':len(records),
    'bubblewrapMatches':observations[0]['matchesRequiredBinary'], 'runtimePackageCount':len(runtime_observations)},indent=2))
