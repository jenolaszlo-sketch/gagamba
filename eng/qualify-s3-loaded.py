#!/usr/bin/env python3
"""Actual loaded Release assembly/mapping observations in disposable copies.
This does not publish qualification records or create a trusted installation.
"""
import argparse, hashlib, json, os, pathlib, shutil, subprocess, time, uuid
p=argparse.ArgumentParser()
p.add_argument('--fixture',type=pathlib.Path,required=True)
p.add_argument('--artifacts',type=pathlib.Path,required=True)
o=p.parse_args();assert os.getuid()!=0 and os.getgid()!=0
o.artifacts.mkdir(parents=True,exist_ok=True)
modes=['matching','entry-replaced','dependency-replaced','same-filename','wrong-location','missing-component','unexpected-assembly','dynamic-assembly']
results=[]
for mode in modes:
    root=pathlib.Path('/tmp/gagamba-s3-loaded-'+mode+'-'+uuid.uuid4().hex);root.mkdir(mode=0o700)
    prefix=o.fixture.stem
    names=[prefix+'.dll',prefix+'.deps.json',prefix+'.runtimeconfig.json','Gagamba.Execution.Linux.M1.dll','Gagamba.Execution.dll']
    hashes={}
    for name in names:
        data=(o.fixture.parent/name).read_bytes();hashes[name]=hashlib.sha256(data).hexdigest()
        (root/name).write_bytes(data);(root/name).chmod(0o600)
    output=o.artifacts/(mode+'.json')
    command=['/usr/lib/dotnet/dotnet','exec','--depsfile',str(root/(prefix+'.deps.json')),'--runtimeconfig',str(root/(prefix+'.runtimeconfig.json')),
             str(root/o.fixture.name),'--b6-loaded',mode,str(output.resolve())]
    start=time.monotonic_ns()
    with (o.artifacts/(mode+'.log')).open('w') as log:
        log.write('COMMAND '+json.dumps(command)+'\n');log.flush()
        child=subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,timeout=20)
    result=json.loads(output.read_text()) if output.exists() else {'Passed':False,'Error':'process produced no observation'}
    assert child.returncode==0 and result['Passed'],(mode,child.returncode,result)
    assert result['PositiveMapped'] and not result['TrustedInstallationReturned']
    results.append(dict(mode=mode,command=command,privateCopy=str(root),inputSha256=hashes,elapsedNs=time.monotonic_ns()-start,exitCode=child.returncode,result=result))
    print(mode,'PASS (native mapping/inventory only)',flush=True)
metadata=dict(scope='Native mapping and actual assembly observations, not full LoadedInstallation or native loader qualification',
              uid=os.getuid(),kernel=os.uname().release,passed=len(results),failed=0,skipped=0,results=results,
              retained='Private test-only copies retained for review; all child processes waited/reaped')
(o.artifacts/'results.json').write_text(json.dumps(metadata,indent=2)+'\n')
