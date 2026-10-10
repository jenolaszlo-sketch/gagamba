# AR-7 P01B — Windows full restricting-SID + LPAC construction gate

Date: 2026-10-10. Slice: bounded feasibility gate only.
**Conclusion: INCONCLUSIVE, unsupported in this execution context.**

No candidate process was ever created, so this run is **not** a `BLOCKED BY THIS PROBE`
verdict on the restricting-SID + LPAC hypothesis. The gate stopped in fixture setup:
fixture-owned window-station creation was denied (`ERROR_ACCESS_DENIED`) in this
assistant execution context. Windows continues refusing `restricted-offline-closure-v1`;
no P01C follows.

## Gate scope (from the post-P01 decision, section 3)

This is the pre-release **construction/token-establishment** sub-gate only:
derive a fully restricted primary token (`CreateRestrictedToken` with
`DISABLE_MAX_PRIVILEGE`, no `WRITE_RESTRICTED`/`SANDBOX_INERT`) carrying one fresh
execution SID, preflight an empty caller restricting list, compose with a documented
zero-capability LPAC + creation-time Jobs, create the target **suspended**, inspect
its token/Job membership externally, then terminate without ever releasing target
instructions. Useful closure, ARAP repeat, descendants, and standard-account
deployment were not reached and are not claimed.

## Source binding and environment

| Field | Run 1 | Run 2 |
| --- | --- | --- |
| Probe commit (clean) | `f41670a477f0fbace416fa0099dbc57644d699c3` | `7b45c20c1176660633680ea64c17d81f22524552` |
| Nonce | `1390153c4fb84fa5a89603ee6541b75e` | `ba357230728d4d6fb13b435a625c13d2` |
| p01b.cpp SHA-256 | `E522A631…CD5623` | `14649D0E…772AA51` |
| Binary SHA-256 | `BAB39481…9373913` | `BC88B2F1…54F567E3` |
| Driver SHA-256 (same) | `C326AF98…B5E2D4AA` | `C326AF98…B5E2D4AA` |
| Reused p01.cpp SHA-256 | `BBE95A8A…5E99A` (identical to P01 native source) | same |

Host (both runs): Windows NT `10.0.26200.0` x64, MSVC `14.51.36231`, public SDK
`10.0.26100.0`, PowerShell `5.1.26100.9444`, .NET SDK `10.0.401` (inventory).
Caller `LAPTOP-KBD8D8JG\Laszlos`: medium integrity `S-1-16-8192`, elevation 0,
Administrators deny-only — same privilege nuance as P01 (filtered administrator,
not a separately provisioned standard account). Execution context: console
session 1, own process jobbed with **zero UI restrictions**.

Gate source: [p01b.cpp](../../spikes/Ar7Feasibility/p01b.cpp),
[driver](../../spikes/Ar7Feasibility/run-p01b.ps1). Durable per-run records:
[P01B observations](ar7-feasibility/P01B/). Full transcripts under ignored
`artifacts/ar7/P01B/<nonce>/`.

## What was established before the stop (both runs)

- Caller non-elevated (in-code check), prior restricting list empty (`RESTRICT_COUNT caller 0`).
- Full restricted token created: `DISABLE_MAX_PRIVILEGE=1`, fresh execution SID
  present exactly once in the restricting list.
- Per-nonce AppContainer profile created with a package SID distinct from the
  execution SID.
- Owned fixture root + staged copy created with narrow DACLs (owner/system full,
  package + execution SID read/execute only); no host ACL, account, service, or
  firewall change.

## Stop point (both runs)

`GATE_ERROR owned station:5` — `CreateWindowStationW` for the fixture-owned
station/desktop failed with `ERROR_ACCESS_DENIED` before Jobs, target creation,
or any authority test. Run 1 assigned a mandatory-label SACL at creation; the
single authorized bounded correction (commit `7b45c20`) removed the SACL while
keeping the DACL byte-identical. Run 2 stopped identically, **falsifying the
SACL hypothesis**. The remaining cause is undetermined within authorized means:
own-process inspection shows a jobbed console-session-1 context with no UI
restrictions, which does not by itself explain the denial.

## Cleanup (both runs, independently verified)

Gate self-report plus external check agree: profile deleted (`S_OK`), package
folder absent, registry mapping not-found, staged file and fixture root removed,
driver watchdog never expired, fixture absent, no surviving `p01b`/`fixture`
processes. The station/desktop were never created, so there was nothing further
to remove. Preserved run directories under ignored `artifacts/` are the evidence
record, not residue. See [independent cleanup](ar7-feasibility/P01B/independent-cleanup.json).

## Consequence and non-claims

- The restricting-SID + LPAC hypothesis is **untested**: no restricted process
  ran, no ARAP repeat was attempted, no token composition was observed in a
  target. This outcome must not be cited as a candidate `BLOCKED` verdict.
- Interactive-user execution context is untested; the denial is scoped to this
  assistant execution context on this host.
- The one bounded setup correction is spent. Per the agreed sequence there is no
  P01C and no second native Windows composition in this phase: the Windows
  native track ends here, Windows keeps refusing the restricted profile, and the
  VM fallback stays pending per the post-P01 decision.
- Production providers, contracts, packaging, and the frozen baseline are
  unchanged. P02 evidence and its freeze are unaffected.
