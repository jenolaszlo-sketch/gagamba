# FILESYSTEM-VERIFICATION-REVIEW.md — descriptor-relative verification

Independent evidence: code inspection of `InstalledFileSystem.cs` / `InstalledPlatform.cs`; the
producer's native test independently re-run on WSL2 as a non-root user (1 passed, 0 skipped);
`probes/` and `evidence/trust-anchor-probe.log`; the WSL2 host probe.

## 1. Required mechanisms (present)

| Mechanism | Location |
|---|---|
| `openat2` with `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS` | `OpenPath`, `OpenDirectory` via `PreparationInterop.RequiredResolve` |
| `O_NOFOLLOW`, `O_CLOEXEC`, `O_PATH` pin, `O_DIRECTORY` | `OpenPath`/`OpenDirectory` |
| fd-based hashing (never by path) | `ReopenPinned` → `ReadPinned` hashes the fd |
| before/after `fstat` + stable identity | `Same`, `Stable`, `Reacquire` compare dev/ino/mode/uid/gid/nlink/size/blocks/mtime/ctime |
| exact-set enumeration | `Walk` + `EnsureExact` |
| unsupported-kernel fail-closed | `EnsureLinuxX64` throws `NotSupportedException` if `openat2` unsupported → `Unavailable` |

`ReopenPinned` converts an `O_PATH` pin to a readable fd through `/proc/self/fd/<fd>` and re-`fstat`s
it. `RequireRegular` demands `nlink == 1`, no setuid/setgid, and `NoCapabilities` rejects file
capability xattrs. `EnsureNotSparse` rejects objects whose allocated blocks are smaller than their
size. Exact-set verification rejects any unlisted file or directory, any extra metadata file, and a
receipt-presence mismatch.

## 2. Mandatory probes — reproduced

The producer's `InstalledFileSystemNativeTests` was independently re-built from the reviewed source
copy and executed on WSL2 (Ubuntu 26.04.1, kernel `6.6.114.1-microsoft-standard-WSL2`, **uid 1000
non-root**, `openat2` present) against an independently extracted candidate:

```
Passed! - Failed: 0, Passed: 1, Skipped: 0
```

That single test is a multi-mutation probe with a valid positive control
(`VerifyPayload(root, receiptPresent)` succeeds first), then asserts refusal for:

1. unlisted extra file (`unlisted.bin`);
2. symlink artifact substitution;
3. FIFO artifact substitution;
4. hardlinked artifact (external second name → `nlink > 1`);
5. incorrect mode (`0644` on an artifact);
6. deleted artifact (missing);
7. mutated artifact byte;
8. **symlink ancestor substitution** (directory symlink to the candidate root);
9. sparse file (`SetLength` with no blocks) — refusal message contains "Sparse/abnormal".

Each negative asserts `InvalidDataException`; the valid candidate is re-verified between cases.

The reviewer additionally confirmed via the trust-anchor probe that `check-staged` on a user-owned
tree is refused (`Directory is group/other writable`) and that the exact-set/Hardlink/symlink rules
are the same as `VerifyPayload` (shared `Walk`/`EnsureExact`).

## 3. Producer-reported probes inspected (not fully re-executed by the reviewer)

The producer's `ptrace-race-probes.txt` (inspected) exercised two mutation races with a positive
control before and after:

- **Changed bytes/timestamps during hashing:** an unprivileged `ptrace` hook changed `mtime`/`ctime`
  on the opened M1 fd at a read syscall while content and mode stayed fixed → the verifier refused
  (`Metadata changed while reading`).
- **Byte-identical directory substituted during enumeration:** the candidate directory at the
  original path was swapped (same content, different inode) while the root fd was open → the
  verifier refused (`directory enumeration`).

The reviewer reproduced the **code paths** that implement these refusals (`Stable`, `Reacquire`,
`Same` compare `mtime`/`ctime`/`ino`/`dev`), but did not re-run the `ptrace` probes. They are
classified "producer test inspected" in the probe ledger.

## 4. Filesystem identity / TOCTOU

- Content identity and object identity are both checked: `fstat` snapshots include `dev`, `ino`,
  `mode`, `uid`, `gid`, `nlink`, `size`, `blocks`, `mtime`, `ctime`; every directory and file is
  re-acquired by name after hashing and compared (`Reacquire`), and after the whole walk
  `RecheckSelectors` re-opens `active`, `installations`, `qualifications`, the root, the generation,
  the manifest, the receipt and any selected record and compares snapshots.
- Parent-path replacement is detected by `Reacquire` and by a second `OpenAbsoluteDirectory(InstallRoot)`
  in `RecheckSelectors`.

**States exactly which attacks are prevented:** symlink substitution, symlink ancestor substitution,
hardlink inflation, FIFO/device substitution, extra/missing files and directories, sparse/abnormal
objects, incorrect mode/ownership/nlink, setuid/setgid, file capabilities, in-place timestamp/size
mutation during reading, directory/pointer/generation replacement during the verification window,
and traversal outside the generation root.

**Remaining residuals (outside the accepted model):** a root actor replacing bytes or restoring
timestamps; a kernel/OS compromise; snapshot inconsistency of the underlying filesystem. The
implementation does not claim perfect TOCTOU elimination — the design (§5.2, §5.5) names the
root-only residual. Non-root cannot write `/opt/gagamba` (root-owned 0755) or its ancestors, and
root is trusted.

## 5. Verdict (D)

**Native verification and file-identity checks enforce the accepted symlink, ownership, exact-set and
mutation rules.** Non-root reproduction passed with a valid positive control; the highest-risk cases
(symlink ancestor, hardlink, sparse, mutation) were independently demonstrated. Privileged
root-owned positive qualification remains pending (see §12 and NATIVE-QUALIFICATION-LIMITS.md).
