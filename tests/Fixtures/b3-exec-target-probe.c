/* Static finite target: inspect descriptors without opening an inventory descriptor. */
#define _GNU_SOURCE
#include <fcntl.h>
#include <stdio.h>
#include <unistd.h>
int main(void)
{
    fputs("TARGET_FDS=", stdout);
    int first=1;
    for(int fd=0;fd<64;fd++) if(fcntl(fd,F_GETFD)>=0)
    {printf("%s%d",first?"":",",fd);first=0;}
    fputc('\n',stdout);fflush(stdout);
    sleep(2);
    return 0;
}
