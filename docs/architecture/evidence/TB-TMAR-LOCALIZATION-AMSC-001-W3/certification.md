# TB-TMAR-LOCALIZATION-AMSC-001-W3 — Certify (tooba-architecture-certify)

- Task: `TB-TMAR-LOCALIZATION-AMSC-001-W3`
- Parent: `TB-TMAR-LOCALIZATION-AMSC-001-W2` (commit `6bc3ee2d`)
- Skill: `tooba-architecture-certify`
- Target: `src/backend/Modules/Localization/Tooba.Localization.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `6bc3ee2d`

---

## 1. Verdict

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
```

Accepted lineage: W0 `5a0b882b` → W1 `074fc3a4` → W2 `6bc3ee2d` → W3 (this commit).

## 2. Structure gate (mandatory precondition)

W2 evidence `docs/architecture/evidence/TB-TMAR-LOCALIZATION-AMSC-001-W2/structure.md` supplies a
current PASS for this exact surface:

| Gate | Value |
| --- | --- |
| Structure-State | `READY_FOR_CERTIFY` |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |

Re-checked defensively against current disk state — no drift since W2.

## 3. Physical tree

All 33 production `.cs` files verified in place; no root dump; no stale/duplicate copy; no
`TypeForwardedTo`; no namespace alias.

```text
Contracts/    Errors/(3) Ports/(3) Resources/(2 .resx)
Domain/       Aggregates/Language.cs Enums/(2)
Application/  Composition/(2) Languages/{Commands(3),Queries(1),Validators(5)} Models/(1) Ports/(1)
Infrastructure/ LocalizationModule.cs Adapters/(2) Bootstrap/(1) Languages/(1)
                Persistence/(2) Persistence/Migrations/(3)
Endpoints/    LocalizationEndpointModule.cs Admin/(2)
```

## 4. Path ↔ namespace proof

Every production file's declared namespace equals the project + path-derived namespace.
**Violations: 0.** Two `Domain/Enums/*.cs` files carry a UTF-8 BOM; the guard strips it before
matching (this was the only nuance found, and it is not a namespace defect).

## 5. Alias / shim proof

`TypeForwardedTo`: 0. Namespace alias workarounds: 0. `global using Tooba.Localization*` shims: 0.
Duplicate compatibility types: 0.

## 6. Endpoint ownership

| Item | Value |
| --- | --- |
| HTTP applicability | `HTTP_OWNING` |
| Owner | `Tooba.Localization.Endpoints` |
| Routes | `GET /v1/admin/languages`, `POST /v1/admin/languages`, `PUT /v1/admin/languages/{code}`, `PATCH /v1/admin/languages/{code}` — **4** |
| Route count | 4 |
| Composition entry | `LocalizationEndpointModule.MapLocalizationModuleEndpoints` |
| Host route ownership | **ZERO** |
| Duplicate mapping | **NONE** |

Host retains only `HostLocalizationAdminAuthorizer` (`ALLOWED_SECURITY_ADAPTER`) and composition-root
wiring (`Program.cs`, `ToobaModuleComposition.cs`).

## 7. Request → handler → validator matrix

| Request | `IRequest` | `IRequestHandler` | `ISender` dispatch | Classification | Validator | Transport codes |
| --- | --- | --- | --- | --- | --- | --- |
| `ListLanguagesAdminQuery` | ✓ | ✓ | ✓ | `VALIDATOR_REQUIRED_PRESENT` | `ListLanguagesAdminQueryValidator` | marker (none) |
| `CreateLanguageCommand` | ✓ | ✓ | ✓ | `VALIDATOR_REQUIRED_PRESENT` | `CreateLanguageCommandValidator` | 7 |
| `UpdateLanguageCommand` | ✓ | ✓ | ✓ | `VALIDATOR_REQUIRED_PRESENT` | `UpdateLanguageCommandValidator` | 6 |
| `PatchLanguageCommand` | ✓ | ✓ | ✓ | `VALIDATOR_REQUIRED_PRESENT` | `PatchLanguageCommandValidator` | 1 |

