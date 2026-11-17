# Exact superseding amendment and two-file scope

## Contract change

For the **Managed API and ownership** section of the original frozen E-6 release preparation contract, replace the row naming internal property `CapabilityHash` with internal get-only property `SealedLaunchRecordHash`, type `byte[]?`. It remains property 9 in the projection table and constructor argument 9, at zero-based position 8. The corresponding constructor parameter is `byte[]? sealedLaunchRecordHash`. It has no default. Both candidate and independent expected projections supply the same role. Do not retain the old managed spelling as an alias. [Original contract](../rc2b-06-b6-alt-b-slice-e-06-release-prep-entry-auth-01-evidence/contract.md)

In the original contract's **Governing representation** table, the row at offsets 30–61 continues to name the normative protocol `capabilityHash` digest. Interpret that row's managed projection source as `SealedLaunchRecordHash`. It still carries the raw 32-byte digest of the exact complete sealed validation launch record from the original L invocation, expected independently for each side. No wire vocabulary, protocol concept, input bytes, generation or actor relationship is amended.

| Affected location | Before | After |
|---|---|---|
| Projection property and its references | `ValidationReleaseProjection.CapabilityHash` | `ValidationReleaseProjection.SealedLaunchRecordHash` |
| Projection constructor argument 9 and assignment | `byte[]? capabilityHash` | `byte[]? sealedLaunchRecordHash` |
| Validator's matched input local and the existing length check and copy | `capabilityHash` | `sealedLaunchRecordHash` |
| Projection generated getter and backing field | `get_CapabilityHash`, `<CapabilityHash>k__BackingField` | `get_SealedLaunchRecordHash`, `<SealedLaunchRecordHash>k__BackingField` |
| Private `ValidationReleasePreparation.Binding` positional slot, comparison and encoding references | `byte[] CapabilityHash` | `byte[] SealedLaunchRecordHash` |
| Private `Binding` generated property, getter, init setter and backing field | `CapabilityHash`, `get_CapabilityHash`, `set_CapabilityHash`, `<CapabilityHash>k__BackingField` | `SealedLaunchRecordHash`, `get_SealedLaunchRecordHash`, `set_SealedLaunchRecordHash`, `<SealedLaunchRecordHash>k__BackingField` |
| Private `Binding` generated constructor and `Deconstruct` parameter 7 and generated record member display label | `CapabilityHash` | `SealedLaunchRecordHash` |
| Test projection hash accessor, reflected `ProjectionMembers` entry and missing-member selector | `CapabilityHash` | `SealedLaunchRecordHash` |

The permitted source delta from `53ffe7a21408a7e9674cf50b7098a26887c070b5` consists of seven exact `CapabilityHash` and five exact `capabilityHash` substitutions in the implementation file, and three exact `CapabilityHash` substitutions in its focused test file. Generated metadata changes follow from those source edits. The independent fixed digest fixtures, including the zero-input fixture, and their existing local names stay as recorded. The two generated `MissingMember` theory display names carrying this property change accordingly. New test logic, build definitions, executable files and additions to the production caller graph are outside the authorization.

| Source path relative to Gagamba | Authorized changes |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Validation/ValidationReleasePreparation.cs` | The property and directly necessary parameter, local, private copied-field and use-site substitutions above. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/ValidationReleasePreparationTests.cs` | The three corresponding accessor, reflection and selector updates above. |

Both files preserve namespace, type and method signatures, property nullability and visibility, bounded clone behavior, validation order, every status and null-material rule, the two independent projections and separate `Compare` digest expectation, and the exact result bytes. The old naming test and all other source and tests remain byte-identical in the remediation. Any additional difference requires a separate decision. [Verification gate](verification.md)
