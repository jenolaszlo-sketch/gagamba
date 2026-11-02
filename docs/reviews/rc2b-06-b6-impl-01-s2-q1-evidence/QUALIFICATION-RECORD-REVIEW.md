# QUALIFICATION-RECORD-REVIEW.md — receipt/record association (B6-T20c)

Independent evidence: `probes/ReviewerProbeTests.cs` (P18–P25, P27); the producer's T20c suite
independently re-run; code inspection of `InstalledMetadata.cs` and `InstalledFileSystem.VerifyActive`.

## 1. Selection mechanics (code inspection)

1. `InstalledMetadata.Receipt(bytes, manifest)` parses the receipt, rejects duplicate/unknown fields,
   requires `installedByUid == 0`, requires `artifactSetSha256 == manifest.Digest` **and**
   `manifestFileSha256 == manifest.Digest`, and — only if `expectedDigestSource ==
   "qualification-record"` — validates `qualificationId` with `ValidId` and reads
   `qualificationRecordSha256`. A `"unverified-candidate"` receipt must have both association fields
   null.
2. `ValidId` accepts 1–128 characters from `[A-Za-z0-9._-]`, rejects an empty value, leading/trailing
   `.`, the whole values `.` and `..`, and any path separator/whitespace/non-ASCII. It uses
   `char.IsAsciiLetterOrDigit` (strict ASCII, case-sensitive).
3. `SelectedRecordName` re-validates and constructs **exactly one** name: `id + ".json"`. A malformed
   identifier throws before any filesystem lookup.
4. `VerifyActive` opens `qualifications/<id>.json` with `openat2` `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|
   RESOLVE_NO_MAGICLINKS`, `O_NOFOLLOW`, requiring regular file, `uid=0`, `gid=0`, `mode=0444`,
   `nlink=1`. There is no directory enumeration and no latest-file-wins fallback.
5. `AcceptRecord` recomputes the record SHA-256 from the actual bytes and requires it to equal the
   receipt's `qualificationRecordSha256`, then requires field equality with the manifest:
   `qualificationId`, `artifactSetSha256 == manifest.Digest`, `profileId`, `profileVersion`, `rid`,
   `scope`, `buildConfiguration == "Production"`, `sourceCommit`, `sourceTree`.

## 2. Accepted B6-T20c matrix (nine cases)

| # | Case | Expected | Evidence | Result |
|---|---|---|---|---|
| 1 | Fresh installed candidate, no qualification | Installed/Unqualified | `VerifyActive` returns `Fresh` when `QualificationId` null; probe "fresh" | Supported |
| 2 | Missing selected record | Unqualified | `ReadTrustedFile` open fails → refusal; probe "missing" | Supported |
| 3 | Record hash mismatch | Unqualified | hash compared from bytes; P21 / probe "wrong-hash" | Supported |
| 4 | Multiple records, one receipt-selected id | Only selected evaluated | single-name construction, no enumeration; probe "multiple" | Supported |
| 5 | Stale/superseded record | No fallback | selection by id only; probe "stale" (scope old) | Supported |
| 6 | Record for another artifact digest | Unqualified | `artifactSetSha256 == manifest.Digest`; probe "other-digest" | Supported |
| 7 | Record for another RID/profile | Unqualified | field equality; probes "rid"/"profile"; P14–P16, P27 | Supported |
| 8 | Unaccepted producer-only evidence | Unqualified | no root-owned `0444` record; probe "producer" | Supported |
| 9 | Conflicting qualification/build metadata | Unqualified | `buildConfiguration=="Production"`; P23 / probe "configuration"/"conflict" | Supported |

Malformed identifiers are covered by producer cases and reviewer probes P22 and P27 (empty, `.`,
`..`, leading/trailing dot, `/`, `\`, whitespace, 129-char, non-ASCII). Identifier validation is
demonstrably **before** filesystem lookup (`SelectedRecordName` throws; the test asserts
`SelectedRecordName` throws for invalid ids without touching a filesystem).

Positive control: a matching production-shaped record from the test-owned model of the trusted
namespace is accepted (P18), and the same parser/selector/comparator runs in production. Negative
control: a tampered record is refused ("qualification record hash", trust-anchor probe).

## 3. Qualification authority boundary

A passing deterministic association test is **not** evidence that the host has installed an accepted
root-owned record. On this host `/opt/gagamba` does not exist and the reviewer did **not** create it
(HD-1 privileged installation is not authorized for this review). The `/etc/gagamba` two-part
operator decision (see FINDINGS F02) is a `qualify`-tool safeguard and is not consulted by the
verifier; the reviewer confirms that no real root-owned acceptance record exists, so no real
`Qualified` state was produced or claimed.

**Verdict (C): the receipt selects exactly one accepted qualification record, with no self-assertion
and no fallback.** This is deterministic (portable) evidence; native root-owned acceptance remains a
qualification obligation.
