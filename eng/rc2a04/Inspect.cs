using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

// Inspection tool only: deliberately separate from both ordinary package consumers.
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var exported = assembly.GetExportedTypes().OrderBy(x => x.FullName).ToArray();
var restricted = exported.Where(t => t.Namespace == "Gagamba.Execution.Restricted.Contracts").ToArray();
var inventory = exported.Select(t => new {
    type = t.FullName,
    constructors = t.GetConstructors().Select(c => c.ToString()).ToArray(),
    properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Select(p => new { name = p.Name, type = p.PropertyType.ToString(), publicSetter = p.SetMethod?.IsPublic == true }).ToArray(),
    methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Select(m => m.ToString()).ToArray()
}).ToArray();
foreach (var type in restricted)
{
    if ((type.Name.EndsWith("View") || type.Name == "ManifestDecodeResult") && type.GetConstructors().Length != 0) throw new Exception("Public construction: " + type);
    if (type.GetProperties().Any(p => p.SetMethod?.IsPublic == true)) throw new Exception("Public setter: " + type);
    string[] forbidden = ["Launch", "Prepare", "Release", "Stop", "Recover", "Authorize", "Renew", "Lookup"];
    if (type.GetMethods().Any(m => forbidden.Any(f => m.Name.StartsWith(f, StringComparison.OrdinalIgnoreCase)))) throw new Exception("Control API: " + type);
}
using var stream = File.OpenRead(args[1]);
using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
var reader = provider.GetMetadataReader();
var sourceLinkKind = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");
var sourceLinks = reader.CustomDebugInformation.Select(h => reader.GetCustomDebugInformation(h)).Where(i => reader.GetGuid(i.Kind) == sourceLinkKind).Select(i => System.Text.Encoding.UTF8.GetString(reader.GetBlobBytes(i.Value))).ToArray();
if (sourceLinks.Length != 1 || !sourceLinks[0].Contains(args[2], StringComparison.Ordinal)) throw new Exception("Missing exact SourceLink revision");
Console.WriteLine(JsonSerializer.Serialize(new { result = "PASS", assembly = assembly.FullName, restrictedTypes = restricted.Length, fullAssemblyIncludesLegacyLifecycleSpi = true, sourceLinks, inventory }, new JsonSerializerOptions { WriteIndented = true }));
