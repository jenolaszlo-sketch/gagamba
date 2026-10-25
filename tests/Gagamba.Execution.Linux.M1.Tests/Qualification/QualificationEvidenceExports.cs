// RC2A-02A machine-readable evidence exports (test-only). Exports are emitted
// from the authoritative committed test source (matrix and vector definitions)
// and from actual sweep observations. The evidence run sets
// GAGAMBA_RC2A02A_EXPORT_DIR and GAGAMBA_RC2A02A_SOURCE_COMMIT; without the
// directory variable the exports land next to the test assembly.
using System.Text.Json;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal static class QualificationEvidenceExports
{
    internal static readonly string[] ImmutableSlots =
    {
        "schema",
        "implementation",
        "invocation.toolPath",
        "invocation.inputPath",
        "invocation.workDirectory",
        "authority.noAmbientEnvironment",
        "authority.noIpcEndpoints",
        "authority.noExtraFds",
        "mount.toolExecutable",
        "core.disabled",
        "channels.stdinEof",
        "authority.networkQualification",
        "resources.inodeQualification",
    };

    internal static string ExportDirectory =>
        Environment.GetEnvironmentVariable("GAGAMBA_RC2A02A_EXPORT_DIR") ?? AppContext.BaseDirectory;

    internal static string SourceCommit =>
        Environment.GetEnvironmentVariable("GAGAMBA_RC2A02A_SOURCE_COMMIT") ?? string.Empty;

    internal static string WriteJson(string fileName, Action<Utf8JsonWriter> body)
    {
        Directory.CreateDirectory(ExportDirectory);
        var path = Path.Combine(ExportDirectory, fileName);
        using (var stream = File.Create(path))
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            body(writer);
        }

        return path;
    }

    internal static void WriteMutationCoverageMatrix()
    {
        var matrix = QualificationFieldMatrix.Cases;
        var path = WriteJson("mutation-coverage-matrix.json", writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("schema", "gagamba.rc2a-02a.mutation-coverage/1");
            writer.WriteString("sourceCommit", SourceCommit);
            writer.WriteNumber("fieldCount", QualificationFieldMatrix.Fields.Length);
            writer.WriteNumber("caseCount", matrix.Length);
            writer.WriteStartArray("classes");
            foreach (var mutationClass in QualificationFieldMatrix.MutationClasses)
            {
                writer.WriteStringValue(mutationClass);
            }

            writer.WriteEndArray();

            writer.WriteStartArray("fields");
            foreach (var field in QualificationFieldMatrix.Fields)
            {
                writer.WriteStartObject();
                writer.WriteNumber("index", field.Index);
                writer.WriteString("fieldId", field.FieldId);
                writer.WriteString("kind", field.Kind);
                writer.WriteStartObject("applicable");
                foreach (var mutationClass in QualificationFieldMatrix.ApplicableClasses(field.Kind))
                {
                    writer.WriteStartArray(mutationClass);
                    foreach (var item in matrix.Where(caseRow =>
                        string.Equals(caseRow.FieldId, field.FieldId, StringComparison.Ordinal) &&
                        string.Equals(caseRow.MutationClass, mutationClass, StringComparison.Ordinal)))
                    {
                        writer.WriteStringValue(item.CaseId);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
                writer.WriteStartObject("notApplicable");
                foreach (var mutationClass in QualificationFieldMatrix.MutationClasses)
                {
                    if (!QualificationFieldMatrix.IsApplicable(field.Kind, mutationClass))
                    {
                        writer.WriteString(mutationClass, QualificationFieldMatrix.NotApplicableReason(field.Kind, mutationClass));
                    }
                }

                writer.WriteEndObject();
                writer.WriteNumber("caseCount", matrix.Count(caseRow =>
                    string.Equals(caseRow.FieldId, field.FieldId, StringComparison.Ordinal)));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("cases");
            foreach (var item in matrix)
            {
                writer.WriteStartObject();
                writer.WriteString("caseId", item.CaseId);
                writer.WriteString("fieldId", item.FieldId);
                writer.WriteString("mutationClass", item.MutationClass);
                writer.WriteString("rowClass", item.CaseClass);
                writer.WriteString("description", item.Description);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        Assert.True(new FileInfo(path).Length > 0);
    }

    internal static void WriteVectorManifest()
    {
        var path = WriteJson("vector-manifest.json", writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("schema", "gagamba.rc2a-02a.vector-manifest/1");
            writer.WriteString("sourceCommit", SourceCommit);
            writer.WriteString("generator", "Independent ReferenceEncoder byte construction re-derived by Q2A_V_VectorQualificationTests; SHA-256 via System.Security.Cryptography.SHA256.");
            writer.WriteStartObject("golden");
            writer.WriteString("id", "Q2A-V-01-golden");
            writer.WriteNumber("bytes", QualificationGolden.Hex.Length / 2);
            writer.WriteString("sha256", QualificationGolden.DigestHex);
            writer.WriteString("hex", QualificationGolden.Hex);
            writer.WriteEndObject();
            writer.WriteStartArray("vectors");
            foreach (var vector in QualificationVectorRecipes.All)
            {
                var committed = QualificationVectorBytes.ById[vector.Id];
                writer.WriteStartObject();
                writer.WriteString("id", vector.Id);
                writer.WriteString("purpose", vector.Purpose);
                writer.WriteString("recipe", vector.Recipe);
                writer.WriteNumber("bytes", committed.Hex.Length / 2);
                writer.WriteString("sha256", committed.Sha256);
                writer.WriteBoolean("expectedSupports", vector.ExpectedSupports);
                if (committed.Hex.Length <= 8192)
                {
                    writer.WriteString("hex", committed.Hex);
                }
                else
                {
                    writer.WriteBoolean("hexOmittedForSize", true);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        Assert.True(new FileInfo(path).Length > 0);
    }

    internal static void WriteFlipSweep(QualificationFlipSweepResult sweep, IReadOnlyList<QualificationWireRegion> regions)
    {
        var regionByPosition = new string[sweep.ByteCount];
        foreach (var region in regions)
        {
            for (var i = 0; i < region.Length; i++)
            {
                regionByPosition[region.Start + i] = region.SlotId;
            }
        }

        var path = WriteJson("flip-sweep-survivors.json", writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("schema", "gagamba.rc2a-02a.flip-sweep/1");
            writer.WriteString("sourceCommit", SourceCommit);
            writer.WriteNumber("goldenBytes", sweep.ByteCount);
            writer.WriteNumber("mutations", sweep.Accepted + sweep.Refused);
            writer.WriteNumber("accepted", sweep.Accepted);
            writer.WriteNumber("refused", sweep.Refused);
            writer.WriteNumber("invalidValueAtZero", sweep.InvalidValueAtZero);
            writer.WriteNumber("durationMs", (long)sweep.Duration.TotalMilliseconds);
            writer.WriteStartObject("errorCounts");
            foreach (var pair in sweep.ErrorCounts.OrderBy(pair => (int)pair.Key))
            {
                writer.WriteNumber(pair.Key.ToString(), pair.Value);
            }

            writer.WriteEndObject();
            writer.WriteStartArray("immutableSlots");
            foreach (var slot in ImmutableSlots)
            {
                writer.WriteStringValue(slot);
            }

            writer.WriteEndArray();
            writer.WriteStartArray("survivors");
            foreach (var position in sweep.Survivors.Order())
            {
                writer.WriteStartObject();
                writer.WriteNumber("position", position);
                writer.WriteString("slot", regionByPosition[position]);
                writer.WriteNumber("acceptedValueCount", sweep.SurvivorValues[position].Count);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        });

        Assert.True(new FileInfo(path).Length > 0);
    }
}

public sealed class Q2A_E_EvidenceExportTests
{
    [Fact]
    public void Q2A_E_01_MutationCoverageMatrixExportIsWritten()
    {
        QualificationEvidenceExports.WriteMutationCoverageMatrix();
        var path = Path.Combine(QualificationEvidenceExports.ExportDirectory, "mutation-coverage-matrix.json");
        Assert.True(File.Exists(path));
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        Assert.Equal(72, document.RootElement.GetProperty("fieldCount").GetInt32());
        Assert.Equal(QualificationFieldMatrix.Cases.Length, document.RootElement.GetProperty("caseCount").GetInt32());
        Assert.Equal(72, document.RootElement.GetProperty("fields").GetArrayLength());
        Assert.Equal(QualificationFieldMatrix.Cases.Length, document.RootElement.GetProperty("cases").GetArrayLength());
    }

    [Fact]
    public void Q2A_E_02_VectorManifestExportIsWritten()
    {
        QualificationEvidenceExports.WriteVectorManifest();
        var path = Path.Combine(QualificationEvidenceExports.ExportDirectory, "vector-manifest.json");
        Assert.True(File.Exists(path));
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        Assert.Equal(23, document.RootElement.GetProperty("vectors").GetArrayLength());
        Assert.Equal(
            QualificationGolden.DigestHex,
            document.RootElement.GetProperty("golden").GetProperty("sha256").GetString());
        Assert.Equal(873, document.RootElement.GetProperty("golden").GetProperty("bytes").GetInt32());
    }
}
