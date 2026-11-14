# Independent binary layout, variant and confirmation results

Offsets are zero-based within payloads. B2 §3 and SPEC §§6.2.3–6.2.5
establish the v2 layout. F1 applies the status-only change and the
normative owner IE rulings define the integrated v3-pre tail.

| Bytes | Width | Independent reconstruction for 9/36 |
|---|---:|---|
| 0–31 | 32 | Raw original artifact-set digest. Installation generation is the same value. |
| 32–63 | 32 | Raw recovery or owner closure selected by the expected role. |
| 64–65 | 2 | Owner protocol u16 LE 1. |
| 66–67 | 2 | Coordinator protocol u16 LE 1. |
| 68–69 | 2 | Bootstrap protocol u16 LE 1. |
| 70 | 1 | Role 0 for 9, role 1 for 36. |
| 71 | 1 | Pre status 0, legacy qualified comparison is 1. |
| 72–75 | 4 | Reserved zero, ending exactly at byte 76. |
| 76 | 1 | Integrated present form only, ID width 26 (`1a`). |
| 77–102 | 26 | UTF-8 exact `ae4-linux-fixed-fixture-v1`. |
| 103 | 1 | Version width 1 (`01`). |
| 104 | 1 | UTF-8 exact `1` (`31`). |

B2 uses 76 bytes only for both roles. Integrated allows 76 without any
prefix or 105 with one complete pair. The 29-byte pair is
`1a6165342d6c696e75782d66697865642d666978747572652d76310131`.
IE-01 places the choice solely in length. IE-02 gives counts in octets,
bounded 1–64, with exact remaining length. IE-03 limits the choice to
the original independently bound fixed profile. The 105-byte form fits
both 256-byte message and 512-byte outer caps. Base checks and expected
profile apply in both forms. Padding, additional pairs and alternate
profile bytes refuse.

| Message | Layout and caller sequence |
|---|---|
| B2 37 | 246 bytes with the bound pair. Original .NET GUID at 0–15, six 32-byte digests at 16, 48, 80, 112, 144, 176 (set, owner, recovery, coordinator, filter, runtime), four LE u16 constants 1 at 208, 210, 212, 214, status 0 at 216, profile count/ID/count/version at 217/218/244/245. Direct C→O, before B2 owner 36. |
| Recovery 10 | 36 bytes. Success/reason at 0/1, zero u16 at 2, raw expected artifact digest at 4. Pair (1,0) progresses only after matching 9 in selected context. (0,1–5) is a refusal, codes digest/closure/protocol/generation/qualified. B2 requires success before owner spawn. Integrated requires success before rendezvous. |
| B2 recovery 12 | 48 bytes. Original .NET GUID at 0–15 and raw expected artifact digest at 16–47. After checked B2 36, before Ready. Integrated owner-channel ID 12 remains StagedObjects, a different namespace. |

M1OP header version remains u16 LE 1, separate from JSON 3. The
48-byte frame and RecoveryWire XOR of octet 0 are inherited. Integrated
owner 36 uses the accepted direct namespace. The executed vectors are payload
and supplied-context controls. Actor authentication, channel sequences,
rights and Ready/staging effects were reviewed statically.

Independent packing matched all five GOLD payloads for 9/36/37/10/12.
Against reconstructed v3 W01/W02/W03 the only changed bytes were 71,
71 and 216 respectively. W04/W05 matched GOLD exactly. The independent
IE-V01–03 packing matched committed positives and the adopted owner text
reproduced all twelve IE-V01–12 bytes in the preparation record.

