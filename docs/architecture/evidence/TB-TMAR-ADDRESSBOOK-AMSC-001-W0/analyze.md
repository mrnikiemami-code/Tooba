# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Analyze (AMSC Wave 0)

- Task: `TB-TMAR-ADDRESSBOOK-AMSC-001-W0`
- Mode: `ANALYSIS_ONLY`
- Skill: `.cursor/skills/tooba-architecture-analyze/SKILL.md`
- Target: `src/backend/Modules/AddressBook/Tooba.AddressBook.*`
- Branch: `main`
- HEAD at analysis start: `3256fc7acf581232d2a14eba30e1cd0f52fa486e` (== `origin/main`)
- Goal of the AMSC run: drive `AddressBook` to a professional, Visual-Studio-standard,
  zero-foreign-coupling surface that can be lifted out as an independent microservice.
- Structure handoff owner: `.cursor/skills/tooba-architecture-structure/SKILL.md`

## Scope and bounded recovery unit

Bounded to the **AddressBook module surface** (5 production projects, 35 hand-written production
`.cs` files + 4 EF-generated migration/snapshot files).

The module is already `structureCertified: true` under `ARCH-COMPLETE-002` in
`docs/architecture/tmar-module-structure-manifests.json` (entry `module: "AddressBook"`, line 1401),
with accepted disposition history:

- `TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001` (initial certification)
- `TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001`
- `ADDRESSBOOK-OFFER-STYLE-FOLDERS-001` / `TB-TMAR-ADDRESSBOOK-POST-REALIGN-REPAIR-001`

Ownership and coupling are therefore **not** reopened as an ownership question — they are re-verified
against current disk. This wave audits current-disk **cohesion, canonical-mechanism and
folder-granularity** debt that the existing certificate must not hide, and it re-verifies every
previously certified invariant at this HEAD.

## Canonical mechanism discovery (performed before judging)

See `canonical-mechanisms.md`. Summary of what was **discovered** (not assumed):

| Concern | Canonical mechanism found in repository | Reference used |
| --- | --- | --- |
| Result / expected failure | `Tooba.BuildingBlocks.Results.Result` / `Result<T>` + `SemanticError` | BuildingBlocks, Offer |
| Typed fault exception | `Tooba.BuildingBlocks.SemanticException(SemanticError)` (`.Error.Code`) | BuildingBlocks, UserPreference |
| Fault→Result composition | `Application/Composition/<Module>Operation.ExecuteAsync` | **UserPreference, AccessControl, Wishlist, Content, Party, Story, ProductQnA, BulkInquiry, Media, Localization, OperatorProfile, Identity, PageComposition** (13 modules) |
| API response mapping | `ApiResponseFactory.From(Result<T>)` / `From(Result)` / `Created` / `FromFailure` | BuildingBlocks, UserPreference, AccessControl |
| Error catalog | `IErrorCatalogContributor` + `ErrorDescriptor` + `IErrorDefinitionCatalog` + `SafeErrorMapper` | Offer, AccessControl |
| Localization | `IErrorResourceSet` + `AddressBookErrors.resx` / `.fa.resx` + `IErrorMessageLocalizer` | Offer, AccessControl |
| Stable codes | `Tooba.<Module>.Contracts.Errors.<Module>ErrorCodes` | Offer, AccessControl |
| Validation codes | `<Module>ValidationCodes` in `Application/Validation/` | Offer, AccessControl |
| CQRS | `AddToobaCqrsFoundation` (MediatR 12.5.0) | `TmarFoundation.cs` |
| Logging | `ILogger<T>` + `ObservabilityLogScope` | BuildingBlocks |
| Tracing / correlation | `ToobaTelemetry`, `IModuleCallTracer`, `ICorrelationIdProvider` (`X-Correlation-Id`) | BuildingBlocks |
| Size / cohesion guard | `TmarSourceSizeGuard` + `Baselines/tmar-source-size-baseline.json` | `Tooba.Host.Tests` |