`endpointReachableRequests = 4`, `validatorRequiredCount = 4`, `noValidatorRequiredCount = 0`.
MediatR `12.5.0` via `AddToobaCqrsFoundation`. Validators emit stable machine codes only — never
Persian/English text, never business codes. Endpoints contain no `DbContext`, no
`ILanguageDirectory`, no `SemanticException`, no `Results.Json`.

## 8. Localization coverage

| Item | Value |
| --- | --- |
| Declared codes (`LanguageErrorCodes`) | **19** |
| Registered descriptors (`LocalizationErrorCatalogContributor`) | **19** |
| Unregistered declared codes | **NONE** |
| Duplicate descriptor ownership | **ZERO** |
| Resource set | `LocalizationErrorResourceSet` (`localization.` keyspace) |
| Resource files | `LocalizationErrors.resx` + `LocalizationErrors.fa.resx` |
| Keys present in both cultures | **19 / 19** |
| Raw `localization.language.*` literals in production outside the codes class | **ZERO** |
| `ex.Message` / `exception.Message` used as a user-facing contract | **ZERO** |
| Endpoint-level `Accept-Language` parsing | **ZERO** |
| Hard-coded Persian/English user-facing text | **NONE** |

The only Persian literal in production is `"فارسی"` in `LanguageDirectory.BootstrapAsync` — seed data
(the native name of the seeded `fa-IR` row), not a user-facing message. Bootstrap ordering means
`Language.Create` validates and assigns before the literal could ever surface as an error message.

Descriptor ownership is unique. Composed-catalog uniqueness is additionally guarded by the
repository-wide `ErrorCatalogUniqueCodeGuardTests`.

## 9. API result / error mapping

- All four handlers return `Result` / `Result<T>`; endpoints map via `ApiResponseFactory.From(...)`.
- `Results.Json` / `Results.BadRequest` / `Results.Problem`: **ZERO**.
- Local `ProblemDetails` builder / local error mapper: **ZERO**.
- `catch`-and-map in endpoints for expected failures: **ZERO**.
- Failure classification by `ex.Message` parsing: **ZERO**.
- Unknown/unexpected exceptions are **not** converted to business failures — they propagate to the
  canonical global `IExceptionPresentationService` boundary. `LocalizationOperation` only maps a
  `ContractOperationException` whose code is a **declared Localization code**
  (`LanguageErrorCodes.IsKnown`), so a foreign-owned code such as `content.publish.check.body`
  propagates untouched.
- Success shape preserved: the Admin language DTO shape (`LanguageAdminResponse`) is unchanged.
- No duplicate-suppression mechanism introduced anywhere.
- No new shared-errors project/layer created.

## 10. Typed fault mechanism

| Layer | Mechanism |
| --- | --- |
| Domain aggregate (`Language`) | `SemanticException` + `SemanticError(LanguageErrorCodes.*)` |
| Application mapping/parsing (`LanguageMappings`) | `ContractOperationException(LanguageErrorCodes.*)` |
| Infrastructure directory (`LanguageDirectory`) | `SemanticException` + `SemanticError(LanguageErrorCodes.*)` |
| Application seam (`LocalizationOperation`) | maps both by declared code; unknown codes propagate |

Classification is strictly typed-code based. No legacy mapper, no message heuristics.

## 11. Logging / sensitive data

`ILogger<T>` with structured dotted value-free event names only. No `Console.WriteLine`, no
`Debug.WriteLine`, no custom logger framework, no second telemetry pipeline. No passwords, tokens,
OTP secrets, cookies, `Authorization` headers, session secrets or credentials are logged anywhere in
the module. `Sensitive-Logging-State = NONE`.

### 11a. Framework outbox invariant (single tolerated raw literal)

