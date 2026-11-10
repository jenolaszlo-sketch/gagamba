namespace Gagamba.Execution.Linux.M1.Tests.Validation;

/// <summary>Hand-written canonical CJSON-1 R/Wc/P fixture bytes bound to the
/// accepted P1 K fixtures (B2/pre and B3/post). These are test-only,
/// non-authoritative fixtures; they are not genuine issued review,
/// owner-acceptance, or administrative-approval records. Expected digests were
/// computed independently with the platform SHA-256 primitive over the exact
/// fixture bytes (see docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p2-evidence/
/// fixtures/digests.txt).</summary>
internal static class DetachedEvidenceFixtures
{
    internal const string R2 = """{"actionScriptSha256":"86225f4a7b3df026973fd7c9351859cb7bba1a16492653e6a61a4fba401c6226","artifactSetSha256":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","caseId":"case-2","catalogSha256":"793aa2aa8975140d05e39d334bcc5f18bf2c1c78c4b2ca1106d0d52329b487c0","conclusion":"supported-for-bounded-validation","fixtureManifestSha256":"50d4e8cb9fc5c382c007cef7ab804dd95394849724066862eba6bd804f15eeec","fixturePolicySha256":"05a8fb8555a389a702a49a80344c69512a000136ec74a894af0ce16e5b3cd70e","hostId":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","phase":"pre","qualificationId":null,"qualificationRecordSha256":null,"schemaVersion":1,"topology":"b2-lifeline"}""";

    internal const string Wc2 = """{"artifactSetSha256":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","caseId":"case-2","catalogReviewSha256":"82e28e80ac6d664e1621ef8dfe6f31b9769bed0fa83d08f21fc4da9a02ea7362","catalogSha256":"793aa2aa8975140d05e39d334bcc5f18bf2c1c78c4b2ca1106d0d52329b487c0","decision":"accept-exact-bounded-validation-catalog","hostId":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","phase":"pre","qualificationId":null,"qualificationRecordSha256":null,"schemaVersion":1,"topology":"b2-lifeline"}""";

    internal const string P2 = """{"accepted":true,"artifactSetSha256":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","caseId":"case-2","catalogReviewSha256":"82e28e80ac6d664e1621ef8dfe6f31b9769bed0fa83d08f21fc4da9a02ea7362","catalogSha256":"793aa2aa8975140d05e39d334bcc5f18bf2c1c78c4b2ca1106d0d52329b487c0","hostId":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","ownerCatalogAcceptanceSha256":"a93b8cd4f5d0d8380cdfffa6915de14e4b60a5c9ed5dcc7e54dd3f98a3db9be2","phase":"pre","qualificationId":null,"qualificationRecordSha256":null,"schemaVersion":1,"topology":"b2-lifeline"}""";

    internal const string Hr2 = "82e28e80ac6d664e1621ef8dfe6f31b9769bed0fa83d08f21fc4da9a02ea7362";
    internal const string Hw2 = "a93b8cd4f5d0d8380cdfffa6915de14e4b60a5c9ed5dcc7e54dd3f98a3db9be2";
    internal const string Hp2 = "9b1cc43b3e03cb91fb227446106df2e99ebf2a1acd7721951fd5567b576db92e";

    internal const string R3 = """{"actionScriptSha256":"8896b2cd0065c26a26034a502bbcfe31a1986483dba9d6808b0717c15e3d8d33","artifactSetSha256":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","caseId":"case-3","catalogSha256":"255d017e6ec54d29442e4336d8ecfe4258e0647bb9054f044df73dba67174151","conclusion":"supported-for-bounded-validation","fixtureManifestSha256":"bfe1c53f2005fb9871decdf099051116e094858919840d8930b3aeccfd17079b","fixturePolicySha256":"093ce53d520cfd73e46bf891d0ace3cb3a17cefeeefd10a54a1699be82fac919","hostId":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","phase":"post","qualificationId":"Q1","qualificationRecordSha256":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff","schemaVersion":1,"topology":"b3-integrated"}""";

    internal const string Wc3 = """{"artifactSetSha256":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","caseId":"case-3","catalogReviewSha256":"7d96bc2f3e6e9075cd2691aabc9588dfd2c14fe557d35281c5d9e24ee56c48ec","catalogSha256":"255d017e6ec54d29442e4336d8ecfe4258e0647bb9054f044df73dba67174151","decision":"accept-exact-bounded-validation-catalog","hostId":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","phase":"post","qualificationId":"Q1","qualificationRecordSha256":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff","schemaVersion":1,"topology":"b3-integrated"}""";

    internal const string P3 = """{"accepted":true,"artifactSetSha256":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb","caseId":"case-3","catalogReviewSha256":"7d96bc2f3e6e9075cd2691aabc9588dfd2c14fe557d35281c5d9e24ee56c48ec","catalogSha256":"255d017e6ec54d29442e4336d8ecfe4258e0647bb9054f044df73dba67174151","hostId":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","ownerCatalogAcceptanceSha256":"293e4af952e297cea56e80ebc95889d3249f32f585048413d43fb06fac440a75","phase":"post","qualificationId":"Q1","qualificationRecordSha256":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff","schemaVersion":1,"topology":"b3-integrated"}""";

    internal const string Hr3 = "7d96bc2f3e6e9075cd2691aabc9588dfd2c14fe557d35281c5d9e24ee56c48ec";
    internal const string Hw3 = "293e4af952e297cea56e80ebc95889d3249f32f585048413d43fb06fac440a75";
    internal const string Hp3 = "67c81e9e44b3bf30c24afeda581dfe0e4728b56b190a6a6070d524793a718768";
}
