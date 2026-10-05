# TB-TMAR-IDENTITY-AMSC-001-W3 — Certify

## Mode

`ARCHITECT_DIRECT_AMSC` — Certify. No production behavior change: this wave records and locks the
ARCH-COMPLETE-002 certification state produced by W0 → W2 and adds the durable certification guard.

Baseline: `branch = main`, `HEAD == origin/main == 7c79f8c6` (W2).

## Verdict

| Field | Value |
|---|---|
| Verdict | `COMPLETE_REFERENCE_PATTERN` |
| Lock | `ARCH-COMPLETE-002` |
| Structure-State | `CERTIFIED` (W2 `READY_FOR_CERTIFY` preserved as historical W2 truth) |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| SolutionExplorer-State | `CANONICAL` (`/Modules/Identity/`, 5 projects) |
| PathNamespace-State | `EXACT` |
| RootAllowlist-State | `ENFORCED` |
| PhysicalCopy-State | `CLEAN` |
| File-Cohesion-State | `COHESIVE` |
| Foreign App/Infra/Domain coupling | `ZERO` |
| Endpoint-Ownership-State | `MODULE_OWNED` (13 routes) |
| Blocking residual debt | `ZERO` |
| `microserviceExtractable` | `true` |
| `automaticNextImplementationTask` | `NONE` |

## Wave lineage (accepted)

| Wave | Skill | Commit |
|---|---|---|
| W0 Analyze | `tooba-architecture-analyze` | `91eec1fd` |
| W1 Migrate | `tooba-architecture-migrate` | `93a6b192` |
| W2 Structure | `tooba-architecture-structure` | `7c79f8c6` |
| W3 Certify | `tooba-architecture-certify` | *(this commit)* |

The earlier `TB-TMAR-IDENTITY-AMC-001` W1→W6 lineage (implementation `aafd14e0`, docs stamp
`c7e473cd`) remains in the repository as historical evidence only; the AMSC-001 W0→W3 lineage is
authoritative for the current Identity module.

## ARCH-COMPLETE-002 certification checklist

| Requirement | Evidence | State |
|---|---|---|
| Application capability folders | `Auth/{Commands,Queries,Models,Validators}` + `Composition/IdentityOperation.cs` + shared `Models`/`Options`/`Ports`; no `Commands`/`Queries`/`Validators` technical-axis root | PASS |
| Endpoints capability folders | root = `IdentityEndpointModule.cs` only; `Auth/`, `Errors/` | PASS |
| Infrastructure capability folders | `Adapters/`, `Authentication/`, `Contacts/`, `Events/`, `ExternalIdentity/`, `Mfa/`, `Otp/`, `PasswordHashing/`, `Persistence/Migrations/`, `SecurityEvents/`, `Sessions/`; root = `IdentityModule.cs` only | PASS |
| Contracts capability folders | `Auth/`, `Actors/`, `Contacts/`, `Errors/`, `Resources/`; root = empty | PASS |
| Domain capability folders | `Aggregates/`, `Enums/`, `Events/`, `Rules/`; root = empty | PASS |
| Path ↔ namespace exact equality | all five projects, path-derived equality (EF `Persistence/Migrations` block-scoped namespace exempt) | PASS |
| Root allowlists | per-project allowlists match disk exactly; forbidden roots absent | PASS |
| CQRS / MediatR | 13 endpoint-reachable `IRequest` types, each with a real `IRequestHandler<,>`, `ISender`-only endpoints, MediatR 12.5.0 | PASS |
| Validator coverage | exhaustive: 13 endpoint-reachable = 9 `VALIDATOR_REQUIRED` (all present) + 4 `NO_VALIDATOR_REQUIRED` | PASS |
| API result pattern | `ApiResponseFactory.From(...)` for every failure path; the only raw `Results.Json` is the intentional locked `201` register DTO; zero `Results.BadRequest/Problem` | PASS |
| Stable error-code owner | `Contracts/Errors/IdentityErrorCodes.cs` (12 codes) → 12 registered descriptors in `IdentityErrorCatalogContributor`; zero duplicate descriptor ownership | PASS |
| Localization | `IdentityErrors.resx` + `IdentityErrors.fa.resx`; every declared `identity.*` code carries a Persian title; zero hard-coded client-facing fault text | PASS |
| Typed faults | Domain/Infrastructure throw `ContractOperationException(<IdentityErrorCodes.*>)`; `Application/Composition/IdentityOperation.cs` is the single `catch (ContractOperationException ex)` → `SemanticError(ex.Code)` seam | PASS |
| Logging / telemetry / correlation | canonical; 9 structured event names, `tooba.identity.otp.delivery` counter; no ad-hoc pipeline; no sensitive value logged | PASS |
| Contracts-only boundaries | the only foreign seam is `Tooba.CustomerProfile.Contracts` (`ICustomerProfileDirectory`) consumed by `Tooba.Identity.Application`; zero foreign Application/Domain/Infrastructure project edge in any Identity project | PASS |
| No cross-module persistence/join | own `identity` schema only | PASS |
| Schema preservation | 0 migration files touched; migration IDs, `Up`/`Down`, designer + snapshot identity unchanged | PASS |
| Durable guards | W3 cert guard + W2 structure guard + inherited `IdentityModuleAmcW1/W2/W5`, `IdentityValidatorCoverageGuardTests`, `AuthenticationV2CanonicalizationGuardTests` and the Identity/Auth HTTP + lifecycle suites | PASS |
| Manifest + SoT | manifest `modules` Identity entry `structureCertified: true` with the AMSC-001 certification note; `structureLock.certifiedModules` contains `Identity` exactly once; AMSC W0→W3 SoT records; Master Recovery module checkpoint | PASS |
| Host closure | no `Host/Tooba.Host/Identity` folder, no `Tooba.Host.Identity` namespace; the generic Host auth platform seam (session middleware, current-session adapter, throttle seam) is retained and is not Identity ownership | PASS |

