#define _GNU_SOURCE
// Disposable P03 only: Linux transports/host-IPC matrix for the frozen P02
// composition. No production code. Modes: positive (host netns, no ns/seccomp)
// and candidate (bwrap + filter, entered by the harness). All endpoints are
// synthetic fixtures; all output is bounded; alarm(12) backstops every run.
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
#include <sys/select.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <sys/mman.h>
#include <sys/mount.h>
#include <sys/prctl.h>
#include <sys/syscall.h>
#include <sys/wait.h>
#include <sys/stat.h>
#include <netinet/in.h>
#include <net/if.h>
#include <arpa/inet.h>
#include <linux/io_uring.h>
#include <linux/netlink.h>
#include <linux/rtnetlink.h>
#include <linux/if_link.h>
#include <linux/if_addr.h>

static void result(const char *op, long rc) { int e = rc < 0 ? errno : 0; printf("%s rc=%ld errno=%d\n", op, rc, e); fflush(stdout); }

// ---- minimal io_uring plumbing (layouts verified against liburing/kernel) ----
static int UR;
static uint8_t *USQ, *UCQ, *USQES;
static struct io_uring_params UP;
static unsigned *usq_tail, *usq_mask, *usq_arr, *ucq_head, *ucq_mask;
static struct io_uring_cqe *ucq_ring;
static int ur_ready = 0;

static int ur_setup(void) {
    memset(&UP, 0, sizeof UP);
    UR = syscall(__NR_io_uring_setup, 8, &UP);
    result("uring_setup", UR);
    if (UR < 0) return -1;
    size_t sq_sz = UP.sq_off.array + UP.sq_entries * sizeof(unsigned);
    size_t cq_sz = UP.cq_off.cqes + UP.cq_entries * sizeof(struct io_uring_cqe);
    size_t sqes_sz = UP.sq_entries * sizeof(struct io_uring_sqe);
    USQ = mmap(0, sq_sz, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, UR, IORING_OFF_SQ_RING);
    UCQ = mmap(0, cq_sz, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, UR, IORING_OFF_CQ_RING);
    USQES = mmap(0, sqes_sz, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, UR, IORING_OFF_SQES);
    if (USQ == MAP_FAILED || UCQ == MAP_FAILED || USQES == MAP_FAILED) { result("uring_mmap", -1); close(UR); return -1; }
    usq_tail = (unsigned *)(USQ + UP.sq_off.tail); usq_mask = (unsigned *)(USQ + UP.sq_off.ring_mask);
    usq_arr = (unsigned *)(USQ + UP.sq_off.array);
    ucq_head = (unsigned *)(UCQ + UP.cq_off.head); ucq_mask = (unsigned *)(UCQ + UP.cq_off.ring_mask);
    ucq_ring = (struct io_uring_cqe *)(UCQ + UP.cq_off.cqes);
    ur_ready = 1;
    return 0;
}
static int ur_wait(void) {
    long r = syscall(__NR_io_uring_enter, UR, 0, 1, IORING_ENTER_GETEVENTS, NULL, 0);
    if (r < 0) { result("uring_enter_wait", r); return -9999; }
    unsigned h = __atomic_load_n(ucq_head, __ATOMIC_ACQUIRE);
    int res = ucq_ring[h & *ucq_mask].res;
    __atomic_store_n(ucq_head, h + 1, __ATOMIC_RELEASE);
    return res;
}
static int ur_submit(struct io_uring_sqe *s) {
    unsigned t = __atomic_load_n(usq_tail, __ATOMIC_RELAXED);
    unsigned i = t & *usq_mask;
    memcpy(USQES + (size_t)i * sizeof(struct io_uring_sqe), s, sizeof(*s));
    usq_arr[i] = i;
    __atomic_store_n(usq_tail, t + 1, __ATOMIC_RELEASE);
    long r = syscall(__NR_io_uring_enter, UR, 1, 0, 0, NULL, 0);
    if (r < 0) { result("uring_enter_submit", r); return -1; }
    return 0;
}
// uring socket+connect+send+recv chain for stream and connected-dgram use.
static void uring_stream(const char *tag, int domain, int type, int proto,
                         const struct sockaddr *sa, socklen_t sl,
                         const void *sendb, size_t sendn) {
    printf("== %s/uring ==\n", tag);
    if (!ur_ready && ur_setup() < 0) return;
    struct io_uring_sqe s;
    char rbuf[512];
    memset(&s, 0, sizeof s);
    s.opcode = IORING_OP_SOCKET; s.fd = domain; s.len = proto; s.off = type; s.user_data = 1;
    if (ur_submit(&s) < 0) return;
    int fd = ur_wait(); result("uring_socket", fd);
    if (fd < 0) return;
    struct timeval tv = {2, 0};
    setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
    setsockopt(fd, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof tv);
    memset(&s, 0, sizeof s);
    s.opcode = IORING_OP_CONNECT; s.fd = fd; s.addr = (uint64_t)(uintptr_t)sa; s.off = sl; s.user_data = 2;
    if (ur_submit(&s) < 0) { close(fd); return; }
    long cr = ur_wait(); result("uring_connect", cr);
    if (cr < 0) { close(fd); return; }
    memset(&s, 0, sizeof s);
    s.opcode = IORING_OP_SEND; s.fd = fd; s.addr = (uint64_t)(uintptr_t)sendb; s.len = sendn; s.user_data = 3;
    if (ur_submit(&s) < 0) { close(fd); return; }
    long wr = ur_wait(); result("uring_send", wr);
    if (wr < 0) { close(fd); return; }
    memset(&s, 0, sizeof s);
    s.opcode = IORING_OP_RECV; s.fd = fd; s.addr = (uint64_t)(uintptr_t)rbuf; s.len = sizeof rbuf - 1; s.user_data = 4;
    if (ur_submit(&s) < 0) { close(fd); return; }
    long rr = ur_wait(); result("uring_recv", rr);
    if (rr > 0) { rbuf[rr] = 0; printf("uring_reply %s\n", rbuf); }
    close(fd);
}