**Key discovery:** the repository has **two** established fault mechanisms and `AddressBook` uses
**neither** of them:

1. `SemanticException(SemanticError)` — the framework-level typed fault, already mapped by
   `SafeErrorMapper` into `MappedSafeError` (catalog lookup by stable code).
2. `<Module>Exception` + `<Module>Operation.ExecuteAsync` — the module-local typed fault used by
   13 sibling modules (including the immediately preceding AMSC run, `AccessControl`).

`AddressBook` instead throws `System.InvalidOperationException` with **Persian technical messages**,
and its handlers do not wrap calls in any `Operation` composition at all.

## Structured state fields

1. **Foundation-State** — `FOUNDATION_READY` (5 projects present, `structureCertified: true`, all
   required capability folders exist).
2. **Ownership-State** — `correct` for all five layers. No `MUST_SPLIT` file.
3. **File-Cohesion-State** — `COHESIVE` for every production file. Largest hand-written file is
   `AddressBookDirectory.cs` (220 LOC). No mixed `*Contracts.cs` bundle, no god-file.
4. **Oversized/God-File-State** — `NONE`. No file exceeds any module size guard; no AddressBook entry
   exists in `tmar-source-size-baseline.json`.
5. **Localization-State** — `EXCEPTION_MESSAGE_BASED`. `AddressBookErrors.resx` /
   `.fa.resx` carry exactly one key (`customer.address.missing`); the actual user-facing failure
   vocabulary of the module lives as hard-coded Persian strings inside thrown
   `InvalidOperationException` messages (Domain + Infrastructure, 14 sites) and as developer seed
   values (7 sites, not user-facing).
6. **API-Result-Pattern-State** — `AD_HOC` (**blocker**). Handlers return raw DTO / `Unit`; endpoints
   therefore use raw `Results.Json(...)`, `Results.NoContent()` and a manually threaded
   `StatusCodes.Status201Created`. `ApiResponseFactory` is injected and used **only** for the two
   session/not-found guard paths.
7. **Stable-Error-Code-State** — `CATALOGUED` but **structurally incomplete**:
   `AddressBookErrorCodes.AddressMissing` is catalogued; `AddressBookErrorCodes.SessionRequired`
   (= `customer.session.required`) is owned by the shared Foundation contributor and is correctly
   consumed without re-registration. However **no stable code exists at all** for the five real
   business/ownership failures the module produces (foreign/missing address on update, delete and
   set-default, plus invalid actor). Those currently surface as unhandled `InvalidOperationException`
   → `platform.unexpected` **HTTP 500**.
8. **Logging-State** — `CANONICAL`. No `Console.WriteLine`, no `Debug.WriteLine`, no second pipeline,
   no `ILogger` misuse. The module currently logs nothing (acceptable; the global boundary logs).
9. **Sensitive-Logging-State** — `NONE`.
10. **OpenTelemetry-State** — `CANONICAL`. No `ActivitySource.StartActivity`, no `new Meter`, no
    manual `traceparent`.
11. **Correlation-Trace-State** — `CANONICAL`. No `AsyncLocal`, no custom header, no
    `Guid.NewGuid()`-as-correlation.
12. **CQRS-State** — `PARTIAL`. 6 real `IRequest<T>` types, 6 real `IRequestHandler<,>`, 6 `ISender`
    dispatch sites, zero direct directory access from endpoints — **but** handler return types are
    raw `CustomerAddressRecord` / `IReadOnlyList<CustomerAddressRecord>` / `Unit` instead of the
    canonical `Result<T>` / `Result`.
13. **Validator-Coverage-State** — `EXHAUSTIVE` (5 `VALIDATOR_REQUIRED` + 1
    `NO_VALIDATOR_REQUIRED`, guarded by `AddressBookValidatorCoverageGuardTests`). No gap.
