using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public static class Wp05LeaseFixtureHost
{
    public static int Run(string[] args)
    {
        string? journalDirectory = null;
        string? readyPath = null;
        string? acquiredPath = null;
        string? role = null;
        for (var index = 0; index + 1 < args.Length; index += 2)
        {
            switch (args[index])
            {
                case "--journal": journalDirectory = args[index + 1]; break;
                case "--ready": readyPath = args[index + 1]; break;
                case "--acquired": acquiredPath = args[index + 1]; break;
                case "--role": role = args[index + 1]; break;
            }
        }

        if (journalDirectory is null || role is null)
            return 2;
        Directory.CreateDirectory(journalDirectory);
        var journal = new OwnershipJournal(new JournalFileStorage(), journalDirectory);
        if (!journal.TryAcquireWriter(out var writer, out var error))
            return 3;
        using (writer)
        {
            if (role == "owner")
            {
                if (readyPath is null)
                    return 4;
                File.WriteAllText(readyPath,
                    $"pid={Environment.ProcessId}\njournal={journalDirectory}\nlock={Path.Combine(journalDirectory, OwnershipJournal.DefaultLockName)}\nsequence={writer!.BaselineSequence}\n");
                Thread.Sleep(Timeout.Infinite);
                return 0;
            }

            if (role == "replacement")
            {
                if (acquiredPath is null)
                    return 5;
                File.WriteAllText(acquiredPath,
                    $"pid={Environment.ProcessId}\nsequence={writer!.BaselineSequence}\n");
                return 0;
            }
        }

        return 6;
    }
}
