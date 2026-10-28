# RC2A-04 owner acceptance — local packages and clean consumer

Commission: GAGAMBA-RC2A-04-PRESERVE-01 (owner acceptance and evidence preservation).
Date recorded: 2026-10-28. Recorded from the verified preservation baseline below.
This is a documentation-only acceptance record. It changes no implementation, package,
release, or qualification state.

## 1. Disposition

**RC2A-04 is ACCEPTED WITH QUALIFICATIONS.**

The owner accepts the completed RC2A-04 local NuGet packaging, clean-consumer
qualification, and mixed dependency-graph verification on the basis of the producer
evidence, the independently recomputed package bindings, and the independent review's
**SUPPORTED WITH QUALIFICATIONS** verdict. No further RC2A-04 remediation is required
for the accepted scope.

This acceptance does **not** authorize RC2B, package publication, release tagging,
Hufu changes, sandbox activation, native-platform qualification, or any subsequent
milestone.

## 2. Binding identities

| Identity | Value |
|---|---|
| Package source commit | `82cf8983a61f60a95d0aed0233ef0efac985148b` |
| Package source tree | `d37ad20f2d7e5ee4ee705e4c78b766df32b61f0c` |
| Evidence commit | `ec4e73bb3a922873e7b00cab4a9d85e23bc31d1c` (tree `58a5bad05f55b6ee43ee406e4f0930c1d77473a8`) |
| Independent review commit | `c5ba61f3ec6baf6afd37ed150e8dd28c01fbfeb0` (tree `7423b5d0b79d2da40d130eccd0fb894c975c5926`) |
| Review report | `docs/reviews/rc2a-04-package-clean-consumer-glm-independent-review.md` (blob `f89602c836e31683b039e2330ad6aacc3d5d3167`, byte SHA-256 `f6b8717752d34bc5e2b6b4efbba01af1685a8f96b2f3c2a7138b46847f25cab9`) |

Ancestry is linear and verified: `82cf898` (parent `3a5730a`) → `ec4e73b` →
`c5ba61f`; the acceptance commit below is a direct child of the review commit. No
history was reset, rebased, squashed, amended, or force-pushed. The review commit is
the preservation baseline.

Retained actual package artifacts remain outside the canonical repository under the
producer's `.gagamba-rc2a04/attempt-03/` directory; they were located, inspected, and
hash-verified by the independent review and are not copied or regenerated here.

## 3. Accepted results (verified provenance)

Full evidence: [producer qualification record](../evidence/rc2a-04/README.md) and
[independent review](../reviews/rc2a-04-package-clean-consumer-glm-independent-review.md).

- **Package inventory.** Five nupkg and five snupkg candidates at version
  `0.1.0-rc2a.82cf8983a61f60a95d0aed0233ef0efac985148b`: `Gagamba.Execution`,
  `Gagamba.Execution.Linux`, `Gagamba.Execution.MacOS`, `Gagamba.Execution.Windows`,
  `Gagamba.Runtime`. All ten SHA-256 values were independently recomputed by the
  review from the actual retained files and match
  [packages.json](../evidence/rc2a-04/packages.json); all five nuspecs bind repository
  commit `82cf898`, coherent dependency graph, and portable PDBs with SourceLink
  documents pinned to the same commit
  ([all-symbols.json](../evidence/rc2a-04/all-symbols.json)). Review §2 records the
  recomputed hash table; this decision does not duplicate it.
- **Clean consumer.** Independently reproduced by the review in its own isolated
  environment: PASS with **1,227 checks** across **21 literal vectors** from the
  accepted RC2A-02A vector manifest, with `Q2A-V-19` and `Q2A-V-20` correctly reported
  as not materialized ([results.json](../evidence/rc2a-04/results.json)).
- **Negative compilation refusals.** All four independently reproduced by the review:
  view construction `CS1729`, schema setter `CS0200`, `Codec.Launch` `CS0117`,
  `reference.Release` `CS1061`.
- **Regression qualification.** Windows and WSL2 each **505 passed / 0 failed / 0
  skipped** (48 public + 457 precise portable) with TRX provenance independently
  hash-verified; WSL2 remains WSL2, never native Linux.
- **Mixed dependency graph.** Six packages — the five Gagamba candidates plus actual
  `Penghou.Hufu 0.1.0-preview.11` — resolved and loaded managed types from a
  controlled feed with no conflicts, downgrades, unexpected packages, or activation.
- **CI.** Run `40649809860` (workflow `ci`, run 37) on `main` at the evidence commit:
  conclusion `success`, 10 jobs passed, 1 skipped.

## 4. Preserved qualifications and boundaries

The following restrictions are retained verbatim and are not resolved by this
acceptance:

1. **Native Linux and macOS were not tested.** Consumer qualification exists only for
   Windows 11 native and WSL2 Ubuntu; WSL2 is never native Linux. Native Linux and
   macOS consumer qualification remain **NOT RUN**.
2. **Hufu Sandbox integration remains unqualified.** The Sandbox package is
   unavailable; core-package coexistence in the mixed graph demonstrates nothing
   about the anticipated Sandbox integration graph.
3. **Actual sandbox activation was not exercised.** Mixed-graph type loading and
   passive decoding confer no execution authority; no provider construction, runtime
   creation, lifecycle, release, control, or recovery path was executed.
4. **V-19/V-20 were not materialized as valid 65,536-byte payloads.** They remain
   historical digest/recipe-bound vectors with no standalone byte export; the clean
   consumer tested size-boundary admission/refusal only, and no independent
   materialization is claimed.
5. **The six independent-review findings N1–N6 remain documented and non-blocking**
   (receipt path provenance; `results.json`/`hufu-core.json` commit-only origin;
   self-counted assertion semantics; unverifiable untracked-file check; WSL2 legs not
   rerun by the reviewer; full re-pack not re-executed). See review §7.

Boundary interpretations that this acceptance expressly does not make:

- Dependency restoration and managed type loading are **not** runtime integration
  qualification, and grant no live authority.
- Windows/WSL2 results are **not** broadened into cross-platform support claims.
- Unresolved boundaries above are **not** marked completed.
- The complete [RC2A-03 owner qualification and exclusion register §§5–6](restricted-consumer-contract-rc2a-03-owner-acceptance.md)
  carries forward unchanged, including R2A-F03 (no binary/package-byte
  reproducibility claim in either direction), the V-19/V-20/E2 digest-bound limit,
  E6 platform limits, and passive-only semantics. Recorded package hashes and
  repeatable test results are not a binary reproducibility proof.

## 5. Protected scope

No modifications were made to `src/`, `native/`, `tests/`, package manifests,
dependency versions, build or CI workflows, release configuration, existing evidence,
or existing review reports. No builds, repackaging, publication, tags, or runtime
tests were performed. Protected trees at the package source commit remain:
`src` `b8c167e3d7340a562221591bdc2b6df7adb372d1`, `native`
`85631d2f04512376621d3d09f51d67fd90f45335`, `tests`
`9c906b349b0bf0cffdee331fb5700f9b7828c199`. The only intended repository change is
this document.

## 6. Next milestone

**RC2B is NOT STARTED.** R1-F02/F03 remain OPEN for RC-2B, and AE4-B01/HUFU-R retain
their outstanding dispositions. RC2B requires separate scope definition and explicit
owner authorization. No subsequent milestone starts automatically from this record.