14. **Contracts-Boundary-State** — `CLEAN`. `Contracts/` holds only a cross-module DTO, two narrow
    cross-module ports and module-owned stable codes. No Application dump.
15. **Cross-Module-Coupling-State** — `LEGAL_CONTRACTS_ONLY`. Zero foreign
    Application/Infrastructure/Domain references, zero foreign `DbContext`/`DbSet`, zero
    cross-module SQL/EF join. Foreign edges are `Tooba.Order.Contracts` only (both csproj and
    usings).
16. **Cross-Module-Join-State** — `NONE`. `AddressBookDirectory` touches only its own
    `Addresses` set.
17. **Persistence-Ownership-State** — `CORRECT`. One DbContext, schema `address_book`, module-owned
    migrations, module-owned outbox registration, no foreign FK/DbSet.
18. **Endpoint-Ownership-State** — `MODULE_OWNED`. 6 module-owned routes; Host AddressBook HTTP
    ownership ZERO (legacy Host `AddressBook/AddressBookEndpoints.cs` deleted).
19. **Host-Residue-State** — ZERO illegal. Remaining Host references are all legitimate
    `ALLOWED_COMPOSITION_ROOT` / contract consumption:
    - `Composition/ToobaModuleComposition.cs` — `new AddressBookModule()` (module registration)
    - `Program.cs` — `AddAddressBookEndpointPresentation()`, `MapAddressBookModuleEndpoints()`,
      CQRS assembly scan, `IAddressBookCheckoutLookup` → `IAddressBookDirectory` alias
    - `Development/DevelopmentSchemaMigrator.cs` — development seed call
    - `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` — migration descriptor
20. **Schema-Migration-State** — `UNCHANGED`. Two migrations
    (`20260825171858_InitialAddressBook`, `20260913180000_AddRecipientNameParts`) + snapshot. No
    drift planned in this run; W1 must not regenerate them.
21. **Behavior-Preservation-Risk** — `LOW` for structure/rename work, **`MEDIUM` for the
    result/fault change** (F1/F2): the change alters the HTTP outcome for ownership failures from
    `500 platform.unexpected` to a catalogued `404 customer.address.missing`. This is a *defect
    repair to the documented intent* (the route contract, the catalog code and the resx key already
    exist), not a product redesign — but it must be called out and guarded, not silently applied.
22. **Canonical-Reference-Used** — `UserPreference` (fault→Result `Operation` composition +
    `Result<T>` handlers + `api.From(result)` endpoints; the closest single-capability sibling),
    `Offer` (catalog contributor shape, `Contracts.Errors` codes, `Application/Validation/`
    precedent), `AccessControl` (the immediately preceding AMSC run: module-local exception +
    `Operation`), BuildingBlocks (`Result`, `SemanticError`, `SemanticException`,
    `ApiResponseFactory`, `SafeErrorMapper`).
23. **Final-Disposition** — `READY_TO_MIGRATE`.

## Findings

### F1 — `API-Result-Pattern-State = AD_HOC`: handlers return raw DTO/`Unit` (blocker for W1)

All six endpoint-reachable handlers bypass the canonical `Result` contract:

| Request | Current return type | Canonical (UserPreference/Offer shape) |
| --- | --- | --- |
| `ListCustomerAddressesQuery` | `IReadOnlyList<CustomerAddressRecord>` | `Result<IReadOnlyList<CustomerAddressRecord>>` |
| `GetCustomerAddressQuery` | `CustomerAddressRecord?` | `Result<CustomerAddressRecord>` |
| `CreateCustomerAddressCommand` | `CustomerAddressRecord` | `Result<CustomerAddressRecord>` |
| `UpdateCustomerAddressCommand` | `CustomerAddressRecord` | `Result<CustomerAddressRecord>` |
| `DeleteCustomerAddressCommand` | `Unit` | `Result` |
| `SetDefaultCustomerAddressCommand` | `CustomerAddressRecord` | `Result<CustomerAddressRecord>` |

