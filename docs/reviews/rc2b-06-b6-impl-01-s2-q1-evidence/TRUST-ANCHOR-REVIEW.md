# TRUST-ANCHOR-REVIEW.md — where authority comes from

Independent evidence: code inspection of `src/Gagamba.Execution.Linux.M1/Installed/*` and
`eng/InstallTool/Program.cs`; the trust-anchor probe `probes/trust_anchor_probe.sh` run on WSL2
(nonprivileged); probe log `evidence/trust-anchor-probe.log`.

## 1. The only authority-bearing entry point

`InstalledFileSystem.VerifyActive()` is `internal static`, takes **no arguments**, and hard-codes
`private const string InstallRoot = "/opt/gagamba";`. It is the **only** method that can return a
trusted `InstalledResult`, and `InstalledResult.Qualified` is only produced at the end of that
method after: (a) the fixed root and all ancestors pass the ownership/writability predicate, (b)
`active`/generation/manifest/receipt digests all agree, (c) the exact artifact set and per-file bytes
match, (d) the receipt-bound qualification record is accepted, and (e) `InstalledPayload.CheckHost`
passes. `InstalledMetadata.cs` documents it: *"Only VerifyActive supplies a trusted record. Metadata
comparison by itself is never an installation decision."*

The public surface (`InstalledArtifactInspection`) is:

- `InspectActive()` → returns a human-readable string; only this can say `Qualified`, and it calls
  the fixed-root `VerifyActive()`.
- `CheckPayload(root, receiptPresent)` → `void`.
- `CheckPlatform(manifestBytes)` → `void`.
- `CheckAssociation(manifest, receipt, record)` → `void`.

`VerifyPayload` and `VerifyStaged` both return `void` and their XML docs state they *"return no
trusted identity."* There is no public factory, alternate constructor, caller-supplied digest,
environment variable or PATH-selected component that can produce a Qualified state. `eng/InstallTool`
exposes exactly these methods; `verify-active` takes no arguments.

## 2. No hidden test bypass

- No `#if`/`#else`/`[Conditional]`/`Environment.GetEnvironmentVariable`/`GAGAMBA_*` token exists in
  any file under `src/…/Installed/` or `eng/InstallTool/`.
- No test profile, environment flag or constructor converts untrusted metadata into Qualified.
- The deterministic T20c test injects a `Dictionary<string,byte[]> trusted` that models only the
  root-controlled record namespace and calls the pure helpers `InstalledMetadata.Receipt`,
  `SelectedRecordName`, `AcceptRecord` directly. That dictionary is test-local; it is not reachable
  from `VerifyActive`, which obtains record bytes only from a validated, root-owned file descriptor
  under `/opt/gagamba/qualifications`.

## 3. Adversarial objective — forged internally consistent candidate

`probes/trust_anchor_probe.sh` built a **fully self-consistent, user-owned** triple from the real
candidate manifest: a receipt whose `artifactSetSha256`/`manifestFileSha256` equal the manifest
digest and whose `qualificationRecordSha256` equals the record hash; and a record binding the same
digest, profile, version, RID, scope, source commit/tree and `buildConfiguration="Production"`.

Observed behaviour on WSL2 (uid 1000, `/opt/gagamba` absent):

| Invocation | Input | Result |
|---|---|---|
| `check-payload <candidate>` | non-root candidate | **exit 0** (positive control) |
| `check-staged <candidate>` | user-owned `/tmp` ancestors | **refused** — *"Directory is group/other writable."* |
| `verify-active` | fixed root | **`Unqualified Directory is group/other writable.`** (exit 1) |
| `check-association manifest receipt record` | self-consistent forged triple | **exit 0** (pure logic accepts) |
| `check-association manifest receipt tampered-record` | RID changed to `linux-arm64` | **refused** — *"qualification record hash"* |
| forged files ownership | — | `gaga:gaga 0644` (not root) |
| `/opt/gagamba` after the probe | — | absent (no privileged state created) |

**Expected result achieved: Unqualified.** Every digest agreed, the directory name matched, the
receipt fields matched the record and the artifact hashes matched — yet no Qualified state was
produced, because the trust root is fixed and root-owned and the association helper returns no
identity. The pure association logic accepting the triple is exactly the intended separation: a
consistent metadata model is *not* authority.

## 4. Attempted authority sources (all rejected as authority)

| Supposed authority source | Used as authority? |
|---|---|
| Caller-provided identity | No — `VerifyActive` takes no arguments |
| Candidate-provided identity | No — receipt/manifest are re-derived from the fixed root |
| User-owned receipt | No — must be root-owned `0444` under the fixed root |
| User-owned record | No — must be root-owned `0444` under the fixed root |
| Producer test report | No — a report/`acceptedAtUtc` cannot authenticate itself |
| Artifact-directory metadata | No — candidate directories are `trusted:false` |
| Environment variable | No — none consulted |
| PATH-selected component | No — fixed `/usr/bin/bwrap`, fixed runtime path |
| `/etc/gagamba/accepted-qualifications` decision | Not read by the verifier (only by the `qualify` tool) |

The verifier's acceptance basis is the root-owned `0444` record at
`/opt/gagamba/qualifications/<id>.json` selected by the receipt-bound `qualificationId`, exactly as
the accepted design §4.5 requires.

## 5. Residuals (outside the accepted model, stated not hidden)

A root/admin actor who owns `/opt/gagamba` can replace the tree, the receipt and the record. The
accepted design excludes root compromise (design §3.1, §5.5) and documents this residual. The
reviewer confirms the implementation does not claim otherwise.

## 6. Verdict (B)

**Internally consistent but user-controlled metadata cannot produce a Qualified state.** The only
authority path is the fixed, root-owned `/opt/gagamba` closure plus an accepted root-owned
qualification record, and the public surface returns no identity from caller-supplied roots. This is
a Nonprivileged (deterministic + WSL inspection) result; the equivalence to a real root-owned
installation is a native-qualification obligation (see NATIVE-QUALIFICATION-LIMITS.md).
