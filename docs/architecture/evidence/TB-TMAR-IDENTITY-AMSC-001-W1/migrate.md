# TB-TMAR-IDENTITY-AMSC-001-W1 — Migrate

## Mode

`ARCHITECT_DIRECT_AMSC` — Wave 1 (Migrate). Behavior-preserving canonicalization of the Identity
error/localization/presentation seams. **No ownership move, no schema change, no route change, no
folder restructure** (that is W2).

Baseline: `branch = main`, `HEAD == origin/main == 91eec1fd` (W0).

Skills: `.cursor/skills/tooba-architecture-migrate/SKILL.md` (V2).

## Scope executed

| # | W0 blocker closed | Change |
|---|---|---|
| 1 | Three OTP-delivery codes emitted but unregistered | Added `IdentityErrorCodes.OtpDeliveryRateLimited`, `.OtpDeliveryInvalidDestination`, `.OtpDeliveryUnconfigured` and three matching `ErrorDescriptor`s in `IdentityErrorCatalogContributor` |
| 2 | Raw `InvalidOperationException` + string literals | `OtpDeliveryProviderSender` now throws `ContractOperationException(IdentityErrorCodes.*)` |
| 3 | Two parallel classification seams | `IdentityOperation.ExecuteAsync` now catches the canonical `ContractOperationException` → `SemanticError(ex.Code)`; `ChangePasswordCommandHandler` and `RequestOtpLoginCommandHandler` dropped their local `catch (ArgumentException)` / `catch (InvalidOperationException)` classification and route through the single seam |
| 4 | Six hard-coded Persian fault messages in a Domain rule | `LoginIdentifierNormalizer` throws `ContractOperationException(IdentityErrorCodes.ValidationFailed)`; no user-facing prose in Domain |
| 5 | Missing `IdentityErrors.fa.resx` | Added `Contracts/Resources/IdentityErrors.fa.resx` with Persian titles for all **12** keys |
| 6 | Twelve duplicate `using` directives (CS0105) | Deduplicated in 18 files |

Deliberately **not** changed in W1: folder/namespace moves, validator additions, removal of
unreferenced transport records — those are structural and belong to W2.

## Canonical mechanisms used

- `Tooba.BuildingBlocks.ContractOperationException` (typed, code-carrying cross-boundary fault).
- `Tooba.Identity.Application.Composition.IdentityOperation` as the **single** fault→`Result`
  seam, exactly mirroring `Tooba.CustomerProfile.Application.Composition.CustomerProfileOperation`.
- `Tooba.BuildingBlocks.Presentation.Errors.ErrorDescriptor` / `ErrorClassification` /
  `IErrorCatalogContributor` (no parallel mapper, no suppression, no duplicate descriptor owner).
- `Tooba.BuildingBlocks.Localization.IErrorResourceSet` + `.resx` (the existing mechanism; no second
  localization system).

## Error code inventory after W1

| Code | HTTP | Classification | Descriptor owner | resx | fa.resx |
|---|---|---|---|---|---|
| `identity.validation.failed` | 400 | Validation | Identity | ✔ | ✔ |
| `identity.challenge.invalid` | 400 | Business | Identity | ✔ | ✔ |
| `identity.authentication.failed` | 401 | Platform | Identity | ✔ | ✔ |
| `identity.session.invalid` | 401 | Platform | Identity | ✔ | ✔ |
| `identity.identifier.conflict` | 409 | Conflict | Identity | ✔ | ✔ |
| `identity.rate_limited` | 429 | Platform | Identity | ✔ | ✔ |
| `identity.tenant.untrusted` | 400 | Platform | Identity | ✔ | ✔ |
| `identity.otp.delivery.unavailable` | 400 | Business | Identity | ✔ | ✔ |
| `identity.otp.delivery.rate_limited` | 429 | Platform | Identity | ✔ | ✔ |
| `identity.otp.delivery.invalid_destination` | 400 | Business | Identity | ✔ | ✔ |
| `identity.otp.delivery.unconfigured` | 400 | Business | Identity | ✔ | ✔ |
| `identity.password.change.failed` | 400 | Business | Identity | ✔ | ✔ |

12 declared codes, 12 registered descriptors, 12 EN titles, 12 FA titles. Single descriptor owner.

## Behavior preservation

