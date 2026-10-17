/* Test-only WP04 coordinator fault fixture. It never executes the target. */
#define _GNU_SOURCE
#include <fcntl.h>
#include <errno.h>
#include <limits.h>
#include <poll.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/prctl.h>
#include <sys/resource.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>

#ifndef WP04_MODE
#define WP04_MODE 0
#endif
#ifndef WP04_HOST_MNT_INODE
#define WP04_HOST_MNT_INODE 0ULL
#endif

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

static int read_text(const char *path, char *buffer, size_t capacity, size_t *length)
{
    int fd = open(path, O_RDONLY | O_CLOEXEC);
    if (fd < 0) return -1;
    size_t used = 0;
    while (used + 1 < capacity)
    {
        ssize_t count = read(fd, buffer + used, capacity - used - 1);
        if (count < 0 && errno == EINTR) continue;
        if (count < 0) { close(fd); return -1; }
        if (count == 0) break;
        used += (size_t)count;
    }
    close(fd);
    if (used + 1 >= capacity) return -1;
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
    for (char *line = status; line < status + length; )
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

static int write_held_report(void)
{
    unsigned char config[202];
    if (pread(3, config, sizeof(config), 0) != (ssize_t)sizeof(config)
        || memcmp(config, "GGM1CFG\0", 8) != 0) return 81;

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

#if WP04_MODE == 9
    char *size = strstr(mountinfo, "size=7168k");
    if (size == NULL) return 87;
    size[8] = size[8] == '8' ? '9' : '8';
#elif WP04_MODE == 10
    if (WP04_HOST_MNT_INODE == 0) return 88;
    namespace_inodes[1] = WP04_HOST_MNT_INODE;
#endif

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
        put64(payload + 144 + i * 16, *(uint64_t *)(config + 98 + i * 16));
        put64(payload + 152 + i * 16, *(uint64_t *)(config + 106 + i * 16));
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
    memcpy(frame, "GGM1CTL\0", 8);
    put16(frame + 8, 1);
    put16(frame + 10, 1);
    put32(frame + 12, (uint32_t)(96 + payload_length));
    put64(frame + 16, 1);
    memcpy(frame + 24, config + 18, 16);
    memcpy(frame + 40, config + 34, 16);
    memcpy(frame + 56, config + 66, 32);
    memcpy(frame + 96, payload, payload_length);
    if (write_all(5, frame, 96 + payload_length) != 0) return 90;
    (void)close(5);
    return 0;
}

int main(void)
{
#if WP04_MODE == 0
    /* Real process remains inside the started Bubblewrap domain without HELD. */
#elif WP04_MODE == 8
    (void)close(5);
    return 0;
#elif WP04_MODE >= 9
    int report_result = write_held_report();
    if (report_result != 0) return report_result;
#elif WP04_MODE >= 5
    unsigned char config[98];
    if (pread(3, config, sizeof(config), 0) != (ssize_t)sizeof(config)
        || memcmp(config, "GGM1CFG\0", 8) != 0) return 80;
    unsigned char frame[96];
    memset(frame, 0, sizeof(frame));
    memcpy(frame, "GGM1CTL\0", 8);
    put16(frame + 8, 1);
    put16(frame + 10, 1);
    put32(frame + 12, sizeof(frame));
    put64(frame + 16, WP04_MODE == 6 ? 2 : 1);
    memcpy(frame + 24, config + 18, 16);
    memcpy(frame + 40, config + 34, 16);
    memcpy(frame + 56, config + 66, 32);
    if (WP04_MODE == 5) frame[56] ^= 1;
    if (WP04_MODE == 7) frame[40] ^= 1;
    (void)write(5, frame, sizeof(frame));
    (void)close(5);
#else
    unsigned char frame[96];
    memset(frame, 0, sizeof(frame));
    put16(frame + 8, 1);
    put32(frame + 12, sizeof(frame));
    put16(frame + 10, WP04_MODE == 3 ? 2 : 1);
    if (WP04_MODE != 2) memcpy(frame, "GGM1CTL\0", 8);
    if (WP04_MODE >= 4) put64(frame + 16, 1);
    if (WP04_MODE == 1)
    {
        (void)write(5, frame, 12);
        (void)close(5);
    }
    else
    {
        (void)write(5, frame, sizeof(frame));
        (void)close(5);
    }
#endif
    struct pollfd control = { .fd = 4, .events = POLLIN | POLLHUP | POLLERR };
    for (;;)
    {
        int result = poll(&control, 1, -1);
        if (result < 0 && errno == EINTR) continue;
        if (result < 0) return 90;
        return 0;
    }
}
