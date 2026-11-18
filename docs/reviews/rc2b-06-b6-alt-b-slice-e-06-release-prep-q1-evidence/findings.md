# Q1 findings register

Candidate `4202a2f7a99b85cfe1caf70d6a583f58e6551283`, tree `f987f32ced441263167649307003536dd493dd66`. Zero open product findings or managed acceptance blockers.

| ID | Severity and requirement | Reproduction and expected / actual outcome | Qualification impact | Next action |
|---|---|---|---|---|
| Q1-F02 | Closed historical blocker. E-2 naming rule and F02. | Rebuild F01, blocked and remediated cores in Debug and Release. Run the unchanged E-2 method. Expected and actual were Pass, prohibited-token Fail, Pass. Reflection counted 7 blocked names, 7 exact replacements, 0 remediated prohibited names and 0 replacement collisions. | Naming conflict closed for the exact candidate. | Reconcile this closure at bounded owner acceptance. |
| Q1-E01 | Non-blocking environmental observation. Managed runner provenance. | The frozen VSTest command timed out on all five selections per configuration after 15 seconds, exit 1, with zero tests. Authorized in-process xUnit v3 executed every selection, exit 0, with matching case membership. | The xUnit execution satisfies the gate. | Preserve both runner receipts. |
| Q1-E02 | Recovered infrastructure failure. Windows PowerShell 5.1 build policy. | Two sandboxed runs stopped at `owner-release` during restore, exit 1, with zero errors reported. Escalated execution used the unchanged 51-control script on the exact source archive and a fresh output root. All named outcomes matched, exit 0. | The approved rerun satisfies the gate. Restore used `NuGetAudit=false`; no new advisory coverage is claimed. | Preserve the incomplete runs and the successful replay. |

[Targeted E-2, reflection and policy replay](reproduction.md) · [Summary](raw/summary.json) · [Initial sandboxed restore failure](raw/policy-exact/owner-release.log.gz) · [Second sandboxed restore failure](raw/policy-retry/owner-release.log.gz) · [Successful 51-control ledger](raw/policy-confirmed/results.json) · [Carry-forward](carry-forward.md)
