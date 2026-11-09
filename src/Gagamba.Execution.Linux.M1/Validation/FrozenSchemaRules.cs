namespace Gagamba.Execution.Linux.M1.Validation;

/// <summary>Identifier, path, and text rules from the accepted frozen schemas
/// (P0 record sections 6-10; S2 ValidId grammar). These helpers are
/// non-authoritative and perform no I/O.</summary>
internal static class FrozenSchemaRules
{
    internal static bool IsValidId(string value)
    {
        if (value.Length is < 1 or > 128) return false;
        if (value[0] == '.' || value[^1] == '.') return false;
        if (value is "." or "..") return false;
        foreach (var character in value)
        {
            var allowed = character is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '.' or '_' or '-';
            if (!allowed) return false;
        }

        return true;
    }

    internal static bool IsCanonicalUuid(string value)
    {
        if (value.Length != 36) return false;
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index is 8 or 13 or 18 or 23)
            {
                if (character != '-') return false;
                continue;
            }

            if (character is not (>= '0' and <= '9' or >= 'a' and <= 'f')) return false;
        }

        return true;
    }

    internal static bool IsAbsolutePosixPath(string value)
    {
        if (value.Length is < 2 or > 1024 || value[0] != '/' || value[^1] == '/') return false;
        var components = value.Split('/');
        for (var index = 1; index < components.Length; index++)
        {
            var component = components[index];
            if (component.Length == 0 || component == "." || component == "..") return false;
        }

        return true;
    }
}