`Infrastructure/Persistence/LocalizationOutboxRegistration.cs` throws
`InvalidOperationException("Localization integration event is not registered.")` from
`GetEventTypeName`. This is the repository-wide outbox-registration idiom, byte-for-byte identical in
13 modules (including the already-certified CustomerProfile, AddressBook, Content, BulkInquiry,
OperatorProfile, ProductQnA, UserPreference, Wishlist, Media, Story, PageComposition and Reviews
implementations). It is a programmer-error invariant, is unreachable from any HTTP request (the
module publishes no integration events), and is never a user-facing contract — so it is neither a
localization nor a message-classification defect. The W1 guard intentionally scopes its
`InvalidOperationException(` rejection to `Domain` + `Application`; `Infrastructure` was left
unchanged. Recorded explicitly here rather than silently exempted, and the W3 guard pins it to this
one file so it cannot spread.

## 12. OpenTelemetry / correlation continuity

`ToobaTelemetry` (`ActivitySource`/`Meter` named `Tooba`) is reused; no second `ActivitySource`, no
`Meter`, no correlation provider, no custom header, no raw `AsyncLocal`, no direct
`ActivitySource.StartActivity(...)` in Application/Endpoints, no manual `traceparent` parsing. The
module issues no cross-module calls of its own, so no `IModuleCallTracer` decoration is required;
foreign consumers decorate their own calls to `ILanguageLookup` / `ILanguageActivationPort`.

## 13. File cohesion / size

| File | LOC | Assessment |
| --- | --- | --- |
| `Infrastructure/Languages/LanguageDirectory.cs` | 231 | cohesive |
| `Domain/Aggregates/Language.cs` | 120 | cohesive |
| `Endpoints/Admin/LocaleAdminEndpoints.cs` | 107 | cohesive |
| `Application/Composition/LanguageMappings.cs` | 76 | cohesive |
| `Application/Models/LanguageSnapshot.cs` | 69 | cohesive capability model file |
| `Persistence/Migrations/LocalizationDbContextModelSnapshot.cs` | 155 | EF-generated |

No Localization file appears in `Baselines/tmar-source-size-baseline.json`; no file approaches the
800 LOC `ARCH-SIZE-001` ceiling. No god-file, no over-split, no artificial parallel decomposition.

## 14. Host authority classification

| Host reference | Classification |
| --- | --- |
| `HostLocalizationAdminAuthorizer` | `ALLOWED_SECURITY_ADAPTER` |
| `Program.cs` (`AddLocalizationEndpointPresentation`, CQRS assembly registration, `ILocalizationAdminAuthorizer` DI, `MapLocalizationModuleEndpoints`) | `ALLOWED_COMPOSITION_ROOT` |
| `ToobaModuleComposition.cs` (`new LocalizationModule()`) | `ALLOWED_COMPOSITION_ROOT` |

`ILLEGAL_BUSINESS_AUTHORITY = 0`, `ILLEGAL_PERSISTENCE_AUTHORITY = 0`,
`ILLEGAL_ENDPOINT_OWNERSHIP = 0`. `hostHttpOwnershipState = ZERO`.
`HostFinalClosureState = PRESERVED` — no Host production folder or file was added in W0–W3.
`SinkFolderRegression = ZERO`.

## 15. Cross-module dependency inventory

Localization production references **no foreign module at all**:

| Project | References |
| --- | --- |
| `Contracts` | `Tooba.BuildingBlocks` |
| `Domain` | `Tooba.BuildingBlocks`, own `Contracts` |
| `Application` | `Tooba.BuildingBlocks`, own `Domain`, own `Contracts` |
| `Endpoints` | `Tooba.BuildingBlocks`, own `Application`, own `Contracts` |
| `Infrastructure` | own `Contracts`, own `Application`, `Tooba.ModuleContracts`, `Tooba.Persistence` |

`foreignAppInfraDomainCoupling = ZERO`. Contracts-only boundary is satisfied by construction.

