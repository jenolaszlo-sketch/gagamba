# Complete bootstrap witnesses and targeted refusals

Normative witness appendix to the [owner decision](../rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md).

P-B2 and P-I are synthetic complete structural witnesses for the **B2
v3-pre** and **integrated v3-pre** schemas respectively. Each code block
is the exact compact UTF-8 JSON payload with no BOM or terminating newline.
Member order is fixture-specific. These witnesses use the original B2
association and closure shapes, SPEC §6.2.2 identity/profile constants,
F2 fixture and budget mapping, BJ-01–04, and the new closed v3 inventory.
All digests, namespaces, paths, times and IDs are synthetic examples.

For each witness provide corresponding independently constructed expected
data for its original invocation, secret, controller, installed pins and
identity, evidence directory and deadline, and for integrated its profile,
runtime, issuer, store generation and times. In this structural oracle the
expected semantic values are those printed in that positive witness. Supply
the separate expected capability SHA-256 as raw 32 bytes decoded from
`ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad`, which is SHA-256 of the synthetic UTF-8 bytes `abc`. This does not
establish any authentication, original process, installation or protected
source. It makes BJ-03 lexical acceptance and BJ-04 equality separately
testable. In an actual invocation, future verified caller inputs are required.
None may be copied from payload data to satisfy its own comparison.

| ID | UTF-8 bytes | SHA-256 | Expected |
|---|---:|---|---|
| P-B2 | 2693 | `52626efe08bac30435ffde51036413380a31cf17bdbfff8f2fc01841455ddfbf` | Pass structural checks |
| P-I | 4519 | `46ed4d54b119baacb4cb737dc70d1924cfca0a3241298c524081b6f691de069c` | Pass structural checks |
| P-LIMIT | 8192 | `70de1b51f0e1efc9cab3e2e83bf9e0e04f9a54e37bcb749d6d0e7c88571f4c27` | Pass structural checks |
| P-WS | 4521 | `d474bb76f6ee9362cd69ed60426965dd6c31c40fdc5c2b0e28b8459ef61a987d` | Pass structural checks |

### P-B2

```json
{"Version":3,"Execution":"00000000-0000-0000-0000-000000000001","Generation":"00000000-0000-0000-0000-000000000002","Session":"00000000-0000-0000-0000-000000000003","ControllerPid":123,"ControllerUid":1000,"Secret":"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=","Recovery":{"Directory":"/opt/gagamba/installations/1111111111111111111111111111111111111111111111111111111111111111/recovery","Sha256":{"Gagamba.Execution.Linux.M1.Recovery.dll":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff","Gagamba.Execution.Linux.M1.Recovery.deps.json":"6666666666666666666666666666666666666666666666666666666666666666","Gagamba.Execution.Linux.M1.Recovery.runtimeconfig.json":"7777777777777777777777777777777777777777777777777777777777777777","Gagamba.Execution.Linux.M1.dll":"8888888888888888888888888888888888888888888888888888888888888888","Gagamba.Execution.dll":"9999999999999999999999999999999999999999999999999999999999999999"}},"Owner":{"Directory":"/opt/gagamba/installations/1111111111111111111111111111111111111111111111111111111111111111/owner","Sha256":{"Gagamba.Execution.Linux.M1.Owner.dll":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","Gagamba.Execution.Linux.M1.Owner.deps.json":"6666666666666666666666666666666666666666666666666666666666666666","Gagamba.Execution.Linux.M1.Owner.runtimeconfig.json":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc","Gagamba.Execution.Linux.M1.dll":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd","Gagamba.Execution.dll":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"}},"EvidenceDirectory":"/var/lib/gagamba/evidence/case-1","AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","Integrated":null,"DelegatedParent":null,"B2InstallationIdentity":{"ArtifactSetSha256":"1111111111111111111111111111111111111111111111111111111111111111","OwnerClosureSha256":"2222222222222222222222222222222222222222222222222222222222222222","RecoveryClosureSha256":"3333333333333333333333333333333333333333333333333333333333333333","CoordinatorSha256":"4444444444444444444444444444444444444444444444444444444444444444","SeccompFilterSha256":"5555555555555555555555555555555555555555555555555555555555555555","RuntimeInventorySha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","Generation":"1111111111111111111111111111111111111111111111111111111111111111","ManifestSchema":1,"OwnerProtocol":1,"CoordinatorProtocol":1,"Bootstrap":1,"ProfileId":"ae4-linux-fixed-fixture-v1","ProfileVersion":"1","Qualified":false,"qualificationId":null,"qualificationRecordSha256":null},"B2DeadlineNs":3000000000}
```

