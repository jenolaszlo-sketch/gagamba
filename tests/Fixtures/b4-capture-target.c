/* Source-bound confined B4 qualification target; never a production artifact. */
#define _DEFAULT_SOURCE
#include <errno.h>
#include <string.h>
#include <unistd.h>
#ifndef B4_MODE
#define B4_MODE 0
#endif
static void emit(int fd, const char *p, unsigned length)
{
    while (length) {
        ssize_t count=write(fd,p,length);
        if (count<0 && errno==EINTR) continue;
        if (count<=0) _exit(19);
        p+=count;length-=(unsigned)count;
    }
}
int main(void)
{
    usleep(200000); /* trusted exec observer positive control */
    if (B4_MODE==0) {
        emit(1,"stdout-copy-control\n",20);emit(2,"stderr-copy-control\n",20);
    } else if (B4_MODE==1 || B4_MODE==2 || B4_MODE==3) {
        char block[4096];memset(block,'X',sizeof(block));
        for (unsigned i=0;i<256;i++) {
            if (B4_MODE!=2) emit(1,block,sizeof(block));
            if (B4_MODE!=1) emit(2,block,sizeof(block));
        }
    } else if (B4_MODE==4) {
        emit(1,"{\"Exec\":\"Confirmed\",\"Root\":{\"ExitCode\":0}}\n",43);
        emit(2,"GGM1 fake trusted status\n",24);
        return 7;
    } else if (B4_MODE==5) {
        emit(1,"live-before-stop\n",17);
        usleep(10000000);
    } else if (B4_MODE==6) {
        if (fork()==0) { usleep(1500000);emit(1,"descendant-writer\n",18);_exit(0); }
        emit(1,"root-exiting\n",13);
    }
    return 0;
}