| ID | Source | Parent | Bytes | Change / construction | Expected | Observed |
|---|---|---|---:|---|---|---|
| E01 | B2 §3 / IE-01–03 | B2 | 76 | Build role 0 from reviewer hash labels | pass | pass |
| E02 | B2 §3 / IE-01–03 | B2 | 76 | Build role 1 from reviewer hash labels | pass | pass |
| E03 | B2 §3 / IE-01–03 | Integrated | 76 | Build role 0 from reviewer hash labels | pass | pass |
| E04 | B2 §3 / IE-01–03 | Integrated | 105 | Build role 1 from reviewer hash labels | pass | pass |
| E05 | B2 §3 / IE-01–03 | Integrated | 105 | Build role 0 from reviewer hash labels | pass | pass |
| E06 | B2 §3 / IE-01–03 | Integrated | 76 | Build role 1 from reviewer hash labels | pass | pass |
| E07 | B2 §3 / IE-01–03 | E04 | 77 | Replace whole tail with zero prefix only | prefix 1 | prefix 1 |
| E08 | B2 §3 / IE-01–03 | E05 | 103 | Keep complete ID and omit second prefix | prefix 1 | prefix 1 |
| E09 | B2 §3 / IE-01–03 | E04 | 105 | First prefix 65 | prefix 1 | prefix 1 |
| E10 | B2 §3 / IE-01–03 | E05 | 105 | Second prefix 3 | prefix 2 or tail length | prefix 2 or tail length |
| E11 | B2 §3 / IE-01–03 | E04 | 105 | Change ID initial a to A | profile bytes | profile bytes |
| E12 | B2 §3 / IE-01–03 | E05 | 105 | Change ASCII version 1 to 0 | profile bytes | profile bytes |
| E13 | B2 §3 / IE-01–03 | E04 | 104 | Delete final version byte | prefix 2 or tail length | prefix 2 or tail length |
| E14 | B2 §3 / IE-01–03 | E05 | 106 | Append extra tail byte 7f | prefix 2 or tail length | prefix 2 or tail length |
| E15 | B2 §3 / IE-01–03 | E03 | 74 | Truncate within reserved bytes | length | length |
| E16 | B2 §3 / IE-01–03 | E01 | 105 | Append otherwise valid integrated tail | length | length |
| E17 | B2 §3 / IE-01–03 | E04 | 105 | Route unchanged pre status 0 to v2 selection | status | status |
| E18 | B2 §3 / IE-01–03 | E03 | 76 | Change pre status to legacy 1 | status | status |
| E19 | B2 §3 / IE-01–03 | E02 | 76 | Replace pre status with 255 | status | status |
| E20 | B2 §3 / IE-01–03 | E01 | 76 | Set final reserved byte to 3 | reserved | reserved |
| E21 | B2 §3 / IE-01–03 | E04 | 105 | Put recovery role in owner message | role | role |
| E22 | B2 §3 / IE-01–03 | E03 | 76 | Substitute valid another artifact digest | digest | digest |
| E23 | B2 §3 / IE-01–03 | E04 | 105 | Substitute wrong closure in tail-present form | digest | digest |
| E24 | B2 §3 / IE-01–03 | E02 | 76 | Encode owner protocol big-endian | protocol | protocol |
| E25 | B2 §3 / IE-01–03 | E03 | 76 | Retain base but supply different expected profile | expected profile | expected profile |
| E26 | B2 §3 / IE-01–03 | E06 | 76 | Select unsupported legacy v1 evidence path | context | context |
| E27 | B2 §3 / IE-01–03 | E04 | 134 | Append second complete pair | prefix 2 or tail length | prefix 2 or tail length |
| F01 | B2 37 / F1 | F01 | 246 | Independent complete 37 | pass | pass |
| F02 | B2 37 / F1 | F01 | 246 | Legacy 1 at 216 | status | status |
| F03 | B2 37 / F1 | F01 | 246 | Use integrated selection | context | context |
| F04 | B2 37 / F1 | F01 | 246 | Change first session byte | session | session |
| F05 | B2 37 / F1 | F01 | 246 | Change profile version | profile bytes | profile bytes |
| K01 | SPEC §6.2.4 / B2 §3 / F1 | B2 10 | 36 | Valid response after matching 9 | pass | pass |
| K02 | SPEC §6.2.4 / B2 §3 / F1 | Integrated 10 | 36 | Valid response after matching 9 | pass | pass |
| K03 | SPEC §6.2.4 / B2 §3 / F1 | B2 12 | 48 | Valid confirmation after verified 36 | pass | pass |
| K04 | SPEC §6.2.4 / B2 §3 / F1 | Integrated 10 | 36 | Other artifact digest | digest | digest |
| K05 | SPEC §6.2.4 / B2 §3 / F1 | B2 10 | 35 | Drop final digest octet | length | length |
| K06 | SPEC §6.2.4 / B2 §3 / F1 | B2 10 | 36 | Valid negative with legacy reason 5 | well-formed refusal | well-formed refusal |
| K07 | SPEC §6.2.4 / B2 §3 / F1 | B2 10 | 36 | Success with reason 3 | pair | pair |
| K08 | SPEC §6.2.4 / B2 §3 / F1 | B2 10 | 36 | Select unsupported v1 evidence context | context | context |
| K09 | SPEC §6.2.4 / B2 §3 / F1 | B2 12 | 48 | Other artifact digest | digest | digest |
| K10 | SPEC §6.2.4 / B2 §3 / F1 | B2 12 | 48 | Prefix session octet wrong | session | session |
| K11 | SPEC §6.2.4 / B2 §3 / F1 | B2 12 | 47 | Drop final digest octet | length | length |
| K12 | SPEC §6.2.4 / B2 §3 / F1 | Integrated 12 | 48 | Route B2 12 to integrated context | context | context |
| K13 | SPEC §6.2.4 / B2 §3 / F1 | B2 12 | 48 | No preceding verified 36 | sequence | sequence |
| K14 | SPEC §6.2.4 / B2 §3 / F1 | B2 10 | 36 | No preceding matching 9 | sequence | sequence |


