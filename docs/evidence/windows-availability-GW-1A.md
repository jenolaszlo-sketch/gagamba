# GW-1A Windows availability evidence (compact reviewed summary)

Date: 2026-10-03. Raw report: `artifacts/windows-probe-20261003-155836-d653b52c.json`
(ignored raw dir; this file is the retained summary). Availability only:
no sandboxed launch was attempted and no provider is claimed.

- Entrypoint: `eng/probe.ps1` (Release). Also `eng/probe.sh` (cross-platform;
  Windows-only legs report Unsupported off-Windows).
- Probe: `spikes/Gw1aProbe` — 7/7 mandatory Passed + W1-MINIMAL-LAUNCH NotRun
  (informational), aggregate Passed, cleanup Confirmed. The probe contains no
  launch-API P/Invoke by construction: the target API is resolved, never invoked.
- Verdict: **EXPORT-PRESENT-SCHEMA-UNPINNED**.
- Module: `C:\Windows\system32\processmodel.dll`, 315,392 bytes, file/product
  `10.0.26100.9444`, x64, 25 exports. OS build 10.0.26200.0, display 25H2.
- Dynamic-load resolution (full System32 path, at least as strict as the
  documented `LOAD_LIBRARY_SEARCH_SYSTEM32` pattern): all three resolved —
  `Experimental_CreateProcessInSandbox`, `Experimental_CreateProcessAsUserInSandbox`,
  `Experimental_QuerySandboxSupport`. PE export-table cross-check agrees.
- Full sandbox export surface (10 names): `BrokerSandboxedCreateProcess`,
  `Experimental_CompileSandboxSpecificationInternal`,
  `Experimental_CreateProcessAsUserInSandbox`,
  `Experimental_CreateProcessInSandbox`, `Experimental_FreeSandboxSpecification`,
  `Experimental_QuerySandboxSupport`, `SbeBrokerSandboxedCreateProcess`,
  `SbeCreateProcessAsUserInSandbox`,
  `SbeCreateProcessAsUserInSandboxWithPolicyRulesEnforcement`,
  `SbeCreateProcessHooks_IsSandboxedLaunch`; plus PSEC
  `IsProcessSecurityEnvironmentVersionSupported` /
  `QueryProcessSecurityEnvironmentSupport` (the 25H2+ successor contract).
- Leads for GW-1B: the OS exposes spec compile/free helpers
  (`Experimental_CompileSandboxSpecificationInternal`,
  `Experimental_FreeSandboxSpecification`) — signature discovery via MXC source
  or documented review, not prose inference.
- Job membership: **in-job=TRUE** for this shell — nested-job rules will apply
  to any engine-created job (per the documented `ui_restrictions` note). Probe
  and future conformance runners must record this per run.
- Header: none publicly (matches Learn 2026-06-01); local Windows SDK has no
  `*sandbox*.h` under `um/`.
- Schema: UNPINNED. Surveyed: Learn `createprocessinsandbox` 2026-06-01
  (SBOX file id, version `"0.1.0"`, field table) and `microsoft/mxc` main
  `external/windows-sdk/BaseContainerSpecification.fbs` (MIT) +
  `ProcessSecurityEnvironment.fbs`. Pin requires commit pin + license check +
  `flatc --conform` + review.
- I/O transport question confirmed open: `inheritHandles` must be FALSE and
  inherited handles are unsupported, so stdout/stderr capture path needs proof
  in GW-1B (STARTUPINFO redirection vs engine duplication).
- Export presence is not usability (MXC os-version-support v0.8.0): Tier 1 needs
  an enabled BaseContainer contract on 25H2+, else fallback tiers.

Next: GW-1B — pin schema, build a spec compiler against the compile/free
helpers or FlatBuffer bindings, attempt minimal launch with captured I/O, then
denial/descendant/race/workload evidence and the provider ADR.
