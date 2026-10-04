# TB-TMAR-ADDRESSBOOK-AMSC-001-W1 — Verification

## 1. Build

```text
dotnet build src/backend/Tooba.slnx -v q --nologo
→ 0 Error(s)
→ 146 Warning(s)
```

Baseline at W0 was `0 errors / 146 warnings`. **Parity — no new warning introduced.**

## 2. Focused module tests

```text
dotnet test Tooba.Host.Tests --filter "FullyQualifiedName~AddressBook"
→ Failed: 0, Passed: 21, Skipped: 4, Total: 25
```

| | W0 baseline | W1 |
| --- | --- | --- |
| Passed | 20 | **21** |
| Failed | 1 (F8 stale guard) | **0** |
| Skipped | 4 (Postgres container unavailable) | 4 |

The 4 skips are `AddressBookPostgresTests` and are environmental (Testcontainers/Docker not
available), identical before and after.

## 3. Post-change source scans (module-scoped)

### 3.1 Raw HTTP result construction

```text
rg "Results\.Json|Results\.NoContent|StatusCodes\.Status201" src/backend/Modules/AddressBook
→ 0 hits
```

### 3.2 Untyped faults

```text
rg "InvalidOperationException" src/backend/Modules/AddressBook
→ 1 hit
  Infrastructure/Outbox/AddressBookOutboxRegistration.cs:25
```

Disposition: this is the module's `IOutboxModuleRegistration.GetEventTypeName` implementation,
which throws for an integration-event type the module **never registers** (AddressBook currently
publishes no external events). It is a framework-contract guard on a `Type`, not a business outcome
reachable from any endpoint, and it carries no user-facing text. Recorded as `RESIDUE_NOT_A_BLOCKER`.
It is **not** a W0 F2 site (F2 enumerated 14 Domain/Infrastructure sites; all 14 are closed).

### 3.3 Hard-coded user-facing text

```text
rg "[Arabic-block]" src/backend/Modules/AddressBook --glob "*.cs"
→ XML documentation comments only
  + 7 development-seed data values in Infrastructure/Adapters/AddressBookDevelopmentSeed.cs
```

Both categories are the ones W0 §1 already classified as **non-user-facing**. Zero user-facing
Persian literals remain in Domain/Application/Infrastructure logic. The former Persian fault text now
lives only in `AddressBookErrors.fa.resx`.

### 3.4 Foreign coupling

```text
rg "Tooba\.(?!AddressBook|BuildingBlocks|ModuleContracts|Persistence)" src/backend/Modules/AddressBook --glob "*.cs"
→ Tooba.Order.Contracts.Fulfillment (2 sites: Customer actor resolver, Development seed)
```

Both use the single immutable `StorefrontGuestActor.ActorId` constant. **Unchanged from W0.**

### 3.5 Resx key coverage

```text
AddressBookErrors.resx     : 12 keys
AddressBookErrors.fa.resx  : 12 keys
AddressBookErrorCodes      : 13 constants (12 owned + customer.session.required consumed, not owned)
```

Every owned code (`customer.address.*`) has both a catalog descriptor and an `en`/`fa` resource entry.
`customer.session.required` is intentionally absent from both files (shared Foundation owner).

## 4. Guard deltas

| Guard | W0 | W1 |
| --- | --- | --- |
| `AddressBookValidatorCoverageGuardTests` | 20 pass / **1 fail** (F8) | **21 pass / 0 fail** |
| `AddressBookPhysicalStructureGuardTests` | pass | pass (allowlist gained the canonical `Composition` folder) |
| `AddressBookCanonicalPresentationGuardTests` | pass | pass |

## 5. Pre-existing repo-wide drift (unchanged, out of scope)

Reproduced identically before and after W1. None is an AddressBook regression:

| Guard / area | Nature |
| --- | --- |
| `TmarCompleteReferenceStructureGateTests` | Catalog namespace drift (`Tooba.Catalog.Contracts.Cart` vs `Tooba.Catalog.Contracts`) |
| `TmarSourceSizeAndInfraAppTests` | stale untracked sibling `.tmp-baseline` worktree scanned by the shared size guard (also: Promotion→Inventory/Party/Pricing edges, Catalog foldering) |
| `TmarFoundationTests.Infrastructure_to_foreign_Domain_edges_do_not_expand_beyond_baseline` | `Tooba.Promotion.Infrastructure -> Tooba.Inventory.Domain` |
| `CorrelationRuntimeTests` | Cart/Inventory DI gap: `Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort` is not registered, so `CartDirectory` cannot be constructed |
| Catalog / Story / Party / Identity / CustomerProfile / Host-Admin solution-grouping guards | pre-existing `.slnx` / manifest expectations unrelated to AddressBook |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | SoT freshness gate, driven by the same pre-existing set |

AddressBook appears in **none** of these failures. The one AddressBook-adjacent non-Architecture
failure observed after the W1 source change was `StorefrontRecipientCanonicalizationTests`, which was
a **direct consequence of the intentional fault-type change** (not pre-existing drift) and was
repaired in W1 by retargeting its `Assert.Throws` to `SemanticException`.

## 6. Behaviour verification (canonical mapping)

| Case | Expected after W1 | Mechanism |
| --- | --- | --- |
| `GET` list, no session (Production) | 401 `customer.session.required` | endpoint guard, unchanged |
| `GET` own address | 200 raw DTO JSON | `api.From(Result<T>)` success branch |
| `GET` missing address | 404 `customer.address.missing` | `NotFoundIfNull` in the query handler |
| `GET` **foreign** address | 404 `customer.address.missing` (never 403) | same path — indistinguishability preserved (W0 F6) |
| `POST` valid | 201 + `Location: /v1/customer/addresses/{id}` + raw DTO | `api.Created(location, result)` |
| `PUT`/`default` foreign or missing | 404 `customer.address.missing` | `SemanticException` → `AddressBookOperation` → `api.From` |
| `DELETE` own | 204 No Content | `api.From(Result)` success branch |
| Any field-shape fault escaping transport validation | 400 `customer.address.<field>_invalid` | Domain `SemanticException` → catalog descriptor |

No route, request DTO, response DTO or schema changed.
