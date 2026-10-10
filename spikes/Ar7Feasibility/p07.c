#define _GNU_SOURCE
// Disposable P07 only: crash/ownership/release-semantics worker for the frozen
// P02/P03/P04 composition. No production code. Every mode is bounded;
// alarm(12) backstops every run. argv: mode cg root nonce input N.
// Startup barrier: modes that release a runnable root print READY + identity,
// then block on one stdin byte (GO) from the manager. The manager validates
// every PID externally (cgroup membership + stat starttime + cmdline) before
// writing GO. Modes `hold`/`hang` intentionally skip the barrier (C4/C9
// victims): `hold` prints RELEASED then sleeps; `hang` never prints.
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <errno.h>
#include <unistd.h>
#include <fcntl.h>
#include <signal.h>
#include <sched.h>
#include <dirent.h>
#include <sys/mman.h>
#include <sys/mount.h>
#include <sys/prctl.h>
#include <sys/syscall.h>
#include <sys/wait.h>
#include <sys/stat.h>

#ifndef PR_SET_PDEATHSIG
#define PR_SET_PDEATHSIG 1
#endif

static void result(const char *op, long rc) { int e = rc < 0 ? errno : 0; printf("%s rc=%ld errno=%d\n", op, rc, e); fflush(stdout); }

// Detach a finite child's stdout so the manager's pipe observes the ROOT's
// lifetime only (C7 pattern): root exit must be visible while the domain is
// still non-empty. pipeholder's child intentionally keeps stdout (C8 test).
static void detach_stdout(void) {
    int dn = open("/dev/null", O_WRONLY | O_CLOEXEC);
    if (dn >= 0) { dup2(dn, 1); close(dn); }
}

static void wait_go(void) {
    printf("READY\n"); fflush(stdout);
    char b = 0;
    ssize_t n = read(0, &b, 1);
    printf("go_read rc=%zd byte=%d\n", n, (int)(unsigned char)b); fflush(stdout);
    if (n != 1) _exit(126);
}

