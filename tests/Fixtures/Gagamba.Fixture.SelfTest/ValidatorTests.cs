// GP-1A unit self-tests: protocol + evidence validators, workspace bounds.
// Process integration lives in Gagamba.Fixture.Harness (actual runs).
using Gagamba.Fixture.Host;
using Xunit;
using System.Text.Json.Nodes;

public sealed class ProtocolValidatorTests
{
    private const string Run = "run-1";
    private const string Worker = "w-1";

    [Fact]
    public void Accepts_Ready_Result_Sequence()
    {
        string ready = FixtureProtocol.BuildRaw(Run, Worker, 0, "Ready", new JsonObject { ["workspace"] = "x" });
        Assert.True(FixtureProtocol.ValidateWorkerLine(ready, Run, Worker, 0).Ok);
        string result = FixtureProtocol.BuildRaw(Run, Worker, 1, "Result", new JsonObject { ["ok"] = true });
        Assert.True(FixtureProtocol.ValidateWorkerLine(result, Run, Worker, 1).Ok);
    }

    [Fact]
    public void Rejects_Unknown_Version()
    {
        string line = FixtureProtocol.BuildRaw(Run, Worker, 0, "Ready", new JsonObject())
            .Replace("\"protocolVersion\":1", "\"protocolVersion\":99");
        var r = FixtureProtocol.ValidateWorkerLine(line, Run, Worker, 0);
        Assert.False(r.Ok);
        Assert.Equal("unknown-version", r.Reason);
    }

    [Fact]
    public void Rejects_Unknown_Kind()
    {
        string line = FixtureProtocol.BuildRaw(Run, Worker, 0, "Frobnicate", new JsonObject());
        var r = FixtureProtocol.ValidateWorkerLine(line, Run, Worker, 0);
        Assert.False(r.Ok);
        Assert.StartsWith("unknown-kind", r.Reason);
    }

    [Fact]
    public void Rejects_Bad_Sequence()
    {
        string line = FixtureProtocol.BuildRaw(Run, Worker, 7, "Result", new JsonObject { ["ok"] = true });
        var dup = FixtureProtocol.ValidateWorkerLine(line, Run, Worker, 1);
        Assert.False(dup.Ok);
        Assert.Equal("bad-sequence", dup.Reason);
        var ooo = FixtureProtocol.ValidateWorkerLine(line, Run, Worker, 5);
        Assert.False(ooo.Ok);
    }

    [Fact]
    public void Rejects_Oversize()
    {
        string big = new string('x', 17 * 1024);
        string line = FixtureProtocol.BuildRaw(Run, Worker, 0, "Ready", new JsonObject { ["p"] = big });
        var r = FixtureProtocol.ValidateWorkerLine(line, Run, Worker, 0);
        Assert.False(r.Ok);
        Assert.Equal("oversize-record", r.Reason);
    }

    [Fact]
    public void Rejects_Mismatched_Identity()
    {
        string line = FixtureProtocol.BuildRaw("other-run", Worker, 0, "Ready", new JsonObject());
        var r = FixtureProtocol.ValidateWorkerLine(line, Run, Worker, 0);
        Assert.False(r.Ok);
        Assert.Equal("mismatched-identity", r.Reason);
    }

    [Fact]
    public void Continue_Bounds_Params_To_16KiB()
    {
        Assert.Throws<InvalidOperationException>(() =>
            FixtureProtocol.BuildContinue(Run, Worker, 1, "exit",
                new JsonObject { ["blob"] = new string('y', 20 * 1024) }));
    }
}

public sealed class EvidenceValidatorTests
{
    private static EvidenceReport GoodReport()
    {
        var cases = FixtureManifest.MandatoryIds
            .Select(id => new CaseRecord(id, "Passed", "Passed", "ok", 1, null))
            .ToList();
        var source = new SourceInfo(new string('a', 40), false, null,
            new() { new("tests/Fixtures/x.cs", new string('b', 64)) });
        var env = new EnvironmentInfo("os", "ver", "arch", "rid", "fs", "outer", "setup", "target", "dotnet");
        var summary = new ReportSummary(cases.Count, cases.Count, 0, 0, 0, true, "Passed");
        return new(1, "FixtureSelfTest", "run-1", DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow,
            source, env, null, new("offline-process-v1", null, null), cases,
            new("Confirmed", "deleted", "ok"), summary);
    }

    [Fact]
    public void Accepts_Good_FixtureSelfTest()
    {
        Assert.Empty(EvidenceValidator.Validate(GoodReport()));
    }

    [Fact]
    public void Rejects_Empty_Cases()
    {
        var good = GoodReport() with { Cases = new(), Summary = new(0, 0, 0, 0, 0, false, "Failed") };
        Assert.NotEmpty(EvidenceValidator.Validate(good));
    }

    [Fact]
    public void Rejects_Missing_Mandatory()
    {
        var good = GoodReport();
        good.Cases.RemoveAt(0);
        var summary = good.Summary with { Total = good.Cases.Count, Passed = good.Cases.Count };
        var bad = good with { Cases = good.Cases, Summary = summary };
        Assert.Contains(EvidenceValidator.Validate(bad), e => e.Contains("missing mandatory"));
    }

    [Fact]
    public void Rejects_Duplicate_And_Invented_Hash()
    {
        var good = GoodReport();
        good.Cases.Add(good.Cases[0]);
        var badProfile = good with { Profile = new("offline-process-v1", "deadbeef", null) };
        var errors = EvidenceValidator.Validate(badProfile);
        Assert.Contains(errors, e => e.Contains("duplicate case"));
        Assert.Contains(errors, e => e.Contains("invented") || e.Contains("hash"));
    }

    [Fact]
    public void Rejects_Fake_Backend_For_SelfTest()
    {
        var good = GoodReport() with { Backend = new("Prod", "1.0") };
        Assert.Contains(EvidenceValidator.Validate(good), e => e.Contains("backend must be null"));
    }

    [Fact]
    public void Workspace_Refuses_Escape_And_Unmarked_Cleanup()
    {
        using var ws = FixtureWorkspace.Create("unit-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Throws<InvalidOperationException>(() => ws.Resolve("../escape.bin"));
            Assert.Throws<InvalidOperationException>(() => ws.Resolve("C:\\Windows\\x"));
            string status = ws.DisposeAndReport();
            Assert.Equal("Confirmed", status);
        }
        finally { try { Directory.Delete(ws.Root, true); } catch { } }
    }
}
