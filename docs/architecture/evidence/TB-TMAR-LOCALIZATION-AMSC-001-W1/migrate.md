# TB-TMAR-LOCALIZATION-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

- Task: `TB-TMAR-LOCALIZATION-AMSC-001-W1`
- Parent: `TB-TMAR-LOCALIZATION-AMSC-001-W0` (commit `5a0b882b`)
- Skill: `tooba-architecture-migrate`
- Target: `src/backend/Modules/Localization/Tooba.Localization.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `5a0b882b`

---

## 1. Foundation readiness

`FOUNDATION_READY` for every destination. Localization already has all five projects with
capability-first folders, an exact path↔namespace tree, and canonical CQRS/result/localization
infrastructure. No foundation creation was required and none was performed.

## 2. What the migrate wave actually changed

W0 proved the module was already canonical for ownership, CQRS, result mapping, logging,
telemetry, correlation, cohesion and schema. W1 therefore made only the changes that were
genuinely required to close the *architecture-quality* gaps found by the audit — no
gratuitous refactoring.

### 2a. Typed-fault unification (single mechanism per layer)

| Before | After |
| --- | --- |
| `LanguageMappings.ParseDirection/ParseCalendar` threw `SemanticException(new SemanticError(...))` — an Application `Composition` helper raising a **Domain-style** fault | throws `ContractOperationException(LanguageErrorCodes.X)` — the Application-layer typed fault carrying the stable code |
| `LocalizationOperation` caught only `SemanticException` | catches `ContractOperationException` **and** `SemanticException`, mapping each by its typed stable code, with unknown codes propagating to the global boundary |

Classification is strictly by typed code. No message/prose heuristics exist.

### 2b. Declared-code guard (owned-code-only mapping)

`LanguageErrorCodes` gained `KnownCodes` + `IsKnown(string?)`, matching the certified
`InventoryErrorCodes` precedent. This guarantees that a `ContractOperationException` carrying a
code owned by another module (for example `content.publish.check.body`, which
`Content.Domain/Rules/ArticlePublicationReadiness.cs` emits) propagates untouched to the canonical
global exception boundary instead of being silently converted into a Localization `Result` failure.

**Behavior preservation for owned codes is exact**: all 19 declared codes remain the mapped set, so
`ParseDirection`/`ParseCalendar` faults still map to `Result` failures with the identical code and
identical `400` status, and every other Localization fault path is unchanged.

### 2c. Validator codes separated from business codes

New `LocalizationValidationCodes` (7 transport-identity codes) under
`Application/Languages/Validators/`. The four validators now emit
`localization.validation.*` instead of business codes.

| Validator | Before | After |
| --- | --- | --- |
| `CreateLanguageCommandValidator` | `InvalidCode`, `InvalidUrlPrefix`, `InvalidDisplayName`, `InvalidNativeName`, `InvalidDirection`, `InvalidCulture`, `InvalidCalendar` | `LocalizationValidationCodes.Language*Required` |
| `UpdateLanguageCommandValidator` | same 6 business codes | same 6 transport codes |
| `PatchLanguageCommandValidator` | `InvalidCode` | `LanguageCodeRequired` |
| `ListLanguagesAdminQueryValidator` | marker (unchanged) | marker (unchanged) |

The validation codes are deliberately **not** registered as error-catalog descriptors, matching the
certified AccessControl/Content/Cart/Offer/Order/Payment precedent where `ValidationBehavior` maps
them through the foundation `validation.failed` descriptor.

**Accepted observable delta (documented, not silent):** a transport-shape rejection previously
surfaced the coarse business code (for example `localization.language.invalid_code`); it now surfaces
the precise transport code through the foundation `validation.failed` descriptor. HTTP status (`400`)
is unchanged. This is the canonical repository behavior for FluentValidation failures and is the
explicit reason the previous coupling was a W0 finding. The **domain-level** `Invalid*` codes remain
fully owned and registered: `Language.ValidateIdentity`, `UpdateIdentityFields` and
`UpdateMutableFields` still throw them for the same persisted-length/shape rules.

## 3. Files changed

| Path | Change |
| --- | --- |
| `Contracts/Errors/LanguageErrorCodes.cs` | added `KnownCodes` + `IsKnown`; XML doc states boundary/immutability contract |
| `Application/Composition/LanguageMappings.cs` | `SemanticException` → `ContractOperationException`; class doc records the typed-fault contract |
| `Application/Composition/LocalizationOperation.cs` | maps both typed mechanisms by declared code; unknown codes propagate |
| `Application/Languages/Validators/LocalizationValidationCodes.cs` | **new** (7 transport codes) |
| `Application/Languages/Validators/CreateLanguageCommandValidator.cs` | transport codes |
| `Application/Languages/Validators/UpdateLanguageCommandValidator.cs` | transport codes |
| `Application/Languages/Validators/PatchLanguageCommandValidator.cs` | transport codes |
| `src/backend/Host/Tooba.Host.Tests/Architecture/LocalizationModuleAmsc001W1MigrateGuardTests.cs` | **new** durable guard (7 facts) |

No file was moved, renamed, created or deleted inside a production project other than the new
validation-codes file. Namespaces are unchanged. No route, DTO, status code, declared error code,
DI registration, migration or schema changed.

## 4. Verification

| Check | Result |
| --- | --- |
| `dotnet build` `Tooba.Localization.Application` | succeeded, 0 warnings, 0 errors |
| `dotnet build` `Tooba.Host.Tests` | succeeded, 0 errors |
| Focused filter (`LocalizationModuleAmsc001W1MigrateGuardTests` + `LocalizationModuleAmc` + `LocalizationFailureSemanticsTests` + `LanguageDirectoryPersistenceTests` + `HostLocalizationAmcGuardTests`) | **36 passed / 0 failed / 0 skipped** |
| `TmarCompleteReferenceStructureGateTests` + error-catalog guards | 6 passed / 1 failed — **pre-existing, unrelated** |

### 4a. Pre-existing failure (NOT caused by this wave, NOT repaired)

```text
TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment
Expected: "Tooba.Catalog.Contracts.Cart"
Actual:   "Tooba.Catalog.Contracts"
```

`src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Cart/*.cs` declare
`namespace Tooba.Catalog.Contracts` while physically sitting in a `Cart/` folder. These files are
tracked at `HEAD` (last touched by `56692b7c`, `TB-TMAR-CART-GOLDEN-001`) and the failing assertion
predates this task. Catalog is **outside** the authorized Localization scope, so it was reported
rather than repaired. Localization itself passes this same namespace-alignment assertion.

## 5. Scope boundary explicitly respected

W0 recorded a genuine pre-existing **cross-module persistence reach-through** outside the
authorized surface: `Catalog.Infrastructure/Directories/CatalogDirectory.cs`
(`LoadPreferredLanguageIdsAsync`) and `Catalog.Infrastructure/Persistence/Migrations/20260909130000_AddQuantityFoundation.cs`
read `localization.languages` with raw SQL. This is a Catalog-owned violation against Localization's
schema. It was **not** repaired: Catalog is not the active target of this AMSC pass, and repairing it
would require a Catalog-side Contracts port plus a Catalog migration. It is recorded here so the
Localization certification verdict can state honestly that *Localization's own* boundary is clean
while a foreign consumer still reaches into its schema.

## 6. Residual debt

| Item | Severity | Note |
| --- | --- | --- |
| `Catalog → localization.languages` raw SQL read | pre-existing, foreign-owner | outside authorized scope; blocks any global "no cross-module persistence" claim, not Localization's own verdict |
| `TmarCompleteReferenceStructureGateTests` Catalog namespace mismatch | pre-existing, foreign-owner | outside authorized scope |
| `LocalizationErrorResourceSet.Owns()` matches the whole `localization.` prefix | watch | currently harmless: the only non-Language `localization.*` string in the repo is a Content publish-check `detail`, not an error catalog code |
| `AddLocalizationEndpointPresentation()` empty composition seam | watch | retained Host registration seam; behavior-neutral |
| Empty untracked `artifacts/` folders | W2 | structure-wave cleanup |

## 7. Wave outcome

`MIGRATED` — `Structure-Handoff-State = REQUIRED`. The touched surface is behavior-preserving,
Contracts-bounded, canonically typed and durably guarded; it is ready for the Structure wave.