Consequence in the Endpoints layer (`AddressBookCustomerReadEndpoints.cs`,
`AddressBookCustomerWriteEndpoints.cs`): raw `Results.Json(items)`, `Results.Json(created,
statusCode: StatusCodes.Status201Created)`, `Results.NoContent()` and a `item is null ? … : …`
ternary that performs business classification (missing address) inside the HTTP layer.

`ApiResponseFactory` **is** already injected into every endpoint and **is** used for the two guard
paths (`FromFailure(new SemanticError(...))`), which proves the canonical stack is wired and only the
handler contract is wrong.

### F2 — No stable error codes for the module's real business failures; raw `InvalidOperationException`

`Contracts/Errors/AddressBookErrorCodes.cs` declares only `AddressMissing` and `SessionRequired`.
Every actual ownership/existence failure is thrown as `InvalidOperationException` with a hard-coded
Persian message:

| File | Line | Site | Current outcome |
| --- | --- | --- | --- |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 123 | `DeleteAsync` foreign/missing address | `500 platform.unexpected` |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 155 | `RequireOwnAsync` (Update / SetDefault) | `500 platform.unexpected` |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 216 | `EnsureActor` empty actor | `500 platform.unexpected` |
| `Domain/Aggregates/CustomerAddress.cs` | 119 | `Create` empty owner | `500 platform.unexpected` |
| `Domain/Aggregates/CustomerAddress.cs` | 197–206 | `ApplyFields` length/required rules (10 sites) | `500 platform.unexpected` |
| `Domain/Aggregates/CustomerAddress.cs` | 223 | `ApplyRecipientNames` half-name | `500 platform.unexpected` |

Two distinct problems:

1. **No typed fault** — `InvalidOperationException` is not a recognized fault type, so
   `SafeErrorMapper` classifies it as `Unexpected` → 500. The user-facing failure is neither stable
   nor localized.
2. **Hard-coded Persian user-facing text** in Domain/Infrastructure — exactly the
   `HARDCODED_TEXT` / `EXCEPTION_MESSAGE_BASED` pattern the Analyze and Certify skills forbid. These
   are not merely log messages: they are the only text the client can receive for these failures.

The existing resx key `customer.address.missing` already documents the intended vocabulary, and
`AddressBookErrorCodes.AddressMissing` already exists — so the repair is *convergence onto the
already-declared contract*, not new product semantics.

### F3 — `Folder-Granularity-State = TECHNICAL_AXIS_FIRST` (blocker for W2)

`Tooba.AddressBook.Application` is organized primarily by technical axis, with a one-file use-case
leaf folder under each axis — 11 unjustified single-file leaf folders:

```text
Application/
  Commands/CreateCustomerAddress/CreateCustomerAddressCommand.cs
  Commands/DeleteCustomerAddress/DeleteCustomerAddressCommand.cs
  Commands/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommand.cs
  Commands/UpdateCustomerAddress/UpdateCustomerAddressCommand.cs
  Queries/GetCustomerAddress/GetCustomerAddressQuery.cs
  Queries/ListCustomerAddresses/ListCustomerAddressesQuery.cs
  Validators/CreateCustomerAddress/CreateCustomerAddressCommandValidator.cs
  Validators/DeleteCustomerAddress/DeleteCustomerAddressCommandValidator.cs
  Validators/GetCustomerAddress/GetCustomerAddressQueryValidator.cs
  Validators/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommandValidator.cs
  Validators/UpdateCustomerAddress/UpdateCustomerAddressCommandValidator.cs
```

Each leaf holds exactly **one** production source file (counted as source files, not declared types —
the command/query files each declare request + handler, which the Structure skill explicitly says does
**not** justify the folder).

This is a direct **certification drift** finding against the existing claim in
`tmar-module-structure-manifests.json` line 1425:

> `"AddressBook.Application intentionally has no root .cs file: Commands/<UseCase>, Queries/<UseCase>, …"`

