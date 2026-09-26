# TB-TMAR-ADDRESSBOOK-POST-REALIGN-REPAIR-001 — AddressBook post-realign repair

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Channel: tooba-main
WorkerId: tooba-worker-01
Parent-Task: ADDRESSBOOK-OFFER-STYLE-FOLDERS-001

## 1. Provenance

| Item | Value |
| --- | --- |
| Task | `TB-TMAR-ADDRESSBOOK-POST-REALIGN-REPAIR-001` |
| Parent realign commit (pre-work HEAD) | `160233602c6ac8cdb257478f95f13e22f2b4079a` |
| Realign commit under repair | `bb520c0dc8b1b298a618eb177c45fd45e042fc0c` |
| Repaired commit | `__SHA__` |
| Scope | AddressBook + minimum solution/test metadata only |

## 2. Canonical error presentation (gap closed)

Before: `AddressBookCustomerReadEndpoints` / `AddressBookCustomerWriteEndpoints` returned raw,
ad-hoc problem-like objects:

```csharp
return Results.Json(new { title = "Not Found", errorCode = AddressBookErrorCodes.AddressMissing }, statusCode: 404);
private static IResult Unauthorized() => Results.Json(new { title = "Unauthorized", errorCode = AddressBookErrorCodes.SessionRequired }, statusCode: 401);
```

After: both endpoint files flow 401/404 through the canonical BuildingBlocks presentation stack
(`ApiResponseFactory.FromFailure(SemanticError)` → `ISafeErrorMapper` → `IErrorDefinitionCatalog` →
`IErrorMessageLocalizer` → ProblemDetails with `errorCode`/`traceId`/`correlationId`/`requestId`).
Success paths are unchanged (`Results.Json(items)`, `Results.Json(created, 201)`, `Results.NoContent()`).

No parallel AddressBook problem pipeline was introduced.

## 3. Error catalog / localization (gap closed)

New module-owned canonical presentation seams:

| File | Role |
| --- | --- |
| `Tooba.AddressBook.Endpoints/Errors/AddressBookErrorCatalogContributor.cs` | registers `customer.address.missing` (NotFound/404) and `customer.session.required` (Forbidden/401) |
| `Tooba.AddressBook.Endpoints/Resources/AddressBookErrorResources.cs` | `ResourceManager` + `IErrorResourceSet` (owns `customer.address.` only) |
| `Tooba.AddressBook.Endpoints/Resources/AddressBookErrors.resx` | en title for `customer.address.missing` |
| `Tooba.AddressBook.Endpoints/Resources/AddressBookErrors.fa.resx` | fa title for `customer.address.missing` |

Registered in `AddAddressBookEndpointPresentation` alongside the existing actor resolver.

`AddressBookErrorResourceSet.Owns` is intentionally limited to `customer.address.` so it never competes
with another module's `customer.session.` resource ownership; `customer.session.required` resolves to the
existing shared resource owner, with the AddressBook catalog descriptor acting as the safe fallback.

## 4. Reference pattern correction

Offer remains a reference, not a mandatory clone. Post-realign placements re-reviewed by responsibility:

| Item | Verdict |
| --- | --- |
| `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | CORRECT — `Adapters` is the certified Infrastructure capability folder and has the Offer/Wallet precedent; the legacy `Development/` folder was correctly evacuated. |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | CORRECT — EF/port adapter. |
| `Infrastructure/DependencyInjection/AddressBookModule.cs` | CORRECT — composition entry, Offer precedent. |
| `Application/{Commands,Queries,Ports,Models,Validators}` | CORRECT — capability folders justified per use case. |
| `Contracts/{Dtos,Ports,Errors}` | CORRECT — cohesion; `Errors` carries the stable machine-code source only. |
| Root allowlists | CORRECT — Endpoints root holds only the composition module. |

No placement was kept merely because Offer has the same folder name; no file was moved for aesthetics.

## 5. Structure guard repair

`AddressBookPhysicalStructureGuardTests` now states that Offer is a *reference, not a mandatory clone*,
and additionally enforces:
- no flat root dump (existing);
- legitimate capability/responsibility folders (existing, capability-driven);
- exact path ↔ namespace (existing);
- **no duplicate physical type copies** (new: type-uniqueness scan across all five projects, excluding
  EF-generated migrations/snapshot and `bin`/`obj`);
- legacy non-Offer-style locations stay absent (existing).

No meaningful structure protection was weakened.

## 6. Filesystem verification

Actual on-disk structure verified (not only namespaces/manifests):

```text
Contract/Dtos/CustomerAddressRecord.cs            present
Contracts/Ports/IAddressBookCheckoutLookup.cs     present
Contracts/Errors/AddressBookErrorCodes.cs         present
Domain/Aggregates/CustomerAddress.cs              present
Infrastructure/Adapters/{AddressBookDirectory,AddressBookDevelopmentSeed}.cs  present
Infrastructure/DependencyInjection/AddressBookModule.cs                       present
Infrastructure/Outbox/AddressBookOutboxRegistration.cs                        present
Infrastructure/Persistence/.../{DbContext,Configurations,Migrations}          present
legacy Domain/CustomerAddress.cs                  ABSENT
legacy Contracts/Customer, Application/Customer  ABSENT
legacy Infrastructure/Directories, Infrastructure/Development  ABSENT
```

Project includes resolve to real paths; no stale root copy; no duplicate physical copy; namespace
equals physical path exactly. Assembly/project paths unchanged.

## 7. Visual Studio / Solution Explorer grouping (gap closed)

`src/backend/Tooba.slnx` now nests all five AddressBook projects under **one** Solution Folder:

```xml
<Folder Name="/Modules/AddressBook/">
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Application/...csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Contracts/...csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Domain/...csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Endpoints/...csproj" />
  <Project Path="Modules/AddressBook/Tooba.AddressBook.Infrastructure/...csproj" />
