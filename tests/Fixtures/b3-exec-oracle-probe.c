#define _GNU_SOURCE
#include <signal.h>
#include <stdlib.h>
#include <string.h>
#include <sys/wait.h>
#include "b3-exec-confirm.h"

int main(int argc, char **argv)
{
    if (argc != 3) return 2;
    struct stat tool;
    if (stat(argv[2], &tool) != 0) return 3;
    int confirm[2];
    if (pipe2(confirm, O_CLOEXEC) != 0) return 4;
    pid_t child = fork();
    if (child < 0) return 5;
    if (child == 0)
    {
        close(confirm[0]);
        if (strcmp(argv[1], "pre-exec-kill") == 0) raise(SIGSTOP);
        if (strcmp(argv[1], "pre-exec-exit") == 0) _exit(0);
        char *const args[] = { argv[2], NULL };
        char *const env[] = { NULL };
        execve(strcmp(argv[1], "failed") == 0 ? "/gagamba-no-such-exec" : argv[2], args, env);
        uint32_t failure = (uint32_t)errno;
        ssize_t n;
        do { n = write(confirm[1], &failure, sizeof(failure)); } while (n < 0 && errno == EINTR);
        _exit(127);
    }
    close(confirm[1]);
    if (strcmp(argv[1], "pre-exec-kill") == 0)
    {
        int stopped;
        if (waitpid(child, &stopped, WUNTRACED) != child || !WIFSTOPPED(stopped)) return 6;
        kill(child, SIGKILL);
    }
    uint32_t failure = 0;
    uint64_t before = b3_exec_now();
    int observed = b3_observe_exec(child, confirm[0], tool.st_dev, tool.st_ino, before + 3000000000ull, &failure);
    uint64_t elapsed = b3_exec_now() - before;
    close(confirm[0]);
    siginfo_t info = {0};
    if (waitid(P_PID, (id_t)child, &info, WEXITED | WNOHANG | WNOWAIT) != 0) return 7;
    printf("{\"Observed\":%d,\"Errno\":%u,\"ChildPid\":%d,\"BeforeExit\":%s,\"ElapsedNs\":%llu}\n",
        observed, failure, child, info.si_pid == 0 ? "true" : "false", (unsigned long long)elapsed);
    fflush(stdout);
    if (observed == 0) {
        /* Parent test inspects the actual inherited descriptor inventory before acknowledging cleanup. */
        struct pollfd command = { .fd = 0, .events = POLLIN };
        (void)poll(&command, 1, 1000);
    }
    if (info.si_pid == 0) kill(child, SIGKILL);
    int status;
    while (waitpid(child, &status, 0) < 0) if (errno != EINTR) return 8;
    return 0;
}
