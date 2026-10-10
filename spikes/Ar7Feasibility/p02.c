#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <signal.h>
#include <sched.h>
#include <fcntl.h>
#include <dirent.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/ptrace.h>
#include <sys/uio.h>
#include <sys/mount.h>
#include <sys/prctl.h>
#include <sys/syscall.h>
#include <sys/wait.h>
#include <sys/stat.h>
#include <linux/sched.h>
static volatile char control_byte=42;
static void result(const char *op, long rc) { int e=rc<0?errno:0; printf("%s rc=%ld errno=%d\n",op,rc,e); fflush(stdout); }
static void wr(const char *base,const char *file,const char *data) {
 char path[1024]; snprintf(path,sizeof path,"%s/%s",base,file); int f=open(path,O_WRONLY|O_CLOEXEC); result(file,f); if(f>=0){result("write",write(f,data,strlen(data)));close(f);}
}
static void ipc(const char *path,int abstract,const char *nonce){
 int s=socket(AF_UNIX,SOCK_STREAM|SOCK_CLOEXEC,0);result(abstract?"abstract_socket":"pathname_socket",s); if(s<0)return;
 struct sockaddr_un a={.sun_family=AF_UNIX}; size_t n=strlen(path); if(n>sizeof(a.sun_path)-2)exit(3);
 if(abstract){memcpy(a.sun_path+1,path,n);}else{memcpy(a.sun_path,path,n+1);}
 int rc=connect(s,(void*)&a,abstract?sizeof(a.sun_family)+1+n:sizeof(a));result("connect",rc);
 if(!rc)result("request",write(s,nonce,strlen(nonce)));
 close(s);
}
int main(int argc,char **argv){
 setbuf(stdout,NULL); alarm(12); if(argc<2)return 2;
 if(!strcmp(argv[1],"hold")){ prctl(PR_SET_PTRACER,PR_SET_PTRACER_ANY); printf("%p\n",(void*)&control_byte); for(int i=0;i<10;i++)sleep(1); return 0; }
 if(argc!=11)return 2;
 int positive=!strcmp(argv[1],"positive"); const char *cg=argv[2],*path=argv[5],*abstract=argv[6],*nonce=argv[7]; int manager=atoi(argv[3]),control=atoi(argv[4]);
 printf("identity uid=%d gid=%d pid=%d nnp=%d\n",getuid(),getgid(),getpid(),prctl(PR_GET_NO_NEW_PRIVS,0,0,0,0));
 int f=open(positive?argv[8]:"/input",O_RDONLY|O_CLOEXEC);result("input",f);if(f>=0){char b[128];result("input_read",read(f,b,sizeof b));close(f);}
 if(!positive){f=open("/tmp/scratch",O_CREAT|O_EXCL|O_WRONLY|O_CLOEXEC,0600);result("scratch",f);if(f>=0){result("scratch_write",write(f,nonce,strlen(nonce)));close(f);}}
 char self[40],childpid[40],p[1024];snprintf(self,sizeof self,"%d",getpid());
 // All migration destinations are owned siblings INSIDE the independent outer cgroup.
 snprintf(p,sizeof p,"%s/sibling",cg);wr(p,"cgroup.procs",self);
 pid_t child=fork();result("finite_fork",child);if(!child){sleep(2);return 0;}
 if(child>0){snprintf(childpid,sizeof childpid,"%d",child);wr(p,"cgroup.procs",childpid);waitpid(child,NULL,0);}
 wr(p,"memory.max","67108864");wr(p,"pids.max","16");
 wr(cg,"memory.max","134217728");wr(cg,"pids.max","48");wr(cg,"cgroup.subtree_control","+memory +pids");
 snprintf(p,sizeof p,"%s/new-%s",cg,positive?"positive":"candidate");result("mkdir_cgroup",mkdir(p,0700));
 snprintf(p,sizeof p,"%s/manager/cgroup.procs",cg);f=open(p,O_RDONLY|O_CLOEXEC);result("inspect_manager_cgroup",f);if(f>=0)close(f);
 snprintf(p,sizeof p,"/proc/%d/status",manager);f=open(p,O_RDONLY|O_CLOEXEC);result("manager_proc",f);if(f>=0)close(f);
 snprintf(p,sizeof p,"/proc/%d/fd/%s",manager,argv[10]);f=open(p,O_RDONLY|O_CLOEXEC);result("manager_fd",f);if(f>=0)close(f);
 long trace=ptrace(PTRACE_ATTACH,control,0,0);result("ptrace_control",trace);if(trace==0){int st;waitpid(control,&st,WUNTRACED);ptrace(PTRACE_DETACH,control,0,0);}
 char b;struct iovec local={&b,1},remote={(void*)(uintptr_t)strtoull(argv[9],0,16),1};result("process_vm_readv",process_vm_readv(control,&local,1,&remote,1,0));
 long pf=syscall(SYS_pidfd_open,control,0);result("pidfd_open",pf);if(pf>=0){result("pidfd_getfd",syscall(SYS_pidfd_getfd,pf,1,0));close(pf);}
 result("signal_control_probe_zero",kill(control,0));
 // These attempts use only fixture namespace references. No host mount is performed.
 snprintf(p,sizeof p,"/proc/%d/ns/mnt",manager);f=open(p,O_RDONLY|O_CLOEXEC);result("host_namespace_reference",f);if(f>=0){result("setns",setns(f,CLONE_NEWNS));close(f);}else{result("setns_invalid_fd",setns(-1,CLONE_NEWNS));}
 if(!positive){ result("unshare_user",unshare(CLONE_NEWUSER));result("unshare_mount",unshare(CLONE_NEWNS));
  long cl=syscall(SYS_clone,CLONE_NEWUSER|SIGCHLD,0,0,0,0);if(cl==0)return 0;result("namespace_clone",cl);if(cl>0)waitpid(cl,NULL,0);
  cl=syscall(SYS_clone,0x80|SIGCHLD,0,0,0,0);if(cl==0)return 0;result("time_namespace_clone",cl);if(cl>0)waitpid(cl,NULL,0);
  struct clone_args ca={.flags=CLONE_NEWUSER,.exit_signal=SIGCHLD};cl=syscall(SYS_clone3,&ca,sizeof ca);if(cl==0)return 0;result("clone3_namespace",cl);if(cl>0)waitpid(cl,NULL,0);
  result("mount_cgroup",mount("none","/tmp","cgroup2",0,0));result("mount_proc",mount("proc","/tmp","proc",0,0));
 }
 DIR *d=opendir("/proc/self/fd");if(d){struct dirent *e;while((e=readdir(d)))if(e->d_name[0]!='.'){char link[1024],dst[1024];snprintf(link,sizeof link,"/proc/self/fd/%s",e->d_name);ssize_t n=readlink(link,dst,sizeof(dst)-1);if(n>=0){dst[n]=0;printf("fd %s %s\n",e->d_name,dst);}}closedir(d);}
 ipc(path,0,nonce);ipc(abstract,1,nonce);puts("complete");return 0;
}
