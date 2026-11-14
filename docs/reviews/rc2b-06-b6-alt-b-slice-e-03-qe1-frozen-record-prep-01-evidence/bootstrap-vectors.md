# Bootstrap changed-field and boundary vectors

FRP-01 prevents a complete accepted bootstrap positive. These are exact
UTF-8 fragments for the settled field and gate checks. J01/J02 are partial
positive witnesses only. All fragments lack the inherited complete root or
identity fields and must refuse if presented as complete bootstraps. The
single-change expectations below apply when transplanted to an otherwise
valid complete record after FRP-01. No actual caller authentication is
asserted. The independently supplied comparator is the raw 32 bytes of
`ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad` unless stated otherwise. This is a synthetic digest.

Each JSON code-block line contains the ID, one display space, then the
literal payload. Encode the payload substring as UTF-8 without BOM or
terminal newline. For J14 retain the literal backslash in the encoded bytes.

| ID | Bytes | Settled field or gate expectation | Rule |
|---|---:|---|---|
| J01 | 230 | Partial B2 fragment passes settled-field checks only | BJ-01–04 |
| J02 | 243 | Partial integrated fragment passes settled-field checks only | BJ-01–04 |
| J03 | 212 | Missing status | BJ-01 |
| J04 | 226 | Numeric status | BJ-01 |
| J05 | 229 | Qualified status | BJ-01 |
| J06 | 207 | Missing ID association | BJ-02 |
| J07 | 228 | Non-null ID association | BJ-02 |
| J08 | 197 | Missing record association | BJ-02 |
| J09 | 228 | Non-null record association | BJ-02 |
| J10 | 147 | Missing hash | BJ-03 |
| J11 | 230 | Uppercase hash | BJ-03 |
| J12 | 229 | 63 characters | BJ-03 |
| J13 | 230 | Non-hex character | BJ-03 |
| J14 | 235 | Escape alias decodes to same text | BJ-03 |
| J15 | 230 | Wrong digest with correct syntax | BJ-04 |
| J16 | 230 | Unsupported bootstrap version | F1 / owner §2 |
| J17 | 230 | Legacy input offered to v3 selector | F1 / owner §2 |
| J18 | 229 | Truncated JSON object | F1 / owner §2 |
| J19 | 230 | J01 bytes with missing expected comparison input. Refuse at the comparison gate | BJ-04 |
| J20 | 230 | J01 bytes with first 31 bytes of the expected digest. Refuse at the comparison gate | BJ-04 |

```text
J01 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J02 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","Integrated":{"InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}}
J03 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"qualificationId":null,"qualificationRecordSha256":null}}
J04 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":0,"qualificationId":null,"qualificationRecordSha256":null}}
J05 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":true,"qualificationId":null,"qualificationRecordSha256":null}}
J06 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationRecordSha256":null}}
J07 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":"","qualificationRecordSha256":null}}
J08 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null}}
J09 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":{}}}
J10 {"Version":3,"AuthorityMode":"validation-pre","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J11 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J12 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"a7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J13 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ga7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J14 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"\u0062a7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J15 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"aa7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J16 {"Version":4,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J17 {"Version":2,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}}
J18 {"Version":3,"AuthorityMode":"validation-pre","AuthorityHash":"ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","B2InstallationIdentity":{"Qualified":false,"qualificationId":null,"qualificationRecordSha256":null}
```

## Size gates

Literal generation for each boundary input is J01 payload followed by ASCII
space octets `20` hex to the stated byte length. This tests size independently
of content. Passing the size gate leaves the partial input structurally
incomplete. A future valid full record can use permitted whitespace for
an exact-size complete positive after FRP-01.

| ID | Bytes | SHA-256 | Expected at size gate |
|---|---:|---|---|
| J21 | 8192 | `8c667f6fe288f1c16586c321d22451f0ad2f6812dd7dfea94d9c716ae8ed421e` | Pass size check only |
| J22 | 8193 | `a510d96b830105eb55658677e7581ec454a1669bf94f17e0b0188b3b2cf01d85` | Refuse above the inclusive bound |