## 16. No cross-module join proof

`LocalizationDbContext` owns only the `localization` schema (`languages` + shared outbox table).
No foreign `DbSet`, no foreign `DbContext`, no EF navigation crossing module ownership, no raw SQL
joining another module's tables, no cross-module transaction assumption.

### 16a. Foreign consumer reach-through (out of scope, disclosed)

Certification is scoped to Localization's own boundary, which is clean. However, a **Catalog-owned**
violation against Localization's schema exists at `HEAD` and is disclosed rather than hidden:

- `Catalog.Infrastructure/Directories/CatalogDirectory.cs` — `LoadPreferredLanguageIdsAsync` executes
  raw SQL `SELECT ... FROM localization.languages`.
- `Catalog.Infrastructure/Persistence/Migrations/20260909130000_AddQuantityFoundation.cs` — raw SQL
  `JOIN localization.languages`.

This is a Catalog-side contract violation (Localization schema reached without a Contracts port).
Repairing it requires a Catalog-side port plus a Catalog migration, which is outside the authorized
Localization surface. It is recorded as a repository-level architecture debt item; it does **not**
invalidate Localization's own certification, and it is explicitly listed so no reader mistakes
`microserviceExtractable = true` for a claim about Catalog's behavior.

## 17. Persistence / schema safety

`localization` schema, one migration (`20260901220000_InitialLocalization`) plus designer and model
snapshot. Migration identifiers, order, Up/Down semantics, snapshot, table, columns, indexes,
constraints and transaction behavior are **unchanged**. `migrationFilesChanged = 0`. No migration was
regenerated for structural cleanup.

## 18. Durable guards

| Guard | Facts | Scope |
| --- | --- | --- |
| `LocalizationModuleAmsc001W3CertGuardTests` | 6 | certification verdict + manifest + SoT + evidence |
| `LocalizationModuleAmsc001W2StructureGuardTests` | 9 | structure |
| `LocalizationModuleAmsc001W1MigrateGuardTests` | 7 | migrate |
| `LocalizationModuleAmcW1SolutionGuardTests` | existing | solution grouping |
| `LocalizationModuleAmcW2StructureGuardTests` | existing | structure (AMC lineage) |
| `LocalizationModuleAmcW3CqrsGuardTests` | existing | CQRS/validators/catalog |
| `LocalizationModuleAmcW4CertGuardTests` | existing | AMC certification |
| `HostLocalizationAmcGuardTests` | existing | Host closure |
| `ErrorCatalogUniqueCodeGuardTests` | repository-wide | catalog uniqueness |

No guard was weakened, no baseline widened, no assertion suppressed.

## 19. Manifest state

`tmar-module-structure-manifests.json` has **exactly one** `modules[]` entry for Localization with
`structureCertified: true`, `lockVersion: ARCH-COMPLETE-002` and allowlists re-verified against disk
in W2. No duplicate entry, no `preCertModules` residue. No manifest change was required or made.

## 20. SoT state

`tmar-current-state.json` records `localizationModuleAmsc001W0..W3`, plus
`localizationAmc001` preserved as historical AMC-001 evidence. `structureLock.certifiedModules`
already contained `Localization` before this pass (verified identical at `dbdd08e7` and at `HEAD`);
it is **not** re-added, duplicated or reordered by this wave.

Repository-global Host root checkpoint (`lastAcceptedTask`, `lastAcceptedCommit`,
`latestAcceptedImplementationWave`, `currentHostCheckpoint`, `nextHostFolder`, `workflowStop`,
`automaticNextImplementationTask`) is **untouched**.

## 21. Focused builds

| Build | Result |
| --- | --- |
| `Tooba.Localization.Contracts` / `.Domain` / `.Application` / `.Infrastructure` / `.Endpoints` | succeeded, 0 errors |
| `Tooba.Host.Tests` | succeeded, 0 errors |

## 22. Focused tests

