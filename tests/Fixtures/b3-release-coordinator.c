/* B3 experimental qualification coordinator. Test evidence only, never
   selected by production. It performs the trusted setup report (like the
   WP04 fixture), consumes exactly one validated 128-byte RELEASE frame, then
   forks and execs the fixed /tool target inside the same restricted domain,
   reports ExecConfirmed/ExecFailed (sequence 2) and the target raw wait
   status (sequence 3, RootWait), and exits. The target inherits fd 1/2 so its
   bounded output reaches the owner's capture pipes. */
#define _GNU_SOURCE
#include <errno.h>
#include <fcntl.h>
#include <limits.h>
#include <poll.h>
#include <signal.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/prctl.h>
#include <sys/resource.h>
#include <sys/stat.h>
#include <sys/syscall.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <time.h>
#include <unistd.h>
#include "release_protocol.h"
#include "b3-exec-confirm.h"

#define CONFIG_TOOL_DEV 98u
#define CONFIG_TOOL_INO 106u
#define CONFIG_INPUT_DEV 114u
#define CONFIG_INPUT_INO 122u
#define CONFIG_COORD_DEV 130u
#define CONFIG_COORD_INO 138u

static unsigned char cfg[202];
static const unsigned char *exec_id, *prep_id, *generation, *digest;
static uint64_t deadline_ns;

static void put16(unsigned char *p, uint16_t value)
{
    p[0] = (unsigned char)value;
    p[1] = (unsigned char)(value >> 8);
}

static void put32(unsigned char *p, uint32_t value)
{
    for (unsigned i = 0; i < 4; ++i) p[i] = (unsigned char)(value >> (8 * i));
}

static void put64(unsigned char *p, uint64_t value)
{
    for (unsigned i = 0; i < 8; ++i) p[i] = (unsigned char)(value >> (8 * i));
}

static uint64_t now_ns(void)
{
    struct timespec value;
    if (clock_gettime(CLOCK_MONOTONIC, &value) != 0) return 0;
    return (uint64_t)value.tv_sec * 1000000000ull + (uint64_t)value.tv_nsec;
}

static int read_text(const char *path, char *buffer, size_t capacity, size_t *length)
{
    int fd = open(path, O_RDONLY | O_CLOEXEC);
    if (fd < 0) return -1;
    size_t used = 0;
    for (;;)
    {
        ssize_t count = read(fd, buffer + used, capacity - used - 1);
        if (count < 0 && errno == EINTR) continue;
        if (count < 0) { close(fd); return -1; }
        if (count == 0) break;
        used += (size_t)count;
        if (used + 1 >= capacity) { close(fd); return -1; }
    }
    close(fd);
    buffer[used] = '\0';
    *length = used;
    return 0;
}

static uint64_t status_number(const char *name, int base)
{
    char status[8192];
    size_t length = 0;
    if (read_text("/proc/self/status", status, sizeof(status), &length) != 0) return UINT64_MAX;
    size_t name_length = strlen(name);
    for (char *line = status; line < status + length;)
    {
        char *end = strchr(line, '\n');
        if (end == NULL) end = status + length;
        if ((size_t)(end - line) > name_length && memcmp(line, name, name_length) == 0 && line[name_length] == ':')
        {
            char *value = line + name_length + 1;
            while (value < end && (*value == ' ' || *value == '\t')) ++value;
            char saved = *end;
            *end = '\0';
            errno = 0;
            unsigned long long parsed = strtoull(value, NULL, base);
            *end = saved;
            return errno == 0 ? (uint64_t)parsed : UINT64_MAX;
        }
        line = end < status + length ? end + 1 : status + length;
    }
    return UINT64_MAX;
}

static int write_all(int fd, const unsigned char *bytes, size_t length)
{
    size_t offset = 0;
    while (offset < length)
    {
        ssize_t count = write(fd, bytes + offset, length - offset);
        if (count < 0 && errno == EINTR) continue;
        if (count <= 0) return -1;
        offset += (size_t)count;
    }
    return 0;
}

