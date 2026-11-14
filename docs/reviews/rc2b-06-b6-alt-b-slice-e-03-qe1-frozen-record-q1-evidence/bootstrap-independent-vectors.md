# Independent bootstrap witness and refusal results

Two calculations have different purposes. The committed owner literals
(L-B, L-I) were extracted as exact bytes without code-block newlines, hashed,
and checked with the annex's specified synthetic inputs. L-B-2/L-I-2
reproduced the exact hashes for owner P-LIMIT and P-WS. Independently,
P-B/P-I were constructed from the transcribed inventory, separate synthetic
GUIDs and expected values, distinct SHA-256 labels, B2 role closure
preimages and a different sorted compact serialization. Owner examples
define their own synthetic projections. Q1 did not derive any actual
peer expectation from the received bytes.

| Witness | UTF-8 bytes | Raw SHA-256 |
|---|---:|---|
| Owner B | 2693 | `52626efe08bac30435ffde51036413380a31cf17bdbfff8f2fc01841455ddfbf` |
| Reviewer B | 2686 | `37736a0adac8202e7105c123a59e1ec8e5e52ab8d678d58b289807be29762e18` |
| Owner I | 4519 | `46ed4d54b119baacb4cb737dc70d1924cfca0a3241298c524081b6f691de069c` |
| Reviewer I | 4515 | `b2443db1968190ef8da3453fe593de8c64023c468daa52ccbc28297d7b39539f` |


The independently supplied projections describe only structural test data.
For new controls, B and I name the valid reviewer parents. The probe
keeps each parent's expected projection and original capability digest
fixed unless a row specifies a comparison-input substitution. The exact
mutations are given below and in the reproducible source. Results show
the intended first failing invariant of this oracle. The contract leaves
diagnostic choice and precedence among independently invalid fields open.

For a single grammar, membership or shape mutation, other data remain
fixed. An omitted or mistyped comparison field may inherently have no
value to compare. X-series field substitutions retain valid scalar
spelling where possible, with the indicated internal equality taking
precedence over a resulting external projection mismatch. The two
multibyte size controls intentionally violate the ASCII path rule.
U8192 isolates that path rule after the size check passes. U8193 is
8,192 Unicode code points and 8,193 octets and refuses at the byte gate.
All accepted member domains and permitted whitespace are ASCII, including
the decoded names, so there is no complete otherwise-valid v3 bootstrap
with literal non-ASCII UTF-8 bytes. Escape spellings are ASCII bytes.

J12 demonstrates ordinary escape spelling of the member name while H14
tests an escaped hex digit in the value. The former is valid and the
latter must refuse. J03/J04 duplicate the same decoded name with a different
spelling in different object depths. C06 confirms that matching a
caller-controlled value expresses only byte agreement. C07 holds the
synthetic context fixed and changes the separately supplied capability
digest. Neither control asserts authenticated provenance.

