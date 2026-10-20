/* WP05 qualification-only RELEASE consumer. It validates the exact 128-byte
   RELEASE frame and records acceptance; it never forks or executes a target.
   This binary is test evidence only and is never selected by production. */
#define _GNU_SOURCE
#include "release_protocol.h"
#include <errno.h>
#include <fcntl.h>
#include <poll.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <unistd.h>

static int hex_value(char c)
{
    if (c >= '0' && c <= '9') return c - '0';
    if (c >= 'a' && c <= 'f') return c - 'a' + 10;
    if (c >= 'A' && c <= 'F') return c - 'A' + 10;
    return -1;
}

static int decode_hex(const char *text, unsigned char *out, size_t length)
{
    if (text == NULL || strlen(text) != length * 2) return -1;
    for (size_t i = 0; i < length; i++)
    {
        int high = hex_value(text[i * 2]);
        int low = hex_value(text[i * 2 + 1]);
        if (high < 0 || low < 0) return -1;
        out[i] = (unsigned char)((high << 4) | low);
    }
    return 0;
}

static uint64_t now_ns(void)
{
    struct timespec value;
    if (clock_gettime(CLOCK_MONOTONIC, &value) != 0) return 0;
    return (uint64_t)value.tv_sec * 1000000000ull + (uint64_t)value.tv_nsec;
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

static int duplicate_to(int source, int target)
{
    if (source == target) return 0;
    if (dup2(source, target) < 0) return -1;
    close(source);
    return 0;
}

static void put16(unsigned char *p, uint16_t value)
{
    p[0] = (unsigned char)value;
    p[1] = (unsigned char)(value >> 8);
}

static void put32(unsigned char *p, uint32_t value)
{
    for (unsigned i = 0; i < 4; i++) p[i] = (unsigned char)(value >> (8 * i));
}

static void put64(unsigned char *p, uint64_t value)
{
    for (unsigned i = 0; i < 8; i++) p[i] = (unsigned char)(value >> (8 * i));
}

static int write_status(
    int fd, uint16_t kind, uint64_t sequence,
    const unsigned char *execution, const unsigned char *preparation,
    const unsigned char *digest, uint32_t payload)
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
    memcpy(frame + 24, execution, 16);
    memcpy(frame + 40, preparation, 16);
    memcpy(frame + 56, digest, 32);
    if (payload_bytes) put32(frame + GGM1_HEADER_BYTES, payload);
    return write_all(fd, frame, total);
}

static int build_status(
    unsigned char *frame, size_t *length, uint16_t kind, uint64_t sequence,
    const unsigned char *execution, const unsigned char *preparation,
    const unsigned char *digest, uint32_t payload, uint32_t total_override)
{
    size_t payload_bytes = kind == GGM1_KIND_EXEC_CONFIRMED ? 0 : 4;
    size_t total = total_override ? total_override : GGM1_HEADER_BYTES + payload_bytes;
    if (total > 128) return -1;
    memset(frame, 0, 128);
    memcpy(frame, GGM1_MAGIC, 8);
    put16(frame + 8, GGM1_VERSION);
    put16(frame + 10, kind);
    put32(frame + 12, (uint32_t)total);
    put64(frame + 16, sequence);
    memcpy(frame + 24, execution, 16);
    memcpy(frame + 40, preparation, 16);
    memcpy(frame + 56, digest, 32);
    if (payload_bytes && total >= GGM1_HEADER_BYTES + 4) put32(frame + GGM1_HEADER_BYTES, payload);
    *length = total;
    return 0;
}