static int write_frame(uint16_t kind, uint64_t sequence, uint32_t payload)
{
    unsigned char frame[100];
    size_t payload_bytes = kind == GGM1_KIND_EXEC_CONFIRMED ? 0 : 4;
    size_t total = GGM1_HEADER_BYTES + payload_bytes;
    memset(frame, 0, sizeof(frame));
    memcpy(frame, GGM1_MAGIC, 8);
    put16(frame + 8, GGM1_VERSION);
    put16(frame + 10, kind);
    put32(frame + 12, (uint32_t)total);
    put64(frame + 16, sequence);
    memcpy(frame + 24, exec_id, 16);
    memcpy(frame + 40, prep_id, 16);
    memcpy(frame + 56, digest, 32);
    if (payload_bytes) put32(frame + GGM1_HEADER_BYTES, payload);
    return write_all(5, frame, total);
}

static int fail(uint32_t code, uint64_t sequence)
{
    (void)write_frame(GGM1_KIND_PROTOCOL_FAILED, sequence, code);
    return 120 + (int)(code % 20);
}

static int load_config(void)
{
    if (pread(3, cfg, sizeof(cfg), 0) != (ssize_t)sizeof(cfg)
        || memcmp(cfg, "GGM1CFG\0", 8) != 0) return -1;
    exec_id = cfg + 18;
    prep_id = cfg + 34;
    generation = cfg + 50;
    digest = cfg + 66;
    deadline_ns = 0;
    for (unsigned i = 0; i < 8; ++i) deadline_ns |= (uint64_t)cfg[194 + i] << (8 * i);
    return deadline_ns == 0 ? -1 : 0;
}

