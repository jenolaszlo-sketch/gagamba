# Publication readiness

The pinned owner starting revision is Q1 `e7240cb1c0443614f6cef67257501ed22318f77b`. The live origin baseline observed during preflight is `4dc0ee0998f0def6cb1cba304a76b3a9f59ac904`, an ancestor of Q1. The owner package adds this decision and its annex. The governing historical pointers and evidence keep their verified bytes. The qualified executable tree remains unchanged.

Before normal fast-forward publication, verify the owner commit's single Q1 parent, tracked and index cleanliness, one worktree, zero stashes, all historical and owner manifest entries, all 44 historical untracked paths and hashes, and the unchanged remote baseline. After publication, verify live main matches the exact owner commit and confirm the triggered GitHub Actions jobs and material skip results against the published E-5 baseline. The applicable workflow for a main push is `ci.yml`. The release tag, qualification branch and manual workflows have separate triggers. Store the final publication and CI receipt outside the self-hashed acceptance package once observed.

[Preflight](owner-preflight.json) · [E-5 CI baseline](../rc2b-06-b6-alt-b-slice-e-06-entry-owner-01-evidence/published-ci.json) · [CI workflow](../../../.github/workflows/ci.yml)
