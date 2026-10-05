# GM-1A macOS lifecycle characterization

Date: 2026-10-05. Host: GitHub `macos-latest`, macOS 26.6.2 build 25G83,
**arm64**, launchd domain `gui/501`. Spike: `spikes/Gm1aLife/` (shell +
system python3 only; portable legs pre-validated on Ubuntu), entrypoint
`eng/gm1a-life.sh`, CI job `macos-probe`. No local Mac exists; first
remote run also validated the harness (one probe bug found and fixed:
escape must be attempted by a non-leader). Raw JSON per run under ignored
`artifacts/` (`gm1a-life-*.json`, also CI artifacts).

## Matrix result (12/12)

| Leg | Mechanism | Verdict |
|---|---|---|
| M1 | PG | common PGID, own group |
| M2 | PG | root exits (rc 0), descendants remain in PG |
| M3 | PG | `killpg` kills the group |
| M4 | PG | supervisor killed → workload SURVIVES (no owner-death) |
| M5 | PG | `setsid` escapee survives group kill; leaf dies (no containment) |
| M6 | launchd | common PGID under launchd job; `print` works in `gui/501` |
| M7 | launchd | root exits → **launchd cleaned remainder=True** (same-PG cleanup observed) |
| M8 | launchd | escaped session (sid 3296) **survived=True**, old-PG leaf dead |
| M9 | launchd | `stop` rc=3 cleans nothing; `bootout` rc=0 all-dead=True |
| M10 | launchd | submitter rc=0, job alive=True (launchd owns the job, not the client) |
| M11 | composed | pipe-EOF watchdog fired, workload dead (constructed cleanup) |
| M12 | composed+escape | leaf dead, escaper alive, fired (composition works, containment absent) |

## Conclusion (working hypothesis confirmed)

```text
process groups
  explicit group kill       YES
  root exit                 YES
  supervisor-death kill     NO
  containment               NO

launchd
  job-root death cleanup    YES, same process group (M7)
  external supervisor death NO (M10: job outlives the client)
  setsid-resistant          NO (M8: escape survives)
  containment               NO

watchdog + PG
  supervisor-death cleanup  YES, constructed (M11)
  arbitrary tree guarantee  NO
  setsid-resistant          NO (M12)
```

macOS exposes a weaker public lifecycle primitive than Windows and
Linux: launchd gives externally managed process-group cleanup, not a
true execution-domain boundary. That is a platform fact, not a Gagamba
failure — and precisely why GP-2 waited. The eventual contract should
describe guarantees/capabilities, never pretend all providers implement
one identical concept.

Method notes: `stop` ≠ `bootout` (only bootout removes + cleans);
`bootout` in `finally` always; unique labels per run; survival means
alive at the END of the window (sustained, not first-poll); zombies
count as dead; `setsid` from a session leader gives EPERM (informative:
launchd already session-isolates job roots, so escape must come from a
non-leader descendant).
