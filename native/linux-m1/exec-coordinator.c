/* WP04 trusted setup coordinator; deliberately no target launch or RELEASE path. */
#define _GNU_SOURCE
#include "protocol.h"
#include "release_protocol.h"
#include <errno.h>
#include <fcntl.h>
#include <limits.h>
#include <poll.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/prctl.h>
#include <sys/resource.h>
#include <sys/stat.h>
#include <sys/syscall.h>
#include <time.h>
#include <unistd.h>

#define CONFIG_MANIFEST_LEN 14u
#define CONFIG_EXEC_ID 18u
#define CONFIG_PREP_ID 34u
#define CONFIG_GENERATION 50u
#define CONFIG_DIGEST 66u
#define CONFIG_TOOL_DEV 98u
#define CONFIG_TOOL_INO 106u
#define CONFIG_INPUT_DEV 114u
#define CONFIG_INPUT_INO 122u
#define CONFIG_COORD_DEV 130u
#define CONFIG_COORD_INO 138u
#define CONFIG_SETUP_DEADLINE 186u
#define CONFIG_DEADLINE 194u

static unsigned char cfg[GGM1_MAX_CONFIG_BYTES];
static const unsigned char *exec_id, *prep_id, *generation, *digest;
static uint64_t deadline_ns;
static uint64_t setup_deadline_ns;
static int have_config;

static uint16_t rd16(const unsigned char *p) { return (uint16_t)p[0] | ((uint16_t)p[1] << 8); }
static uint32_t rd32(const unsigned char *p) { return p[0] | ((uint32_t)p[1] << 8) | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24); }
static uint64_t rd64(const unsigned char *p) { uint64_t v=0; for (unsigned i=0;i<8;i++) v|=(uint64_t)p[i]<<(8*i); return v; }
static void wr16(unsigned char *p,uint16_t v) { p[0]=(unsigned char)v; p[1]=(unsigned char)(v>>8); }
static void wr32(unsigned char *p,uint32_t v) { for(unsigned i=0;i<4;i++) p[i]=(unsigned char)(v>>(8*i)); }
static void wr64(unsigned char *p,uint64_t v) { for(unsigned i=0;i<8;i++) p[i]=(unsigned char)(v>>(8*i)); }
static uint64_t now_ns(void) { struct timespec t; if(clock_gettime(CLOCK_MONOTONIC,&t)) return 0; return (uint64_t)t.tv_sec*1000000000ull+t.tv_nsec; }

static int read_all(int fd,unsigned char *b,size_t cap,size_t *len)
{
    size_t n=0;
    for (;;) {
        ssize_t r=read(fd,b+n,cap-n);
        if(r<0 && errno==EINTR) continue;
        if(r<0) return -1;
        if(r==0) break;
        n+=(size_t)r;
        if(n==cap) { unsigned char x; ssize_t more=read(fd,&x,1); if(more!=0) return -1; break; }
    }
    *len=n; return 0;
}

static int load_config(void)
{
    struct stat st;
    const int need=F_SEAL_WRITE|F_SEAL_GROW|F_SEAL_SHRINK|F_SEAL_SEAL;
    int seals=fcntl(3,F_GET_SEALS);
    if(seals<0 || (seals&need)!=need || fstat(3,&st) || !S_ISREG(st.st_mode)
       || st.st_size<(off_t)GGM1_CONFIG_HEADER_BYTES || st.st_size>GGM1_MAX_CONFIG_BYTES) return -1;
    size_t n=0;
    if(read_all(3,cfg,sizeof(cfg),&n) || n!=(size_t)st.st_size
       || memcmp(cfg,GGM1_CONFIG_MAGIC,8) || rd16(cfg+8)!=GGM1_CONFIG_VERSION
       || rd32(cfg+10)!=n || rd32(cfg+CONFIG_MANIFEST_LEN)!=n-GGM1_CONFIG_HEADER_BYTES) return -1;
    exec_id=cfg+CONFIG_EXEC_ID; prep_id=cfg+CONFIG_PREP_ID; generation=cfg+CONFIG_GENERATION; digest=cfg+CONFIG_DIGEST;
    setup_deadline_ns=rd64(cfg+CONFIG_SETUP_DEADLINE);
    deadline_ns=rd64(cfg+CONFIG_DEADLINE);
    if(!setup_deadline_ns || setup_deadline_ns>=deadline_ns || !now_ns() || now_ns()>=setup_deadline_ns) return -1;
    have_config=1; return 0;
}

static int pipe_role(int fd,int accmode)
{
    struct stat st; int fl=fcntl(fd,F_GETFL);
    return fl>=0 && (fl&O_ACCMODE)==accmode && fstat(fd,&st)==0 && S_ISFIFO(st.st_mode);
}