### P-I

```json
{"Version":3,"Execution":"00000000-0000-0000-0000-000000000001","Generation":"00000000-0000-0000-0000-000000000002","Session":"00000000-0000-0000-0000-000000000003","ControllerPid":123,"ControllerUid":1000,"Secret":"AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=","Recovery":{"Directory":"/opt/gagamba/installations/1111111111111111111111111111111111111111111111111111111111111111/recovery","Sha256":{"Gagamba.Execution.Linux.M1.Recovery.dll":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff","Gagamba.Execution.Linux.M1.Recovery.deps.json":"6666666666666666666666666666666666666666666666666666666666666666","Gagamba.Execution.Linux.M1.Recovery.runtimeconfig.json":"7777777777777777777777777777777777777777777777777777777777777777","Gagamba.Execution.Linux.M1.dll":"8888888888888888888888888888888888888888888888888888888888888888","Gagamba.Execution.dll":"9999999999999999999999999999999999999999999999999999999999999999"}},"Owner":{"Directory":"/opt/gagamba/installations/1111111111111111111111111111111111111111111111111111111111111111/owner","Sha256":{"Gagamba.Execution.Linux.M1.Owner.dll":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","Gagamba.Execution.Linux.M1.Owner.deps.json":"6666666666666666666666666666666666666666666666666666666666666666","Gagamba.Execution.Linux.M1.Owner.runtimeconfig.json":"cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc","Gagamba.Execution.Linux.M1.dll":"dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd","Gagamba.Execution.dll":"eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"}},"EvidenceDirectory":"/var/lib/gagamba/evidence/case-1","AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","DelegatedParent":"/sys/fs/cgroup/gagamba","Integrated":{"Profile":{"OwnerDllPath":"/opt/gagamba/installations/1111111111111111111111111111111111111111111111111111111111111111/owner/Gagamba.Execution.Linux.M1.Owner.dll","OwnerDllSha256":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","DelegatedParentPath":"/sys/fs/cgroup/gagamba","SourceRootPath":"/opt/gagamba/validation-fixtures/7777777777777777777777777777777777777777777777777777777777777777","StagingParentPath":"/var/lib/gagamba/run/va-00000000000000000000000000000001","JournalDirectory":"/var/lib/gagamba/journal/va-00000000000000000000000000000001","ToolSourceName":"tool","InputSourceName":"input","CoordinatorSourceName":"coordinator","FilterSourceName":"filter","ToolSha256":"8888888888888888888888888888888888888888888888888888888888888888","InputSha256":"9999999999999999999999999999999999999999999999999999999999999999","CoordinatorSha256":"4444444444444444444444444444444444444444444444444444444444444444","ToolEnrollmentId":"tool-1","InputEnrollmentId":"input-1","TotalBudgetBytes":16777216,"MaxObjects":4,"ExpiresLeadNanoseconds":2000000000,"IntegratedRecovery":null},"Issuer":"00000000-0000-0000-0000-000000000004","StoreGeneration":"00000000-0000-0000-0000-000000000005","AcceptedAtNs":1000000000,"ExpiresAtNs":3000000000,"Controller":{"Pid":123,"Uid":1000,"Gid":1000,"PidNamespace":"pid:[4026531836]","UserNamespace":"user:[4026531837]"},"Runtime":{"HostPath":"/usr/lib/dotnet/dotnet","HostSha256":"6666666666666666666666666666666666666666666666666666666666666666","RuntimeDirectory":"/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.0","RuntimeInventorySha256":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA","HostFxrDirectory":"/usr/lib/dotnet/host/fxr/10.0.0","HostFxrInventorySha256":"BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB","RuntimeVersion":"10.0.0"},"InstallationIdentity":{"ArtifactSetSha256":"1111111111111111111111111111111111111111111111111111111111111111","OwnerClosureSha256":"2222222222222222222222222222222222222222222222222222222222222222","RecoveryClosureSha256":"3333333333333333333333333333333333333333333333333333333333333333","CoordinatorSha256":"4444444444444444444444444444444444444444444444444444444444444444","SeccompFilterSha256":"5555555555555555555555555555555555555555555555555555555555555555","RuntimeInventorySha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","Generation":"1111111111111111111111111111111111111111111111111111111111111111","ManifestSchema":1,"OwnerProtocol":1,"CoordinatorProtocol":1,"Bootstrap":1,"ProfileId":"ae4-linux-fixed-fixture-v1","ProfileVersion":"1","Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}}
```

