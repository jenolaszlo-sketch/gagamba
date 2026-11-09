using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Validation;

/// <summary>Accepted CJSON-1 canonical byte contract (P0 record section 6).
/// This type performs no I/O and grants no approval, eligibility, invocation, or
/// execution authority; it accepts or refuses exact canonical bytes only.</summary>
internal static class CanonicalJson
{
    internal const int MaximumBytes = 65_536;
    internal const int MaximumDepth = 8;

    internal static bool TryParse(byte[] bytes, out JsonElement root, out string failure)
    {
        try
        {
            root = Parse(bytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            root = default;
            failure = exception.Message;
            return false;
        }
    }

    /// <summary>Returns a detached canonical root element or throws
    /// <see cref="InvalidDataException"/>.</summary>
    internal static JsonElement Parse(byte[] bytes)
    {
        Require(bytes.Length is > 0 and <= MaximumBytes, "CJSON-1 size");
        foreach (var value in bytes)
            Require(value is >= 0x20 and <= 0x7e, "CJSON-1 ASCII printable bytes");
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = MaximumDepth });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("invalid CJSON-1", exception);
        }

        using (document)
        {
            var root = document.RootElement;
            Require(root.ValueKind == JsonValueKind.Object, "CJSON-1 root object");
            ValidateCanonical(root, 0);
            Require(EncodeValidated(root).AsSpan().SequenceEqual(bytes),
                "noncanonical CJSON-1 representation");
            return root.Clone();
        }
    }

    /// <summary>Canonical byte encoding of an already-valid CJSON-1 value tree.
    /// The value is validated first; no normalized or repaired output is produced.</summary>
    internal static byte[] Encode(JsonElement value)
    {
        ValidateCanonical(value, 0);
        return EncodeValidated(value);
    }

    internal static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static bool IsLowerHex(string value, int length) =>
        value.Length == length && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static byte[] EncodeValidated(JsonElement value)
    {
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output,
                   new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            value.WriteTo(writer);
        }

        return output.ToArray();
    }

    private static void ValidateCanonical(JsonElement value, int depth)
    {
        Require(depth <= MaximumDepth, "CJSON-1 depth");
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                string? previous = null;
                foreach (var property in value.EnumerateObject())
                {
                    ValidateName(property.Name);
                    if (previous is not null)
                        Require(string.CompareOrdinal(previous, property.Name) < 0,
                            "CJSON-1 keys are not strictly ascending");
                    previous = property.Name;
                    ValidateCanonical(property.Value, depth + 1);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray()) ValidateCanonical(item, depth + 1);
                break;
            case JsonValueKind.String:
                ValidateText(value.GetString()!, "CJSON-1 string");
                break;
            case JsonValueKind.Number:
                Require(value.TryGetInt64(out var number) && number >= 0 &&
                        value.GetRawText() == number.ToString(CultureInfo.InvariantCulture),
                    "CJSON-1 unsigned canonical integer");
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                break;
            default:
                throw new InvalidDataException("CJSON-1 value kind is not permitted");
        }
    }

    private static void ValidateName(string name) =>
        Require(name.Length > 0 && name.All(character => character is >= ' ' and <= '~'),
            "CJSON-1 property name");

    private static void ValidateText(string text, string diagnostic) =>
        Require(text.Length > 0 && text.All(character => character is >= ' ' and <= '~'), diagnostic);

    private static void Require(bool condition, string diagnostic)
    {
        if (!condition) throw new InvalidDataException(diagnostic);
    }
}
