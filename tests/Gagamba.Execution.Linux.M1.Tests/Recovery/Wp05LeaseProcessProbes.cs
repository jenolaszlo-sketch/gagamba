using System.Diagnostics;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05LeaseProcessProbes
{
    [Fact]
    public async Task FreshProcessLeaseIsNotInheritedAndReplacementReacquires()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
            return;
        }

        var cancellation = TestContext.Current.CancellationToken;
        var root = RepositoryRoot();
        var project = Path.Combine(root, "tests", "Fixtures", "Wp05LeaseFixture", "Wp05LeaseFixture.csproj");
        Assert.True(File.Exists(project), $"fixture project missing: {project}");
        var temp = Path.Combine(Path.GetTempPath(), "gagamba-wp05-lease-" + Guid.NewGuid().ToString("N"));
        var journal = Path.Combine(temp, "journal");
        var ready = Path.Combine(temp, "owner.ready");
        var acquired = Path.Combine(temp, "replacement.acquired");
        Directory.CreateDirectory(journal);
        Process? ownerRunner = null;
        Process? replacementRunner = null;
        try
        {
            ownerRunner = StartFixture(project,
                "--role", "owner", "--journal", journal, "--ready", ready);
            await WaitForFile(ready, cancellation, TimeSpan.FromSeconds(30));
            var ownerInfo = ParseOwner(ready);
            var ownerPid = ownerInfo.Pid;
            var lockPath = ownerInfo.LockPath;
            Assert.True(HasFdLink(ownerPid, lockPath), "owner process must hold the journal lock fd");
            Assert.False(HasFdLink(Environment.ProcessId, lockPath),
                "supervisor must not hold the owner journal lock");

            var owner = Process.GetProcessById(ownerPid);
            Assert.False(owner.HasExited);
            owner.Kill();
            await owner.WaitForExitAsync(cancellation);
            Assert.True(owner.HasExited);

            replacementRunner = StartFixture(project,
                "--role", "replacement", "--journal", journal, "--acquired", acquired);
            await replacementRunner.WaitForExitAsync(cancellation);
            Assert.Equal(0, replacementRunner.ExitCode);
            Assert.True(File.Exists(acquired), "replacement did not record acquisition");
            var replacementPid = ParsePid(acquired);
            Assert.NotEqual(ownerPid, replacementPid);
        }
        finally
        {
            KillQuietly(ownerRunner);
            KillQuietly(replacementRunner);
            try { Directory.Delete(temp, true); } catch { }
        }
    }

    private static Process StartFixture(string project, params string[] arguments)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };
        info.ArgumentList.Add("run");
        info.ArgumentList.Add("--project");
        info.ArgumentList.Add(project);
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add("Release");
        info.ArgumentList.Add("--no-launch-profile");
        info.ArgumentList.Add("--");
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        return Process.Start(info) ?? throw new InvalidOperationException("fixture did not start");
    }

    private static async Task WaitForFile(string path, CancellationToken cancellationToken, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!File.Exists(path))
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException($"timed out waiting for {path}");
            await Task.Delay(50, cancellationToken);
        }
    }

    private static (int Pid, string LockPath) ParseOwner(string path)
    {
        var lines = File.ReadAllLines(path);
        var pid = int.Parse(lines.Single(line => line.StartsWith("pid=", StringComparison.Ordinal))[4..]);
        var lockPath = lines.Single(line => line.StartsWith("lock=", StringComparison.Ordinal))[5..];
        return (pid, lockPath);
    }

    private static int ParsePid(string path)
    {
        var line = File.ReadAllLines(path).Single(value => value.StartsWith("pid=", StringComparison.Ordinal));
        return int.Parse(line[4..]);
    }

    private static bool HasFdLink(int pid, string target)
    {
        var directory = $"/proc/{pid}/fd";
        if (!Directory.Exists(directory))
            return false;
        try
        {
            foreach (var path in Directory.EnumerateFiles(directory))
            {
                var link = new FileInfo(path).LinkTarget;
                if (link == target)
                    return true;
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        return false;
    }

    private static void KillQuietly(Process? process)
    {
        if (process is null)
            return;
        try
        {
            if (!process.HasExited)
                process.Kill();
            process.WaitForExit(5_000);
        }
        catch
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    private static string RepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("GAGAMBA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var full = Path.GetFullPath(configured);
            if (File.Exists(Path.Combine(full, "Gagamba.sln")))
                return full;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gagamba.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Gagamba repository root not found");
    }
}