## Validator classification (exhaustive)

| Classification | Requests |
|---|---|
| `VALIDATOR_REQUIRED` (present) | `RegisterAuthUserCommand`, `CompletePasswordResetCommand`, `RequestIdentifierVerificationCommand`, `CompleteIdentifierVerificationCommand`, `RequestOtpLoginCommand`, `ChangePasswordCommand`, `LoginWithPasswordCommand`, `RefreshAuthSessionCommand`, `CompleteOtpLoginCommand` |
| `NO_VALIDATOR_REQUIRED` | `LogoutSessionCommand`, `LogoutAllSessionsCommand`, `RequestPasswordResetCommand`, `GetAuthMeQuery` |

The three W2 shape-only validators are registered through the existing
`AddToobaCqrsFoundation`/`AddValidatorsFromAssembly` discovery path — no manual invocation in
endpoints or handlers, and no ceremonial validator for the four no-input requests.

`LoginWithPasswordCommandValidator` deliberately does **not** validate `IdentifierKind`: an unknown
kind must still reach the handler so the enumeration-safe collapse to
`identity.authentication.failed` (401) is preserved instead of degrading to
`identity.validation.failed` (400).

## Deliberate non-gap (two recorded watches, both out of scope)

1. **English `IdentityErrors.resx` coverage.** The English set carries the 9 pre-existing catalogued
   keys. The three OTP-delivery codes added in W1 are registered with stable English descriptor
   titles (`IdentityErrorCatalogContributor`) and localized in `IdentityErrors.fa.resx`. Adding
   English `.resx` entries would *change* the English title currently produced for those codes, so it
   is a behavior change and is deliberately excluded from a behavior-preserving certification.
2. **`IdentityOutboxRegistration` unmapped-event guard.** `Infrastructure/Persistence/IdentityOutboxRegistration.cs`
   throws `InvalidOperationException("Unmapped Identity integration event type.")` on an unreachable
   fail-closed branch — byte-for-byte the same pattern as the already-certified
   Offer/Catalog/Party/Pricing/Tax outbox registrations. It is a composition guard, not a boundary
   fault, and is not converted here.

## Pre-existing drift (not Identity, not repaired here)

The full Host suite is red at the W3 starting HEAD `7c79f8c6` with **82 pre-existing failures**, none
of which involves Identity: the `Host/Admin` StoreAppearance file-count guards
(`HostAdminAmcW7/W14R1/W16R1/W29PwIdentity/W35HoldPolicy/StoreLanding/StoreMenu/CheckoutAbuse`),
Grid/Catalog/Party/Reviews/Storefront module guards, `Fulfillment`/`Tax`/`Pricing`/`Promotion` domain
tests, the `TmarDurableGuardTests` Master-Recovery history pins and the source-size baseline guards.
Identity appears in none of them and none was touched or weakened.

`HostAdminAmcW29PwIdentityGuardTests.Host_Admin_count_15_StoreAppearance_evacuated_PW_shells_ABSENT`
is the one failure whose name mentions Identity: it asserts 15 files under `Host/Admin`, while that
folder holds 17 for reasons unrelated to the Identity module. It is red at the W3 starting HEAD and
was left untouched.