</Folder>
```

The former flat entries under `/Modules/` were removed — no AddressBook project remains duplicated at
another solution folder. This is Solution Folder grouping only: no project/assembly rename, no directory
move, no reference change.

Durable guard added: `AddressBookCanonicalPresentationGuardTests.AddressBook_projects_are_grouped_under_one_solution_folder`.

## 8. Behavior lock

| Dimension | State |
| --- | --- |
| Routes / HTTP methods | UNCHANGED (6 module-owned routes) |
| Status codes | UNCHANGED (401 unauthorized, 404 missing address, 200/201/204 success) |
| Success response shapes | UNCHANGED (raw DTO JSON) |
| Machine error codes | UNCHANGED (`customer.address.missing`, `customer.session.required`) |
| Endpoint ownership | UNCHANGED (module endpoints, Host ownership ZERO) |
| CQRS/MediatR behavior | UNCHANGED |
| Validator semantics | UNCHANGED (5/5 + 1 NO_VALIDATOR_REQUIRED) |
| DB schema / tables / migrations | UNCHANGED (`address_book`, NO_SCHEMA_CHANGE) |
| Tenant/security behavior | UNCHANGED (same actor seam / `ICurrentAuthenticatedUser`) |

Behavior-Change = NONE · Schema/Migration-Change = NONE · Route-Change = NONE

## 9. Focused validations

| Command | Result |
| --- | --- |
| `dotnet build Modules/AddressBook/Tooba.AddressBook.Endpoints` | succeeded, 0 errors |
| `dotnet build Host/Tooba.Host.Tests` | succeeded, 0 errors |
| `dotnet test --filter AddressBookFoundationTests\|AddressBook*GuardTests\|Tmar*GuardTests\|TmarCompleteReferenceStructureGateTests` | **Passed 28 / Failed 0** |
| `dotnet test --filter AddressBookPhysicalStructureGuardTests` | Passed 4 / Failed 0 |

No full-repository suite was run; no guard/assertion/baseline was weakened.

## 10. Known pre-existing failures (NOT caused by this repair, out of scope)

### K1 — Duplicate error codes make the canonical catalog unresolvable at runtime (cross-module)

`ErrorDefinitionCatalog` fails fast on a duplicate code (`duplicate_error_descriptor`), but the real Host
DI chain registers 10 contributors producing **136 codes with 8 duplicated codes**:

```text
checkout.authentication_required  <= Cart, Payment, Order
customer.session.required         <= Notification, Support, Wallet, Order
seller.authorization.denied       <= Support, Promotion, Order
admin.authorization.denied        <= Support, Wallet, Payment, Promotion
payment.missing                   <= Payment, Order
payment.unpaid.supply_unavailable <= Payment, Order
inventory.reservation.retry_limit_reached <= Payment, Order
payment.rejected                  <= Payment, Order
```

Reproduced with a temporary DI probe (since removed):
`System.InvalidOperationException : duplicate_error_descriptor:customer.session.required`
at `ErrorDefinitionCatalog..ctor` → `ErrorDefinitionCatalog` is **singleton** and `SafeErrorMapper`
**consumes** it, so resolving `ApiResponseFactory` / `ISafeErrorMapper` fails once the catalog is
materialized.

This is pre-existing (not introduced by this task) and spans Notification, Support, Wallet, Order, Cart,
Promotion, Payment — outside the AddressBook-only scope. AddressBook's own contribution is
`customer.address.missing` (unique) and `customer.session.required` (already a duplicate owned by other
modules). Per the scope decision for this task, AddressBook is attached to the canonical stack and the
cross-module catalog defect is recorded here rather than repaired.

### K2 — `ErrorContractTests` cannot boot the Host test app

`WebApplicationFactory<Program>` fails with
`Unable to resolve service for type 'Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort' while
attempting to activate 'Tooba.Cart.Infrastructure.Directories.CartDirectory'` (8 registered services at
fault). Pre-existing, Cart/Inventory, outside scope.

## 11. Remaining blockers / debt

| ID | Item |
| --- | --- |
| B1 | K1 must be resolved by a dedicated cross-module catalog task (dedupe shared codes or make duplicate registration idempotent). Until then the canonical error stack is not runtime-bootable. |
| B2 | `customer.session.required` has no AddressBook-owned resx entry; it resolves through the shared/fallback mechanism (by design to avoid duplicate resource ownership). |
| R1 | No dedicated `Tooba.AddressBook.Tests` project (unchanged, non-blocking). |
| R2 | Host `CustomerPanelComposer` still consumes the AddressBook Application read port directly (unchanged, non-blocking). |

## 12. Durable recovery state

`docs/architecture/tmar-current-state.json` → `addressBookPostRealignRepair` block records the final
disposition, canonical error state, physical structure state, solution-folder grouping state, focused
validations, known pre-existing failures and remaining debt.

`next Host folder started = false` — no next Host folder was inspected or started.
