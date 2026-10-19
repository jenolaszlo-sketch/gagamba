// Qualification-only SIGPIPE disposition probe. Exits 0 for SIG_DFL, 1 for
// SIG_IGN, and 2 for an installed handler. Not a production artifact.
#include <signal.h>

int main(void)
{
    struct sigaction action;
    if (sigaction(SIGPIPE, 0, &action) != 0)
    {
        return 3;
    }
    if (action.sa_handler == SIG_IGN)
    {
        return 1;
    }
    if (action.sa_handler == SIG_DFL)
    {
        return 0;
    }
    return 2;
}