int main(void)
{
    unsigned char execution[16], preparation[16], generation[16], digest[32];
    if (decode_hex(getenv("WP05_EXPECT_EXEC"), execution, sizeof(execution)) != 0
        || decode_hex(getenv("WP05_EXPECT_PREP"), preparation, sizeof(preparation)) != 0
        || decode_hex(getenv("WP05_EXPECT_GEN"), generation, sizeof(generation)) != 0
        || decode_hex(getenv("WP05_EXPECT_DIGEST"), digest, sizeof(digest)) != 0)
        return 2;

    const char *overall_text = getenv("WP05_EXPECT_OVERALL");
    const char *release_fd_text = getenv("WP05_RELEASE_FD");
    if (overall_text == NULL || release_fd_text == NULL) return 3;
    uint64_t overall = strtoull(overall_text, NULL, 10);
    int release_fd = atoi(release_fd_text);
    if (overall == 0 || release_fd < 0) return 3;
    if (duplicate_to(release_fd, 4) != 0) return 3;

    unsigned char frame[GGM1_RELEASE_BYTES];
    size_t got = 0;
    while (got < sizeof(frame))
    {
        ssize_t count = read(4, frame + got, sizeof(frame) - got);
        if (count < 0 && errno == EINTR) continue;
        if (count <= 0)
        {
            (void)write_all(1, (const unsigned char *)"wp05-release-refused\n", 21);
            return 20;
        }
        got += (size_t)count;
    }

    struct pollfd extra = { .fd = 4, .events = POLLIN };
    int have_extra = poll(&extra, 1, 0);
    if (have_extra < 0 || (have_extra > 0 && (extra.revents & POLLIN)))
    {
        (void)write_all(1, (const unsigned char *)"wp05-release-refused\n", 21);
        return 21;
    }

    if (!ggm1_release_frame_valid(
            frame, sizeof(frame), execution, preparation, digest, generation, overall, now_ns()))
    {
        (void)write_all(1, (const unsigned char *)"wp05-release-refused\n", 21);
        return 22;
    }

    close(4);
    (void)write_all(1, (const unsigned char *)"wp05-release-accepted\n", 22);

    const char *status_fd_text = getenv("WP05_STATUS_FD");
    const char *status_mode = getenv("WP05_STATUS_MODE");
    if (status_fd_text != NULL && status_mode != NULL)
    {
        int status_fd = atoi(status_fd_text);
        if (status_fd < 0 || duplicate_to(status_fd, 5) != 0) return 23;
        uint64_t sequence = 2;
        const char *value_text = getenv("WP05_STATUS_VALUE");
        uint32_t value = value_text == NULL ? 0 : (uint32_t)strtoul(value_text, NULL, 10);
        unsigned char f[128];
        size_t flen = 0;
        if (strcmp(status_mode, "exec_failed") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_FAILED, sequence, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "exec_confirmed") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0) != 0) return 24;
        }
        else if (strcmp(status_mode, "protocol_failed") == 0)
        {
            if (write_status(5, GGM1_KIND_PROTOCOL_FAILED, sequence, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "root_unconfirmed") == 0)
        {
            if (write_status(5, GGM1_KIND_ROOT_WAIT, sequence, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "exec_confirmed_root") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0) != 0) return 24;
            if (write_status(5, GGM1_KIND_ROOT_WAIT, sequence + 1, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "exec_failed_root") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_FAILED, sequence, execution, preparation, digest, value) != 0) return 24;
            if (write_status(5, GGM1_KIND_ROOT_WAIT, sequence + 1, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_wrong_exec") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            f[24] ^= 1;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_wrong_prep") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            f[40] ^= 1;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_wrong_digest") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            f[56] ^= 1;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_wrong_sequence") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, 3, execution, preparation, digest, 0, 0)) return 26;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_replayed_sequence") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            if (write_all(5, f, flen) != 0) return 24;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_duplicate_exec") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            if (write_all(5, f, flen) != 0) return 24;
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence + 1, execution, preparation, digest, 0, 0)) return 26;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_root_after_terminal") == 0)
        {
            if (write_status(5, GGM1_KIND_PROTOCOL_FAILED, sequence, execution, preparation, digest, value) != 0) return 24;
            if (write_status(5, GGM1_KIND_ROOT_WAIT, sequence + 1, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_contradictory") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0) != 0) return 24;
            if (write_status(5, GGM1_KIND_EXEC_FAILED, sequence + 1, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_wrong_magic") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            f[0] ^= 1;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_wrong_version") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            put16(f + 8, 2);
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_short_header") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            if (write_all(5, f, 10) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_truncated_payload") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_ROOT_WAIT, sequence, execution, preparation, digest, value, 100)) return 26;
            if (write_all(5, f, 98) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_length_below_header") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            put32(f + 12, 50);
            if (write_all(5, f, 96) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_length_above_max") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 0)) return 26;
            put32(f + 12, 5000);
            if (write_all(5, f, 96) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_invalid_kind") == 0)
        {
            if (build_status(f, &flen, 999, sequence, execution, preparation, digest, value, 100)) return 26;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_invalid_payload_length") == 0)
        {
            if (build_status(f, &flen, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0, 100)) return 26;
            if (write_all(5, f, flen) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_premature_eof") == 0 || strcmp(status_mode, "d5_no_status") == 0)
        {
            close(5);
            return 0;
        }
        else if (strcmp(status_mode, "d5_extra_trailing") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0) != 0) return 24;
            unsigned char extra[4] = { 0xaa, 0xaa, 0xaa, 0xaa };
            if (write_all(5, extra, sizeof(extra)) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_status_after_terminal") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0) != 0) return 24;
            if (write_status(5, GGM1_KIND_ROOT_WAIT, sequence + 1, execution, preparation, digest, value) != 0) return 24;
            if (write_status(5, GGM1_KIND_ROOT_WAIT, sequence + 2, execution, preparation, digest, value) != 0) return 24;
        }
        else if (strcmp(status_mode, "d5_hold_exec") == 0)
        {
            if (write_status(5, GGM1_KIND_EXEC_CONFIRMED, sequence, execution, preparation, digest, 0) != 0) return 24;
            sleep(5);
        }
        else
        {
            return 25;
        }
    }

    return 0;
}
