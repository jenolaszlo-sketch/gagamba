# NATIVE-QUALIFICATION-LIMITS.md — host suitability and deferred guarantees

Independent evidence: reviewer run of `wsl -d Ubuntu -- bash -lc "stat/findmnt/uname/os-release"`;
verifier run `verify-active` (trust-anchor probe); producer `verification/host-ancestor.log`.

## 1. WSL2 root-directory investigation

Read-only, nonprivileged commands (reviewer, uid-independent; the host runs the default user as
root but no write occurred):

```
$ uname -a
Linux LAPTOP-KBD8D8JG 6.6.114.1-microsoft-standard-WSL2 #1 SMP PREEMPT_DYNAMIC … x86_64 GNU/Linux

$ cat /etc/os-release
PRETTY_NAME="Ubuntu 26.04.1 LTS"   VERSION_ID="26.04"   VERSION="26.04.1 LTS (Resolute Raccoon)"

$ stat -c '%A %a %u:%g %n' / /tmp /opt
drwxrwxrwx 777  0:0 /
drwxrwxrwt 1777 0:0 /tmp
drwxr-xr-x 755  0:0 /opt

$ findmnt -no TARGET,SOURCE,FSTYPE,OPTIONS /
/ /dev/sdf ext4 rw,relatime,discard,errors=remount-ro,data=ordered

$ stat -c '%A %a %u:%g %n' /opt/gagamba
stat: cannot stat '/opt/gagamba': No such file or directory

$ cat /proc/sys/kernel/osrelease
6.6.114.1-microsoft-standard-WSL2
```

Interpretation:

1. **Actual ownership/permissions:** `/` is `drwxrwxrwx`, mode `0777`, owner `root:root`.
2. **Filesystem/mount type:** `/` is the real WSL2 Linux root on `/dev/sdf` (ext4), not a Windows
   DrvFs/9p share and not an overlay. `/mnt/wslg/distro` is a separate read-only mount of the same
   device. So the observation is not a Windows-permissions presentation artifact.
3. **Another mount namespace / environment:** none explains it. `/opt` is the normal `0755 root:root`;
   `/tmp` is the normal sticky `1777`. Only `/` is presented as `0777`.
4. **Verifier correspondence:** `InstalledFileSystem.VerifyActive` opens `/` first (via
   `OpenAbsoluteDirectory`, `trustedAncestors: true`) and calls
   `RequireDirectory(current.Snapshot, trusted:true, uid:0, requireExact0755:false)`. The predicate
   rejects group/other write (`Permissions & 0x12`). For `0777`, `0o777 & 0o022 = 0o022 ≠ 0`, so it
   throws *"Directory is group/other writable."* The reviewer's `verify-active` run returned exactly
   `Unqualified Directory is group/other writable.` (exit 1) — the refusal corresponds to the actual
   checked inode and path.
5. **Predicate correctness:** `RequireDirectory` requires `uid == 0`, `gid == 0`, `nlink >= 2`, no
   setuid/setgid, and no group/other write, and `requireExact0755` where the design demands it
   (install root, generation, directories). It correctly implements the accepted ancestor predicate
   **and does not add a sticky-bit exception** (it simply does not grant one). `/opt` (`0755`) passes;
   `/` (`0777`) fails.

**Refusal classification:** correct and fail-closed. `/` genuinely fails the accepted predicate, so
the verifier must refuse. The reviewer did **not** weaken the predicate, add a sticky-bit exception,
change `/` permissions, or attempt a privileged workaround.

## 2. Why `/` is 0777

This is a WSL2 presentation of the root directory's mode (a known WSL2 behaviour). The reviewer
confirmed it is the actual ext4 root inode (`/dev/sdf`, `findmnt`), not a DrvFs artifact and not
another namespace. Whatever the origin, the accepted predicate is unconditional, so qualification is
blocked on this host.

## 3. Smallest safe future environmental prerequisite (not executed)

To satisfy the root-owned trusted-ancestor requirement without weakening it or touching the host's
global state, a **disposable qualification host** (or a fresh WSL2 instance) is required in which
every ancestor from `/` to the install root is root-owned and not group/other-writable (a native
Linux host with a normal `0755` root, or a WSL2 instance whose `/` is not `0777`). The reviewer
identified but did not execute this.

## 4. Deferred guarantees (not S2 defects)

The following are explicitly out of the bounded S2 source scope (S2 commission §3, §7; design §11;
accepted S1/S2 boundaries) and are recorded as qualifications, not as implementation defects:

| Deferred guarantee | Why deferred |
|---|---|
| Root-owned native installation and a real `Qualified` host state | `/` fails the ancestor predicate; HD-1 privileged execution not authorized here |
| Root-executed installer durability / atomicity proof | privileged execution not authorized (nonprivileged simulation only) |
| Full native dependency closure (ICU / OpenSSL / zlib / native loader) | S2 commission §3 leaves it outstanding |
| Pinned launch, cleared-environment exec, bwrap fd execution | later stage |
| Loaded-assembly `Location` checks and installation-identity echo (`InstallationEchoRequest/Response`, `OwnerInstallationEcho`) | later B6 stage (wire handshake consumes, not defines, the identity) |
| Public provider descriptor v2 | HD-2 minimal passive surface; deferred |
| Pre-RELEASE re-read of `active` and enforcement during recovery | later stage |
| Generation removal with live-holder proof | fails closed (always refuses) until integration exists |
| Native Linux-host (B5-L01), arm64, macOS, Windows restricted execution | out of scope |
| HD-5 invariant-globalization compatibility proof | separate decision |

## 5. Verdict (I)

The reported WSL2 blocker is **genuine**: `/` is `root:root 0777` on the real ext4 root, so the
root-owned trusted-ancestor predicate correctly refuses. Native root-owned installation
qualification is **Pending** on this host. This does not invalidate the deterministic S2
implementation; it prevents a claim of successful real installed-root qualification. The WSL2
observation is genuine, so no repository change/rebase/reset was performed to hide it.