static int object_is(const char *p,size_t d,size_t i)
{
    struct stat st;
    return stat(p,&st)==0 && S_ISREG(st.st_mode)
        && (uint64_t)st.st_dev==rd64(cfg+d) && (uint64_t)st.st_ino==rd64(cfg+i);
}

static int read_status(uint64_t *eff,uint64_t *prm,uint64_t *amb,uint32_t *nnp,uint32_t *sec)
{
    FILE *f=fopen("/proc/self/status","re"); if(!f) return -1;
    char *line=NULL; size_t cap=0; int a=0,b=0,c=0,d=0,e=0; unsigned long long x;
    while(getline(&line,&cap,f)>=0) {
        if(sscanf(line,"CapEff:%llx",&x)==1){*eff=x;a=1;}
        else if(sscanf(line,"CapPrm:%llx",&x)==1){*prm=x;b=1;}
        else if(sscanf(line,"CapAmb:%llx",&x)==1){*amb=x;c=1;}
        else if(sscanf(line,"NoNewPrivs:%u",nnp)==1)d=1;
        else if(sscanf(line,"Seccomp:%u",sec)==1)e=1;
    }
    free(line); fclose(f); return a&&b&&c&&d&&e?0:-1;
}

static int check_mounts(unsigned char *raw,size_t *len)
{
    FILE *f=fopen("/proc/self/mountinfo","re"); if(!f)return -1;
    size_t n=fread(raw,1,3000,f); int bad=ferror(f); fclose(f);
    if(bad || n>=3000)return -1; raw[n]=0; *len=n;
    const char *need[]={"/tool","/input","/coordinator","/proc","/tmp","/dev/shm"};
    for(size_t i=0;i<sizeof(need)/sizeof(need[0]);i++) if(!strstr((char*)raw,need[i])) return -1;
    if(!strstr((char*)raw,"/tmp") || !strstr((char*)raw,"/dev/shm")) return -1;
    /* Bubblewrap argv fixes these mounts; report supplies the complete
       independent mountinfo text for the outer verifier to inspect. */
    return 0;
}

static int write_frame(uint16_t kind,uint64_t seq,const unsigned char *payload,size_t plen)
{
    unsigned char frame[GGM1_MAX_FRAME_BYTES];
    if(plen+GGM1_HEADER_BYTES>sizeof(frame))return -1;
    memset(frame,0,GGM1_HEADER_BYTES); memcpy(frame,GGM1_MAGIC,8);
    wr16(frame+8,GGM1_VERSION); wr16(frame+10,kind); wr32(frame+12,(uint32_t)(96+plen)); wr64(frame+16,seq);
    if(have_config){memcpy(frame+24,exec_id,16);memcpy(frame+40,prep_id,16);memcpy(frame+56,digest,32);}
    if(plen)memcpy(frame+96,payload,plen);
    ssize_t n=write(5,frame,96+plen); return n==(ssize_t)(96+plen)?0:-1;
}

static int fail(uint32_t code,uint64_t seq)
{
    unsigned char p[4]; wr32(p,code);
    if(pipe_role(5,O_WRONLY)) (void)write_frame(GGM1_KIND_PROTOCOL_FAILED,seq,p,sizeof(p));
    return 120+(int)(code%20);
}

