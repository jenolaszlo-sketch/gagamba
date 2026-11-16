using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gagamba.Execution.Linux.M1.Bootstrap;

string inputs = args[0];
byte[] expectedCapability = SHA256.HashData(Encoding.ASCII.GetBytes("Q1 independent original-capability"));
int cases = 0;
foreach (string projectionPath in Directory.EnumerateFiles(inputs, "*.projection").Order())
{
    string id = Path.GetFileNameWithoutExtension(projectionPath);
    var topology = id.StartsWith("B2_", StringComparison.Ordinal) ? ValidationPreTopology.B2 : ValidationPreTopology.Integrated;
    string wirePath = Path.ChangeExtension(projectionPath, ".wire");
    byte[] projectionBytes = File.ReadAllBytes(projectionPath);
    byte[] wire = File.ReadAllBytes(wirePath);
    using var projection = JsonDocument.Parse(projectionBytes.AsMemory());
    bool published = false;
    string result, stack;
    ValidationPreBootstrapData? decoded = null;
    try
    {
        result = ValidationPreCodec.DecodeBootstrap(wire, new(3, topology), expectedCapability,
            projection.RootElement, out decoded).ToString();
        published = decoded is not null;
        stack = "";
    }
    catch (Exception failure)
    {
        result = "throw " + failure.GetType().Name;
        stack = failure.ToString();
        published = decoded is not null;
    }
    Console.WriteLine(JsonSerializer.Serialize(new {
        id, result, published, stack,
        wireSha256 = Convert.ToHexStringLower(SHA256.HashData(wire)),
        projectionSha256 = Convert.ToHexStringLower(SHA256.HashData(projectionBytes))
    }));
    cases++;
}
return cases == 13 ? 0 : 2;
