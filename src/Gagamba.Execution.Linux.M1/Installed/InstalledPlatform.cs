using System.Security.Cryptography;
using System.Text;

namespace Gagamba.Execution.Linux.M1.Installed;

internal static partial class InstalledFileSystem
{
    // OS packages use 0644/0755. Resolve each fixed platform path from / under the same
    // strict ancestor checks as the active installation, and hash only the acquired fd.
    private static byte[] PlatformFile(int dir, string name, uint? mode = null)
    {
        using var file = OpenRegular(dir,name,true,64 * 1024 * 1024,0,0,mode);
        InstallJson.Require((file.Snapshot.Permissions & 0x12) == 0, "writable platform file");
        using var handle = new Microsoft.Win32.SafeHandles.SafeFileHandle((IntPtr)file.Value,ownsHandle:false);
        using var stream = new FileStream(handle,FileAccess.Read);
        var digest = SHA256.HashData(stream);
        Stable(file.Value,file.Snapshot,name);
        Reacquire(dir,name,file.Snapshot,false);
        return digest;
    }

    internal static string PlatformDigest(string directory, string name, uint? mode = null)
    {
        using var dir = OpenAbsoluteDirectory(directory,true);
        return Convert.ToHexStringLower(PlatformFile(dir.Value,name,mode));
    }

    internal static string PlatformInventory(string directory)
    {
        using var root = OpenAbsoluteDirectory(directory,true);
        var files = new SortedDictionary<string,byte[]>(StringComparer.Ordinal);
        int entries = 0;
        long total = 0;
        Visit(root,"",0);
        InstallJson.Require(files.Count > 0,"empty runtime inventory");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (path,digest) in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(path+"\0"));
            hash.AppendData(digest);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());

        void Visit(Fd dir,string prefix,int depth)
        {
            InstallJson.Require(depth <= 8,"runtime inventory depth");
            foreach (string name in Names(dir.Value))
            {
                InstallJson.Require(++entries <= 2048,"runtime inventory count");
                using var pinned = OpenPath(dir.Value,name);
                if (pinned.Snapshot.IsDir)
                {
                    using var child = OpenDirectory(dir.Value,name,true);
                    Same(pinned.Snapshot,child.Snapshot,"runtime directory replaced");
                    Visit(child,prefix+name+"/",depth+1);
                    Reacquire(dir.Value,name,child.Snapshot,true);
                }
                else
                {
                    total = checked(total+pinned.Snapshot.Size);
                    InstallJson.Require(pinned.Snapshot.IsFile && total <= 1024L * 1024 * 1024,"runtime inventory type or size");
                    files.Add(prefix+name,PlatformFile(dir.Value,name));
                    Reacquire(dir.Value,name,pinned.Snapshot,false);
                }
            }
            Stable(dir.Value,dir.Snapshot,"runtime inventory");
        }
    }
}
