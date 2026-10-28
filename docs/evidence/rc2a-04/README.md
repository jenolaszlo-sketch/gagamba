# RC2A-04 local package and clean-consumer qualification

Date: 2026-10-28, Asia/Manila. Producer disposition:
**QUALIFIED WITH RESTRICTIONS / REVIEW REQUIRED**.
This is implementation/qualification evidence, not independent review or owner acceptance.

## Identities and retained artifacts

- Starting main: `cc69f6a5c6892e4fb8f19f6a91ee3ab35ce60979`.
- Exact package source: `82cf8983a61f60a95d0aed0233ef0efac985148b`.
- Source tree: `d37ad20f2d7e5ee4ee705e4c78b766df32b61f0c`.
- Candidate version: `0.1.0-rc2a.82cf8983a61f60a95d0aed0233ef0efac985148b`.
- Accepted passive implementation R1: `271e34b45fa504f6bb7ef7359c4f2f1a9649e49d`;
  RC2A-03 owner decision: `1ce5c37f25c3bf49f55fc6a31a930f8430358d24`;
  independent Q2: `e060c91225274f13d53421b613a5736225ef6de4`.
- Evidence commit: the containing commit; packages bind to the earlier source commit above.

Local artifact location is the sibling Solo checkout's local qualification directory,
`.gagamba-rc2a04/attempt-03/`, outside Gagamba. Actual packages, symbol packages,
isolated feeds/caches, external consumer projects, raw logs and TRX files remain there.
The directory is an ordinary artifact directory, not a Git branch or worktree.
No package publication, release tag or worktree cleanup occurred.

The source delta is root README package documentation plus `eng/rc2a04/` qualification
tooling. No package configuration change proved necessary. Production `src`, native,
tests, workflows, frozen design, historical decisions/reviews/evidence retain their
starting Git identities: see [preservation receipt](preservation.json).
All 30 original untracked files were separately rehashed and remain unchanged.

## Package observations

[Package inventory](packages.json) records all five nupkg and five snupkg SHA-256s,
DLL hashes, zip members, repository commits, license and actual nuspec dependencies.
All packages contain exactly their intended `lib/net10.0/<package>.dll`, README,
and NuGet metadata. There are no native runtime assets, test harnesses, private M1
implementation assets or build/runtime activation targets. SourceLink's own packages
are private build inputs and do not enter the consumer dependency closure.

`Gagamba.Execution` has no package dependencies. Each provider requires the same
candidate Execution version; Runtime requires Execution and all three providers at
that candidate version. NuGet emits lower-bound dependencies; the external project
references use exact bracketed versions and their actual resolved graph is recorded.
Both `Version` and `PackageVersion` were overridden coherently. All repository commit
metadata and portable PDB SourceLink documents point to the full source SHA above;
all five symbol checks passed: [symbol receipt](all-symbols.json).
License expression is Apache-2.0; README is embedded. No preview.4 file was overwritten.

Execution nupkg SHA-256:
`c7a2974229ba4f51ee7092ea6776cd6342d65f613e2ca9267f6d5e976489b0b3`.
Execution snupkg SHA-256:
`5b19313affcd30553530331c414036706827671650b5e01ba68fc3d8c5d03511`.
Execution DLL SHA-256:
`60bd7de0065253bb68af69cde208042cf4912c559cf889b6e4927ec882462b67`.

[Actual DLL API inventory](api.json) contains 57 exported types, of which 18 are in
`Gagamba.Execution.Restricted.Contracts`. Inspection found no public view/result
constructors, public setters, or execution/control methods in that passive namespace.
The full assembly also intentionally contains the older execution lifecycle SPI;
the entire assembly is not represented as an authority-free API. API inspection
uses reflection and portable-PDB metadata in a separate tool. Neither ordinary
consumer uses reflection, internal types, friend access or project/source references.

## Clean consumers and isolation

The Windows clean direct consumer passed **1,227 assertions**, using all **21 literal
vectors** available in the accepted [vector manifest](../restricted-consumer-contract-rc2a-02a-windows/exports/vector-manifest.json).
Each literal vector's byte count and SHA-256 were verified independently before
decode; canonical re-encode, digest verification, wrong-length/mismatched/zero digest,
defensive copies and nested public inspection passed. All 873 proper prefixes of
the golden vector refused without partial values; trailing data, unsupported schema
and identifier refused. Supports/null behavior and passive reference constructor,
GUID/digest validation, copying and content equality passed.

The consumer checked the public 65,536 constant, inclusive input admission and
65,537-byte refusal. It did not construct a valid 65,536-byte exported payload.
The existing precise regression set executed the committed recipe-based size and
encode-limit checks. V19/V20 remain historical digest/recipe-bound vectors with no
standalone byte export; no independent materialization is claimed here.

Four separate expected compilation failures established that an ordinary consumer
cannot construct a view (CS1729), set its schema (CS0200), call Codec.Launch (CS0117),
or call passive-reference.Release (CS1061). These are successful negative checks,
not passing application builds. Full passive-surface evidence also comes from the
unchanged source identity, API inventory and focused existing regressions.

The second consumer restored a coherent six-package graph: the five candidate
Gagamba packages plus actual `Penghou.Hufu 0.1.0-preview.11`. Runtime/provider public
types compiled and loaded via BCL generic collections, without provider construction,
runtime creation or lifecycle calls. A final additional mixed run also loaded the
public Hufu enum `Penghou.Hufu.AuthorityAction`; the exact exercised
[consumer source](mixed-consumer.cs) and [output](mixed-hufu-type-run.txt) are retained.
Hufu core's actual net10 dependency group is empty. No downgrade, conflict,
unexpected package, missing provider or managed type loading failure was observed.