The manifest itself documents the non-canonical shape as the accepted one. It must be corrected in
W2/W3, not preserved.

**Capability-justification test:** AddressBook has exactly **one** business capability
(customer delivery address book). It is *not* a multi-capability module. However the Structure skill
states that a per-use-case leaf folder is `OVER_FOLDERED` **when named after one Command/Query/UseCase
and containing one source file**, independent of capability count. The sibling single-capability
module `UserPreference` demonstrates the canonical shallow shape
(`LocalePreferences/Commands/UpsertUserPreferenceCommand.cs` — plural capability folder, no
per-use-case leaf), and `Party`/`BulkInquiry`/`Wishlist` follow the same convention. Therefore the
11 single-file leaves must be flattened, and the primary axis should be a plural capability folder
with secondary `Commands`/`Queries`/`Validators` under it.

### F4 — `Application/Validators/<UseCase>/` mirrors the same defect

Covered by F3, listed separately because the mirror is at three levels
(`Validators/<UseCase>/<UseCase>CommandValidator.cs`) and because the module already has a
cross-cutting `AddressBookFluentRules.cs` sitting in the same `Validators/` root — evidence that the
`Validators/` root is a real shared folder and its children are not.

### F5 — `Validators/` vs the `Validation/` precedent (consistency, not a blocker)

`AddressBook` uses `Application/Validators/AddressBookFluentRules.cs` (which declares both
`AddressBookValidationCodes` and `AddressBookFluentRules`). `Offer` and `AccessControl` use
`Application/Validation/`. Both `Validators` and `Validation` are accepted by
`AddressBookPhysicalStructureGuardTests.AllowedApplicationFolders`. Because the module has exactly
one capability and `Validators/` will remain the per-request validator home under the capability
folder, this is recorded as a **consistency watch (non-blocking)**, not a defect. W1 will not
silently rename it; if W2/W3 aligns it with the `Validation/` precedent it must be an explicit,
evidenced decision with the guard updated honestly.

### F6 — `GetCustomerAddressQuery` returns `null` for a foreign address

`GetAsync` filters `AddressId == addressId && OwnerUserId == actorUserId` and returns `null` for both
"missing" and "foreign". The endpoint maps `null` → `AddressMissing`. This is correct behavior and
must be preserved exactly (never leak existence of a foreign address). Recorded so W1 does not
"improve" it into a 403.

### F7 — `CustomerAddressWrite` lives in `Application/Models` while `CustomerAddressWriteRequest`
lives in `Endpoints/Customer`

Two near-identical records with different jobs (Application input vs HTTP body). This is the
sanctioned split (owner identity is stripped at the boundary) and is explicitly asserted by
`AddressBookFoundationTests` (`Assert.DoesNotContain("OwnerUserId", moduleWriteSource)`). **Not** a
duplicate-CQRS-shape violation. Recorded as a preserved invariant.

### F8 — Pre-existing stale durable guard (module-adjacent, blocks honest W3 certification)

`AddressBookValidatorCoverageGuardTests.AddressBook_is_certified_with_exactly_one_manifest_entry`
is **RED at this HEAD** (reproduced: 20 passed / 1 failed / 4 skipped):

```text
Error Message: preCertModules must be removed after promotion
  at …AddressBookValidatorCoverageGuardTests.cs:line 148
```

The guard asserts `!doc.RootElement.TryGetProperty("preCertModules")`. That assertion is
**over-broad and stale**: `preCertModules` still legitimately contains `ProductWorkspace`
(`tmar-module-structure-manifests.json` line 1555, `structureCertified: false`), which is an
unrelated, correctly uncertified module. The guard's documented intent — "exactly one certified
AddressBook entry, no AddressBook pre-cert duplicate" — is already satisfied
(`"module": "AddressBook"` appears once, `structureCertified: true`). This is guard **correctness**
to be repaired in W1/W3 without weakening the AddressBook assertions.

### F9 — Pre-existing repo-wide gate drift (proven pre-existing, out of scope)

