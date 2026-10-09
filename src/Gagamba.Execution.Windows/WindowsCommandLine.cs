using System.Text;

namespace Gagamba.Execution.Windows;

// Serialize argv for CreateProcessW using the documented Windows CRT rules.
// The legacy raw command-line route does not pass through this encoder.
internal static class WindowsCommandLine
{
    internal static string Serialize(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(Quote));

    private static string Quote(string argument)
    {
        if (argument.Length > 0 && !argument.Any(c => char.IsWhiteSpace(c) || c == '"'))
            return argument;
        var result = new StringBuilder("\"");
        int slashes = 0;
        foreach (char c in argument)
        {
            if (c == '\\') { slashes++; continue; }
            if (c == '"')
            {
                result.Append('\\', slashes * 2 + 1).Append('"');
                slashes = 0;
                continue;
            }
            result.Append('\\', slashes).Append(c);
            slashes = 0;
        }
        result.Append('\\', slashes * 2).Append('"');
        return result.ToString();
    }
}