static int write_held_report(void)
{
    struct rlimit nofile = { .rlim_cur = 64, .rlim_max = 64 };
    struct rlimit core = { .rlim_cur = 0, .rlim_max = 0 };
    (void)setrlimit(RLIMIT_NOFILE, &nofile);
    (void)setrlimit(RLIMIT_CORE, &core);
    (void)prctl(PR_SET_NO_NEW_PRIVS, 1, 0, 0, 0);
    (void)prctl(PR_SET_DUMPABLE, 0, 0, 0, 0);

    char cgroup[512], mountinfo[3900];
    size_t cgroup_length = 0, mount_length = 0;
    if (read_text("/proc/self/cgroup", cgroup, sizeof(cgroup), &cgroup_length) != 0
        || read_text("/proc/self/mountinfo", mountinfo, sizeof(mountinfo), &mount_length) != 0) return 83;
    while (cgroup_length > 0 && (cgroup[cgroup_length - 1] == '\n' || cgroup[cgroup_length - 1] == '\r'))
        cgroup[--cgroup_length] = '\0';

    struct rlimit actual_nofile, actual_core;
    if (getrlimit(RLIMIT_NOFILE, &actual_nofile) != 0 || getrlimit(RLIMIT_CORE, &actual_core) != 0) return 84;
    uint64_t nnp = status_number("NoNewPrivs", 10);
    uint64_t seccomp = status_number("Seccomp", 10);
    uint64_t cap_eff = status_number("CapEff", 16);
    uint64_t cap_prm = status_number("CapPrm", 16);
    uint64_t cap_amb = status_number("CapAmb", 16);
    const char *namespace_names[] = { "user", "mnt", "pid", "net", "ipc", "uts" };
    uint64_t namespace_inodes[6];
    for (size_t i = 0; i < 6; ++i)
    {
        char path[64];
        struct stat info;
        snprintf(path, sizeof(path), "/proc/self/ns/%s", namespace_names[i]);
        if (stat(path, &info) != 0 || info.st_ino == 0) return 86;
        namespace_inodes[i] = (uint64_t)info.st_ino;
    }

    unsigned char payload[4000];
    memset(payload, 0, sizeof(payload));
    put16(payload, 1);
    put16(payload + 2, 0x000f);
    put32(payload + 4, (uint32_t)getuid());
    put32(payload + 8, (uint32_t)geteuid());
    put32(payload + 12, (uint32_t)getgid());
    put32(payload + 16, (uint32_t)getegid());
    put32(payload + 20, (uint32_t)getpid());
    put32(payload + 24, (uint32_t)nnp);
    put32(payload + 28, (uint32_t)seccomp);
    put32(payload + 32, (uint32_t)prctl(PR_GET_DUMPABLE, 0, 0, 0, 0));
    put64(payload + 40, cap_eff);
    put64(payload + 48, cap_prm);
    put64(payload + 56, cap_amb);
    put64(payload + 64, (uint64_t)actual_nofile.rlim_cur);
    put64(payload + 72, (uint64_t)actual_nofile.rlim_max);
    put64(payload + 80, (uint64_t)actual_core.rlim_cur);
    put64(payload + 88, (uint64_t)actual_core.rlim_max);
    for (size_t i = 0; i < 6; ++i) put64(payload + 96 + i * 8, namespace_inodes[i]);
    for (size_t i = 0; i < 3; ++i)
    {
        put64(payload + 144 + i * 16, *(uint64_t *)(cfg + CONFIG_TOOL_DEV + i * 16));
        put64(payload + 152 + i * 16, *(uint64_t *)(cfg + CONFIG_TOOL_INO + i * 16));
    }
    if (194 + cgroup_length + 2 + mount_length > sizeof(payload)
        || cgroup_length > UINT16_MAX || mount_length > UINT16_MAX) return 89;
    put16(payload + 192, (uint16_t)cgroup_length);
    memcpy(payload + 194, cgroup, cgroup_length);
    put16(payload + 194 + cgroup_length, (uint16_t)mount_length);
    memcpy(payload + 196 + cgroup_length, mountinfo, mount_length);
    size_t payload_length = 196 + cgroup_length + mount_length;

    unsigned char frame[4096];
    memset(frame, 0, sizeof(frame));
    memcpy(frame, GGM1_MAGIC, 8);
    put16(frame + 8, GGM1_VERSION);
    put16(frame + 10, GGM1_KIND_HELD);
    put32(frame + 12, (uint32_t)(96 + payload_length));
    put64(frame + 16, 1);
    memcpy(frame + 24, exec_id, 16);
    memcpy(frame + 40, prep_id, 16);
    memcpy(frame + 56, digest, 32);
    memcpy(frame + 96, payload, payload_length);
    if (write_all(5, frame, 96 + payload_length) != 0) return 90;
    return 0;
}

/* Returns 0 after ExecConfirmed was written; the child pid is stored. */
static int launch_target(int *child_pid, uint32_t *exec_errno)
{
    int confirm[2];
    if (pipe2(confirm, O_CLOEXEC) != 0) return -1;

    pid_t pid = fork();
    if (pid < 0)
    {
        close(confirm[0]);
        close(confirm[1]);
        return -1;
    }

    if (pid == 0)
    {
        close(confirm[0]);
        close(3); /* config memfd */
        close(5); /* coordinator status writer stays with the coordinator */
        /* pipe2 atomically sets CLOEXEC. Keep it set through execve. */
        char *argv[] = { (char *)"/tool", NULL };
        char *envp[] = { NULL };
        execve("/tool", argv, envp);
        uint32_t failure = (uint32_t)errno;
        ssize_t written;
        do { written = write(confirm[1], &failure, sizeof(failure)); } while (written < 0 && errno == EINTR);
        (void)written;
        _exit(127);
    }

    close(confirm[1]);
    *child_pid = pid;
    uint64_t tool_dev = 0, tool_ino = 0;
    for (unsigned i = 0; i < 8; ++i) {
        tool_dev |= (uint64_t)cfg[CONFIG_TOOL_DEV + i] << (8 * i);
        tool_ino |= (uint64_t)cfg[CONFIG_TOOL_INO + i] << (8 * i);
    }
    int observed = b3_observe_exec(pid, confirm[0], (dev_t)tool_dev, (ino_t)tool_ino, deadline_ns, exec_errno);
    close(confirm[0]);
    if (observed < 0) {
        /* Unknown exec is not Confirmed. Collect only this direct fixture child. */
        (void)kill(pid, SIGKILL);
        while (waitpid(pid, NULL, 0) < 0 && errno == EINTR) { }
    }
    /* The caller collects the root status once, including execve failure. */
    return observed;
}