Reproduced at this HEAD (7 failed / 8 passed):

- `TmarCompleteReferenceStructureGateTests` — 3 failures (Catalog module expectation drift).
- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable`.
- `TmarSourceSizeAndInfraAppTests` — 3 failures (stale sibling `.tmp-baseline` worktree scanned by the
  shared size guard; already recorded in `tmar-current-state.json`).

These are **not** AddressBook regressions and are out of this task's bounded scope. They will be
reproduced again after each wave and reported, not repaired.

### F10 — Untracked foreign artifact in the working tree

`docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt` is untracked and unrelated
to AddressBook (leftover from the Order run). It will **not** be committed by this AMSC run.

### F11 — No `Tooba.AddressBook.Tests` project (out of scope, recorded only)

AddressBook behavior and architecture guards live in `Tooba.Host.Tests`. Creating a module test
project is a new-project scope expansion and is not part of this AMSC run (same disposition as
AccessControl W0 F5).

## Illegal dependencies

**None.**

- Zero foreign `*.Application`, `*.Infrastructure`, `*.Domain` references (csproj and `using`).
- Zero foreign `DbContext` / `DbSet` access; `AddressBookDirectory` touches only its own `Addresses`.
- Zero cross-module SQL/EF joins.
- Zero Host business or persistence authority.
- Foreign edge inventory: `Tooba.Order.Contracts` only —
  `Endpoints.csproj` + `Infrastructure.csproj` project references; usings
  `Tooba.Order.Contracts.Fulfillment` in `AddressBookCustomerActorResolver.cs` and
  `AddressBookDevelopmentSeed.cs` (both for the stable `StorefrontGuestActor.ActorId` contract
  constant).
- Reverse edge inventory (foreign → AddressBook): `Tooba.AddressBook.Contracts` only —
  `Order.Application` (`IAddressBookCheckoutLookup`, `CustomerAddressRecord`),
  `CustomerProfile.Application` (`IAddressBookCountPort`).

## Cross-module join inventory

**None.**

## Contracts-only replacement map

No replacement required. The existing boundaries are already the smallest lawful shape:

- `Contracts/Dtos/CustomerAddressRecord.cs` — cross-module read snapshot.
- `Contracts/Ports/IAddressBookCheckoutLookup.cs` — narrow Order checkout read port.
- `Contracts/Ports/IAddressBookCountPort.cs` — narrow CustomerProfile dashboard count port.
- `Contracts/Errors/AddressBookErrorCodes.cs` — module-owned stable codes.

`Application/Ports/IAddressBookDirectory.cs` correctly extends `IAddressBookCheckoutLookup`, keeping
the write/list surface Application-internal.

## Target paths for W1 / W2

```text
Tooba.AddressBook.Application/
  Addresses/                                  (plural capability folder — the module's single real capability)
    Commands/
      CreateCustomerAddressCommand.cs
      DeleteCustomerAddressCommand.cs
      SetDefaultCustomerAddressCommand.cs
      UpdateCustomerAddressCommand.cs
    Queries/
      GetCustomerAddressQuery.cs
      ListCustomerAddressesQuery.cs
    Validators/
      CreateCustomerAddressCommandValidator.cs
      DeleteCustomerAddressCommandValidator.cs
      GetCustomerAddressQueryValidator.cs
      SetDefaultCustomerAddressCommandValidator.cs
      UpdateCustomerAddressCommandValidator.cs
    Models/CustomerAddressWrite.cs            (or keep shared Models/ — W2 decides, evidenced)
    Ports/IAddressBookDirectory.cs            (or keep shared Ports/ — W2 decides, evidenced)
  Composition/AddressBookOperation.cs         (NEW — fault→Result, mirrors UserPreference/AccessControl)
  Validators/AddressBookFluentRules.cs        (shared cross-cutting rules — unchanged)
```

```text
Tooba.AddressBook.Contracts/Errors/AddressBookErrorCodes.cs   (+ AddressOwnershipDenied, ActorRequired …)
Tooba.AddressBook.Domain/Aggregates/CustomerAddress.cs        (InvalidOperationException → SemanticException)
Tooba.AddressBook.Infrastructure/Adapters/AddressBookDirectory.cs (InvalidOperationException → SemanticException)
Tooba.AddressBook.Endpoints/Errors/AddressBookErrorCatalogContributor.cs (+ descriptors)
Tooba.AddressBook.Endpoints/Resources/AddressBookErrors.resx / .fa.resx (+ keys)
```

## Migration order (W1 — behavior-preserving canonicalization)

1. Add `Application/Composition/AddressBookOperation.cs` (`ExecuteAsync<T>` / `ExecuteAsync` /
   `NotFoundIfNull<T>`) mirroring `UserPreferenceOperation` / `AccessControlOperation`.
2. Extend `AddressBookErrorCodes` with the missing stable codes for the real failures
   (ownership/existence, actor required, field-shape) — **without** changing the two existing values.
3. Replace `InvalidOperationException` with `SemanticException(new SemanticError(<Code>))` in
   `CustomerAddress` and `AddressBookDirectory`; drop the hard-coded Persian user-facing messages.
4. Change the six handlers to `Result<T>` / `Result`, wrapping directory calls in
   `AddressBookOperation.ExecuteAsync`; keep the `null`-means-missing semantics of F6.
5. Change the six endpoints to `api.From(result)` / `api.Created(...)`; remove the last raw
   `Results.Json` / `Results.NoContent` / `StatusCodes.Status201Created` sites.
6. Add the new descriptors to `AddressBookErrorCatalogContributor` and the matching keys to both
   `.resx` files; keep `customer.session.required` **unregistered** (shared owner).
7. Repair the stale `AddressBookValidatorCoverageGuardTests` assertion (F8) to its documented intent.
8. Focused build + focused guards + AddressBook behavior tests; commit and push W1.

W2 then flattens the Application tree (F3/F4) and updates the manifest + durable structure guard.
W3 certifies.

## Verification plan

- `dotnet build src/backend/Tooba.slnx` → 0 errors (baseline established: 0 errors, 146 warnings).
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AddressBook` → baseline
  20 passed / 1 failed / 4 skipped; W1 must reach 21 passed / 0 failed / 4 skipped.
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AddressBookPhysicalStructureGuardTests`.
- `dotnet test Tooba.Host.Tests --filter FullyQualifiedName~AddressBookCanonicalPresentationGuardTests`.
- Re-run the F9 gate set to prove the pre-existing drift is unchanged (not widened).
- Post-change re-scan for: raw `Results.Json` in Endpoints, `InvalidOperationException` in
  Domain/Application/Infrastructure, hard-coded Persian string literals outside XML docs/resx,
  foreign Application/Infrastructure/Domain usings.

## Certification blockers (to be closed in W1–W3)

1. **F1** — `API-Result-Pattern-State = AD_HOC`; handlers return raw DTO/`Unit`.
2. **F2** — no stable codes for real business failures; `InvalidOperationException` +
   hard-coded Persian text; ownership failures surface as HTTP 500.
3. **F3/F4** — `TECHNICAL_AXIS_FIRST` Application tree with 11 unjustified single-file leaf folders
   (structure certification drift against manifest line 1425).
4. **F8** — stale `AddressBookValidatorCoverageGuardTests` assertion (RED at HEAD).

F5, F6, F7, F9, F10, F11 are recorded observations, not AddressBook certification blockers.

## Post-Host-Final-Closure regression check

`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` are present in SoT.
This wave adds/modifies **no** Host production file. All Host references to AddressBook remain
`ALLOWED_COMPOSITION_ROOT` / `ALLOWED_CONTRACT_CONSUMPTION`. `HOST_FINAL_CLOSURE_REGRESSION = NONE`.
