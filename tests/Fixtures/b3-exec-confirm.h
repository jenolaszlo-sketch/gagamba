/* Test coordinator exec observation. EOF alone never proves exec. The direct,
 * unreaped child cannot recycle its PID; /proc/<child>/exe must corroborate the
 * already pinned executable object while that child is observable. Fast exit or
 * pre-exec termination may leave Unknown, which is never promoted to Confirmed. */
#ifndef GAGAMBA_B3_EXEC_CONFIRM_H
#define GAGAMBA_B3_EXEC_CONFIRM_H
#include <errno.h>
#include <fcntl.h>
#include <poll.h>
#include <stdint.h>
#include <stdio.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <time.h>
#include <unistd.h>

static uint64_t b3_exec_now(void)
{
    struct timespec ts;
    if (clock_gettime(CLOCK_MONOTONIC, &ts) != 0) return 0;
    return (uint64_t)ts.tv_sec * 1000000000ull + (uint64_t)ts.tv_nsec;
}

/* 0 = corroborated exec; 1 = exact execve errno; -1 = Unknown. Never reaps. */
static int b3_observe_exec(pid_t child, int confirmation, dev_t expected_dev,
                           ino_t expected_ino, uint64_t deadline, uint32_t *exec_errno)
{
    unsigned char bytes[sizeof(uint32_t)];
    size_t used = 0;
    for (;;)
    {
        uint64_t now = b3_exec_now();
        if (now == 0 || now >= deadline) return -1;
        uint64_t remaining = (deadline - now + 999999ull) / 1000000ull;
        struct pollfd p = { .fd = confirmation, .events = POLLIN | POLLHUP | POLLERR };
        int ready = poll(&p, 1, remaining > 100 ? 100 : (int)remaining);
        if (ready < 0 && errno == EINTR) continue;
        if (ready < 0) return -1;
        if (ready == 0) continue;
        ssize_t count = read(confirmation, bytes + used, sizeof(bytes) - used);
        if (count < 0 && errno == EINTR) continue;
        if (count < 0) return -1;
        if (count > 0)
        {
            used += (size_t)count;
            if (used != sizeof(bytes)) continue;
            uint32_t failure = 0;
            for (unsigned i = 0; i < sizeof(bytes); ++i) failure |= (uint32_t)bytes[i] << (i * 8);
            if (failure == 0 || failure > 4095) return -1;
            *exec_errno = failure;
            return 1;
        }
        if (used != 0) return -1;
        char path[64];
        int length = snprintf(path, sizeof(path), "/proc/%d/exe", (int)child);
        if (length <= 0 || (size_t)length >= sizeof(path)) return -1;
        struct stat actual;
        /* Following THIS kernel executable-object link is intentional; no caller path is trusted. */
        if (stat(path, &actual) != 0 || !S_ISREG(actual.st_mode)
            || actual.st_dev != expected_dev || actual.st_ino != expected_ino) return -1;
        *exec_errno = 0;
        return 0;
    }
}
#endif