static int wait_target(pid_t pid, int *raw_status)
{
    for (;;)
    {
        int status = 0;
        pid_t result = waitpid(pid, &status, WNOHANG);
        if (result == pid)
        {
            *raw_status = status;
            return 0;
        }
        if (result < 0 && errno != EINTR) return -1;
        uint64_t now = now_ns();
        if (now == 0 || now >= deadline_ns)
        {
            (void)kill(pid, SIGKILL);
            while (waitpid(pid, &status, 0) < 0 && errno == EINTR) { }
            *raw_status = status;
            return 0;
        }
        struct pollfd idle = { .fd = -1, .events = 0 };
        (void)poll(&idle, 0, 10);
    }
}

int main(void)
{
    if (load_config() != 0) return 80;
    if (write_held_report() != 0) return 81;

    /* Close the filter/status descriptors exactly as the pinned policy
       permits; fd 4 (control) and fd 5 (status) stay live. */
    if (syscall(SYS_close_range, 6u, UINT_MAX, 0u) != 0 && errno != ENOSYS) return 82;

    unsigned char frame[GGM1_RELEASE_BYTES];
    size_t got = 0;
    for (;;)
    {
        uint64_t now = now_ns();
        if (now == 0 || now >= deadline_ns) return fail(30, 2);
        uint64_t remaining_ms = (deadline_ns - now) / 1000000ull;
        struct pollfd control = { .fd = 4, .events = POLLIN | POLLHUP | POLLERR };
        int ready = poll(&control, 1, remaining_ms > 250 ? 250 : (int)remaining_ms);
        if (ready < 0 && errno == EINTR) continue;
        if (ready < 0) return fail(31, 2);
        if (ready == 0) continue;
        if (control.revents & POLLIN)
        {
            while (got < sizeof(frame))
            {
                ssize_t count = read(4, frame + got, sizeof(frame) - got);
                if (count < 0 && errno == EINTR) continue;
                if (count <= 0) return fail(33, 2);
                got += (size_t)count;
            }
            struct pollfd extra = { .fd = 4, .events = POLLIN };
            int have_extra = poll(&extra, 1, 0);
            if (have_extra < 0) return fail(34, 2);
            if (have_extra > 0 && (extra.revents & POLLIN)) return fail(35, 2);
            if (!ggm1_release_frame_valid(frame, sizeof(frame), exec_id, prep_id, digest,
                    generation, deadline_ns, now_ns())) return fail(36, 2);
            break;
        }
        return fail(32, 2);
    }

    close(4);
    int child = 0;
    uint32_t exec_errno = 0;
    int launch = launch_target(&child, &exec_errno);
    if (launch < 0) return fail(40, 2);
    if (launch == 1)
    {
        int raw = 0;
        if (wait_target((pid_t)child, &raw) != 0) return fail(41, 2);
        if (write_frame(GGM1_KIND_EXEC_FAILED, 2, exec_errno) != 0) return 114;
        if (write_frame(GGM1_KIND_ROOT_WAIT, 3, (uint32_t)raw) != 0) return 114;
        (void)close(5);
        return 0;
    }

    if (write_frame(GGM1_KIND_EXEC_CONFIRMED, 2, 0) != 0) return 114;
    int raw = 0;
    if (wait_target((pid_t)child, &raw) != 0) return fail(42, 2);
    if (write_frame(GGM1_KIND_ROOT_WAIT, 3, (uint32_t)raw) != 0) return 114;
    (void)close(5);
    return 0;
}
