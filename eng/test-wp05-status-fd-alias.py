#!/usr/bin/env python3
"""Real native helper FD remapping regression; no sandbox execution claim."""
import argparse,fcntl,hashlib,json,os,pathlib,select,signal,struct,time,uuid
parser=argparse.ArgumentParser();parser.add_argument('binary');parser.add_argument('--expect-exit',type=int,required=True);args=parser.parse_args()
assert os.getuid()!=0
execution,preparation,generation=(uuid.uuid4().bytes for _ in range(3));digest=bytes(range(32));deadline=time.monotonic_ns()+10_000_000_000
frame=bytearray(128);frame[:8]=b'GGM1CTL\0';struct.pack_into('<HHIQ',frame,8,1,2,128,1)
frame[24:40]=execution;frame[40:56]=preparation;frame[56:88]=digest;frame[96:112]=generation;struct.pack_into('<Q',frame,112,deadline)
rr,rw=os.pipe();sr,sw=os.pipe();stdout_r,stdout_w=os.pipe();stderr_r,stderr_w=os.pipe()
pid=os.fork()
if pid==0:
    rc=fcntl.fcntl(rr,fcntl.F_DUPFD_CLOEXEC,16);sc=fcntl.fcntl(sw,fcntl.F_DUPFD_CLOEXEC,16)
    os.dup2(rc,6);os.dup2(sc,4);os.dup2(stdout_w,1);os.dup2(stderr_w,2)
    for fd in set((rr,rw,sr,sw,stdout_r,stdout_w,stderr_r,stderr_w,rc,sc))-{1,2,4,6}:
        os.close(fd)
    env={'PATH':'/usr/bin:/bin','WP05_RELEASE_FD':'6','WP05_STATUS_FD':'4','WP05_EXPECT_EXEC':execution.hex(),
      'WP05_EXPECT_PREP':preparation.hex(),'WP05_EXPECT_GEN':generation.hex(),'WP05_EXPECT_DIGEST':digest.hex(),
      'WP05_EXPECT_OVERALL':str(deadline),'WP05_STATUS_MODE':'exec_confirmed','WP05_STATUS_VALUE':'0'}
    os.execve(args.binary,[args.binary],env)
for fd in (rr,sw,stdout_w,stderr_w):os.close(fd)
try:
    os.write(rw,frame);os.close(rw);rw=-1
    end=time.monotonic()+5;status=None
    while time.monotonic()<end:
        reaped,wait=os.waitpid(pid,os.WNOHANG)
        if reaped:status=wait;break
        time.sleep(.005)
    assert status is not None,'bounded native child exit unavailable'
    raw=os.read(sr,4096);stdout=os.read(stdout_r,4096);stderr=os.read(stderr_r,4096)
    facts={'Binary':args.binary,'BinarySha256':hashlib.sha256(pathlib.Path(args.binary).read_bytes()).hexdigest(),
      'Pid':pid,'Reaped':True,'ExitCode':os.waitstatus_to_exitcode(status),'ReleaseSourceFd':6,'StatusSourceFd':4,
      'NativeStatusHex':raw.hex(),'Stdout':stdout.decode(),'Stderr':stderr.decode()}
    print(json.dumps(facts,indent=2),flush=True)
    assert facts['ExitCode']==args.expect_exit
    if args.expect_exit==0:
        assert len(raw)==96 and raw[:8]==b'GGM1CTL\0' and struct.unpack_from('<H',raw,10)[0]==4
        assert struct.unpack_from('<IQ',raw,12)==(96,2) and raw[24:88]==execution+preparation+digest
    else:assert not raw
finally:
    for fd in (rw,sr,stdout_r,stderr_r):
        if fd>=0:os.close(fd)
