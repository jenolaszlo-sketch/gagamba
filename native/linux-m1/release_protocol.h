#ifndef GAGAMBA_M1_RELEASE_PROTOCOL_H
#define GAGAMBA_M1_RELEASE_PROTOCOL_H
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include "protocol.h"

#define GGM1_RELEASE_BYTES 128u
#define GGM1_RELEASE_SEQUENCE 1u

static inline uint16_t ggm1_rel16(const unsigned char *p)
{
    return (uint16_t)p[0] | ((uint16_t)p[1] << 8);
}

static inline uint32_t ggm1_rel32(const unsigned char *p)
{
    return (uint32_t)p[0] | ((uint32_t)p[1] << 8)
        | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}

static inline uint64_t ggm1_rel64(const unsigned char *p)
{
    uint64_t value = 0;
    for (unsigned i = 0; i < 8; i++) value |= (uint64_t)p[i] << (8 * i);
    return value;
}

/* Validates the exact 128-byte RELEASE frame against the immutable
   coordinator configuration and the local monotonic deadline. */
static inline int ggm1_release_frame_valid(
    const unsigned char *frame, size_t length,
    const unsigned char *execution, const unsigned char *preparation,
    const unsigned char *digest, const unsigned char *generation,
    uint64_t overall_deadline_ns, uint64_t now_ns)
{
    if (length != GGM1_RELEASE_BYTES) return 0;
    if (memcmp(frame, GGM1_MAGIC, 8) != 0) return 0;
    if (ggm1_rel16(frame + 8) != GGM1_VERSION) return 0;
    if (ggm1_rel16(frame + 10) != GGM1_KIND_RELEASE) return 0;
    if (ggm1_rel32(frame + 12) != GGM1_RELEASE_BYTES) return 0;
    if (ggm1_rel64(frame + 16) != GGM1_RELEASE_SEQUENCE) return 0;
    if (memcmp(frame + 24, execution, 16) != 0) return 0;
    if (memcmp(frame + 40, preparation, 16) != 0) return 0;
    if (memcmp(frame + 56, digest, 32) != 0) return 0;
    for (unsigned i = 88; i < 96; i++) if (frame[i] != 0) return 0;
    if (memcmp(frame + 96, generation, 16) != 0) return 0;
    if (ggm1_rel64(frame + 112) != overall_deadline_ns) return 0;
    for (unsigned i = 120; i < 128; i++) if (frame[i] != 0) return 0;
    if (now_ns == 0 || now_ns >= overall_deadline_ns) return 0;
    return 1;
}

#endif