| Contract | State |
|---|---|
| Routes / methods | unchanged (13 `/v1/auth/*`) |
| Success shapes / status codes | unchanged |
| The 9 pre-existing stable codes | byte-identical |
| `identity.identifier.conflict` for a duplicate registration | preserved (was already code-carrying) |
| `identity.validation.failed` (400) for a bad identifier shape / weak password on **register** | preserved — `IdentityDuplicateIdentifierFault` was not converted; the domain/shape faults still surface `identity.validation.failed` |
| `identity.password.change.failed` (400) for a wrong current password | preserved — now emitted through the typed fault instead of `InvalidOperationException` |
| `identity.authentication.failed` (401) collapse for disabled / locked / unknown-identifier-kind | preserved — `LoginWithPasswordCommandHandler` still short-circuits before the seam |
| Enumeration-safe password-reset request (`ArgumentException` swallowed, always 200 `accepted:true`) | preserved — the swallow now catches `ContractOperationException` |
| Refresh rotation, reuse detection, session/security-stamp rules | unchanged |
| `identity.otp.delivery.unconfigured` for fail-closed production delivery | preserved |
| Persistence, schema, migrations | untouched |
| Telemetry event names + `tooba.identity.otp.delivery` counter | unchanged |
| Localization keys + English semantics | unchanged |

### Intentional observable refinement (W1 blocker #1)

`OtpDeliveryOutcomeKind.Unavailable` previously reached the client as
`identity.otp.delivery.unavailable` **400**; it still does — the local `catch` in
`RequestOtpLoginCommandHandler` already collapsed every `InvalidOperationException` to that code, so
the three newly catalogued codes are currently only reachable if a future provider returns
`RateLimited` / `InvalidDestination` / `Misconfigured` through a path that does not pass the handler's
catch. The migration removes the code-losing collapse at the seam; the handler no longer rewrites the
code, so the descriptor's pinned status now governs. No test asserts the old collapsed mapping for
those three kinds.

### Test-assertion alignment (required, not a weakening)

Two tests asserted the *implementation type* and `Message` of the OTP-delivery fault:

- `OtpDeliveryProviderTests.Sender_maps_misconfigured_to_identity_error_code`
- `AuthSecurityHttpTests.Production_otp_delivery_is_fail_closed`

Both were updated to assert the canonical typed fault and its **stable code**
(`Assert.ThrowsAsync<ContractOperationException>` + `ex.Code == "identity.otp.delivery.unconfigured"`).
The asserted behavior (fail-closed production OTP delivery with the same machine code) is unchanged;
only the carrier type/member changed, which is exactly what the typed-fault migration mandates.

## Verification

- `dotnet build src/backend/Tooba.slnx` → **0 errors**. All 12 Identity CS0105 warnings removed.
- Focused suite (`AuthenticationV2CanonicalizationGuardTests`, `IdentityModuleAmcW1/W2/W5`,
  `IdentityValidatorCoverageGuardTests`, `IdentityFoundationTests`, `IdentityLifecycleTests`,
  `AuthenticationHttpTests`, `AuthSecurityHttpTests`, `OtpDeliveryProviderTests`,
  `StorefrontAccountIdentityTests`, `CheckoutIdentityContractTests`, `ArchitectureBoundaryTests`,
  `HostAdminCanon007GuardTests`, `HostAdminCanonicalCertificationGuardTests`,
  `HostAdminAmcCheckoutIdentityGuardTests`) → PASS except three **pre-existing** failures that are
  red at the W1 base commit and are not caused by this wave:
  - `HostAdminAmcW29PwIdentityGuardTests.Host_Admin_count_15_StoreAppearance_evacuated_PW_shells_ABSENT`
    (asserts 15 files under `Host/Admin`; the folder has 17) — unrelated to Identity, untouched here.
  - `IdentityModuleAmcW1SolutionGuardTests.Identity_projects_group_under_Modules_Identity_in_slnx`
    (asserts a flat `/Modules/` folder exists in `Tooba.slnx`; it does not) — repaired in W2.
  - `HostCustomerProfileEvacuationGuardTests.Parent_R1_solution_grouping_remains_exact`
    (same flat-`/Modules/` assertion) — repaired in W2.

## Wave 1 outcome

`MIGRATE_COMPLETE` → W2 (Structure) is unblocked.
