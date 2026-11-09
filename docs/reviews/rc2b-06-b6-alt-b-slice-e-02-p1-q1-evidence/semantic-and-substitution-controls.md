# P1-Q1 — self-consistent semantic-invalidity and substitution controls

Date: 2026-11-09 (Asia/Manila)

All semantic controls below were made hash-consistent with a reviewer `Rehash` routine (recompute the changed artifact and propagate `Hm/Hf/Hs` into F/S/K), so any rejection is attributable to the targeted semantic rule rather than to a digest mismatch. Controls derived from the reviewer b3 chain unless noted. Evidence: `reviewer-vectors.txt`.

## Semantic invalidity with valid hashes

| Control | Accepted invariant (P0 record) | Expected refusal | Observed |
|---|---|---|---|
| S case id changed to `case-r8` (rehashed) | §7.2 duplicated case must equal K | `case id disagreement` | exact |
| F phase changed `pre`→`post` (rehashed) | §7.2/§9.1 phase equality | `phase disagreement` | exact |
| K topology changed to `b2-lifeline` (rehashed) | §7.2/§8.1/§9.1 topology equality | `topology disagreement` | exact |
| F `maximumSessionNs` reduced to 5e11 while K stays 6e11 (rehashed) | §9.1/§7.2 session-limit equality | `session limit disagreement` | exact |
| F/K session limit 1e9 with S timeouts 2e9 (rehashed) | §10.1 timeout ≤ F maximum | `S action timeout exceeds the F maximum session` | exact |
| Release template with F/K `allowRelease` false (rehashed) | §10.2 template/allowRelease equality | `release template disagreement` | exact |
| `"approved":true` inserted into K in canonical position (rehashed) | §7.2 no approval/self-hash fields | `K field set` | exact |
| K `schemaVersion` 1→2 (rehashed) | §7.1 version 1 | `schemaVersion integer` | exact |
| F `bootId` made non-canonical uppercase (rehashed) | §9.1 canonical boot format | `F bootId` | exact |
| M lengths 16777216+16777216 (rehashed) | §8.2 aggregate ≤ 16,777,216 | `M aggregate length` | exact |
| M object extended with `"extra":1` | §8.1 exact object keys | `M object field set` | exact |

Every rejection came from the intended semantic rule; none was a hash-mismatch artifact (rehash step verified by the fact that the same builder passes when unmodified).

## Identity substitution controls

| Control | Detection point | Observed |
|---|---|---|
| Replace M with the b2 chain's M (F/S/K untouched) | F `fixtureManifestSha256` vs recomputed `Hm` | `F fixtureManifestSha256 does not bind M` |
| Replace S with the b2 chain's S | S `fixtureManifestSha256` vs recomputed `Hm` | `S fixtureManifestSha256 does not bind M` |
| Keep K but change S timeouts (S stays valid, K not updated) | K `actionScriptSha256` vs recomputed `Hs` | `K actionScriptSha256 does not bind S` |
| Swap in the b2 chain's K | K `fixtureManifestSha256` vs recomputed `Hm` | `K fixtureManifestSha256 does not bind M` |
| F self-asserts a wrong `Hm` (downstream rehashed) | Recomputed `Hm` from M bytes, not F's claim | `F fixtureManifestSha256 does not bind M` |

No identity is trusted because an input asserts its own hash: every comparison recomputes the digest from the received bytes after the CJSON-1 equality proof.

## Content-addressed behavior note

A mutated K byte that remains schema-valid is accepted as a **new catalog object** (different `Hk`); that is correct content addressing, not a bypass. Fuzzing observed 4 such valid new-K identities out of 400 mutations (all K-slot); every M/F/S mutation was refused because it changes a referenced identity (see `failure-and-resource.md`).
