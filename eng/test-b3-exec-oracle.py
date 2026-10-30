#!/usr/bin/env python3
"""Unprivileged exec/FD primitive tests only; no sandbox or integrated release claim."""
import json, pathlib, subprocess, sys
binary,target=sys.argv[1:3]
for mode in ('successful','failed','pre-exec-kill','pre-exec-exit'):
    with subprocess.Popen([binary,mode,target],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True) as p:
        facts=None
        inventory=None
        while facts is None or (mode=='successful' and inventory is None):
            line=p.stdout.readline()
            assert line, (mode,p.poll(),p.stderr.read())
            if line.startswith('TARGET_FDS='): inventory=[int(x) for x in line.strip().split('=')[1].split(',')]
            else: facts=json.loads(line)
        if mode=='successful':
            assert facts['Observed']==0 and facts['BeforeExit'] and facts['ElapsedNs']<1_000_000_000, facts
            descriptors=sorted(int(x.name) for x in pathlib.Path(f"/proc/{facts['ChildPid']}/fd").iterdir())
            assert inventory==[0,1,2],inventory
            assert descriptors==[0,1,2],descriptors
            facts['InheritedDescriptors']=descriptors
            p.stdin.write('cleanup\n');p.stdin.flush()
        elif mode=='failed': assert facts['Observed']==1 and facts['Errno']==2, facts
        else: assert facts['Observed']==-1, facts
        p.wait(timeout=5)
        assert p.returncode==0,(mode,p.returncode,p.stderr.read())
        assert not pathlib.Path(f"/proc/{facts['ChildPid']}").exists(),facts
        print(json.dumps({'Mode':mode,'Result':'Passed','Facts':facts}))
print('4 passed / 0 failed / 0 skipped; all direct children reaped')
