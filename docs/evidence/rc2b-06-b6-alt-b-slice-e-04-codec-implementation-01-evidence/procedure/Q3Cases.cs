using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;

internal static class Q3Cases
{
    internal static int Run(string output, Func<bool, bool, JsonObject> witness, byte[] digest)
    {
        using var ledger = File.CreateText(Path.Combine(output, "q3-results.jsonl"));
        int failed = 0, total = 0;
        foreach (bool b2 in new[] { true, false })
        {
            string form = b2 ? "B2" : "Integrated";
            var context = new ValidationPreContext(3, b2 ? ValidationPreTopology.B2 : ValidationPreTopology.Integrated);
            string wire = witness(b2, false).ToJsonString(), projection = witness(b2, true).ToJsonString();
            void Check(string id, string rule, string want, string wireText, string projectionText)
            {
                var bytes = Encoding.UTF8.GetBytes(wireText);
                using var doc = JsonDocument.Parse(projectionText);
                string result;
                try {
                    var code = ValidationPreCodec.DecodeBootstrap(bytes, context, digest, doc.RootElement, out var decoded);
                    result = (code == ValidationPreRefusal.None) == (decoded is not null)
                        ? code.ToString() : "unexpected result publication";
                } catch (Exception exception) { result = "throw " + exception.GetType().Name; }
                bool pass = want == "bounded refusal" ? result is "Expectation" or "Comparison" : result == want;
                total++;
                if (!pass) {
                    failed++;
                    File.WriteAllText(Path.Combine(output, $"{form}-{id.Replace('/', '_')}.fixture.json"),
                        JsonSerializer.Serialize(new {wireText,projectionText}));
                }
                ledger.WriteLine(JsonSerializer.Serialize(new {form,id,rule,want,result,pass,
                    wireSha256=Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    projectionSha256=Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(projectionText)))}));
            }
            void Numeric(string token, string result) =>
                Check("uid/"+token, "Frozen §3 rule 2 and typed semantic projection equality", result, wire,
                    projection.Replace("\"ControllerUid\":72", "\"ControllerUid\":"+token, StringComparison.Ordinal));
            Check("complete", "Frozen §3 closed, matching bootstrap", "None", wire, projection);
            foreach (string token in new[] {"72.00","7.2e1","0.072E+3","720000e-4", "72e+0000000000000000000000000000000"})
                Numeric(token, "None");
            foreach (string token in new[] {"72.000000000000001","71.999999999999999","-72","-0.0","0.72e1","\"72\"","true","null"})
                Numeric(token, "Comparison");
            foreach (string token in new[] {"7.2e2147483647","720e-2147483647","72e-2147483648",
                         "72.0e2147483647","0.00072e2147483647","7200e-2147483648"})
                Numeric(token, "bounded refusal");
            foreach (string token in new[] {"72e+2147483648","72e-2147483649","72e+9999999999999999999"})
                Numeric(token, "Expectation");

            const string escapedPath = @"/q1/\u0065vide\u006ece";
            string escapedWire = wire.Replace("/q1/evidence", escapedPath, StringComparison.Ordinal);
            string escapedExpectation = projection.Replace("/q1/evidence", @"/q1/eviden\u0063e", StringComparison.Ordinal);
            Check("two-escaped-strings", "Frozen §3 decoded ordinal string equality", "None", escapedWire, escapedExpectation);
            Check("case-not-folded", "Frozen §3 decoded ordinal string equality", "Comparison", escapedWire,
                escapedExpectation.Replace("/q1/eviden", "/Q1/eviden", StringComparison.Ordinal));
            Check("malformed-surrogate/alias-wire", "Frozen §3 usable typed projection and entry §3 bounded refusal", "bounded refusal",
                escapedWire, projection.Replace("/q1/evidence", @"/q1/\uD800\u0058", StringComparison.Ordinal));
            Check("valid-surrogate-pair-distinct", "Frozen §3 exact semantic comparison without normalization", "Comparison", wire,
                projection.Replace("/q1/evidence", @"/q1/\uD83D\uDE00", StringComparison.Ordinal));
            Check("wire-exponent-still-invalid", "Frozen §3 wire integer grammar", "Schema",
                wire.Replace("\"ControllerUid\":72","\"ControllerUid\":7.2e1", StringComparison.Ordinal), projection);
        }
        Console.WriteLine($"Q3 supplemental assertions={total} matched={total-failed} unexpected={failed}");
        return failed;
    }
}
