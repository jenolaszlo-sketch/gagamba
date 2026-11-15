# Q3 supplemental control re-execution

Independent Q4 execution of the 56 committed Q3 controls. All expected outcomes matched in both configurations. The raw rows carry the frozen-record rule and wire/projection hashes.

| Control | Expected | Debug | Release |
| --- | --- | --- | --- |
| B2/complete | None | None | None |
| B2/uid/72.00 | None | None | None |
| B2/uid/7.2e1 | None | None | None |
| B2/uid/0.072E+3 | None | None | None |
| B2/uid/720000e-4 | None | None | None |
| B2/uid/72e+0000000000000000000000000000000 | None | None | None |
| B2/uid/72.000000000000001 | Comparison | Comparison | Comparison |
| B2/uid/71.999999999999999 | Comparison | Comparison | Comparison |
| B2/uid/-72 | Comparison | Comparison | Comparison |
| B2/uid/-0.0 | Comparison | Comparison | Comparison |
| B2/uid/0.72e1 | Comparison | Comparison | Comparison |
| B2/uid/"72" | Comparison | Comparison | Comparison |
| B2/uid/true | Comparison | Comparison | Comparison |
| B2/uid/null | Comparison | Comparison | Comparison |
| B2/uid/7.2e2147483647 | bounded refusal | Comparison | Comparison |
| B2/uid/720e-2147483647 | bounded refusal | Comparison | Comparison |
| B2/uid/72e-2147483648 | bounded refusal | Comparison | Comparison |
| B2/uid/72.0e2147483647 | bounded refusal | Comparison | Comparison |
| B2/uid/0.00072e2147483647 | bounded refusal | Comparison | Comparison |
| B2/uid/7200e-2147483648 | bounded refusal | Comparison | Comparison |
| B2/uid/72e+2147483648 | Expectation | Expectation | Expectation |
| B2/uid/72e-2147483649 | Expectation | Expectation | Expectation |
| B2/uid/72e+9999999999999999999 | Expectation | Expectation | Expectation |
| B2/two-escaped-strings | None | None | None |
| B2/case-not-folded | Comparison | Comparison | Comparison |
| B2/malformed-surrogate/alias-wire | bounded refusal | Expectation | Expectation |
| B2/valid-surrogate-pair-distinct | Comparison | Comparison | Comparison |
| B2/wire-exponent-still-invalid | Schema | Schema | Schema |
| Integrated/complete | None | None | None |
| Integrated/uid/72.00 | None | None | None |
| Integrated/uid/7.2e1 | None | None | None |
| Integrated/uid/0.072E+3 | None | None | None |
| Integrated/uid/720000e-4 | None | None | None |
| Integrated/uid/72e+0000000000000000000000000000000 | None | None | None |
| Integrated/uid/72.000000000000001 | Comparison | Comparison | Comparison |
| Integrated/uid/71.999999999999999 | Comparison | Comparison | Comparison |
| Integrated/uid/-72 | Comparison | Comparison | Comparison |
| Integrated/uid/-0.0 | Comparison | Comparison | Comparison |
| Integrated/uid/0.72e1 | Comparison | Comparison | Comparison |
| Integrated/uid/"72" | Comparison | Comparison | Comparison |
| Integrated/uid/true | Comparison | Comparison | Comparison |
| Integrated/uid/null | Comparison | Comparison | Comparison |
| Integrated/uid/7.2e2147483647 | bounded refusal | Comparison | Comparison |
| Integrated/uid/720e-2147483647 | bounded refusal | Comparison | Comparison |
| Integrated/uid/72e-2147483648 | bounded refusal | Comparison | Comparison |
| Integrated/uid/72.0e2147483647 | bounded refusal | Comparison | Comparison |
| Integrated/uid/0.00072e2147483647 | bounded refusal | Comparison | Comparison |
| Integrated/uid/7200e-2147483648 | bounded refusal | Comparison | Comparison |
| Integrated/uid/72e+2147483648 | Expectation | Expectation | Expectation |
| Integrated/uid/72e-2147483649 | Expectation | Expectation | Expectation |
| Integrated/uid/72e+9999999999999999999 | Expectation | Expectation | Expectation |
| Integrated/two-escaped-strings | None | None | None |
| Integrated/case-not-folded | Comparison | Comparison | Comparison |
| Integrated/malformed-surrogate/alias-wire | bounded refusal | Expectation | Expectation |
| Integrated/valid-surrogate-pair-distinct | Comparison | Comparison | Comparison |
| Integrated/wire-exponent-still-invalid | Schema | Schema | Schema |
