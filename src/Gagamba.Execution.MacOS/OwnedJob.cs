// One launchd job owned for exactly one activity lifetime. The job is a
// unique label in the user's gui domain; termination is bootout (removes
// the service and cleans its same-PG remainder). The plist file and stdio
// logs live in a private directory removed at disposal. Deterministic:
// bootout, then delete, exactly once. No PIDs anywhere.
using System.Text;

namespace Gagamba.Execution.MacOS;

internal sealed class OwnedJob : IDisposable
{
    public string Label { get; }
    public string Domain { get; }
    public string WorkDir { get; }
    public string ServiceTarget => $"{Domain}/{Label}";
    private bool _disposed;

    private OwnedJob(string label, string domain, string workDir)
    {
        Label = label;
        Domain = domain;
        WorkDir = workDir;
    }

    public static OwnedJob Create(string domain)
    {
        string label = "org.gagamba.exec." + Guid.NewGuid().ToString("N");
        string dir = Path.Combine(Path.GetTempPath(), "gagamba-macos", label);
        Directory.CreateDirectory(dir);
        return new OwnedJob(label, domain, dir);
    }

    public string PlistPath => Path.Combine(WorkDir, Label + ".plist");

    /// <summary>Write the job definition. No KeepAlive, no RunAtLoad (the
    /// provider kickstarts explicitly), no AbandonProcessGroup (absent, so
    /// launchd's same-PG cleanup applies). Environment comes exclusively
    /// from the spec dict; validation rejects bad shapes first.</summary>
    public void WritePlist(string executable, string[] argv,
        string workingDirectory, IReadOnlyDictionary<string, string> environment)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
        sb.AppendLine("<plist version=\"1.0\"><dict>");
        sb.AppendLine($"<key>Label</key><string>{Escape(Label)}</string>");
        sb.AppendLine("<key>ProgramArguments</key><array>");
        sb.AppendLine($"<string>{Escape(executable)}</string>");
        foreach (string a in argv)
            sb.AppendLine($"<string>{Escape(a)}</string>");
        sb.AppendLine("</array>");
        if (!string.IsNullOrWhiteSpace(workingDirectory))
            sb.AppendLine($"<key>WorkingDirectory</key><string>{Escape(workingDirectory)}</string>");
        if (environment.Count > 0)
        {
            sb.AppendLine("<key>EnvironmentVariables</key><dict>");
            foreach (var kv in environment.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                sb.AppendLine($"<key>{Escape(kv.Key)}</key><string>{Escape(kv.Value)}</string>");
            sb.AppendLine("</dict>");
        }
        sb.AppendLine($"<key>StandardOutPath</key><string>{Escape(Path.Combine(WorkDir, "launchd-out.log"))}</string>");
        sb.AppendLine($"<key>StandardErrorPath</key><string>{Escape(Path.Combine(WorkDir, "launchd-err.log"))}</string>");
        sb.AppendLine("</dict></plist>");
        File.WriteAllText(PlistPath, sb.ToString());
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;", StringComparison.Ordinal)
         .Replace("<", "&lt;", StringComparison.Ordinal)
         .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>Authoritative domain termination. Gone-already still
    /// succeeds (idempotent terminate); other failures classify.</summary>
    public (bool Ok, string Error) Bootout()
    {
        try
        {
            var (rc, out_) = Launchd.Bootout(ServiceTarget);
            if (rc == 0) return (true, "");
            // Not-loaded after a successful life is the idempotent case:
            // verify instead of trusting stderr text across releases.
            var (prc, _) = Launchd.Print(ServiceTarget);
            if (prc != 0) return (true, "");
            return (false, $"bootout refused rc={rc}: {FirstLine(out_)}");
        }
        catch (Exception ex)
        {
            return (false, $"bootout fault: {ex.GetType().Name}");
        }
    }

    private static string FirstLine(string s)
    {
        int i = s.IndexOf('\n');
        string line = (i < 0 ? s : s[..i]).Trim();
        return line.Length > 160 ? line[..160] : line;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Best effort in fixed order: bootout, then delete the directory.
        try { Bootout(); } catch { }
        try { System.IO.Directory.Delete(WorkDir, recursive: true); } catch { }
        GC.SuppressFinalize(this);
    }
}