`IdentityModuleAmcW5CertGuardTests` and the two `TmarDurableGuardTests` SoT/Recovery facts were also
red at the W3 starting HEAD (their assertions pin the historical `READY_FOR_CERTIFY` state and the
historical Grid/Seller recovery checkpoint respectively). `IdentityModuleAmcW5CertGuardTests` is the
Identity-owned certification guard and is reconciled here to the AMSC-001 certification (the
historical `READY_FOR_CERTIFY` value is preserved inside `identityModuleAmsc001W2`), so it is now
green — a strengthening, not a weakening. The two `TmarDurableGuardTests` facts remain red because
they pin the repository-global Host recovery checkpoint, which this module-local task must not touch.

## SoT / manifest changes in this wave

- `docs/architecture/tmar-module-structure-manifests.json` — the Identity certified entry gains the
  AMSC-001 `certificationNote`; the stale `Tooba.Identity.Contracts` root-allowlist justification
  (`Auth/Actors/Contacts/Problems`) is corrected to `Errors`. Exactly one Identity entry remains; no
  project allowlist, forbidden-root or forbidden-folder value was weakened.
- `docs/architecture/tmar-current-state.json` — AMSC records
  `identityModuleAmsc001W0` … `identityModuleAmsc001W3` added with their own commit SHAs (W0's
  `PENDING_W0_COMMIT` placeholder reconciled to `91eec1fd`); the pre-existing `identityAmc001`
  (AMC-001) record is reconciled in place to `structureState = CERTIFIED` with
  `amsc001Certified = true` and the corrected 9 + 4 validator classification, so no duplicate
  Identity record exists; `structureLock.certifiedModules` already contained `Identity` and still
  contains it exactly once. The repository-global root checkpoint
  (`lastAcceptedTask`, `nextTask`, `workflowStop`, `automaticNextImplementationTask`) is untouched.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` — module-local Identity AMSC checkpoint with the
  accepted lineage and the `USER_REVIEW_IDENTITY_AMSC_001_W3` stop gate; the authoritative current
  region before the explicit historical boundary is byte-identical.
- `src/backend/Host/Tooba.Host.Tests/Architecture/IdentityModuleAmsc001W3CertGuardTests.cs` — new
  durable certification lock (5 facts).
- `docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3/` — this certification, the SoT/manifest/
  recovery patch scripts and the full-suite baseline log.

## Verification

- `dotnet build src/backend/Tooba.slnx` → **0 errors**, 127 warnings (all pre-existing; the new W3
  guard introduces none).
- Focused Identity suite (`IdentityLifecycleTests`, `IdentityFoundationTests`, `AuthSecurityHttpTests`,
  `AuthenticationHttpTests`, `AuthenticationV2CanonicalizationGuardTests`, `OtpDeliveryProviderTests`,
  `CheckoutIdentityContractTests`, `StorefrontAccountIdentityTests`,
  `IdentityValidatorCoverageGuardTests`, `IdentityModuleAmcW1/W2/W5`,
  `IdentityModuleAmsc001W2StructureGuardTests`, `IdentityModuleAmsc001W3CertGuardTests`) →
  **67 passed / 6 skipped / 0 failed** (62 at the W3 starting HEAD + the 5 new W3 certification facts).
- Full Host suite, W3 tree vs the W3 starting HEAD `7c79f8c6`: **82 failed / 1847 passed / 130 skipped**
  in both trees with an identical failure set by test name (the +5 tests are this wave's new guard).
  **ZERO new failures, zero guard weakened.**
- The one failure whose name mentions Identity
  (`HostAdminAmcW29PwIdentityGuardTests.Host_Admin_count_15_StoreAppearance_evacuated_PW_shells_ABSENT`)
  is red in both trees and belongs to the unrelated Host/Admin file-count guard family.

## Behavior preservation

| Surface | State |
|---|---|
| Routes / methods / response DTO shape | `UNCHANGED` (13 `/v1/auth/*` routes) |
| Status codes and stable `identity.*` code values | `UNCHANGED` (12 codes) |
| Session + actor resolution semantics | `UNCHANGED` |
| Domain invariants, normalization and password policy | `UNCHANGED` |
| Schema, table, columns, keys, migration identity | `UNCHANGED` (0 migration files touched) |
| DI lifetimes / outbox registration | `UNCHANGED` |
| Host composition seams | `UNCHANGED` |

## Handoff

`Structure-State = CERTIFIED`. No next implementation task is authorized:
`automaticNextImplementationTask = NONE`, stop gate `USER_REVIEW_IDENTITY_AMSC_001_W3`.
