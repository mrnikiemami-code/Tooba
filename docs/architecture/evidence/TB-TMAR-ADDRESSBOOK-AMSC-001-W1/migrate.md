# TB-TMAR-ADDRESSBOOK-AMSC-001-W1 — Migrate (AMSC Wave 1)

- Task: `TB-TMAR-ADDRESSBOOK-AMSC-001-W1`
- Mode: `IMPLEMENTATION`
- Skill: `.cursor/skills/tooba-architecture-migrate/SKILL.md`
- Target: `src/backend/Modules/AddressBook/Tooba.AddressBook.*`
- Branch: `main`
- Predecessor: `TB-TMAR-ADDRESSBOOK-AMSC-001-W0` (`docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W0/`)
- W0 disposition: `READY_TO_MIGRATE`

## Closed findings

W0 identified two canonical-mechanism blockers (F1, F2) plus one stale durable guard (F8).
W1 closes all three. Structural flattening (F3/F4) is deliberately deferred to W2 (Structure).

| Finding | State before | State after |
| --- | --- | --- |
| F1 `API-Result-Pattern-State` | `AD_HOC` — 6 handlers returned raw DTO/`Unit`; endpoints used `Results.Json` / `Results.NoContent` / manual `Status201Created` | `CANONICAL` — 6 handlers return `Result<T>` / `Result`; endpoints use `api.From(result)` / `api.Created(location, result)` |
| F2 `Localization-State` / stable codes | `EXCEPTION_MESSAGE_BASED` — 14 raw `InvalidOperationException` sites with hard-coded Persian user-facing text; ownership/missing failures surfaced as HTTP 500 `platform.unexpected` | `CATALOGUED` — every site is `SemanticException(new SemanticError(<stable code>))`; 11 new descriptors + 12 resx keys in both cultures |
| F8 stale guard | `AddressBookValidatorCoverageGuardTests` RED (over-broad `preCertModules` absence assertion) | GREEN — guard asserts its documented intent (no AddressBook entry in `preCertModules`) without weakening any AddressBook assertion |

## 1. Fault → Result composition (new canonical seam)

New file:

```text
Tooba.AddressBook.Application/Composition/AddressBookOperation.cs
```

Mirrors `UserPreferenceOperation` / `AccessControlOperation` and the 12 further sibling modules that
already carry this seam. Three members:

| Member | Contract |
| --- | --- |
| `ExecuteAsync<T>(Func<Task<T>>)` | `SemanticException` → `Result.Failure<T>(ex.Error)`; anything else propagates to the global boundary |
| `ExecuteAsync(Func<Task>)` | same for the non-generic `Result` |
| `NotFoundIfNull<T>(T?, string code)` | `null` → `Result.Failure<T>` with the stable missing code, else success |

`NotFoundIfNull` is used by `GetCustomerAddressQueryHandler` so the missing/foreign decision stays in
Application. This preserves W0 F6 exactly: a foreign address is still indistinguishable from a
missing one and is **never** a 403.

## 2. Handler contract change (6/6)

| Request | Return type before | Return type after |
| --- | --- | --- |
| `ListCustomerAddressesQuery` | `IReadOnlyList<CustomerAddressRecord>` | `Result<IReadOnlyList<CustomerAddressRecord>>` |
| `GetCustomerAddressQuery` | `CustomerAddressRecord?` | `Result<CustomerAddressRecord>` |
| `CreateCustomerAddressCommand` | `CustomerAddressRecord` | `Result<CustomerAddressRecord>` |
| `UpdateCustomerAddressCommand` | `CustomerAddressRecord` | `Result<CustomerAddressRecord>` |
| `DeleteCustomerAddressCommand` | `Unit` | `Result` |
| `SetDefaultCustomerAddressCommand` | `CustomerAddressRecord` | `Result<CustomerAddressRecord>` |

Every handler body is now a single `AddressBookOperation.ExecuteAsync(...)` call around the existing
`IAddressBookDirectory` invocation. No handler gained business logic; the directory port signature is
unchanged, so `IAddressBookCheckoutLookup` (consumed by `Order.Application`) and
`IAddressBookCountPort` (consumed by `CustomerProfile.Application`) are untouched.