static int held_report(unsigned char *p,size_t cap,size_t *len)
{
    if(cap<256)return -1;
    uint64_t eff=0,prm=0,amb=0; uint32_t nnp=0,sec=0;
    if(read_status(&eff,&prm,&amb,&nnp,&sec) || eff || prm || amb || nnp!=1 || sec!=2
       || prctl(PR_GET_DUMPABLE)!=0) return -1;
    struct rlimit nf,core;
    if(getrlimit(RLIMIT_NOFILE,&nf)||nf.rlim_cur!=64||nf.rlim_max!=64
       ||getrlimit(RLIMIT_CORE,&core)||core.rlim_cur!=0||core.rlim_max!=0) return -1;
    if(!object_is("/tool",CONFIG_TOOL_DEV,CONFIG_TOOL_INO)
       ||!object_is("/input",CONFIG_INPUT_DEV,CONFIG_INPUT_INO)
       ||!object_is("/coordinator",CONFIG_COORD_DEV,CONFIG_COORD_INO)) return -1;
    uint64_t ns[6]; const char *names[]={"user","mnt","pid","net","ipc","uts"};
    for(unsigned i=0;i<6;i++){char path[64];snprintf(path,sizeof(path),"/proc/self/ns/%s",names[i]);struct stat s;if(stat(path,&s))return -1;ns[i]=s.st_ino;}
    unsigned char mi[3000],cg[256]; size_t milen=0,cglen=0;
    if(check_mounts(mi,&milen))return -1;
    FILE *cf=fopen("/proc/self/cgroup","re");if(!cf)return -1;cglen=fread(cg,1,sizeof(cg),cf);int cerr=ferror(cf);fclose(cf);if(cerr)return -1;
    uid_t uid=getuid(),euid=geteuid();gid_t gid=getgid(),egid=getegid();
    size_t at=0;wr16(p+at,GGM1_REPORT_VERSION);at+=2;
    wr16(p+at,15);at+=2; /* setup, no-init, release-disabled, HELD */
    wr32(p+at,uid);at+=4;wr32(p+at,euid);at+=4;wr32(p+at,gid);at+=4;wr32(p+at,egid);at+=4;
    wr32(p+at,(uint32_t)getpid());at+=4;wr32(p+at,nnp);at+=4;wr32(p+at,sec);at+=4;wr32(p+at,0);at+=4;wr32(p+at,0);at+=4;
    wr64(p+at,eff);at+=8;wr64(p+at,prm);at+=8;wr64(p+at,amb);at+=8;
    wr64(p+at,nf.rlim_cur);at+=8;wr64(p+at,nf.rlim_max);at+=8;wr64(p+at,core.rlim_cur);at+=8;wr64(p+at,core.rlim_max);at+=8;
    for(unsigned i=0;i<6;i++){wr64(p+at,ns[i]);at+=8;}
    for(size_t off=CONFIG_TOOL_DEV;off<=CONFIG_COORD_INO;off+=8){memcpy(p+at,cfg+off,8);at+=8;}
    wr16(p+at,(uint16_t)cglen);at+=2;if(at+cglen+2+milen>cap)return -1;memcpy(p+at,cg,cglen);at+=cglen;
    wr16(p+at,(uint16_t)milen);at+=2;memcpy(p+at,mi,milen);at+=milen;*len=at;return 0;
}

int main(void)
{
    if(!pipe_role(4,O_RDONLY)||!pipe_role(5,O_WRONLY)||!pipe_role(1,O_WRONLY)||!pipe_role(2,O_WRONLY))return 101;
    unsigned char b;if(read(0,&b,1)!=0)return 102;
    if(load_config())return 103;
    struct rlimit nf={64,64},core={0,0};
    if(setrlimit(RLIMIT_NOFILE,&nf)||setrlimit(RLIMIT_CORE,&core)||prctl(PR_SET_DUMPABLE,0))return fail(10,1);
    close(3);
    if(syscall(SYS_close_range,6u,UINT_MAX,0u)!=0)return fail(11,1);
    if(fcntl(4,F_GETFD)<0||fcntl(5,F_GETFD)<0)return fail(12,1);
    unsigned char report[GGM1_MAX_FRAME_BYTES-GGM1_HEADER_BYTES];size_t n=0;
    if(!now_ns()||now_ns()>=setup_deadline_ns)return fail(14,1);
    if(held_report(report,sizeof(report),&n))return fail(13,1);
    if(!now_ns()||now_ns()>=setup_deadline_ns)return fail(14,1);
    if(write_frame(GGM1_KIND_HELD,1,report,n))return 114;
    struct pollfd pfd={.fd=4,.events=POLLIN|POLLHUP|POLLERR};
    for(;;){
        uint64_t now=now_ns();if(!now||now>=deadline_ns)return fail(30,2);
        uint64_t rem=(deadline_ns-now)/1000000ull;int ms=rem>250?250:(int)rem;
        int r=poll(&pfd,1,ms);if(r<0&&errno==EINTR)continue;if(r<0)return fail(31,2);if(!r)continue;
        if(pfd.revents&POLLIN){
            unsigned char frame[GGM1_RELEASE_BYTES];size_t got=0;
            while(got<sizeof(frame)){
                ssize_t nread=read(4,frame+got,sizeof(frame)-got);
                if(nread<0&&errno==EINTR)continue;
                if(nread<0)return fail(40,2);
                if(nread==0)return fail(33,2);
                got+=(size_t)nread;
            }
            struct pollfd extra={.fd=4,.events=POLLIN};
            int have_extra=poll(&extra,1,0);
            if(have_extra<0)return fail(40,2);
            if(have_extra>0&&(extra.revents&POLLIN))return fail(41,2);
            if(!ggm1_release_frame_valid(frame,sizeof(frame),exec_id,prep_id,digest,generation,deadline_ns,now_ns()))
                return fail(42,2);
            close(4);
            /* WP05 stops at the pre-exec boundary. Target fork/exec is WP10. */
            return 0;
        }
        if(pfd.revents&(POLLHUP|POLLERR))return fail(33,2);
        return fail(34,2);
    }
}
