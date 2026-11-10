# E-2-P2 independent qualification (Q1) — evidence package

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-Q1`
Date: 2026-11-11 (Asia/Manila). Reviewer context: DeepSeek Flash Max (fresh
reviewer session). Scope: independent qualification and documentation only.

Reviewed implementation revision: `9b80969ce766b6d8939775710aa77d080c3ff674`
(tree `fc77bc1d673f787adae2d332141f1dd5f854e883`).

**Disposition: SUPPORTED WITH QUALIFICATIONS** (report §18).

This package contains reviewer-owned, independently constructed and executed
evidence. It is not producer evidence, not owner acceptance, and not
operational approval of R/Wc/P. Every fixture in this package is labeled
non-authoritative test content.

## Contents

| File | Content |
|---|---|
| [baseline-and-revision.md](baseline-and-revision.md) | verified baseline, ancestry, worktree/untracked preservation, identity anchors |
| [schema-reconstruction.md](schema-reconstruction.md) | independent R/Wc/P normative reconstruction from the adopted P0 record and the check-by-check verification matrix |
| [p1-reuse-and-reachability.md](p1-reuse-and-reachability.md) | compiled-token P1 reuse audit; Debug/RELEASE IL scans; authority-surface audit; null-argument observation |
| [p4-release-and-preservation.md](p4-release-and-preservation.md) | RELEASE verification results, P4 classification basis, E-1/F02/P1 preservation |
| [findings-and-qualifications.md](findings-and-qualifications.md) | classified findings Q1-F01..Q1-F06 and retained qualifications |
| [executed-results.md](executed-results.md) | every executed command, observed result, exit code, and reproduction instructions |
| [reviewer-fixtures/](reviewer-fixtures/README.md) | reviewer-owned canonical K/R/Wc/P bytes (B2/pre and B3/post) and digests |
| [probe/](probe/Program.cs) | reviewer-owned probe source (reflection over the compiled assembly) |
| executed/ | raw outputs: probe run, builds, test filters, full suite, verifiers, identity and untracked checks |

## Reproduction

    # 1. Build the reviewed revision (Debug and Release) and the test project
    dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Debug
    dotnet build tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug
    dotnet build src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj -c Release

    # 2. Run the reviewer probe from external scratch (source in probe/)
    dotnet run --project probe/Probe.csproj -c Release -- \
      <repo>/src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll \
      <repo>/src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll \
      <scratch>/reviewer-fixtures
    # expects: SUMMARY|total=97|failed=0, exit 0

    # 3. Tests
    dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~Validation.DetachedEvidence"
    dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~Tests.Validation"
    dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build

    # 4. RELEASE verification (unchanged eng/ verifier)
    dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll
    dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll

The probe source here is the exact reviewed text; it was executed from external
scratch (`%TEMP%\opencode\gagamba-p2-q1\probe`), not from the repository tree.
No production or test source file was created, modified, or deleted by this
commission.