### Size and ordinary semantic JSON controls

P-LIMIT is P-B2 padded with ASCII spaces after the closing brace to exactly
8192 UTF-8 bytes. It remains structurally acceptable with the same inputs.
P-WS is P-I with reversed root member order and leading tab and trailing
LF. It preserves semantic fields and requires no reserialization check.
The separate BJ-03 literal token remains unchanged in both.

## Targeted negative witnesses

Use the positive named in each row. Unless a row specifies a raw mutation
or changed expectation, make exactly the stated semantic change and encode
compactly with the positive's member order. Bounds, duplicate keys, escaping
and BOM mutations are defined at the byte level by the
[reproducible check](mechanical-checks.md). Each row's byte length and
digest are recorded in that check. Every row must refuse structurally with
no result. The last column identifies the intended failing rule with the
other expected inputs retained. Caller-level suitability is outside these
tests.

| ID | Base | Exact mutation | Intended failing rule |
|---|---|---|---|
| N01 | P-I | insert root B2InstallationIdentity=null | BB-01, prohibited presence |
| N02 | P-I | insert root B2DeadlineNs=null | BB-01, prohibited presence |
| N03 | P-B2 | remove root EvidenceDirectory | required root member |
| N04 | P-I | remove Integrated.Runtime.HostSha256 | required nested member |
| N05 | P-I | replace Integrated.Controller with [] | nested object required |
| N06 | P-B2 | replace root Integrated with {} | B2 requires literal null |
| N07 | P-I | remove Integrated.InstallationIdentity.qualificationId | BJ-02, required null |
| N08 | P-I | replace Integrated.InstallationIdentity.qualificationRecordSha256 with "" | BJ-02, null only |
| N09 | P-B2 | replace B2InstallationIdentity.Qualified with 0 | BJ-01, Boolean false |
| N10 | P-I | replace AuthorityHash first character by its uppercase B | BJ-03, literal lowercase |
| N11 | P-B2 | insert root Other=true | unknown member |
| N12 | P-I | append root duplicate Version=3 | duplicate decoded member |
| N13 | P-B2 | replace Version with 4 | unsupported version |
| N14 | P-I | replace Version with 2 | legacy version, refuse on pre route |
| N15 | P-B2 | pad spaces to 8193 bytes | size before parsing |
| N16 | P-I | replace Integrated.Controller.Pid with 124 | controller/root PID mismatch |
| N17 | P-I | insert Integrated.Profile.QualificationFault=null | Debug member prohibited |
| N18 | P-B2 | replace expected capability digest first octet with aa, leave bytes intact | BJ-04, comparison mismatch |
| N19 | P-I | Lowercase Integrated.Runtime.RuntimeInventorySha256 only | field-specific uppercase grammar |
| N20 | P-I | Append a root duplicate with literal escaped name \u0056ersion | decoded duplicate name |
| N21 | P-B2 | replace B2DeadlineNs with null | BB-01, B2 non-null deadline |
| N22 | P-I | prepend UTF-8 BOM | exact UTF-8 document rule |

For N12 replace the last `}` with `,"Version":3}`. For N20 replace
it with the exact byte suffix `,"\u0056ersion":3}`, retaining the
backslash. For N15 use ASCII spaces after the closing brace. For N22 the
prefix is hex `efbbbf`. N18 changes the expected digest only, and its
payload hash must equal P-B2. No bytes in these cases exercise a legacy
runtime or a protected operation.
