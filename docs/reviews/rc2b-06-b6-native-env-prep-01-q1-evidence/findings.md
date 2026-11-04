# Findings

Disposition: **PLAN SUPPORTED WITH QUALIFICATIONS**. No BLOCKER, MAJOR, or MINOR demonstrated. The items below are execution gates, not findings of existing defects or claims of native success.

## O01 — Exact platform bytes (OBSERVATION)

Sections: environment plan §§3–4, runbook §§1–5. Accepted source requires the exact Bubblewrap identity and compiled coordinator/filter pins; Ubuntu package version alone does not satisfy byte identity. Ubuntu advertises bwrap 0.11.1, but no frozen .deb or extracted executable hash, glibc export, static compiler inputs, candidate image digest, or exact hostfxr was independently measured here. A package update or rebuild can produce different bytes despite a matching version, causing hard refusal. The plan explicitly requires signed, frozen inputs and exact hash comparison before provisioning; stop on mismatch, do not substitute. Owner approval of the resulting inputs and any change to an accepted pin is required. This is not evidence that a matching package is obtainable.

## O02 — Delegation and runtime permissions (OBSERVATION)

Sections: environment plan §§6–7, runbook §4. A root-owned cgroup v2 hierarchy and `Delegate=memory pids` do not prove usable delegation. The proposed supervisor leaf, empty interior nodes, limits, bounded authority and non-root atomic-spawn probes are specified but unexecuted. Kernel/WSL/systemd settings may deny these operations or expose broader authority than desired. Stop if probes fail; a reviewed operator mechanism and explicit delegation authorization are required before P05. No change to verifier or privileged runtime fallback is accepted.

## O03 — Qualification bootstrap (OBSERVATION)

Sections: runbook §§5–7. F02 requires independent administrative acceptance of exact record bytes and digest before publication, while installed positive tests need Qualified selection. A record that claims those positives before they exist is circular. The runbook expressly requires truthful bounded scope and stops for a staged-publication owner decision if required evidence depends on Qualified execution. Neither current plan nor this review establishes a valid staged record or authorizes one; preserve separate `/etc` administrative and `/opt` runtime stores. Owner approval and potentially a bounded design decision are necessary before publication. Do not self-approve or infer qualification from installation.

## O04 — Installed driver and scenario recipes (OBSERVATION)

Sections: matrix §§1–5 and final gaps; runbook §7. Existing PN and legacy scripts do not establish installed v2 B2/B3 HELD. No runnable command is claimed for missing scenarios; matrix marks them blocked. An independently commissioned, reviewed, source-bound driver and exact per-case recipes/evidence are required before those cases and before any record claiming them. It is not required for a separately approved image import or bounded platform probe. No production authority bypass or synthetic identity injection is acceptable. Owner must authorize implementation and later test execution.

## O05 — Closure, truncation and capacity (OBSERVATION)

Sections: plan §§4, 9; matrix T20a and gaps; runbook §§7–8. Process maps and feature exercises, not file inventory alone, are needed to qualify owner/recovery runtime and dynamically loaded ICU/OpenSSL/zlib closure. Current path requires ICU; HD-5 invariant mode requires separate compatibility proof and decision. Dedicated 4096-byte evidence-reader `MSG_CTRUNC` remains unforced, and actual build peak/storage/log sizes are unmeasured. These are explicitly pending with stop/measurement gates. No owner approval is needed merely to document them; the future probes and any configuration change need their respective scoped approvals.