## 3. Endpoint contract change (6/6)

| Route | Before | After |
| --- | --- | --- |
| `GET /v1/customer/addresses` | `Results.Json(items)` | `api.From(result)` |
| `GET /v1/customer/addresses/{addressId:guid}` | `item is null ? FromFailure(...) : Results.Json(item)` | `api.From(result)` |
| `POST /v1/customer/addresses` | `Results.Json(created, statusCode: 201)` | `api.Created($"/v1/customer/addresses/{created.Value.AddressId}", created)` |
| `PUT /v1/customer/addresses/{addressId:guid}` | `Results.Json(updated)` | `api.From(result)` |
| `DELETE /v1/customer/addresses/{addressId:guid}` | `Results.NoContent()` | `api.From(result)` → 204 on success |
| `POST /v1/customer/addresses/{addressId:guid}/default` | `Results.Json(updated)` | `api.From(result)` |

The two session guards (`api.FromFailure(new SemanticError(SessionRequired))`) are unchanged.
Zero raw `Results.Json` / `Results.NoContent` / hand-threaded status codes remain in the module.

`api.From(Result)` returns `204 No Content` on success, which preserves the previous
`Results.NoContent()` DELETE contract. `ApiResponseFactory.From<T>` returns a raw JSON value (not an
envelope), which preserves the previous raw-DTO success payloads.

## 4. Typed faults and stable codes

### 4.1 Codes (`Tooba.AddressBook.Contracts/Errors/AddressBookErrorCodes.cs`)

The two pre-existing values are **unchanged** (`customer.address.missing`, `customer.session.required`).
Eleven new stable codes were added; all live in the module-owned `customer.address.` namespace so the
`AddressBookErrorResourceSet.Owns` predicate stays correct and no collision with the shared
`customer.session.` owner occurs:

```text
customer.address.actor_required
customer.address.recipient_name_required
customer.address.contact_mobile_invalid
customer.address.country_invalid
customer.address.province_name_invalid
customer.address.city_name_invalid
customer.address.postal_code_invalid
customer.address.postal_address_invalid
customer.address.building_unit_invalid
customer.address.label_invalid
customer.address.recipient_name_parts_invalid
```

`customer.session.required` remains **unregistered** by AddressBook — it is owned by the shared
Foundation contributor (W0 §3, preserved).

### 4.2 Domain (`Domain/Aggregates/CustomerAddress.cs`)

All 12 `InvalidOperationException` throws became `SemanticException(new SemanticError(<code>))`; the
Persian user-facing strings were deleted from executable code (they now live only in the `.fa.resx`
catalog). `RequireBounded` / `OptionalBounded` / `ApplyRecipientNames` now take a **code** instead of
a message, so the localized text can only come from the catalog.

### 4.3 Infrastructure (`Infrastructure/Adapters/AddressBookDirectory.cs`)

All 3 `InvalidOperationException` throws (delete-missing, require-own, empty-actor) became
`SemanticException(new SemanticError(AddressBookErrorCodes.AddressMissing | ActorRequired))`.

### 4.4 Project reference added

`Tooba.AddressBook.Domain.csproj` → `Tooba.AddressBook.Contracts.csproj`.

This is the **same legal edge already used by AccessControl, Wishlist, Identity, Offer and others**
(Domain raising module-owned typed faults). It is a boundary dependency on the module's own Contracts,
not a foreign module and not an Application/Infrastructure/Domain reference, so
`Cross-Module-Coupling-State` remains `LEGAL_CONTRACTS_ONLY` and `microserviceExtractable` remains
`true` — the two projects move together.

### 4.5 Catalog + resources

`AddressBookErrorCatalogContributor` gained 11 `Validation` / `400` descriptors (via a local `V(...)`
helper) alongside the existing `NotFound` / `404` address-missing descriptor.
`AddressBookErrors.resx` and `AddressBookErrors.fa.resx` each went from **1** key to **12** keys, so
every stable code now has a real localized title in both cultures (previously 0 of the 11 new
outcomes had any localized text at all).

## 5. Behavior change (explicit, bounded)

W0 §21 flagged `Behavior-Preservation-Risk = MEDIUM` for this change. The only client-visible
behavior difference is on the previously-defective path:

