#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <signal.h>
#include <sys/syscall.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <unistd.h>

/* Reviewer-only control: kernel ground truth for a SIGKILL death observed
   via wait4, so that a .NET Process.ExitCode of 137 can be bound to
   WIFSIGNALED(sig=9) behavior on this exact kernel/runtime profile. */

int main(void)
{
    sigset_t blocked;
    sigemptyset(&blocked);
    sigprocmask(SIG_SETMASK, &blocked, NULL);

    pid_t child = fork();
    if (child == 0) {
        /* child: park until killed */
        for (;;) pause();
    }
    if (child < 0) { perror("fork"); return 2; }

    int rc = kill(child, SIGKILL);
    int killed_err = errno;
    int status = 0;
    pid_t got = wait4(child, &status, 0, NULL);
    if (got < 0) { perror("wait4"); return 2; }

    int signaled = WIFSIGNALED(status);
    int termsig = WIFSIGNALED(status) ? WTERMSIG(status) : -1;
    printf("kill_rc=%d kill_errno=%d wait_pid=%d signaled=%dtermsig=%d\n",
           rc, killed_err, (int)got, signaled, termsig);
    return 0;
}