Independent positive payload bytes

~~~text
E01 d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd37543307227046f0ff73f573494097d869c9e8ea2e0758cc62beaf4227cf594501bdef54b4010001000100000000000000
E02 d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd37543307222a6564c6553e4230098d3fecbaca22eca7a494894dac8ce8819b6bfd3e02df11010001000100010000000000
E03 d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd37543307227046f0ff73f573494097d869c9e8ea2e0758cc62beaf4227cf594501bdef54b4010001000100000000000000
E04 d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd37543307222a6564c6553e4230098d3fecbaca22eca7a494894dac8ce8819b6bfd3e02df110100010001000100000000001a6165342d6c696e75782d66697865642d666978747572652d76310131
E05 d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd37543307227046f0ff73f573494097d869c9e8ea2e0758cc62beaf4227cf594501bdef54b40100010001000000000000001a6165342d6c696e75782d66697865642d666978747572652d76310131
E06 d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd37543307222a6564c6553e4230098d3fecbaca22eca7a494894dac8ce8819b6bfd3e02df11010001000100010000000000
F01 12ab80792334ba879ce8123456def987d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd37543307222a6564c6553e4230098d3fecbaca22eca7a494894dac8ce8819b6bfd3e02df117046f0ff73f573494097d869c9e8ea2e0758cc62beaf4227cf594501bdef54b44be5c415f4f9aac3ab5a1067089ac2179f39b4f392301302ad943b8ba31aa473a7e2b92d01732d0490e6bce5469286f0be25564f8708d329b325540537ed1c7e8cef127dd6c912b2d64fc0a795af81a956838fe19be9100a0201268fcbd943160100010001000100001a6165342d6c696e75782d66697865642d666978747572652d76310131
K01/K02 01000000d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd3754330722
K03 12ab80792334ba879ce8123456def987d65a5cc149c731a54b134d7a8bbb08bb8429d4e96f8504258154dd3754330722
~~~

All 46 binary cases matched expectation. Ten permit structural progression,
one recognizes a well-formed refusal, and 35 reject on an indicated
invariant. Sequence controls supply a synthetic predecessor state. They
do not run a transport or protocol state machine.
