# INSTALLATION-TRANSACTION-REVIEW.md — privileged installer

Independent evidence: code inspection of `eng/linux-m1/install_common.py`; the 11 transaction tests
independently re-run on WSL2 (11 passed); the transaction log at
`evidence/verification/verification/transactions-final.log`; `probes/trust_anchor_probe.sh`.

The installer was **not executed as root**: HD-1 privileged execution is not authorized for this
review. All findings are from code inspection plus nonprivileged simulation.

## 1. Transaction structure (code inspection)

`install(candidate, expected_digest, generation)`:

- requires `os.geteuid() == 0` and a WSL2 x86-64 host;
- requires `generation == expected_digest` (64 hex) and `digest == expected_digest`;
- parses the candidate manifest independently (`_manifest`), rejecting duplicate paths, bad modes,
  traversal paths and non-hex hashes;
- `_preflight_payload` rejects **all** special entries and links before reading any artifact bytes
  and enforces the exact candidate name set;
- runs the trusted verifier (`_tool("check-payload", candidate)`), which enforces the full 14-artifact
  grammar and production build identity;
- acquires an exclusive `flock` on `.install.lock` (root-owned 0600);
- `_check_existing` refuses to reinstall the active digest, refuses an active pointer with retained
  installations, and re-verifies any existing generation;
- stages under `installations/.staging-<random>` (mode `0700` during copy, then `0755`), copies each
  artifact from a pinned fd (`_copy_artifact`), re-checks `dev/ino/size/nlink/mtime/ctime`, verifies
  the digest while copying, applies the final mode, and `fsync`s the file and its directory;
- writes `manifest.json` and a first-install receipt atomically (`_atomic_write`: temp + `fchmod` +
  `fsync` + `rename` + directory `fsync`);
- runs `check-staged` on the fully staged generation (as root, so the trusted-ancestor predicate is
  evaluated) and only then publishes;
- `_publish_generation` `fsync`s the staged tree, `renameat2(RENAME_NOREPLACE)` publishes the
  generation, `fsync`s `installations`, then atomically writes `active` last and `fsync`s the root.

`qualify(record)` requires root, a matching root-owned decision file
(`/etc/gagamba/accepted-qualifications/<id>.json`, `_acceptance`), re-verifies the active staged
payload and platform, writes the record durably to `qualifications/<id>.json` (`RENAME_NOREPLACE`,
idempotent on collision with identical bytes) **before** atomically replacing
`installations/<digest>/install-receipt.json` with the bound receipt. An interruption can therefore
leave a retained generation and/or an unselected record, never a bound receipt without its record.

## 2. Adversarial scenarios — independent simulation

The producer's `test_install_common.py` was independently re-run (11/11 passed on WSL2). Cases:

| # | Scenario | Test / code evidence | Result |
|---|---|---|---|
| 1 | Missing candidate artifact | `_preflight_payload` exact-name check | Refuses |
| 2 | Mutated candidate artifact | `check-payload` digest gate | Refuses |
| 3 | Existing destination generation | `_check_existing` + `RENAME_NOREPLACE` | Refuses |
| 4 | Partial staging | `.staging-*` never published until complete; `check-staged` gate | Refuses |
| 5 | Interrupted copy | `_same_file` + digest during copy; stage cleanup on failure | Refuses / retained |
| 6 | Interrupted manifest publication | staging tree removed; `active` untouched | Refuses |
| 7 | Interrupted receipt writing | `active` is written last; receipt is atomic | Refuses |
| 8 | Interrupted active-pointer update | `test_interruption_after_rename_retains_both_generations` | Pass (both retained) |
| 9 | Concurrent installers | `flock` on `.install.lock` | Serialized |
| 10 | Concurrent verification vs pointer change | `test_reader_sees_complete_generation_on_both_sides_of_pointer_swap` | Pass |
| 11 | Two generations, same apparent identity, differing bytes | generation name == digest; digest recomputed; `RENAME_NOREPLACE` | Refuses |
| 12 | Candidate directory replaced between inspection and copy | fd-pinned copy + `_same_file` (`test_pinned_source_changed_while_copying_refuses`) | Refuses |

Additional code guarantees: symlink/traversal refusal (`test_manifest_path_traversal_refuses`,
`test_preflight_rejects_symlink_before_artifact_content_read`), extra-entry refusal
(`test_preflight_rejects_extra_candidate_entry`), duplicate JSON key refusal
(`test_duplicate_json_key_refuses`).

## 3. Durability assessment

The transaction performs file `fsync` before every `rename`, and directory `fsync` after every
`rename`/creation, including the `active` pointer. Publication order is: staged tree `fsync` →
generation `renameat2(NOREPLACE)` → `installations` `fsync` → `active` temp-write/`fsync`/`rename` →
root `fsync`. This is genuine crash-ordering, not merely atomic pathname replacement: a crash before
the `active` rename leaves the previous active generation selected (the "interruption after rename"
test confirms both generations are retained and the old pointer is unchanged).

Caveats (bounded, not defects against the accepted design):

- A crash between the generation `rename` and the `active` `rename` leaves an unpublished,
  unselected generation directory — retained, not removed (HD-4 favours retention).
- `_atomic_write` does not delete its temp file if `_write_all`/`fsync` raises before the rename; the
  temp is left in the parent directory (cleaned on the `qualify` path by the `finally` block, but a
  crash could leave `active.tmp-*`/`.tmp-*` residue). This is benign (unreferenced) and consistent
  with the accepted "retain rather than optimistically remove" policy.

## 4. Native obligation

A successful user-owned filesystem simulation does **not** prove root behaviour. Root-only steps
(`os.mkdir`/`fchmod` root ownership, `renameat2`, `flock`, `check-staged` under the real
`/opt/gagamba`) were not executed. This is recorded as a native qualification obligation
(NATIVE-QUALIFICATION-LIMITS.md), separate from the implementation review.

## 5. Verdict (E)

**A partial or concurrently changing generation cannot be selected as complete.** The installer
refuses on any incomplete/mismatched/extra/unsafe candidate, serializes cooperating writers, copies
from pinned fds with digest re-verification, and publishes the generation and then the `active`
pointer with correct `fsync`/`RENAME_NOREPLACE` ordering. Root-executed durability remains
unverified.
