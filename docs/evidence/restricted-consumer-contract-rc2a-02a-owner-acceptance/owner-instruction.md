# Owner instruction — RC2A-02A preservation and acceptance

Date: 2026-10-25 (Asia/Manila). This file records the owner's instruction for
this documentation-only slice. It authorizes no implementation, no test
execution and no RC2A-02B work.

## Verbatim decision authorization

> RC2A-02A Windows Contract Qualification has completed independent GLM review.
>
> **Independent verdict: SUPPORTED WITH QUALIFICATIONS.**
>
> The owner authorizes recording:
>
> **RC2A-02A ACCEPTED WITH QUALIFICATIONS.**
>
> Acceptance is limited to the Windows qualification implementation, executed
> tests, canonical vectors, mutation coverage, boundary assertions and
> supporting evidence identified by the independent review.

## Required actions authorized by the instruction

1. Preserve the complete GLM independent review and its supporting probe
   evidence (already committed on `review/gagamba-rc2a-02a-glm-q1`).
2. Bind the review to immutable Git identities and SHA-256 hashes.
3. Record owner acceptance and all three P3 qualifications.
4. Update the qualification plan, roadmap, work queue and handoff.
5. Prepare the next RC2A-02B stage for separate authorization (preparation only).

## Explicit prohibitions

- Do not implement or execute RC2A-02B.
- Do not merge accepted branches (the review branch is not merged into the
  decision branch).
- Do not recreate, overwrite, reformat, amend or recommit the GLM review.
- Do not alter historical raw evidence or rewrite test counts.
- Do not invent closure evidence for deferred obligations.
- Do not claim new tests were executed by this slice.
- No production or test source changes; documentation only.

## Owner-stated acceptance-basis numbers (as recorded by the independent review)

72/72 fields; 279 deterministic field cases; 222,615 mutation inputs
independently reproduced (114,378 accepted, 108,237 refused); 103/103 GLM probe
checks; 21/21 committed hexadecimal vector digests verified; four independent
encoding/parsing paths agreeing on the golden and minimal vectors; 14 refusal
categories reproduced; Windows public tests 67/0/0; required set 457/0/0 twice;
committed-plan filter 473/0/41; evidence hashes and Git bindings verified; no
confirmed production or qualification defects. These are the review's
independently reproduced results; this record neither reran nor extends them.

## Owner-stated interpretation constraints

- Do not describe the mutation results as proof that all semantic
  transformations are invalid; accepted mutations are canonical descriptions of
  the graphs they parse to.
- Do not characterize the byte-157 re-segmentation as a security bypass or a
  production defect; canonical representation validation and sandbox execution
  authorization are separate responsibilities.