// ---- direct-syscall helpers ----
static void tcp_direct(const char *tag, int family, const struct sockaddr *sa, socklen_t sl,
                       const void *sendb, size_t sendn) {
    printf("== %s/direct ==\n", tag);
    int s = socket(family, SOCK_STREAM | SOCK_CLOEXEC, 0); result("socket", s);
    if (s < 0) return;
    struct timeval tv = {2, 0};
    setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
    setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof tv);
    long rc = connect(s, sa, sl); result("connect", rc);
    if (rc == 0) {
        rc = write(s, sendb, sendn); result("request", rc);
        char b[512]; ssize_t n = read(s, b, sizeof b - 1);
        result("reply", n);
        if (n > 0) { b[n] = 0; printf("direct_reply %s\n", b); }
    }
    close(s);
}
static void udp_direct(const char *tag, const struct sockaddr_in *sa, const void *sendb, size_t sendn) {
    printf("== %s/direct ==\n", tag);
    int s = socket(AF_INET, SOCK_DGRAM | SOCK_CLOEXEC, 0); result("socket", s);
    if (s < 0) return;
    struct timeval tv = {2, 0};
    setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
    long rc = sendto(s, sendb, sendn, 0, (const struct sockaddr *)sa, sizeof *sa); result("request", rc);
    char b[512]; ssize_t n = recvfrom(s, b, sizeof b - 1, 0, NULL, NULL);
    result("reply", n);
    if (n > 0) { b[n] = 0; printf("direct_reply %s\n", b); }
    close(s);
}
static void unix_direct(const char *tag, const struct sockaddr_un *sa, socklen_t sl,
                        const void *sendb, size_t sendn) {
    printf("== %s/direct ==\n", tag);
    int s = socket(AF_UNIX, SOCK_STREAM | SOCK_CLOEXEC, 0); result("socket", s);
    if (s < 0) return;
    struct timeval tv = {2, 0};
    setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
    setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof tv);
    long rc = connect(s, (const struct sockaddr *)sa, sl); result("connect", rc);
    if (rc == 0) {
        rc = write(s, sendb, sendn); result("request", rc);
        char b[512]; ssize_t n = read(s, b, sizeof b - 1);
        result("reply", n);
        if (n > 0) { b[n] = 0; printf("direct_reply %s\n", b); }
    }
    close(s);
}
static size_t dns_query(const char *nonce, uint8_t *out) {
    uint8_t *p = out;
    *p++ = 0xA9; *p++ = 0x37; *p++ = 0x01; *p++ = 0x00;
    *p++ = 0x00; *p++ = 0x01; *p++ = 0x00; *p++ = 0x00;
    *p++ = 0x00; *p++ = 0x00; *p++ = 0x00; *p++ = 0x00;
    size_t nl = strlen(nonce);
    *p++ = (uint8_t)nl; memcpy(p, nonce, nl); p += nl;
    *p++ = 7; memcpy(p, "ar7test", 7); p += 7; *p++ = 0;
    *p++ = 0; *p++ = 1; *p++ = 0; *p++ = 1;
    return (size_t)(p - out);
}
static uint16_t cksum_be(const uint8_t *b, size_t n) {
    uint32_t s = 0; size_t i;
    for (i = 0; i + 1 < n; i += 2) s += ((uint16_t)b[i] << 8) | b[i + 1];
    if (i < n) s += (uint16_t)b[i] << 8;
    s = (s >> 16) + (s & 0xffff); s += s >> 16;
    return (uint16_t)~s;
}

