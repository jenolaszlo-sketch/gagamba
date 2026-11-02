namespace Gagamba.Execution.Linux.M1.Installed;

// Narrow read-only operator interface. Only the fixed-root inspection can report installed qualification.
public static class InstalledArtifactInspection
{
    public static string InspectActive()
    {
        var result = InstalledFileSystem.VerifyActive();
        return result.State switch
        {
            InstalledState.Qualified => $"Qualified {result.Digest} {result.QualificationId}",
            InstalledState.InstalledUnqualified => $"Installed/Unqualified {result.Digest}",
            InstalledState.Unqualified => $"Unqualified {result.Reason}",
            _ => $"Unavailable {result.Reason}"
        };
    }

    public static void CheckPayload(string root, bool receiptPresent)
    {
        if (receiptPresent) InstalledFileSystem.VerifyStaged(root);
        else InstalledFileSystem.VerifyPayload(root,false);
    }

    public static void CheckPlatform(byte[] manifestBytes) =>
        InstalledPayload.CheckPlatform(InstalledManifest.Parse(manifestBytes));

    public static void CheckAssociation(byte[] manifestBytes, byte[] receiptBytes, byte[] recordBytes)
    {
        var manifest = InstalledManifest.Parse(manifestBytes);
        InstalledMetadata.AcceptRecord(recordBytes,InstalledMetadata.Receipt(receiptBytes,manifest),manifest);
    }
}