**Hufu Sandbox remains unavailable as an actual package.** Core-package coexistence
does not demonstrate the anticipated Sandbox integration graph. No Hufu source,
dependency constraints or provider selection were modified. Hufu integration remains
outside this commission; absence of the Sandbox package is a distinct prerequisite.

Each scenario restored through a generated NuGet.Config with sources and fallback
folders cleared, only the controlled filesystem feed configured, exact direct package
versions and an initially absent independent cache. Actual assets contained only
package libraries, exactly one expected package folder and the enumerated graph.
Every cached `.nupkg.metadata` source matched that controlled feed. See
[graphs](graphs.json), [direct project](direct.csproj.example) and
[mixed project](mixed.csproj.example). No global-cache, source-project, remote source
or sibling build-output fallback was used by consumers.

The feed inputs were explicitly copied from existing machine-cache nupkg archives,
with versions and hashes in [feed inputs](feed-inputs.json). Build feed: SourceLink
GitHub/Common and Build.Tasks.Git 10.0.401 plus System.IO.Hashing 10.0.12. Consumer
feed: five newly built candidates plus the actual Hufu core archive. Build-cache
contents were not used as consumer packages.

## Execution receipts and reproduction

Windows: SDK 10.0.401, runtime 10.0.12, win-x64, Windows 10.0.26200.
Ubuntu 26.04 **WSL2**: SDK 10.0.113, runtime 10.0.13; Microsoft WSL2 kernel
6.6.114.1. WSL2 execution here is the managed regression subset, not an additional
native Linux clean-package consumer qualification. Native Linux and macOS consumer
qualification remain **NOT RUN**.

| Environment | Exact public restricted filter | Exact portable M1 filter | Failed | Skipped |
|---|---:|---:|---:|---:|
| Windows | 48 | 457 | 0 | 0 |
| Ubuntu WSL2 | 48 | 457 | 0 | 0 |

Filters are `FullyQualifiedName~Gagamba.Execution.Tests.Restricted.` and
`FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.`.
No broad native-inclusive filter or sandbox probe was run. These are fresh actual
counts, not substituted historical counts. [Results](results.json) includes parsed
TRX counters and hashes of retained raw TRX files. [Logs](logs.txt) preserves consumer,
environment and regression output with operational directory paths substituted.

Reproduce from the exact source commit in a separate ordinary checkout if needed:

```text
python eng/rc2a04/qualify.py --artifacts <new absolute directory outside repository>
```

[Commands](commands.json) records exact arguments and exit codes. Expand
`${REPOSITORY}`, `${ARTIFACTS}`, `${WSL_REPOSITORY}`, `${WSL_ARTIFACTS}` and
`${WSL_BUILD}` to the chosen source/artifact paths. The runner's actual pack command
is `dotnet pack <project> -c Release --no-restore --artifacts-path <build-output>
-o <feed> -p:Version=<candidate> -p:PackageVersion=<candidate>
-p:RepositoryCommit=<full-source-SHA> -p:SourceRevisionId=<full-source-SHA>
-p:ContinuousIntegrationBuild=true -p:NuGetAudit=false`, repeated for the five projects.
The runner reproduces the initial mixed run. To reproduce the additional Hufu enum
loading check, replace the generated mixed Program.cs with the recorded
mixed-consumer.cs and run `dotnet run --project <mixed.csproj> -c Release --no-restore`.

Failed attempts remain in local `attempt-01` and `attempt-02`: the former stopped
at a genuinely missing isolated build-feed System.IO.Hashing dependency; the latter
stopped at a consumer-harness compile error (nullable success has no enum None).
They were corrected in new ordinary main commits, never counted as qualification.
The final exact-source attempt-03 passed. No accepted commit was amended or reset.

## Inheritance, security and review gate

The complete [RC2A-03 owner qualification and exclusion register](../../decisions/restricted-consumer-contract-rc2a-03-owner-acceptance.md)
§§5–6 remains inherited, including R1-F04/D4a versus D4b timing/provenance limits;
Windows/WSL2 distinctions and E6; V19/V20/E2; R2A-F03 binary versus result
reproducibility and its corrected origin/citation disclosure; 40-count erratum;
RC2A-02B authorization audit-trail limitation; canonical-rigidity correction;
precise/broad-filter accounting and preserved failed attempts; bounded re-encode
reasoning and raw/normalized byte distinctions; full-stage reconciliation; scoped
R2A-F01/F02/F04 and R1-F01/F05 dispositions. RC-2B R1-F02/F03, HUFU-R/R1-F06 and
AE4-B01 retain their outstanding dispositions. Mode work remains descriptive.
This new commission authorizes only its local package work and does not rewrite
the earlier decision's historical non-authorization wording.

Successful decoding, matching digests and passive reference equality confer no
execution authority, provider provenance, establishment, liveness, freshness,
expiry, temporal semantics, release/control/recovery or runtime confinement.
No cross-process hash determinism, binary/package-byte reproducibility, published
distribution, native sandbox, RC2B or Hufu integration claim follows from these
results. Test-result repeatability and recorded package hashes are not a binary
reproducibility proof. Production and native identities establish preservation only.

Independent review must verify the exact source/evidence commits, retained actual
packages against recorded hashes, nuspec and PDB metadata, public-only consumers,
restore-source isolation, precise filters and all inherited limitations. This record
does not self-issue that review. The containing evidence/status commit is to be
pushed directly to main without force and observed through automatically triggered
CI; its final run ID, exact head SHA and conclusion belong to the completion receipt.
No manual workflow dispatch or package publisher run is authorized here.

Next owner decision: after independent review, accept RC2A-04 with these restrictions
or require bounded remediation, retaining the separate unavailable Hufu Sandbox gate.
No subsequent milestone starts automatically.
