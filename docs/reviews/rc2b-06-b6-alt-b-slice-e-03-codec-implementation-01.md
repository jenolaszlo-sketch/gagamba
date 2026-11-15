# Alternative B Slice E-3 v3-pre codec implementation

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-IMPLEMENTATION-01`

Date 2026-11-15 (Asia/Manila)

**E-3 CODEC IMPLEMENTATION COMPLETE — READY FOR INDEPENDENT Q1**

The first E-3 package implements only the isolated managed bootstrap and
binary payload codec. BB-02 Option A is enforced by a separate parser
and types, with no repository legacy or trusted-model dependencies.
Producer qualification passed. Independent implementation Q1 and owner
acceptance remain pending.

| Binding | Verified identity |
|---|---|
| Canonical main baseline | `1c7fb672055cc3322eb1633d37e5e84d764cb66d`, tree `cd3b29e8e5ad8790e0795418408b8d871e920f58` |
| Entry owner decision and evidence | `028a7c1858b5e7b506deff9d2d8d263afe3344ac` and `ebd4a064e4cbad696f60898940cc2c0f8f4ea6e9` |
| Adopted record | `f6a71562845d2cfe0aee5495864a509c5caabe7a`, raw SHA-256 `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |
| Binding owner rulings and schema | `f3639dc619dbfee39859afb7da553e2167d9b83d`, `93fe2c9155a21b12fd3486b9da743260897d508e`, and `a7248152d09e19d8f3775db397f340949fa9ee57` |
| Independent specification Q1 | `5f8b92e4874378bb365fcec80d794f964aa6f821`, evidence `1cb5ac2c2b467cbfb1c0d4ac85682fb979ffe961` |
| Tested code and test tree | `62f3448400459215d98c52e1ac2d24dbb531184a` before staging this report, its evidence, and the current-state blocks. All six tested blobs are bound in the traceability annex. |
| Worktree | One canonical main worktree, no stashes, 44 historical untracked paths matched original lengths and SHA-256 values. |

The only code additions are `ValidationPreCodec.cs`,
`ValidationPreCodecData.cs` and `ValidationPreCodecBinary.cs` in
`src/Gagamba.Execution.Linux.M1/Bootstrap/`. Managed coverage lives in
`ValidationPreCodecBootstrapTests.cs`, `ValidationPreCodecBinaryTests.cs`
and `ValidationPreCodecVectors.cs` in
`tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/`. The existing
SDK compile and test friend paths cover these files. No existing legacy
source, tests, package, project, solution, tooling or frozen record changed.
[Entry decision](../decisions/rc2b-06-b6-alt-b-slice-e-03-entry-owner-01.md) ·
[Six-file manifest and normative traceability](rc2b-06-b6-alt-b-slice-e-03-codec-implementation-01-evidence/normative-traceability.md).

The bootstrap decoder enforces one strict UTF-8 JSON object bounded
to 8,192 octets, 11 closed objects and all 99 required rows. Complete
B2 and integrated owner fixtures pass byte and SHA-256 checks. The
managed qualification also checks malformed or missing members, both
B2-only presence rules, exact original projection relationships and
binding, and allowed JSON variants. Q1-E01 accepts the valid escaped
member name, rejects duplicate decoded names, and rejects escapes in
the literal digest value. BJ-03 decodes exactly 64 literal lowercase
ASCII hexadecimal characters and BJ-04 compares the 32 bytes to a separate
expected digest. Both qualification associations remain literal null
and Qualified remains Boolean false. Unknown, legacy and mixed contexts
refuse without fallback. BB-02 remains open and unremediated.

The binary validator covers the 76-byte B2 and 76/105-byte integrated
9/36, B2-only 37, and inherited 36-byte 10 and B2-only 48-byte 12 with
supplied context. It checks exact offsets, lengths, profile pair, status
zero, digests, session and confirmation conditions. A well-formed negative
10 never progresses. The 258-byte release preimage and 32-byte
ReleaseRequest field remain outside codec operations. The result is
structural data only. Authentication, provenance, authority, message
delivery and operational effects remain caller or downstream duties.

Producer checks passed 146 focused managed cases, comprising 122 new
and 24 unchanged legacy regression cases, with zero failures and skips.
Eight Debug/Release builds succeeded. The metadata scan found zero
production inbound references to codec types or methods in M1, Owner,
Recovery or InstallTool in both configurations and detected known
callers in the test DLL. The unchanged verifier returned Release PASS
exit 0 and the expected Debug-as-production FAIL exit 1. See the
[commands, compiled identities and P4 evidence](rc2b-06-b6-alt-b-slice-e-03-codec-implementation-01-evidence/managed-and-revision-gates.md).
The initial sandbox-temp testhost startup error was corrected by moving
test artifacts into the regular external Windows temporary root.
The results cover managed specification and revision isolation, with
dynamic reflection and native/protected behavior outside the measured
surface.

E3-ENTRY-F01 remains closed. E-2 stays closed with its 39 qualifications and
23 obligations, including the unassigned first E-1 production consumer,
P0 external durability, and the protected-reader and policy deferrals.
External historical Git ancestry is unverified. P4 retains F-R1/O-R1,
O-R2, O-R3, S10/S11, S28, first-effect dominance, downgrade and the
observation-to-effect residual. E-4/E-6 P4 re-audits remain mandatory.
Composite Q-E1 stays open, with GAVA1 and cross-format handoff in E-4
and runtime correlation and the atomic set in E-6. `NATIVE-PENDING`
and all 331 inherited native skips remain unqualified.

Recommend `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-Q1` to verify the
exact committed inputs, construct independent vectors, review refusal
and authority boundaries and reproduce compiled isolation, RELEASE and
P4 evidence. A later owner decision must adjudicate findings and accept
the implementation separately. Production activation, external API
publication, BB-02 remediation, downstream implementation and protected
execution remain outside this work package.

The implementation commit is the commit containing this report. At
evidence capture the live authorized destination
`https://github.com/jenolaszlo-sketch/gagamba.git` reported
`a30d1807686ee15d39ef7e9103e8b992062bd971` on main, with eleven prior
local commits ahead. Publication is subject to final local preservation,
content review and an ordinary fast-forward check. The final handoff
records the resulting remote tip and transport outcome.
