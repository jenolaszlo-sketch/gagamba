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
    private readonly object _gate = new();
    private readonly ILaunchdTransport _transport;
    private bool _loaded;
    private bool _removed;

    private OwnedJob(string label, string domain, string workDir, ILaunchdTransport transport)
    {
        Label = label;
        Domain = domain;
        WorkDir = workDir;
        _transport = transport;
    }

    public static OwnedJob Create(string domain, ILaunchdTransport? transport = null)
    {
        string label = "org.gagamba.exec." + Guid.NewGuid().ToString("N");
        // CreateTempSubdirectory uses an atomic, private (0700 on Unix)
        // directory. A shared, predictably named parent can expose the plist
        // (arguments and environment) and the target's stdout/stderr logs.
        string dir = Directory.CreateTempSubdirectory("gagamba-macos-").FullName;
        return new OwnedJob(label, domain, dir, transport ?? new ProcessLaunchdTransport());
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

    public void MarkLoaded() { lock (_gate) _loaded = true; }

    public JobObservation Observe()
    {
        lock (_gate)
        {
            if (_removed) return new JobObservation(JobObservationKind.ConfirmedNotFound, null);
            return Launchd.Observe(_transport.Run("print", ServiceTarget));
        }
    }

    /// <summary>Bootout is only confirmed by recognized absence. Errors
    /// retain both the label and the private directory for retry.</summary>
    public (bool Ok, string Error) Cleanup()
    {
        lock (_gate)
        {
            if (_removed) return (true, "");
            if (_loaded)
            {
                HelperResult bootout = _transport.Run("bootout", ServiceTarget);
                JobObservation observed = Launchd.Observe(_transport.Run("print", ServiceTarget));
                if (observed.Kind != JobObservationKind.ConfirmedNotFound)
                    return (false, $"bootout unconfirmed ({bootout.Diagnostic}); print: {observed.Reason}");
                _loaded = false;
            }
            try
            {
                Directory.Delete(WorkDir, recursive: true);
                _removed = true;
                return (true, "");
            }
            catch (Exception ex)
            { return (false, $"job directory cleanup fault: {ex.GetType().Name}: {ex.Message}"); }
        }
    }

    private static string FirstLine(string s)
    {
        int i = s.IndexOf('\n');
        string line = (i < 0 ? s : s[..i]).Trim();
        return line.Length > 160 ? line[..160] : line;
    }

    public void Dispose()
    { var result = Cleanup(); if (!result.Ok) throw new InvalidOperationException(result.Error); }
}