| ID | Category / source | Parent | Exact mutation or construction | Bytes | Expected | Observed invariant / result |
|---|---|---|---|---:|---|---|
| L-B | Owner literals | B | Committed literal, using documented synthetic expectations | 2693 | Pass | pass |
| P-B | Independent positive | B | Reviewer fixture, sorted members | 2686 | Pass | pass |
| L-I | Owner literals | I | Committed literal, using documented synthetic expectations | 4519 | Pass | pass |
| P-I | Independent positive | I | Reviewer fixture, sorted members | 4515 | Pass | pass |
| L-B-2 | Owner literals | B | Documented exact owner limit or whitespace transform | 8192 | Pass | pass |
| L-I-2 | Owner literals | I | Documented exact owner limit or whitespace transform | 4521 | Pass | pass |
| M01 | BB-01 | B | B2InstallationIdentity omitted | 1834 | Refuse | missing B2InstallationIdentity |
| M03 | BB-01 | B | B2InstallationIdentity = null | 1864 | Refuse | object B2InstallationIdentity. |
| M05 | BB-01 | I | B2InstallationIdentity = null | 4545 | Refuse | extra B2InstallationIdentity |
| M07 | BB-01 | I | Insert reviewer B2 identity object at integrated root | 5367 | Refuse | extra B2InstallationIdentity |
| M02 | BB-01 | B | B2DeadlineNs omitted | 2659 | Refuse | missing B2DeadlineNs |
| M04 | BB-01 | B | B2DeadlineNs = null | 2679 | Refuse | I64+ B2DeadlineNs |
| M06 | BB-01 | I | B2DeadlineNs = null | 4535 | Refuse | extra B2DeadlineNs |
| M08 | BB-01 | I | B2DeadlineNs = 22998000000 | 4542 | Refuse | extra B2DeadlineNs |
| M09 | BJ-01/02 | B | B2InstallationIdentity / Qualified omitted | 2668 | Refuse | missing B2InstallationIdentity.Qualified |
| M10 | BJ-01/02 | I | Integrated / InstallationIdentity / Qualified = true | 4514 | Refuse | False Integrated.InstallationIdentity.Qualified |
| M11 | BJ-01/02 | B | B2InstallationIdentity / Qualified = "false" | 2688 | Refuse | False B2InstallationIdentity.Qualified |
| M12 | BJ-01/02 | I | Integrated / InstallationIdentity / Qualified = 0 | 4511 | Refuse | False Integrated.InstallationIdentity.Qualified |
| M13 | BJ-01/02 | B | B2InstallationIdentity / qualificationId omitted | 2663 | Refuse | missing B2InstallationIdentity.qualificationId |
| M14 | BJ-01/02 | I | Integrated / InstallationIdentity / qualificationRecordSha256 omitted | 4482 | Refuse | missing Integrated.InstallationIdentity.qualificationRecordSha256 |
| M15 | BJ-01/02 | B | B2InstallationIdentity / qualificationRecordSha256 = "" | 2684 | Refuse | Null B2InstallationIdentity.qualificationRecordSha256 |
| M16 | BJ-01/02 | I | Integrated / InstallationIdentity / qualificationId = {} | 4513 | Refuse | Null Integrated.InstallationIdentity.qualificationId |
| M17 | BJ-01/02 | B | B2InstallationIdentity / qualificationId = "accepted-record-84" | 2702 | Refuse | Null B2InstallationIdentity.qualificationId |
| M18 | BJ-01/02 | I | Integrated / InstallationIdentity / qualificationRecordSha256 = "48d9489bd6dfc146e7287fb6fb5c0469a0dfd7b788dcba13e8f0a48dbec4eb42" | 4577 | Refuse | Null Integrated.InstallationIdentity.qualificationRecordSha256 |
| M19 | BJ-01/02 | I | Integrated / InstallationIdentity / qualificationId = "" | 4513 | Refuse | Null Integrated.InstallationIdentity.qualificationId |
| M20 | BJ-01/02 | B | B2InstallationIdentity / qualificationRecordSha256 = {} | 2684 | Refuse | Null B2InstallationIdentity.qualificationRecordSha256 |
| M21 | BB §4 | I | Integrated / Profile / IntegratedRecovery = {} | 4513 | Refuse | Null Integrated.Profile.IntegratedRecovery |
| M22 | BB §4 | I | Integrated / Runtime = [] | 4061 | Refuse | object Integrated.Runtime. |
| M23 | BB §4 | B | DelegatedParent = "/test" | 2689 | Refuse | Null DelegatedParent |
| M24 | BB §4 | I | Integrated / Controller / Gid omitted | 4504 | Refuse | missing Integrated.Controller.Gid |
| J01 | BB §4 JSON | B | Add UTF-8 BOM | 2689 | Refuse | JSON syntax |
| J02 | BB §4 JSON | I | Insert invalid UTF-8 C0 AF in evidence path | 4502 | Refuse | invalid UTF-8 |
| J03 | BB §4 JSON | B | Insert root duplicate as \\u0056ersion | 2703 | Refuse | duplicate |
| J04 | BB §4 JSON | I | Insert nested duplicate as P\\u0069d | 4531 | Refuse | duplicate |
| J05 | BB §4 JSON | I | Append trailing comma | 4516 | Refuse | JSON syntax |
| J06 | BB §4 JSON | B | Append second object | 2689 | Refuse | JSON syntax |
| J07 | BB §4 JSON | B | Replace final path component with \\uD800 | 2690 | Refuse | surrogate |
| J08 | BB §4 JSON | I | Prepend JSON comment | 4520 | Refuse | JSON syntax |
| J09 | BB §4 JSON | B | Replace final path component with \\q | 2686 | Refuse | JSON syntax |
| J10 | BB §4 JSON | B | Prefix non-JSON Unicode NBSP | 2688 | Refuse | JSON syntax |
| J11 | BB §4 JSON | I | Add ASCII tab/CR/linefeed, reverse root order | 4518 | Pass | pass |
| J12 | BB §4 JSON | I | Spell key as \\u0041uthorityHash, value unchanged | 4520 | Pass | pass |
| J13 | BB §4 JSON | B | Spell evidence path slash as \\/ | 2687 | Pass | pass |
| J14 | BB §4 JSON | B | Replace evidence ASCII q with U+00E9 | 2687 | Refuse | Path EvidenceDirectory |
| J15 | BB §4 JSON | B | Extra = false | 2700 | Refuse | extra Extra |
| J16 | BB §4 JSON | I | Integrated / Runtime / Extra = false | 4529 | Refuse | extra Integrated.Runtime.Extra |
| J17 | BB §4 JSON | I | Integrated / Profile / QualificationFault = null | 4541 | Refuse | extra Integrated.Profile.QualificationFault |
| J18 | BB §4 JSON | I | Integrated / Profile / QualifiedInstallation = null | 4544 | Refuse | extra Integrated.Profile.QualifiedInstallation |
| H01 | BJ-03 | B | AuthorityHash = "439DD7B0C114D5AB904011E81E3CC07CDCB01B68D62BAD21E6ED9E8393DE0859" | 2686 | Refuse | CapabilityHex AuthorityHash |
| H02 | BJ-03 | B | AuthorityHash = "439dd7b0C114D5AB904011E81E3CC07CDCB01B68D62BAD21E6ED9E8393DE0859" | 2686 | Refuse | CapabilityHex AuthorityHash |
| H03 | BJ-03 | B | AuthorityHash = "g39dd7b0c114d5ab904011e81e3cc07cdcb01b68d62bad21e6ed9e8393de0859" | 2686 | Refuse | CapabilityHex AuthorityHash |
| H04 | BJ-03 | B | AuthorityHash = "439dd7b0c114d5ab904011e81e3cc07cdcb01b68d62bad21e6ed9e8393de085" | 2685 | Refuse | CapabilityHex AuthorityHash |
| H05 | BJ-03 | B | AuthorityHash = "439dd7b0c114d5ab904011e81e3cc07cdcb01b68d62bad21e6ed9e8393de0859a" | 2687 | Refuse | CapabilityHex AuthorityHash |
| H06 | BJ-03 | B | AuthorityHash = " 439dd7b0c114d5ab904011e81e3cc07cdcb01b68d62bad21e6ed9e8393de0859" | 2687 | Refuse | CapabilityHex AuthorityHash |
| H07 | BJ-03 | B | AuthorityHash = "439dd7b0c114d5ab904011e81e3cc07cdcb01b68d62bad21e6ed9e8393de0859 " | 2687 | Refuse | CapabilityHex AuthorityHash |
| H08 | BJ-03 | B | AuthorityHash = "0x439dd7b0c114d5ab904011e81e3cc07cdcb01b68d62bad21e6ed9e8393de0859" | 2688 | Refuse | CapabilityHex AuthorityHash |
| H09 | BJ-03 | B | AuthorityHash = "" | 2622 | Refuse | CapabilityHex AuthorityHash |
| H10 | BJ-03 | B | AuthorityHash = null | 2624 | Refuse | CapabilityHex AuthorityHash |
| H11 | BJ-03 | B | AuthorityHash = "Q53XsMEU1auQQBHoHjzAfNywG2jWK60h5u2eg5PeCFk=" | 2666 | Refuse | CapabilityHex AuthorityHash |
| H12 | BJ-03 | B | AuthorityHash = [1,2] | 2625 | Refuse | CapabilityHex AuthorityHash |
| H13 | BJ-03 | B | AuthorityHash omitted | 2603 | Refuse | missing AuthorityHash |
| H14 | BJ-03 | B | Escape first hex digit as its JSON Unicode escape | 2691 | Refuse | AuthorityHash literal token |
| C01 | BJ-04 | B | Digest of separate reviewer synthetic capability label | 2686 | Pass | pass |
| C02 | BJ-04 | B | Flip high bit of expected last octet | 2686 | Refuse | expected digest equality |
| C03 | BJ-04 | B | Expected digest omitted | 2686 | Refuse | expected digest width |
| C04 | BJ-04 | B | Keep first 31 expected octets | 2686 | Refuse | expected digest width |
| C05 | BJ-04 | B | Append an expected octet | 2686 | Refuse | expected digest width |
| C06 | BJ-04 | B | Matching bytes supplied as caller-controlled arbitrary bytes | 2686 | Pass | pass |
| C07 | BJ-04 | B | Different synthetic capability within unchanged projection/catalog context | 2686 | Refuse | expected digest equality |
| S8191 | BB §4 size | I | Ordinary JSON whitespace to 8191 bytes | 8191 | Pass | pass |
| S8192 | BB §4 size | I | Ordinary JSON whitespace to 8192 bytes | 8192 | Pass | pass |
| S8193 | BB §4 size | I | Ordinary JSON whitespace to 8193 bytes | 8193 | Refuse | size |
| U8192 | BB §4 size | B | U+00E9 in path, padded to 8192 bytes and 8191 code points | 8192 | Refuse | Path EvidenceDirectory |
| U8193 | BB §4 size | B | U+00E9 in path, padded to 8193 bytes and 8192 code points | 8193 | Refuse | size |
| S00 | BB §4 size | B | Empty payload | 0 | Refuse | size |
| V01 | BB-03 version | I | Version = 1 | 4515 | Refuse | V3 Version |
| V02 | BB-03 version | I | Version = 2 | 4515 | Refuse | V3 Version |
| V03 | BB-03 version | I | Version = 4 | 4515 | Refuse | V3 Version |
| V04 | BB-03 version | I | Version = "3" | 4517 | Refuse | V3 Version |
| V05 | BB-03 version | I | Version = true | 4518 | Refuse | V3 Version |
| V06 | BB-03 version | B | Version token 3.0 | 2688 | Refuse | integer token |
| V07 | BB-03 version | B | Version token 3e0 | 2688 | Refuse | integer token |
| V08 | BB-03 version | B | Version token 03 | 2687 | Refuse | JSON syntax |
| V09 | BB-03 version | I | AuthorityMode = "validation-post" | 4516 | Refuse | PreMode AuthorityMode |
| V10 | BB-03 version | I | Route B2 bytes to integrated selection | 2686 | Refuse | extra B2DeadlineNs.B2InstallationIdentity |
| V11 | BB §4 | B | Original projection unavailable | 2686 | Refuse | original data missing |
| X01 | BB §4 relationships | B | B2InstallationIdentity / Generation = "4b506a731831d713519e593c2fa842715d2b3320e4eb3aa263a453d2d0a50636" | 2686 | Refuse | installation generation equality |
| X02 | BB §4 relationships | I | Integrated / Controller / Uid = 2071 | 4515 | Refuse | controller equality |
| X03 | BB §4 relationships | I | Integrated / ExpiresAtNs = 20400000000 | 4515 | Refuse | integrated interval |
| X04 | BB §4 relationships | I | Integrated / Runtime / RuntimeInventorySha256 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" | 4515 | Refuse | runtime inventory bytes |
| X05 | BB §4 relationships | I | Integrated / Profile / OwnerDllSha256 = "abababababababababababababababababababababababababababababababab" | 4515 | Refuse | owner DLL digest |
| X06 | BB §4 relationships | I | Integrated / Profile / JournalDirectory = "/srv/review/journal/va-2" | 4484 | Refuse | execution directory |
| X07 | BB §4 relationships | I | EvidenceDirectory = "/srv/review/evidence/other" | 4518 | Refuse | original projection |
| X08 | BB §4 relationships | I | Integrated / Profile / CoordinatorSha256 = "abababababababababababababababababababababababababababababababab" | 4515 | Refuse | coordinator equality |
| X09 | BB §4 relationships | I | Integrated / Runtime / HostFxrDirectory = "/usr/lib/dotnet/host/fxr/10.0.22" | 4515 | Refuse | runtime paths |
| X10 | BB §4 relationships | B | Recovery / Directory = "/opt/unrelated/recovery" | 2609 | Refuse | role directory |
| X11 | BB §4 relationships | I | Integrated / Profile / DelegatedParentPath = "/sys/fs/cgroup/other" | 4514 | Refuse | delegated equality |
| D01 | BB §4 scalar table | B | B2DeadlineNs = 9223372036854775808 | 2694 | Refuse | I64+ B2DeadlineNs |
| D02 | BB §4 scalar table | B | B2DeadlineNs = 0 | 2676 | Refuse | I64+ B2DeadlineNs |
| D03 | BB §4 scalar table | I | ControllerUid = true | 4515 | Refuse | Principal ControllerUid |
| D04 | BB §4 scalar table | I | Integrated / Controller / PidNamespace = "pid:[18446744073709551616]" | 4525 | Refuse | PidNs Integrated.Controller.PidNamespace |
| D05 | BB §4 scalar table | I | Integrated / Runtime / RuntimeVersion = "10.0.021" | 4516 | Refuse | RuntimeVersion Integrated.Runtime.RuntimeVersion |
| D06 | BB §4 scalar table | I | Integrated / Profile / ToolEnrollmentId = ".Q1-tool" | 4514 | Refuse | ValidId Integrated.Profile.ToolEnrollmentId |
| D07 | BB §4 scalar table | B | Secret = "Hx4dHBsaGRgXFhUUExIREA8ODQwLCgkIBwYFBAMCAQ9=" | 2686 | Refuse | Secret32 Secret |
| D08 | BB §4 scalar table | I | Integrated / Runtime / RuntimeInventorySha256 = "77b82b61ef9fed9ccd12853a2b3bd630624ba236baf4a0956921113f56b7d5fb" | 4515 | Refuse | UpperSha Integrated.Runtime.RuntimeInventorySha256 |
| D09 | BB §4 scalar table | B | B2InstallationIdentity / RecoveryClosureSha256 = "0000000000000000000000000000000000000000000000000000000000000000" | 2686 | Refuse | Sha B2InstallationIdentity.RecoveryClosureSha256 |
| D10 | BB §4 scalar table | B | Session = "00000000-0000-0000-0000-000000000000" | 2686 | Refuse | Guid Session |
| D11 | BB §4 scalar table | I | EvidenceDirectory = "/srv/review/../evidence" | 4515 | Refuse | Path EvidenceDirectory |
| D12 | BB §4 scalar table | I | Integrated / Profile / TotalBudgetBytes = 16777215 | 4515 | Refuse | Budget Integrated.Profile.TotalBudgetBytes |
| D13 | BB §4 scalar table | I | Integrated / Profile / ExpiresLeadNanoseconds = 120000000001 | 4517 | Refuse | Lead Integrated.Profile.ExpiresLeadNanoseconds |


Executed totals were 110 cases, 13 structural passes including four
owner reproductions, and 97 refusals. Each matched the expected result.
These results exercise BB-01/03, BJ-01–04 and the strict/closed JSON
contract. BB-02 is a separate static implementation observation.
