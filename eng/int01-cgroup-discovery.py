#!/usr/bin/env python3
"""Read-only discovery. Access bits are not authorization or proof of process migration."""
import argparse, json, os, pathlib, stat

def metadata(path):
    try:
        info=path.stat()
        return {'Path':str(path),'Uid':info.st_uid,'Gid':info.st_gid,'Mode':oct(stat.S_IMODE(info.st_mode)),
                'Device':info.st_dev,'Inode':info.st_ino,'Readable':os.access(path,os.R_OK),'Writable':os.access(path,os.W_OK)}
    except OSError as error:return {'Path':str(path),'Error':str(error)}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--report',required=True);args=parser.parse_args()
    root=pathlib.Path('/sys/fs/cgroup');groups=[];pending=[(root,0)]
    while pending and len(groups)<128:
        path,depth=pending.pop(0);group=metadata(path)
        for name in ('cgroup.controllers','cgroup.subtree_control','cgroup.type','cgroup.events'):
            try:group[name]=(path/name).read_text()[:4096].strip()
            except OSError as error:group[name]=str(error)
        group['Controls']=[metadata(path/name) for name in ('cgroup.procs','cgroup.kill','memory.max','pids.max') if (path/name).exists()]
        groups.append(group)
        if depth<3:
            try:pending.extend((entry,depth+1) for entry in path.iterdir() if entry.is_dir())
            except OSError:pass
    status=pathlib.Path('/proc/self/status').read_text().splitlines()
    report={'Disposition':'BLOCKED_ENVIRONMENT / NOT_REPRODUCED','Host':list(os.uname()),'Uid':os.getuid(),'Gid':os.getgid(),
        'Pid':os.getpid(),'ParentPid':os.getppid(),'Sid':os.getsid(0),'ProcessGroup':os.getpgrp(),
        'Membership':pathlib.Path('/proc/self/cgroup').read_text().strip(),
        'Status':[line for line in status if line.startswith(('CapInh:','CapPrm:','CapEff:','CapAmb:','NoNewPrivs:','NSpid:'))],
        'Namespaces':{name:os.readlink('/proc/self/ns/'+name) for name in ('pid','user','cgroup','mnt')},
        'Cgroups':groups,'InventoryTruncated':bool(pending),'ScanScope':'directories through depth 3; deeper descendants not inventoried',
        'DirectMigrationCommonAncestor':metadata(root/'cgroup.procs'),'ServiceMediatedLaunchAttempted':False,
        'AuthorizedParent':None,'AuthorizedLaunchProcedure':None,'MigrationAttempted':False,
        'Qualification':'No confinement, migration, independent recovery survival, domain stop or cleanup qualified'}
    pathlib.Path(args.report).write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({key:value for key,value in report.items() if key!='Cgroups'},indent=2))
    print(f'{len(groups)} cgroups inspected read-only; '+str(sum(group.get('Writable',False) for group in groups))+' writable directory entries observed')
if __name__=='__main__':main()