| Filter | Result |
| --- | --- |
| `LocalizationModuleAmsc001` (W1 + W2 + W3) | **22 passed / 0 failed** |
| `Localization*` + `ErrorCatalogUniqueCodeGuardTests` | **49 passed / 0 failed** |
| `TmarSourceSizeGuard` synthetic facts | passed |

## 23. Pre-existing failures at the W3 starting HEAD (NOT caused by this wave)

All five are red at the W3 starting HEAD `6bc3ee2d` and at the W0 starting HEAD `dbdd08e7` for
reasons provably independent of this task. This wave changed **zero** of the files they read:

| Failing test | Root cause | Owner |
| --- | --- | --- |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | `CurrentAuthority()` truncates at the first `HISTORICAL / SUPERSEDED` (master-recovery line 337); the `Current Grid work checkpoint (CURRENT` marker sits at line 655, so the guard's expectation is already inside the historical region | repository-global recovery checkpoint |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | pins a stale 16-entry `structureLock.certifiedModules` list; the live SoT (unchanged by this wave, identical at `dbdd08e7` and `HEAD`) carries 23 entries | repository-global recovery checkpoint |
| `TmarSourceSizeAndInfraAppTests.Hand_written_source_size_does_not_expand_beyond_baseline` | scans an untracked, git-ignored `.tmp-baseline/` working copy present in the workspace; the baseline JSON on disk is itself dirty | workspace hygiene |
| `TmarSourceSizeAndInfraAppTests.Source_size_inventory_evidence_exists_and_matches_scan_count` | stale `docs/evidence/TB-TMAR-BOUNDARY-V1-R1/source-size-inventory.json` (`fileCount 7352` vs `1845` scanned) | stale evidence |
| `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline` | 3 `Tooba.Promotion.Infrastructure → {Inventory,Party,Pricing}.Application` edges absent from the baseline | Promotion |

No Localization file, migration, manifest or guard is involved in any of them. This matches the
same class of inherited drift that the certified Inventory AMSC W3 checkpoint recorded as
non-blocking watch. **No guard was weakened and no unrelated file was repaired to make them green.**

## 24. Residual non-blocking debt

1. `Catalog → localization.languages` raw SQL read (Catalog-owned; §16a) — requires a separate
   Catalog task.
2. `LocalizationErrorResourceSet.Owns()` matches the whole `localization.` prefix while only
   `localization.language.*` is declared. Currently harmless: the sole non-Language
   `localization.*` string in the repository is a Content publish-check `detail`
   (`"localization.language.inactive"`), not a registered catalog code. Narrowing it would require
   cross-module verification of the whole keyspace; retained as watch.
3. `AddLocalizationEndpointPresentation()` is an empty composition seam retained for Host
   registration — behavior-neutral, kept to preserve the existing registration contract.
4. `Application/Models/LanguageSnapshot.cs` holds 7 related record shapes (snapshots, directory
   specs, admin response). Acceptable today; watch for growth.
5. Transport-shape rejections now surface precise `localization.validation.*` codes through the
   foundation `validation.failed` descriptor instead of the coarse business codes (HTTP 400
   unchanged). This is the canonical repository behavior and is recorded as an explicit, accepted
   observable delta in W1.

## 25. Microservice extractability

`microserviceExtractable = true` for Localization:

- zero foreign Application/Infrastructure/Domain/Endpoints project edges;
- Contracts-only boundary satisfied by construction;
- own `localization` schema with own migrations;
- canonical Result/ApiResponseFactory/error-catalog/resource-set/localization;
- canonical logging, telemetry and correlation;
- CQRS with exhaustive validator classification;
- module-owned HTTP surface with a thin Host security adapter.

## 26. Certification verdict

`COMPLETE_REFERENCE_PATTERN` — `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
`blockingResidualDebt = ZERO`.
Stop gate: `USER_REVIEW_LOCALIZATION_AMSC_001_W3`. `automaticNextImplementationTask = NONE`.