int main(int argc, char **argv) {
    setbuf(stdout, NULL); alarm(12);
    if (argc != 7) { printf("argc=%d\n", argc); return 2; }
    const char *mode = argv[1], *cg = argv[2], *nonce = argv[4];
    long N = atol(argv[6]);
    (void)nonce;
    printf("identity uid=%d gid=%d pid=%d ppid=%d nnp=%d\n", getuid(), getgid(), getpid(), getppid(),
           prctl(PR_GET_NO_NEW_PRIVS, 0, 0, 0, 0));
    int f = open(argv[5], O_RDONLY | O_CLOEXEC); result("input", f);
    if (f >= 0) { char b[128]; result("input_read", read(f, b, sizeof b)); close(f); }

    if (!strcmp(mode, "sleeper")) {
        wait_go();
        pid_t c = fork(); result("fork", c);
        if (c == 0) { detach_stdout(); sleep((unsigned int)(N > 0 ? N : 1)); _exit(0); }
        printf("child %d sleep %ld\n", (int)c, N);
        goto trailer; // root exits while child sleeps (C7 pattern)
    } else if (!strcmp(mode, "root-hold")) {
        wait_go();
        printf("root_holding %ld\n", N);
        sleep((unsigned int)(N > 0 ? N : 30)); // long-lived root, no children (C6/C10 victim)
        return 0;
    } else if (!strcmp(mode, "holdchild")) {
        wait_go();
        pid_t c = fork(); result("fork", c);
        if (c == 0) { detach_stdout(); sleep((unsigned int)(N > 0 ? N : 30)); _exit(0); }
        printf("child %d sleep %ld root_holding\n", (int)c, N);
        sleep((unsigned int)(N > 0 ? N : 30)); // root AND descendant alive together (R3)
        goto trailer;
    } else if (!strcmp(mode, "trickle")) {
        wait_go();
        // R4: bounded incremental spawner. SIGPIPE ignored so the spawn loop
        // survives the manager closing the pipe (stop must come from the
        // lifecycle domain, never from pipe fate).
        signal(SIGPIPE, SIG_IGN);
        long want = N > 0 ? N : 30;
        if (want > 30) want = 30;
        long made = 0; int fail_errno = 0;
        for (long i = 0; i < want; i++) {
            pid_t c = fork();
            if (c < 0) { fail_errno = errno; break; }
            if (c == 0) { detach_stdout(); sleep(10); _exit((int)i); }
            made++;
            printf("spawned %ld\n", made);
            usleep(100000);
        }
        printf("trickle_done %ld fail_errno %d\n", made, fail_errno);
        int st = 0;
        while (waitpid(-1, &st, 0) > 0) {}
        goto trailer;
    } else if (!strcmp(mode, "pipeholder")) {
        wait_go();
        pid_t c = fork(); result("fork", c);
        if (c == 0) { sleep((unsigned int)(N > 0 ? N : 5)); printf("holder_done\n"); _exit(0); }
        printf("child %d holds_pipe %ld\n", (int)c, N);
        goto trailer; // root exits; inherited stdout keeps manager draining (C8)
    } else if (!strcmp(mode, "exit17")) {
        wait_go();
        return 17;
    } else if (!strcmp(mode, "daemonizer")) {
        wait_go();
        pid_t c = fork(); result("fork1", c);
        if (c == 0) {
            setsid();
            pid_t g = fork(); result("fork2", g);
            if (g == 0) { detach_stdout(); printf("generation daemon sleep %ld\n", N); sleep((unsigned int)(N > 0 ? N : 6)); _exit(0); }
            _exit(0); // intermediate exits: grandchild reparented, same leaf
        }
        int st = 0; waitpid(c, &st, 0);
        printf("intermediate_reaped status=%d\n", st);
        goto trailer;
    } else if (!strcmp(mode, "forker")) {
        wait_go();
        long want = N > 0 ? N : 5;
        if (want > 60) want = 60;
        long made = 0; int fail_errno = 0;
        for (long i = 0; i < want; i++) {
            pid_t c = fork();
            if (c < 0) { fail_errno = errno; break; }
            if (c == 0) { detach_stdout(); sleep(3); _exit((int)i); }
            made++;
        }
        printf("fork_made %ld fail_errno %d\n", made, fail_errno);
        int st = 0; long reaped = 0;
        while (waitpid(-1, &st, 0) > 0) reaped++;
        printf("fork_reaped %ld\n", reaped);
        goto trailer;
    } else if (!strcmp(mode, "reexec")) {
        wait_go();
        printf("generation first exec_self\n");
        char *const av[] = { argv[0], "reexeced", argv[2], argv[3], argv[4], argv[5], argv[6], NULL };
        execv(argv[0], av);
        result("execv", -1);
        return 3;
    } else if (!strcmp(mode, "reexeced")) {
        printf("generation second running\n");
        goto trailer;
    } else if (!strcmp(mode, "interfere")) {
        wait_go();
        char path[1024];
        snprintf(path, sizeof path, "%s/manager/cgroup.procs", cg);
        int m = open(path, O_WRONLY | O_CLOEXEC); result("cg_manager_open", m);
        if (m >= 0) { char pb[32]; int l = snprintf(pb, sizeof pb, "%d", getpid()); result("cg_manager_write", write(m, pb, (size_t)l)); close(m); }
        snprintf(path, sizeof path, "%s/sibling/cgroup.procs", cg);
        int s = open(path, O_WRONLY | O_CLOEXEC); result("cg_sibling_open", s);
        if (s >= 0) { char pb[32]; int l = snprintf(pb, sizeof pb, "%d", getpid()); result("cg_sibling_write", write(s, pb, (size_t)l)); close(s); }
        int o = open("/sys/fs/cgroup/cgroup.procs", O_WRONLY | O_CLOEXEC); result("cg_host_open", o);
        if (o >= 0) close(o);
        int ns = open("/proc/1/ns/pid", O_RDONLY | O_CLOEXEC); result("host_ns_open", ns);
        if (ns >= 0) { result("setns_host", setns(ns, CLONE_NEWPID)); close(ns); }
        result("mkdir_mnt", mkdir("/tmp/mnt-p07", 0700));
        result("mount_tmpfs", mount("none", "/tmp/mnt-p07", "tmpfs", 0, ""));
        // R7-cleanup: the mount attempt is expected to fail; never leave the
        // fixture mountpoint behind on the host (positives run unconfined).
        result("rmdir_mnt", rmdir("/tmp/mnt-p07"));
        result("pdeathsig_set", prctl(PR_SET_PDEATHSIG, SIGKILL, 0, 0, 0));
        int sig = 0; result("pdeathsig_get", prctl(PR_GET_PDEATHSIG, (unsigned long)&sig, 0, 0, 0));
        printf("pdeathsig_val %d\n", sig);
        result("pdeathsig_clear", prctl(PR_SET_PDEATHSIG, 0, 0, 0, 0));
        goto trailer;
    } else if (!strcmp(mode, "gate")) {
        // R2-replacement pre-target exec gate: test-only instrumentation, not
        // a confinement mechanism. Bubblewrap execs THIS as the namespace
        // root. It records PRETARGET_GATE_REACHED, blocks on the
        // controller-owned stdin channel, performs no fork/spawn/exec while
        // blocked, and execves the actual target ONLY on the exact expected
        // nonce. Any received bytes are echoed as AUTH_RECEIVED first, so the
        // absence of that marker proves no authorization ever arrived.
        printf("PRETARGET_GATE_REACHED nonce=%s\n", nonce);
        char ab[128]; size_t al = 0; int ch;
        while (al + 1 < sizeof ab && (ch = getchar()) != EOF && ch != '\n') ab[al++] = (char)ch;
        ab[al] = 0;
        if (al > 0 || ch != EOF) printf("AUTH_RECEIVED bytes=%zu\n", al);
        else { printf("GATE_REFUSED eof\n"); return 125; }
        if (!strcmp(ab, nonce)) {
            printf("GATE_AUTH_OK exec_target\n");
            char *const av[] = { argv[0], "spinwork", argv[2], argv[3], argv[4], argv[5], "0", NULL };
            execv("/target", av);
            result("execv_target", -1);
            return 3;
        }
        printf("GATE_REFUSED mismatch\n");
        return 125;
    } else if (!strcmp(mode, "spinwork")) {
        // Barrier-free bounded useful work for the gate's post-release exec:
        // touches 16 MiB across 16 mappings, prints a checksum, exits via the
        // trailer. Needs no stdin, forks nothing.
        unsigned long sum = 0;
        for (long i = 0; i < 16; i++) {
            char *pg = mmap(NULL, 1 << 20, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
            if (pg == MAP_FAILED) { result("spin_mmap", -1); break; }
            for (size_t j = 0; j < (1 << 20); j += 4096) { pg[j] = (char)(i + (j >> 12)); sum += (unsigned char)pg[j]; }
            munmap(pg, 1 << 20);
        }
        printf("spinwork_sum %lu\n", sum);
        { FILE *sf = fopen("/proc/self/status", "r");
          int sm = -1, nf = -1;
          if (sf) { char sl[256];
            while (fgets(sl, sizeof sl, sf)) {
              if (!strncmp(sl, "Seccomp:", 8)) sm = atoi(sl + 8);
              else if (!strncmp(sl, "Seccomp_filters:", 16)) nf = atoi(sl + 16); }
            fclose(sf); }
          printf("target seccomp=%d filters=%d\n", sm, nf); }
        goto trailer;
    } else if (!strcmp(mode, "hang")) {
        sleep(30); // no READY by design: C9 setup-hang victim
        return 0;
    } else if (!strcmp(mode, "hold")) {
        printf("RELEASED\n"); // C4/C11 victim marker, no barrier by design
        sleep(10); // stays under alarm(12): suite-mode C11 exits cleanly
        goto trailer;
    } else { printf("unknown-mode\n"); return 2; }

    // Descriptor inventory: only prepared stdio may appear at case end.
 trailer:
    {
        DIR *d = opendir("/proc/self/fd");
        if (d) {
            struct dirent *e;
            while ((e = readdir(d))) if (e->d_name[0] != '.') {
                char link[1024], dst[1024];
                snprintf(link, sizeof link, "/proc/self/fd/%s", e->d_name);
                ssize_t n = readlink(link, dst, sizeof(dst) - 1);
                if (n >= 0) { dst[n] = 0; printf("fd %s %s\n", e->d_name, dst); }
            }
            closedir(d);
        }
    }
    {
        char p[1024]; snprintf(p, sizeof p, "%s/sibling", cg);
        int s = open(p, O_RDONLY | O_DIRECTORY | O_CLOEXEC); result("sibling_visible", s);
        if (s >= 0) close(s);
    }
    puts("complete"); return 0;
}
