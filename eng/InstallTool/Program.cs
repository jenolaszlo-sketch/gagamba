using Gagamba.Execution.Linux.M1.Installed;

try
{
    switch (args)
    {
        case ["verify-active"]:
            var result = InstalledArtifactInspection.InspectActive();
            Console.WriteLine(result);
            return result.StartsWith("Qualified ",StringComparison.Ordinal) ? 0 : 1;
        case ["check-payload",var path]:
            InstalledArtifactInspection.CheckPayload(path,false);
            break;
        case ["check-staged",var staging]:
            InstalledArtifactInspection.CheckPayload(staging,true);
            break;
        case ["check-platform",var hostManifest]:
            InstalledArtifactInspection.CheckPlatform(Read(hostManifest));
            break;
        case ["check-association",var manifest,var receipt,var record]:
            InstalledArtifactInspection.CheckAssociation(Read(manifest),Read(receipt),Read(record));
            break;
        default:
            Console.Error.WriteLine("Expected verify-active, check-payload DIR, check-staged DIR, check-platform MANIFEST or check-association MANIFEST RECEIPT RECORD");
            return 2;
    }
    return 0;
}
catch (Exception e)
{
    Console.Error.WriteLine("Refused: " + e.Message);
    return 1;
}

static byte[] Read(string path)
{
    using var stream = File.OpenRead(path);
    if (stream.Length is <= 0 or > 262144) throw new InvalidDataException("metadata length");
    var bytes = new byte[(int)stream.Length];
    stream.ReadExactly(bytes);
    if (stream.ReadByte() != -1) throw new InvalidDataException("metadata changed");
    return bytes;
}
