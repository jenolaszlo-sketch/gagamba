#define _GNU_SOURCE
// Disposable P04 only: resource/writable-storage bounds for the frozen P02/P03
// composition. No production code. Modes run one bounded case each; every case
// touches what it measures (untouched mappings are non-evidence under
// overcommit). alarm(12) backstops every run. argv: mode cg root nonce input N.
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
#include <pthread.h>
#include <sys/mman.h>
#include <sys/mount.h>
#include <sys/prctl.h>
#include <sys/syscall.h>
#include <sys/wait.h>
#include <sys/stat.h>
#include <sys/shm.h>
#include <sys/ipc.h>

static void result(const char *op, long rc) { int e = rc < 0 ? errno : 0; printf("%s rc=%ld errno=%d\n", op, rc, e); fflush(stdout); }
static long rss_kb(void) {
    FILE *f = fopen("/proc/self/status", "r");
    if (!f) return -1;
    char line[256]; long rss = -1;
    while (fgets(line, sizeof line, f)) {
        if (!strncmp(line, "VmRSS:", 6)) { rss = atol(line + 6); break; }
    }
    fclose(f); return rss;
}
static int task_count(void) {
    DIR *d = opendir("/proc/self/task");
    if (!d) return -1;
    int n = 0; struct dirent *e;
    while ((e = readdir(d))) if (e->d_name[0] != '.') n++;
    closedir(d); return n;
}
static void *touch1m(void *a) {
    (void)a;
    char *p = mmap(NULL, 1 << 20, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
    if (p == MAP_FAILED) return NULL;
    unsigned long sum = 0;
    for (size_t i = 0; i < (1 << 20); i += 4096) { p[i] = (char)(i >> 12); sum += (unsigned char)p[i]; }
    printf("thread_touched %lu\n", sum);
    sleep(1);
    munmap(p, 1 << 20);
    return (void *)1;
}

int main(int argc, char **argv) {
    setbuf(stdout, NULL); alarm(12);
    if (argc != 7) { printf("argc=%d\n", argc); return 2; }
    const char *mode = argv[1], *cg = argv[2], *nonce = argv[4];
    long N = atol(argv[6]);
    printf("identity uid=%d gid=%d pid=%d nnp=%d\n", getuid(), getgid(), getpid(),
           prctl(PR_GET_NO_NEW_PRIVS, 0, 0, 0, 0));
    int f = open(argv[5], O_RDONLY | O_CLOEXEC); result("input", f);
    if (f >= 0) { char b[128]; result("input_read", read(f, b, sizeof b)); close(f); }

    if (!strcmp(mode, "mem")) {
        size_t bytes = (size_t)N * 1048576UL;
        char *p = mmap(NULL, bytes, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS, -1, 0);
        result("mmap", p == MAP_FAILED ? -1 : 0);
        if (p != MAP_FAILED) {
            unsigned long sum = 0;
            for (size_t i = 0; i < bytes; i += 4096) { p[i] = (char)(i >> 12); sum += (unsigned char)p[i]; }
            printf("touched_sum %lu rss_kb %ld\n", sum, rss_kb());
            sleep(1);
        }
    } else if (!strcmp(mode, "threads")) {
        pthread_t *th = calloc((size_t)(N > 0 ? N : 1), sizeof(pthread_t));
        long created = 0; int fail_errno = 0;
        for (long i = 0; i < N; i++) {
            if (pthread_create(&th[i], NULL, touch1m, NULL) != 0) { fail_errno = errno; break; }
            created++;
        }
        printf("threads_created %ld fail_errno %d task_count %d\n", created, fail_errno, task_count());
        for (long i = 0; i < created; i++) pthread_join(th[i], NULL);
        free(th);
    } else if (!strcmp(mode, "scratch-fill")) {
        long total = 0, count = 0;
        char *blk = malloc(1 << 20); memset(blk, 0xA5, 1 << 20);
        for (int i = 0; i < 16; i++) {
            char path[64]; snprintf(path, sizeof path, "/tmp/fill-%d", i);
            int w = open(path, O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600);
            if (w < 0) { result("fill_open", w); break; }
            ssize_t n = write(w, blk, 1 << 20); close(w);
            if (n != (1 << 20)) { result("fill_write", n); break; }
            total += n; count++;
        }
        printf("scratch_bytes %ld scratch_files %ld\n", total, count);
        for (int i = 0; i < count; i++) {
            char path[64]; snprintf(path, sizeof path, "/tmp/fill-%d", i);
            unlink(path);
        }
        result("scratch_cleanup", 0);
        free(blk);
    } else if (!strcmp(mode, "inode-fill")) {
        long count = 0;
        for (int i = 0; i < 20000; i++) {
            char path[64]; snprintf(path, sizeof path, "/tmp/inode-%d", i);
            int w = open(path, O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600);
            if (w < 0) { result("inode_open", w); break; }
            close(w); count++;
        }
        printf("inode_count %ld\n", count);
        for (int i = 0; i < count; i++) {
            char path[64]; snprintf(path, sizeof path, "/tmp/inode-%d", i);
            unlink(path);
        }
        result("inode_cleanup", 0);
    } else if (!strcmp(mode, "fd-consume")) {
        int fds[1024]; long count = 0;
        for (int i = 0; i < 1024; i++) {
            fds[i] = open("/dev/null", O_RDONLY | O_CLOEXEC);
            if (fds[i] < 0) { result("fd_open", fds[i]); break; }
            count++;
        }
        printf("fd_count %ld\n", count);
        for (long i = 0; i < count; i++) close(fds[i]);
    } else if (!strcmp(mode, "shm-probe")) {
        int id = shmget(IPC_PRIVATE, 16 << 20, IPC_CREAT | 0600); result("shmget", id);
        if (id >= 0) {
            char *p = shmat(id, NULL, 0); result("shmat", p == (void *)-1 ? -1 : 0);
            if (p != (void *)-1) {
                unsigned long sum = 0;
                for (size_t i = 0; i < (16 << 20); i += 4096) { p[i] = (char)(i >> 12); sum += (unsigned char)p[i]; }
                printf("shm_touched %lu rss_kb %ld\n", sum, rss_kb());
                shmdt(p);
            }
            result("shm_rmid", shmctl(id, IPC_RMID, NULL));
        }
    } else if (!strcmp(mode, "shm-path")) {
        char path[64]; snprintf(path, sizeof path, "/dev/shm/p04-%d", getpid());
        int w = open(path, O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600); result("shm_open", w);
        if (w >= 0) {
            char blk[4096]; memset(blk, 0x5A, sizeof blk);
            long total = 0;
            for (int i = 0; i < 256; i++) {
                ssize_t n = write(w, blk, sizeof blk);
                if (n != (ssize_t)sizeof blk) { result("shm_write", n); break; }
                total += n;
            }
            printf("shm_bytes %ld path %s\n", total, path);
            close(w);
            // Left in place deliberately: teardown must destroy it (private
            // shm) or the host oracle finds it (shared shm => finding).
        }
    } else if (!strcmp(mode, "oomscore")) {
        f = open("/proc/self/oom_score_adj", O_RDONLY | O_CLOEXEC); result("oom_read", f);
        if (f >= 0) { char b[32]; result("oom_readval", read(f, b, sizeof b)); close(f); }
        f = open("/proc/self/oom_score_adj", O_WRONLY | O_CLOEXEC); result("oom_open_w", f);
        if (f >= 0) {
            result("oom_lower", write(f, "-500", 4));
            lseek(f, 0, SEEK_SET);
            result("oom_raise", write(f, "500", 3));
            close(f);
        }
    } else if (!strcmp(mode, "mountinfo")) {
        f = open("/proc/self/mountinfo", O_RDONLY | O_CLOEXEC); result("mountinfo", f);
        if (f >= 0) {
            char b[8192]; ssize_t n = read(f, b, sizeof b - 1);
            result("mountinfo_read", n);
            if (n > 0) { b[n] = 0; printf("mounts<<\n%s\n>>\n", b); }
            close(f);
        }
    } else if (!strcmp(mode, "emptydir-probe")) {
        int a = open("/sys/p04probe", O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600);
        result("sys_create", a);
        if (a >= 0) { close(a); result("sys_remove", unlink("/sys/p04probe")); }
        int b = open("/run/p04probe", O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600);
        result("run_create", b);
        if (b >= 0) { close(b); result("run_remove", unlink("/run/p04probe")); }
    } else if (!strcmp(mode, "big-output")) {
        char blk[4096]; memset(blk, 'Q', sizeof blk);
        long total = 0;
        for (int i = 0; i < 256; i++) {
            ssize_t n = write(1, blk, sizeof blk);
            if (n != (ssize_t)sizeof blk) break;
            total += n;
            if (i % 16 == 15) printf("progress %ld\n", total);
        }
        printf("emitted_stdout %ld\n", total);
        for (int i = 0; i < 16; i++) {
            ssize_t n = write(2, blk, sizeof blk);
            if (n != (ssize_t)sizeof blk) break;
        }
        return 17;
    } else if (!strcmp(mode, "crash-abort")) {
        f = open("crash-marker", O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600);
        result("marker_write", f);
        if (f >= 0) { write(f, nonce, strlen(nonce)); close(f); }
        fflush(stdout); abort();
    } else if (!strcmp(mode, "null-writes")) {
        char blk[4096]; memset(blk, 0, sizeof blk);
        f = open("/dev/null", O_WRONLY | O_CLOEXEC); result("null_open", f);
        if (f >= 0) {
            long total = 0;
            for (int i = 0; i < 256; i++) {
                ssize_t n = write(f, blk, sizeof blk);
                if (n != (ssize_t)sizeof blk) break;
                total += n;
            }
            printf("null_bytes %ld\n", total); close(f);
        }
        f = open("/dev/zero", O_RDONLY | O_CLOEXEC); result("zero_open", f);
        if (f >= 0) {
            char b[4096]; long total = 0;
            for (int i = 0; i < 256; i++) {
                ssize_t n = read(f, b, sizeof b);
                if (n != (ssize_t)sizeof b) break;
                total += n;
            }
            printf("zero_bytes %ld\n", total); close(f);
        }
        f = open(argv[5], O_WRONLY | O_CLOEXEC); result("rofile_write", f);
        if (f >= 0) close(f);
        f = open("/sys/x", O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600);
        result("sys_write", f);
        if (f >= 0) { close(f); unlink("/sys/x"); }
    } else { printf("unknown-mode\n"); return 2; }

    // Descriptor inventory: only prepared stdio may appear at case end.
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