| Scenario | Before | After |
| --- | --- | --- |
| `GET`/`PUT`/`DELETE`/`default` on a foreign or missing address | HTTP **500** `platform.unexpected` | HTTP **404** `customer.address.missing` (localized) |
| Empty/invalid actor reaching Domain/Infrastructure | HTTP **500** `platform.unexpected` | HTTP **400** `customer.address.actor_required` (localized) |
| Field-shape rejection escaping transport validation | HTTP **500** `platform.unexpected` | HTTP **400** `customer.address.<field>_invalid` (localized) |

This converges onto the contract the module **already declared** (the route, the
`customer.address.missing` code and its resx key all predate this wave). It is not a product
redesign, no route/schema/DTO changed, and no success payload changed.

## 6. Durable guards

| Guard | Change | Justification |
| --- | --- | --- |
| `AddressBookPhysicalStructureGuardTests.AllowedApplicationFolders` | `Composition` added to the approved Application folder set | `Composition/` is the canonical fault-to-Result seam folder in **14 sibling modules** (UserPreference, AccessControl, Wishlist, Content, Party, Story, ProductQnA, BulkInquiry, Media, Localization, OperatorProfile, Identity, PageComposition, plus Offer's `Validation/` sibling). The AddressBook allowlist was simply incomplete; no folder was renamed and no guard was weakened. |
| `AddressBookValidatorCoverageGuardTests` (F8) | over-broad `Assert.False(TryGetProperty("preCertModules"))` replaced by `Assert.DoesNotContain(preCert, m => m.module == "AddressBook")` | Restores the guard's documented intent. `preCertModules` legitimately still holds `ProductWorkspace` (`structureCertified: false`). No AddressBook assertion was removed: `structureCertified: true`, `lockVersion`, single-entry and `AddressBook`-absence-from-preCert are all still asserted. |
| `AddressBookFoundationTests.Endpoint_uses_session_and_rejects_missing_production_actor` | `AddressMissing` assertion retargeted from the read endpoint to `GetCustomerAddressQuery.cs`; added `api.From(result)` / no-`Results.Json(` / no-`Results.NoContent(` assertions | The missing-address outcome moved from the HTTP layer into the Application handler (F6 preserved). The new assertions are **stronger** for the canonical contract: they now prove the endpoint maps `Result` and that no raw `Results.*` success path remains. |
| `AddressBookFoundationTests` (`SemanticException` assertions) | `Assert.Throws<InvalidOperationException>` → `Assert.Throws<SemanticException>` (5 sites) | Reflects the intentional fault-type change; the same invariant (owner/field-shape rejection) is asserted. |
| `StorefrontRecipientCanonicalizationTests.Address_entity_keeps_legacy_recipient_until_both_parts_exist` | `Assert.Throws<InvalidOperationException>` → `Assert.Throws<SemanticException>` (1 site) | Cross-module consumer of `CustomerAddress.ApplyRecipientNames` half-name rejection; same invariant, typed fault. This is the **only** non-AddressBook test file touched, and only because it directly asserts the aggregate's fault type. |

No guard was deleted. No baseline was widened. `guardsWeakened = NONE`.

## 7. Manifest / SoT updates

- `docs/architecture/tmar-module-structure-manifests.json` → `AddressBook` entry:
  - `certificationNote` extended with the W1 closure (Localization `EXCEPTION_MESSAGE_BASED` →
    `CATALOGUED`, API-Result-Pattern `AD_HOC` → `CANONICAL`), the bounded behavior change, and the
    W2 handoff.
  - `Tooba.AddressBook.Domain.rootAllowlistJustification` now records the Contracts reference and why
    it is legal.
  - `Tooba.AddressBook.Application.rootAllowlistJustification` now records `Composition/`.
  - `structureCertified` stays `true`; `lockVersion` stays `ARCH-COMPLETE-002`. **No module was added
    to or removed from `modules` / `uncertifiedHttpOwningModules` / `preCertModules`.**
- `docs/architecture/tmar-current-state.json` → `addressBookModuleAmsc001W1` added.

## 8. Deferred to W2 (Structure)

`Folder-Granularity-State = TECHNICAL_AXIS_FIRST` is **unchanged** in W1:

```text
Application/Commands/<UseCase>/…            4 single-file leaves
Application/Queries/<UseCase>/…             2 single-file leaves
Application/Validators/<UseCase>/…          5 single-file leaves
```

11 unjustified single-file leaf folders remain. W1 deliberately did not move them, because the
migrate skill owns behaviour-preserving canonicalization (fault/Result/localization) and the
structure skill owns the capability-first flattening + manifest-disk reconciliation. W1 only updated
the *content* of the existing files in place, so the W2 move is a pure `git mv` + namespace edit with
no semantic merge.

Also deferred (recorded, not repaired):

- W0 F5 — `Application/Validators/` vs the `Offer`/`AccessControl` `Application/Validation/`
  precedent. Consistency watch; both are accepted by the physical-structure guard.
- W0 F9 — pre-existing repo-wide gate drift (`TmarCompleteReferenceStructureGateTests` Catalog
  namespace drift, `TmarSourceSizeAndInfraAppTests` stale `.tmp-baseline` worktree, plus the
  Catalog/Story/Party/Identity/CustomerProfile solution-grouping guards). Reproduced identically
  before and after W1; **not** AddressBook regressions.
- W0 F10 — untracked `TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt`; not committed by this run.
- W0 F11 — no `Tooba.AddressBook.Tests` project; out of scope.

## 9. Verification

```text
dotnet build src/backend/Tooba.slnx                       → 0 errors / 146 warnings (baseline parity)
dotnet test --filter FullyQualifiedName~AddressBook       → 21 passed / 0 failed / 4 skipped
                                                            (W0 baseline was 20 passed / 1 failed / 4 skipped;
                                                             F8 repaired, +1 pass)
```

Post-change re-scan of the module:

| Pattern | Hits |
| --- | --- |
| `Results.Json` / `Results.NoContent` / `StatusCodes.Status201` in `Endpoints` | **0** |
| `InvalidOperationException` in Domain / Application / Infrastructure | **0** (1 remains in `Outbox/AddressBookOutboxRegistration.cs` — an unreachable framework-contract guard for an event type the module never registers; not a business outcome, recorded as residue) |
| Hard-coded Persian **user-facing** literals outside XML docs / resx | **0** (remaining Persian is XML documentation, plus 7 non-personal development-seed *data values* in `AddressBookDevelopmentSeed.cs`, which W0 §1 already classified as non-user-facing) |
| Foreign `*.Application` / `*.Infrastructure` / `*.Domain` using | **0** |

## 10. Invariants preserved

| Invariant | Status |
| --- | --- |
| Zero foreign Application/Infrastructure/Domain coupling | preserved |
| Foreign edges remain `Tooba.Order.Contracts` only (`StorefrontGuestActor.ActorId`, 2 sites) | preserved |
| `IAddressBookCheckoutLookup` / `IAddressBookCountPort` signatures | unchanged |
| `OwnerUserId` never accepted from the HTTP body | preserved (`AddressBookFoundationTests` green) |
| `CustomerAddressWrite` (Application) vs `CustomerAddressWriteRequest` (Endpoints) split (W0 F7) | preserved |
| Foreign/missing address is never a 403 (W0 F6) | preserved |
| Schema `address_book`, 2 migrations, snapshot, indexes | untouched |
| Outbox registration, DbContext, DI composition | untouched |
| Host references remain `ALLOWED_COMPOSITION_ROOT` / `ALLOWED_CONTRACT_CONSUMPTION` | untouched — **no Host production file modified** |
| `HOST_FINAL_CLOSURE_REGRESSION` | `NONE` |
| `microserviceExtractable` | `true` |

## 11. Handoff

```text
W1 exit state: LocalizationState = CATALOGUED
               ApiResultPatternState = CANONICAL
               StableErrorCodeState = CATALOGUED
               CqrsState = CANONICAL
               FolderGranularityState = TECHNICAL_AXIS_FIRST   (W2)
               BehaviorPreservationRisk = MEDIUM_CLOSED_BOUNDED
               structureHandoffState = REQUIRED
```

`automaticNextTask = TB-TMAR-ADDRESSBOOK-AMSC-001-W2` (skill `tooba-architecture-structure`).
