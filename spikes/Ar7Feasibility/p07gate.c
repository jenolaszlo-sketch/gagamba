/* Disposable R2-replacement pre-target gate. Test-only instrumentation: it
 * provides NO confinement, lifecycle enforcement, or additional authority.
 * Bubblewrap launches this binary directly as the namespace root (bound
 * read-only at /gate). It shares no code with /target's initialization: it
 * never prints an identity preamble, never opens/reads /input, never
 * forks/spawns, and execves the actual target ONLY on the exact expected
 * nonce read from its controller-owned stdin channel. Any received bytes
 * are echoed as AUTH_RECEIVED first. argv: gate nonce cg root input.
 */
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

int main(int argc, char **argv) {
    setbuf(stdout, NULL);
    alarm(12);
    if (argc != 5) return 2;
    const char *nonce = argv[1], *cg = argv[2], *root = argv[3], *input = argv[4];
    (void)cg; (void)root; (void)input;
    printf("PRETARGET_GATE_REACHED nonce=%s pid=%d\n", nonce, getpid());
    { FILE *sf = fopen("/proc/self/status", "r");
      int sm = -1, nf = -1;
      if (sf) { char sl[256];
        while (fgets(sl, sizeof sl, sf)) {
          if (!strncmp(sl, "Seccomp:", 8)) sm = atoi(sl + 8);
          else if (!strncmp(sl, "Seccomp_filters:", 16)) nf = atoi(sl + 16); }
        fclose(sf); }
      printf("gate seccomp=%d filters=%d\n", sm, nf); }
    char ab[128];
    size_t al = 0;
    int ch;
    while (al + 1 < sizeof ab && (ch = getchar()) != EOF && ch != '\n') ab[al++] = (char)ch;
    ab[al] = 0;
    if (al > 0 || ch != EOF) printf("AUTH_RECEIVED bytes=%zu\n", al);
    else { printf("GATE_REFUSED eof\n"); return 125; }
    if (!strcmp(ab, nonce)) {
        printf("GATE_AUTH_OK exec_target\n");
        char *const av[] = { "/target", "spinwork", (char *)cg, (char *)root,
                             (char *)nonce, (char *)input, "0", NULL };
        execv("/target", av);
        printf("execv_target rc=-1\n");
        return 3;
    }
    printf("GATE_REFUSED mismatch\n");
    return 125;
}