int main(int argc, char **argv) {
    setbuf(stdout, NULL); alarm(12);
    // mode cg root nonce dtoken input tcp4 tcp6 udp dns proxy meta tcp6ok metaok dnsok agent daemon generic abstract
    if (argc != 20) { printf("argc=%d\n", argc); return 2; }
    int positive = !strcmp(argv[1], "positive");
    const char *cg = argv[2], *nonce = argv[4], *dtoken = argv[5];
    int tcp4p = atoi(argv[7]), tcp6p = atoi(argv[8]), udpp = atoi(argv[9]);
    int dnsp = atoi(argv[10]), proxyp = atoi(argv[11]), metap = atoi(argv[12]);
    int tcp6ok = atoi(argv[13]), metaok = atoi(argv[14]), dnsok = atoi(argv[15]);
    printf("identity uid=%d gid=%d pid=%d nnp=%d\n", getuid(), getgid(), getpid(),
           prctl(PR_GET_NO_NEW_PRIVS, 0, 0, 0, 0));
    int f = open(positive ? argv[6] : "/input", O_RDONLY | O_CLOEXEC); result("input", f);
    if (f >= 0) { char b[128]; result("input_read", read(f, b, sizeof b)); close(f); }
    if (!positive) {
        f = open("/tmp/scratch", O_CREAT | O_EXCL | O_WRONLY | O_CLOEXEC, 0600); result("scratch", f);
        if (f >= 0) { result("scratch_write", write(f, nonce, strlen(nonce))); close(f); }
    }
    // Prepared channel: nonce echo over stdin/stdout.
    {
        fd_set rf; FD_ZERO(&rf); FD_SET(0, &rf);
        struct timeval tv = {3, 0};
        char line[128]; int ok = 0;
        if (select(1, &rf, NULL, NULL, &tv) == 1) {
            ssize_t n = read(0, line, sizeof line - 1);
            if (n > 0) {
                line[n] = 0;
                char want[96]; snprintf(want, sizeof want, "P03-REQ %s\n", nonce);
                if (!strcmp(line, want)) { printf("P03-REP %s\n", nonce); ok = 1; }
            }
        }
        result("stdio_echo", ok ? 0 : -1);
    }
    pid_t child = fork(); result("finite_fork", child);
    if (child == 0) { sleep(2); return 0; }
    if (child > 0) waitpid(child, NULL, 0);

    struct sockaddr_in v4; memset(&v4, 0, sizeof v4);
    v4.sin_family = AF_INET; v4.sin_addr.s_addr = htonl(0x7f000001);
    struct sockaddr_in6 v6; memset(&v6, 0, sizeof v6);
    v6.sin6_family = AF_INET6; v6.sin6_addr.s6_addr[15] = 1;

    // Endpoint 1: IPv4 TCP.
    v4.sin_port = htons(tcp4p);
    tcp_direct("tcp4", AF_INET, (struct sockaddr *)&v4, sizeof v4, nonce, strlen(nonce));
    uring_stream("tcp4", AF_INET, SOCK_STREAM, IPPROTO_TCP,
                 (struct sockaddr *)&v4, sizeof v4, nonce, strlen(nonce));
    // Endpoint 2: IPv6 TCP.
    if (tcp6ok) {
        v6.sin6_port = htons(tcp6p);
        tcp_direct("tcp6", AF_INET6, (struct sockaddr *)&v6, sizeof v6, nonce, strlen(nonce));
        uring_stream("tcp6", AF_INET6, SOCK_STREAM, IPPROTO_TCP,
                     (struct sockaddr *)&v6, sizeof v6, nonce, strlen(nonce));
    } else puts("tcp6 skipped (INCONCLUSIVE leg)");
    // Endpoint 3: UDP.
    v4.sin_port = htons(udpp);
    udp_direct("udp", &v4, nonce, strlen(nonce));
    {
        // connected-UDP io_uring chain
        printf("== udp/uring ==\n");
        if (!ur_ready && ur_setup() < 0) puts("udp uring unavailable");
        else {
            struct io_uring_sqe s; char rbuf[512];
            memset(&s, 0, sizeof s);
            s.opcode = IORING_OP_SOCKET; s.fd = AF_INET; s.len = IPPROTO_UDP; s.off = SOCK_DGRAM;
            if (ur_submit(&s) == 0) {
                int fd = ur_wait(); result("uring_socket", fd);
                if (fd >= 0) {
                    struct timeval tv = {2, 0};
                    setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
                    memset(&s, 0, sizeof s);
                    s.opcode = IORING_OP_CONNECT; s.fd = fd;
                    s.addr = (uint64_t)(uintptr_t)&v4; s.off = sizeof v4;
                    if (ur_submit(&s) == 0) {
                        long cr = ur_wait(); result("uring_connect", cr);
                        if (cr == 0) {
                            memset(&s, 0, sizeof s);
                            s.opcode = IORING_OP_SEND; s.fd = fd;
                            s.addr = (uint64_t)(uintptr_t)nonce; s.len = strlen(nonce);
                            if (ur_submit(&s) == 0) {
                                long wr = ur_wait(); result("uring_send", wr);
                                if (wr >= 0) {
                                    memset(&s, 0, sizeof s);
                                    s.opcode = IORING_OP_RECV; s.fd = fd;
                                    s.addr = (uint64_t)(uintptr_t)rbuf; s.len = sizeof rbuf - 1;
                                    if (ur_submit(&s) == 0) {
                                        long rr = ur_wait(); result("uring_recv", rr);
                                        if (rr > 0) { rbuf[rr] = 0; printf("uring_reply %s\n", rbuf); }
                                    }
                                }
                            }
                        }
                    }
                    close(fd);
                }
            }
        }
    }
    // Endpoint 5: DNS-like (UDP + TCP).
    if (dnsok) {
        uint8_t q[256]; size_t ql = dns_query(nonce, q);
        v4.sin_port = htons(dnsp);
        printf("== dns-udp/direct ==\n");
        {
            int s = socket(AF_INET, SOCK_DGRAM | SOCK_CLOEXEC, 0); result("socket", s);
            if (s >= 0) {
                struct timeval tv = {2, 0};
                setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
                long rc = sendto(s, q, ql, 0, (struct sockaddr *)&v4, sizeof v4); result("request", rc);
                char b[512]; ssize_t n = recvfrom(s, b, sizeof b, 0, NULL, NULL); result("reply", n);
                if (n >= 2) result("dns_id_match", (b[0] == (char)0xA9 && b[1] == (char)0x37) ? 0 : -1);
                close(s);
            }
        }
        printf("== dns-tcp/direct ==\n");
        {
            int s = socket(AF_INET, SOCK_STREAM | SOCK_CLOEXEC, 0); result("socket", s);
            if (s >= 0) {
                struct timeval tv = {2, 0};
                setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
                setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof tv);
                long rc = connect(s, (struct sockaddr *)&v4, sizeof v4); result("connect", rc);
                if (rc == 0) {
                    uint8_t framed[258]; framed[0] = (ql >> 8) & 0xff; framed[1] = ql & 0xff;
                    memcpy(framed + 2, q, ql);
                    rc = write(s, framed, ql + 2); result("request", rc);
                    char b[512]; ssize_t n = read(s, b, sizeof b); result("reply", n);
                    close(s);
                } else close(s);
            }
        }
        printf("== dns-udp/uring ==\n");
        {
            // connected-UDP chain carrying the DNS query
            struct io_uring_sqe s; char rbuf[512];
            if (!ur_ready && ur_setup() < 0) puts("dns uring unavailable");
            else {
                memset(&s, 0, sizeof s);
                s.opcode = IORING_OP_SOCKET; s.fd = AF_INET; s.len = IPPROTO_UDP; s.off = SOCK_DGRAM;
                if (ur_submit(&s) == 0) {
                    int fd = ur_wait(); result("uring_socket", fd);
                    if (fd >= 0) {
                        struct timeval tv = {2, 0};
                        setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
                        memset(&s, 0, sizeof s);
                        s.opcode = IORING_OP_CONNECT; s.fd = fd;
                        s.addr = (uint64_t)(uintptr_t)&v4; s.off = sizeof v4;
                        if (ur_submit(&s) == 0) {
                            long cr = ur_wait(); result("uring_connect", cr);
                            if (cr == 0) {
                                memset(&s, 0, sizeof s);
                                s.opcode = IORING_OP_SEND; s.fd = fd;
                                s.addr = (uint64_t)(uintptr_t)q; s.len = ql;
                                if (ur_submit(&s) == 0) {
                                    long wr = ur_wait(); result("uring_send", wr);
                                    if (wr >= 0) {
                                        memset(&s, 0, sizeof s);
                                        s.opcode = IORING_OP_RECV; s.fd = fd;
                                        s.addr = (uint64_t)(uintptr_t)rbuf; s.len = sizeof rbuf;
                                        if (ur_submit(&s) == 0) {
                                            long rr = ur_wait(); result("uring_recv", rr);
                                            if (rr >= 2) result("dns_id_match", (rbuf[0] == (char)0xA9 && rbuf[1] == (char)0x37) ? 0 : -1);
                                        }
                                    }
                                }
                            }
                        }
                        close(fd);
                    }
                }
            }
        }
    } else puts("dns skipped (INCONCLUSIVE leg)");
    // Endpoint 6: proxy-like.
    {
        char req[256]; snprintf(req, sizeof req,
            "CONNECT registry.invalid:443 HTTP/1.1\r\nHost: registry.invalid:443\r\nX-Nonce: %s\r\n\r\n", nonce);
        v4.sin_port = htons(proxyp);
        tcp_direct("proxy", AF_INET, (struct sockaddr *)&v4, sizeof v4, req, strlen(req));
        uring_stream("proxy", AF_INET, SOCK_STREAM, IPPROTO_TCP,
                     (struct sockaddr *)&v4, sizeof v4, req, strlen(req));
    }
    // Endpoint 7: simulated metadata.
    if (metaok) {
        char req[256]; snprintf(req, sizeof req,
            "GET /metadata HTTP/1.1\r\nHost: meta\r\nX-Nonce: %s\r\n\r\n", nonce);
        struct sockaddr_in m; memset(&m, 0, sizeof m);
        m.sin_family = AF_INET; m.sin_addr.s_addr = htonl(0xA9FEA9FE); m.sin_port = htons(metap);
        tcp_direct("metadata", AF_INET, (struct sockaddr *)&m, sizeof m, req, strlen(req));
        uring_stream("metadata", AF_INET, SOCK_STREAM, IPPROTO_TCP,
                     (struct sockaddr *)&m, sizeof m, req, strlen(req));
    } else puts("metadata skipped (INCONCLUSIVE leg)");
    // Endpoints 8/9/11: Unix sockets.
    {
        struct sockaddr_un pa, ab, ag, da;
        memset(&pa, 0, sizeof pa); pa.sun_family = AF_UNIX;
        snprintf(pa.sun_path, sizeof pa.sun_path, "%s", argv[18]);
        memset(&ab, 0, sizeof ab); ab.sun_family = AF_UNIX;
        size_t al = strlen(argv[19]);
        if (al > sizeof ab.sun_path - 2) return 3;
        memcpy(ab.sun_path + 1, argv[19], al);
        memset(&ag, 0, sizeof ag); ag.sun_family = AF_UNIX;
        snprintf(ag.sun_path, sizeof ag.sun_path, "%s", argv[16]);
        memset(&da, 0, sizeof da); da.sun_family = AF_UNIX;
        snprintf(da.sun_path, sizeof da.sun_path, "%s", argv[17]);
        unix_direct("unix-path", &pa, sizeof pa, nonce, strlen(nonce));
        uring_stream("unix-path", AF_UNIX, SOCK_STREAM, 0,
                     (struct sockaddr *)&pa, sizeof pa, nonce, strlen(nonce));
        unix_direct("unix-abstract", &ab, sizeof(sa_family_t) + 1 + al, nonce, strlen(nonce));
        uring_stream("unix-abstract", AF_UNIX, SOCK_STREAM, 0,
                     (struct sockaddr *)&ab, sizeof(sa_family_t) + 1 + al, nonce, strlen(nonce));
        // Endpoint 10: agent-like framing.
        printf("== agent/direct ==\n");
        {
            int s = socket(AF_UNIX, SOCK_STREAM | SOCK_CLOEXEC, 0); result("socket", s);
            if (s >= 0) {
                struct timeval tv = {2, 0};
                setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
                setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof tv);
                long rc = connect(s, (struct sockaddr *)&ag, sizeof ag); result("connect", rc);
                if (rc == 0) {
                    uint8_t msg[40]; msg[0] = 0; msg[1] = 33; msg[2] = 11;
                    memcpy(msg + 3, nonce, 32);
                    rc = write(s, msg, 35); result("request", rc);
                    char b[256]; ssize_t n = read(s, b, sizeof b); result("reply", n);
                    if (n >= 3) result("agent_answer", (b[2] == 12) ? 0 : -1);
                    close(s);
                } else close(s);
            }
        }
        unix_direct("daemon", &da, sizeof da, dtoken, strlen(dtoken));
        uring_stream("daemon", AF_UNIX, SOCK_STREAM, 0,
                     (struct sockaddr *)&da, sizeof da, dtoken, strlen(dtoken));
    }
    // Raw sockets, safely bounded to loopback.
    printf("== raw/direct ==\n");
    {
        int s = socket(AF_INET, SOCK_RAW | SOCK_CLOEXEC, IPPROTO_ICMP); result("raw_socket", s);
        if (s >= 0) close(s);
        s = socket(AF_INET, SOCK_DGRAM | SOCK_CLOEXEC, IPPROTO_ICMP); result("ping_socket", s);
        if (s >= 0) {
            struct timeval tv = {2, 0};
            setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
            uint8_t echo[40]; memset(echo, 0, sizeof echo);
            echo[0] = 8; echo[1] = 0; echo[6] = getpid() >> 8; echo[7] = getpid() & 0xff;
            echo[8] = 0; echo[9] = 1;
            uint16_t c = cksum_be(echo, sizeof echo);
            echo[2] = c >> 8; echo[3] = c & 0xff;
            struct sockaddr_in dst; memset(&dst, 0, sizeof dst);
            dst.sin_family = AF_INET; dst.sin_addr.s_addr = htonl(0x7f000001);
            long rc = sendto(s, echo, sizeof echo, 0, (struct sockaddr *)&dst, sizeof dst);
            result("ping_send", rc);
            char b[128]; ssize_t n = recvfrom(s, b, sizeof b, 0, NULL, NULL);
            result("ping_recv", n);
            if (n >= 20 + 8) {
                uint8_t *ip = (uint8_t *)b + ((b[0] & 0x0f) * 4);
                result("ping_echo_reply", (ip[0] == 0 && !memcmp(ip + 4, echo + 4, 4)) ? 0 : -1);
            }
            close(s);
        }
    }
    // Endpoint 4: namespace-local loopback inspection + netlink.
    printf("== loopback/direct ==\n");
    {
        f = open("/proc/net/dev", O_RDONLY | O_CLOEXEC); result("proc_net_dev", f);
        if (f >= 0) {
            char b[1024]; ssize_t n = read(f, b, sizeof b - 1);
            if (n > 0) { b[n] = 0; printf("net_dev_has_lo %d\n", strstr(b, "lo:") ? 1 : 0); }
            close(f);
        }
        f = open("/sys/class/net", O_RDONLY | O_DIRECTORY | O_CLOEXEC); result("sys_class_net", f);
        if (f >= 0) {
            DIR *d = fdopendir(f);
            if (d) { struct dirent *e; while ((e = readdir(d))) printf("sys_net %s\n", e->d_name); closedir(d); }
            else close(f);
        }
        int s = socket(AF_NETLINK, SOCK_RAW | SOCK_CLOEXEC, NETLINK_ROUTE); result("netlink_socket", s);
        if (s >= 0) {
            struct timeval tv = {2, 0};
            setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, &tv, sizeof tv);
            struct { struct nlmsghdr h; struct ifinfomsg i; } req;
            memset(&req, 0, sizeof req);
            req.h.nlmsg_len = NLMSG_LENGTH(sizeof req.i);
            req.h.nlmsg_type = RTM_GETLINK; req.h.nlmsg_flags = NLM_F_REQUEST | NLM_F_DUMP;
            req.h.nlmsg_seq = 1; req.i.ifi_family = AF_UNSPEC;
            long rc = send(s, &req, req.h.nlmsg_len, 0); result("getlink_send", rc);
            char b[4096]; int lo_up = -1, lo_idx = -1, done = 0;
            while (!done) {
                ssize_t n = recv(s, b, sizeof b, 0);
                if (n <= 0) { result("getlink_recv", n); break; }
                struct nlmsghdr *h;
                for (h = (struct nlmsghdr *)b; NLMSG_OK(h, (size_t)n); h = NLMSG_NEXT(h, n)) {
                    if (h->nlmsg_type == NLMSG_DONE) { done = 1; break; }
                    if (h->nlmsg_type != RTM_NEWLINK) continue;
                    struct ifinfomsg *ifi = NLMSG_DATA(h);
                    struct rtattr *a; int al = (int)(h->nlmsg_len - NLMSG_LENGTH(sizeof *ifi));
                    for (a = IFLA_RTA(ifi); RTA_OK(a, al); a = RTA_NEXT(a, al)) {
                        if (a->rta_type == IFLA_IFNAME && !strcmp((char *)RTA_DATA(a), "lo")) {
                            lo_idx = ifi->ifi_index;
                            lo_up = (ifi->ifi_flags & IFF_UP) ? 1 : 0;
                        }
                    }
                }
            }
            printf("lo_index %d lo_up %d\n", lo_idx, lo_up);
            // Bring-lo-up attempt (expected EPERM without CAP_NET_ADMIN).
            if (lo_idx > 0) {
                struct { struct nlmsghdr h; struct ifinfomsg i; } up;
                memset(&up, 0, sizeof up);
                up.h.nlmsg_len = NLMSG_LENGTH(sizeof up.i);
                up.h.nlmsg_type = RTM_NEWLINK; up.h.nlmsg_flags = NLM_F_REQUEST | NLM_F_ACK;
                up.h.nlmsg_seq = 2; up.i.ifi_family = AF_UNSPEC; up.i.ifi_index = lo_idx;
                up.i.ifi_flags = IFF_UP; up.i.ifi_change = IFF_UP;
                rc = send(s, &up, up.h.nlmsg_len, 0); result("newlink_send", rc);
                if (rc > 0) {
                    ssize_t n = recv(s, b, sizeof b, 0); result("newlink_ack", n);
                    if (n >= (ssize_t)sizeof(struct nlmsghdr)) {
                        struct nlmsghdr *h = (struct nlmsghdr *)b;
                        if (h->nlmsg_type == NLMSG_ERROR) {
                            struct nlmsgerr *e = NLMSG_DATA(h);
                            printf("newlink_errno %d\n", -e->error);
                        }
                    }
                }
            }
            close(s);
        }
    }
    // Namespace/mount authority inventory (P02 construction context, not counted denials).
    if (!positive) {
        result("unshare_user", unshare(CLONE_NEWUSER));
        result("unshare_mount", unshare(CLONE_NEWNS));
        long cl = syscall(SYS_clone, CLONE_NEWUSER | CLONE_NEWNET | SIGCHLD, 0, 0, 0, 0);
        if (cl == 0) return 0;
        result("namespace_clone", cl);
        if (cl > 0) waitpid(cl, NULL, 0);
        result("mount_cgroup", mount("none", "/tmp", "cgroup2", 0, 0));
        char p[1024];
        snprintf(p, sizeof p, "/proc/1/root%s/sibling/cgroup.procs", cg);
        f = open(p, O_WRONLY | O_CLOEXEC); result("init_root_cgroup", f);
        if (f >= 0) close(f);
        snprintf(p, sizeof p, "/proc/%d/ns/net", 1);
        f = open(p, O_RDONLY | O_CLOEXEC); result("init_net_reference", f);
        if (f >= 0) { result("setns_init", setns(f, 0)); close(f); }
        snprintf(p, sizeof p, "/proc/self/ns/net");
        f = open(p, O_RDONLY | O_CLOEXEC); result("self_net_reference", f);
        if (f >= 0) { result("setns_self", setns(f, 0)); close(f); }
        else result("setns_invalid_fd", setns(-1, CLONE_NEWNS));
    } else {
        f = open("/proc/self/ns/net", O_RDONLY | O_CLOEXEC); result("self_net_reference", f);
        if (f >= 0) { result("setns_self", setns(f, 0)); close(f); }
    }
    // Descriptor inventory: only prepared stdio may appear.
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
    // All migration destinations stay inside the independent outer cgroup.
    {
        char p[1024]; snprintf(p, sizeof p, "%s/sibling", cg);
        f = open(p, O_RDONLY | O_DIRECTORY | O_CLOEXEC); result("sibling_visible", f);
        if (f >= 0) close(f);
    }
    puts("complete"); return 0;
}
