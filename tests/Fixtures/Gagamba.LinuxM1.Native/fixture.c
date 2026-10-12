/* Gagamba Linux M1 native fixture (WP02): minimal modes for later
 * useful-work / failure / constructor-marker qualification. WP02 builds,
 * copies, hashes and binds this artifact but never executes it. */
#include <stdio.h>
#include <string.h>
#include <unistd.h>

__attribute__((constructor)) static void ctor_marker(void)
{
    const char marker[] = "gagamba-m1-fixture: ctor-marker\n";
    (void)!write(STDERR_FILENO, marker, sizeof(marker) - 1);
}

int main(int argc, char **argv)
{
    if (argc > 1 && strcmp(argv[1], "--ctor-marker") == 0)
    {
        printf("gagamba-m1-fixture: ctor-marker\n");
        return 0;
    }

    if (argc > 1 && strcmp(argv[1], "--fail") == 0)
    {
        fprintf(stderr, "gagamba-m1-fixture: requested failure\n");
        return 3;
    }

    printf("gagamba-m1-fixture: useful-work\n");
    return 0;
}
