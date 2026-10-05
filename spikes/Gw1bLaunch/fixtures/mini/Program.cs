// Workload fixture: prints a synthetic marker. No packages, no network.
Console.WriteLine("mini-workload-ok");
return MiniLib.Answer();

// Named type so the L5 CSC probes can reference this assembly.
public static class MiniLib
{
    public static int Answer() => 0;
}
