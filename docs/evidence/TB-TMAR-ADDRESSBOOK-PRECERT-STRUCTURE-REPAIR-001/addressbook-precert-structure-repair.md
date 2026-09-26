# TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001 — AddressBook pre-cert structure blocker closure

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK

## 1. Parent audit + commit

| Item | Value |
| --- | --- |
| Audit parent | `TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001` |
| Audit parent commit | `bb4cc9f1f77d34d368f4278f7086ab89399c5bd3` |
| Audit evidence | `docs/evidence/TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001/addressbook-structure-audit.md` |
| Pre-work `HEAD` / `origin/main` | `bb4cc9f1f77d34d368f4278f7086ab89399c5bd3` (equal) |
| Pre-work working tree | clean |

## 2. Blocker closure matrix (AB-B1 … AB-B5)

| Blocker | Problem | Closure performed | State |
| --- | --- | --- | --- |
| **AB-B1** | `Application/AddressBookContracts.cs` dumped `CustomerAddressWrite` + `IAddressBookDirectory` at Application root | Split to `Application/Customer/Models/CustomerAddressWrite.cs` (`.Customer.Models`) and `Application/Customer/Ports/IAddressBookDirectory.cs` (`.Customer.Ports`); old root file deleted | CLOSED |
| **AB-B2** | `Infrastructure/AddressBookDirectory.cs` at Infrastructure root **and** migrations in top-level `Infrastructure/Migrations` | Directory moved to `Infrastructure/Directories/AddressBookDirectory.cs` (`.Infrastructure.Directories`); all 4 migration/snapshot files moved to `Infrastructure/Persistence/Migrations/` (`.Infrastructure.Persistence.Migrations`); old root file and old folder deleted | CLOSED |
| **AB-B3** | `Contracts/CustomerAddressContracts.cs` dumped record + checkout port at Contracts root | Moved to `Contracts/Customer/CustomerAddressContracts.cs` (`.Contracts.Customer`); old root file deleted | CLOSED |
| **AB-B4** | No durable AddressBook endpoint-reachable validator-coverage guard | Added `Tooba.Host.Tests/Architecture/AddressBookValidatorCoverageGuardTests.cs` enforcing the exact 6 = 5 + 1 inventory, DI resolution of 5 concrete validators, ISender-only endpoints, no endpoint directory call, root-capability-file evacuation and manifest pre-cert state | CLOSED |
| **AB-B5** | No AddressBook manifest entry / stale SoT | Added AddressBook PRE-CERT entry under `preCertModules` (`structureCertified: false`, `lockVersion: ARCH-COMPLETE-002`) and refreshed the AddressBook/current-recovery SoT fields | CLOSED |

## 3. Old → new file map

| Old path | New path |
| --- | --- |
| `Tooba.AddressBook.Application/AddressBookContracts.cs` | `Tooba.AddressBook.Application/Customer/Models/CustomerAddressWrite.cs` + `Tooba.AddressBook.Application/Customer/Ports/IAddressBookDirectory.cs` |
| `Tooba.AddressBook.Infrastructure/AddressBookDirectory.cs` | `Tooba.AddressBook.Infrastructure/Directories/AddressBookDirectory.cs` |
| `Tooba.AddressBook.Infrastructure/Migrations/*.cs` (4) | `Tooba.AddressBook.Infrastructure/Persistence/Migrations/*.cs` (4, same filenames) |
| `Tooba.AddressBook.Contracts/CustomerAddressContracts.cs` | `Tooba.AddressBook.Contracts/Customer/CustomerAddressContracts.cs` |

No compatibility shim, no type alias, no duplicated type, no `TypeForwardedTo`.

## 4. Namespace old → new map

| File | Old namespace | New namespace |
| --- | --- | --- |
| `CustomerAddressWrite.cs` | `Tooba.AddressBook.Application` | `Tooba.AddressBook.Application.Customer.Models` |
| `IAddressBookDirectory.cs` | `Tooba.AddressBook.Application` | `Tooba.AddressBook.Application.Customer.Ports` |
| `AddressBookDirectory.cs` | `Tooba.AddressBook.Infrastructure` | `Tooba.AddressBook.Infrastructure.Directories` |
| `20260825171858_InitialAddressBook.cs` | `Tooba.AddressBook.Infrastructure.Migrations` | `Tooba.AddressBook.Infrastructure.Persistence.Migrations` |
| `20260825171858_InitialAddressBook.Designer.cs` | `Tooba.AddressBook.Infrastructure.Migrations` | `Tooba.AddressBook.Infrastructure.Persistence.Migrations` |
| `20260913180000_AddRecipientNameParts.cs` | `Tooba.AddressBook.Infrastructure.Migrations` | `Tooba.AddressBook.Infrastructure.Persistence.Migrations` |
| `AddressBookDbContextModelSnapshot.cs` | `Tooba.AddressBook.Infrastructure.Migrations` | `Tooba.AddressBook.Infrastructure.Persistence.Migrations` |
| `CustomerAddressContracts.cs` | `Tooba.AddressBook.Contracts` | `Tooba.AddressBook.Contracts.Customer` |

## 5. Exact consumer updates

| Consumer | Change |
| --- | --- |
| `Application/Customer/Create/…Command.cs` | `using Tooba.AddressBook.Contracts;` → `…Contracts.Customer;` + added `…Application.Customer.Models;` and `…Application.Customer.Ports;` |
| `Application/Customer/Update/…Command.cs` | same pattern |
| `Application/Customer/Get/…Query.cs` | same pattern |
| `Application/Customer/List/…Query.cs` | same pattern |
| `Application/Customer/SetDefault/…Command.cs` | `…Contracts.Customer;` + `…Application.Customer.Ports;` |
| `Application/Customer/Delete/…Command.cs` | added `using Tooba.AddressBook.Application.Customer.Ports;` |
| `Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` | `using Tooba.AddressBook.Application;` → `…Application.Customer.Models;` + `…Application.Customer.Ports;` |
| `Infrastructure/AddressBookModule.cs` | `…Application;` → `…Application.Customer.Ports;` + added `using Tooba.AddressBook.Infrastructure.Directories;` |
| `Infrastructure/Directories/AddressBookDirectory.cs` (new) | usings repointed to `…Application.Customer.Models`, `…Application.Customer.Ports`, `…Contracts.Customer` |
| `Host/Tooba.Host/Program.cs` | `typeof(Tooba.AddressBook.Application.Customer.Ports.IAddressBookDirectory).Assembly`; `Tooba.AddressBook.Contracts.Customer.IAddressBookCheckoutLookup` → `…Application.Customer.Ports.IAddressBookDirectory` |
| `Host/Tooba.Host/Customer/CustomerPanelComposer.cs` | `using Tooba.AddressBook.Application;` → `…Application.Customer.Ports;` |
| `Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontCheckoutService.cs` | `using Tooba.AddressBook.Contracts;` → `…Contracts.Customer;` |
| `Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs` | `…Contracts;` → `…Contracts.Customer;` and qualified ctor parameter `Tooba.AddressBook.Contracts.Customer.IAddressBookCheckoutLookup` |
| `Host/Tooba.Host.Tests/**` (18 test files) | `using Tooba.AddressBook.Contracts;` → `…Contracts.Customer;` |
| `Host/Tooba.Host.Tests/AddressBookFoundationTests.cs` | `…Application;` → `…Application.Customer.Models;` + `…Application.Customer.Ports;`; added `using Tooba.AddressBook.Infrastructure.Directories;` |

No production behavior, route shape, HTTP status, response DTO or handler behavior changed — these are namespace/using repoints only.

## 6. Migration relocation proof

- `git mv Migrations Persistence/Migrations` (rename detected by git, `RM` status).
- All four filenames preserved: `20260825171858_InitialAddressBook.cs`, `20260825171858_InitialAddressBook.Designer.cs`, `20260913180000_AddRecipientNameParts.cs`, `AddressBookDbContextModelSnapshot.cs`.
- Only the `namespace` declaration changed (4 files). No `[Migration("…")]` identifier, no `Up`/`Down` body, no `ModelSnapshot` content, no column/table/index/constraint change.
- EF Core discovers migrations from the `DbContext` assembly, so relocation + namespace change requires no regeneration.

**Schema-Change-State = NO_SCHEMA_CHANGE.** No new migration, no migration regeneration, no migration semantic change, `AddressBookDbContext.Schema` still `address_book`, seed ids and idempotency unchanged.

## 7. Final root `.cs` lists (all five projects)

| Project | Root `.cs` files | Allowlist |
| --- | --- | --- |
| `Tooba.AddressBook.Contracts` | (none) | `[]` |
| `Tooba.AddressBook.Domain` | `CustomerAddress.cs` | `[CustomerAddress.cs]` |
| `Tooba.AddressBook.Application` | (none) | `[]` |
| `Tooba.AddressBook.Endpoints` | `AddressBookEndpointModule.cs` | `[AddressBookEndpointModule.cs]` |
| `Tooba.AddressBook.Infrastructure` | `AddressBookModule.cs` | `[AddressBookModule.cs]` |

Expected final tree exactly matches the task's EXPECTED POST-REPAIR STRUCTURE section
(Contracts/Customer, Application/Customer/{Create,Delete,Get,List,SetDefault,Update,Models,Ports} + Validators,
Infrastructure/{Development,Directories,Persistence/Migrations}, Endpoints/Customer).

## 8. Final path ↔ namespace state

Every AddressBook production `.cs` derives its declared namespace exactly from its physical path.
Mismatch count = **0**. Alias declarations in AddressBook production source = **0**.
`Path-Namespace-State = EXACT`, `Alias-Workaround-State = NO_ALIAS_NO_FOREIGN_GLOBAL_ALIAS`.
No stale `Tooba.AddressBook.Infrastructure.Migrations` or `Tooba.AddressBook.Application` root-namespace reference remains anywhere in the repository.

## 9. Endpoint request / handler / validator matrix (unchanged)

| # | Route | Request | Handler | Validator | Classification |
| --- | --- | --- | --- | --- | --- |
| 1 | `GET /v1/customer/addresses` | `ListCustomerAddressesQuery` | `ListCustomerAddressesQueryHandler` | — | NO_VALIDATOR_REQUIRED |
| 2 | `GET /v1/customer/addresses/{addressId:guid}` | `GetCustomerAddressQuery` | `GetCustomerAddressQueryHandler` | `GetCustomerAddressQueryValidator` | VALIDATOR_REQUIRED / PRESENT |
| 3 | `POST /v1/customer/addresses` | `CreateCustomerAddressCommand` | `CreateCustomerAddressCommandHandler` | `CreateCustomerAddressCommandValidator` | VALIDATOR_REQUIRED / PRESENT |
| 4 | `PUT /v1/customer/addresses/{addressId:guid}` | `UpdateCustomerAddressCommand` | `UpdateCustomerAddressCommandHandler` | `UpdateCustomerAddressCommandValidator` | VALIDATOR_REQUIRED / PRESENT |
| 5 | `DELETE /v1/customer/addresses/{addressId:guid}` | `DeleteCustomerAddressCommand` | `DeleteCustomerAddressCommandHandler` | `DeleteCustomerAddressCommandValidator` | VALIDATOR_REQUIRED / PRESENT |
| 6 | `POST /v1/customer/addresses/{addressId:guid}/default` | `SetDefaultCustomerAddressCommand` | `SetDefaultCustomerAddressCommandHandler` | `SetDefaultCustomerAddressCommandValidator` | VALIDATOR_REQUIRED / PRESENT |

Counts: requests **6**, VALIDATOR_REQUIRED **5**, present **5**, missing **0**, NO_VALIDATOR_REQUIRED **1**
(`ListCustomerAddressesQuery`, reason `NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT`).
`Validator-Coverage-State = COMPLETE_5_OF_5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED`.

## 10. Durable guard location and what it enforces

`src/backend/Host/Tooba.Host.Tests/Architecture/AddressBookValidatorCoverageGuardTests.cs`
(AddressBook has no dedicated Tests project; this mirrors the AccessControl precedent and the Host
`Architecture/` location already used by `TmarCompleteReferenceStructureGateTests`).

Enforces, with whitespace-insensitive source matching:

1. exactly 6 endpoint-reachable requests, 5 VALIDATOR_REQUIRED, 1 NO_VALIDATOR_REQUIRED;
2. the only NO_VALIDATOR_REQUIRED request is `ListCustomerAddressesQuery`;
3. endpoint `new <Command|Query>` constructions match the manifest set exactly;
4. all 5 required validators resolve via `AddToobaCqrsFoundation` DI to their exact concrete types and implement `IValidator<TRequest>`, while `ListCustomerAddressesQuery` registers none;
5. all 6 requests are MediatR `IBaseRequest` types and endpoints declare `ISender sender`, contain no `IAddressBookDirectory`, `IValidator` or `ValidateAsync` reference (XML doc comments are stripped before the negative assertions so prose is not punished);
6. AddressBook remains uncertified: the manifest entry exists in `preCertModules` with `structureCertified: false` and AddressBook is absent from the certified `modules` set;
7. the four legacy root paths are absent and the five replacement paths exist.

No production behavior is implemented in the guard.

## 11. Host reference / ownership preservation

| Item | State |
| --- | --- |
| Host AddressBook folder residue | ZERO (no `Tooba.Host/AddressBook` directory) |
| Host AddressBook HTTP ownership | ZERO — only `app.MapAddressBookModuleEndpoints()` |
| Module-owned route count | 6 |
| `AddressBookEndpointModule` | single composition entry; `MapGroup("/v1/customer/addresses")` appears once |
| ISender dispatch | preserved in both read and write endpoint files |
| Real MediatR handlers | 6 of 6 |
| Legacy dispatcher | none introduced |
| Direct directory call from endpoints | none |

Host references remain the previously classified `ALLOWED_COMPOSITION_ROOT` /
`ALLOWED_CONTRACT_CONSUMPTION` set plus the `STRUCTURAL_DEBT_ONLY` `CustomerPanelComposer` consumer, whose
only change here was a namespace-only `using` repoint.

## 12. Cross-module dependency proof

`Tooba.Order.Contracts` remains the only foreign module dependency, consumed solely as
`Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`. Absent after the repair:
`Tooba.Order.Application`, `Tooba.Order.Infrastructure`, `Tooba.Order.Domain`, Host types.
`Cross-Module-Boundary-State = ORDER_CONTRACTS_ONLY`.

## 13. Manifest changes (`structureCertified = false`)

`docs/architecture/tmar-module-structure-manifests.json` gains a new top-level `preCertModules` array holding
the AddressBook PRE-CERT entry (`structureCertified: false`, `lockVersion: ARCH-COMPLETE-002`) with the five
project root allowlists from section 7, the blocker-era forbidden root files
(`AddressBookContracts.cs`, `AddressBookDirectory.cs`, `CustomerAddressContracts.cs`) and
`forbiddenTopLevelFolders: ["Migrations"]` for Infrastructure. The eight certified `modules` entries are
untouched, `uncertifiedHttpOwningModules` is unchanged, and AddressBook is NOT added to `modules`.

JSON validated after edit: `modules = 8` certified, `preCertModules = 1`.

## 14. SoT changes

`docs/architecture/tmar-current-state.json`:

- `currentHostEvacuation.currentTask` → `TB-TMAR-ADDRESSBOOK-PRECERT-STRUCTURE-REPAIR-001`; `currentPhase` → `PRECERT_STRUCTURE_BLOCKER_CLOSURE`.
- new `addressBookPreCertRepair` block recording the repair task, audit parent + commit + evidence path, the five closed blockers, `hostAddressBookResidue = []`, `hostOwnedRouteCount = 0`, `moduleOwnedRouteCount = 6`, `validatorCoverage = COMPLETE_5_OF_5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED`, `pathNamespace = EXACT`, `aliasWorkaroundState = NO_ALIAS_NO_FOREIGN_GLOBAL_ALIAS`, `crossModuleState = ORDER_CONTRACTS_ONLY`, `structureCertified = false`, `certificationPending = true`, `manifestEntry = PRE_CERT_IN_preCertModules`, `nextTask = TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001`.
- top-level `nextTask` → `TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001`; `nextTaskGate` → `ADDRESSBOOK_ARCH_COMPLETE_002_STRUCTURE_CERTIFICATION`.

No unrelated historical TMAR state, certified module entry, checkout state or frontend state was rewritten.
JSON validated after edit.

## 15. Focused build results

| Command | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Contracts.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build …/Tooba.AddressBook.Application.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build …/Tooba.AddressBook.Infrastructure.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | Build succeeded, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --no-restore` | Build succeeded, 0 errors |

No solution build, no broad integration/architecture suite.

## 16. Focused test / guard results

| Command | Result |
| --- | --- |
| `dotnet test … --filter FullyQualifiedName~AddressBookFoundationTests\|FullyQualifiedName~AddressBookValidatorCoverageGuardTests` | **Passed 14, Failed 0** |
| `dotnet test … --filter FullyQualifiedName~TmarCompleteReferenceStructureGateTests` | **Passed 3, Failed 0** |
| `dotnet test … --filter FullyQualifiedName~TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | **FAILED — pre-existing SoT drift (see section 18)** |

## 17. Explicit untouched list

- Frontend (zero files touched) — `frontendFrozen = true`.
- Checkout behavior — `PAUSED_AT_SAFE_W5_CHECKPOINT` unchanged.
- Cart / Payment / Settlement / Fulfillment / AccessControl / Offer / Order / StoreContext behavior and their manifest entries and `structureLock.certifiedModules` — unchanged.
- AddressBook routes, HTTP status semantics, response DTO semantics, actor seam behavior, business validation/domain rules, Request/Handler behavior — unchanged.
- Persistence: schema, migration identifiers/semantics, `AddressBookDbContext` behavior, deterministic seed ids and idempotency — unchanged.
- `docs/architecture/tmar-module-structure-manifests.json` certified `modules` and `uncertifiedHttpOwningModules` — unchanged.
- Only unavoidable namespace-only consumer updates were made, including the two Order.Application `Storefront/Services` `using` repoints required to compile (no Order behavior change).

## 18. Residual / disclosed debt

| ID | Item | Note |
| --- | --- | --- |
| R1 | `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` fails on `nextTask` (expected `TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001`, actual `TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001`). | Pre-existing SoT-vs-guard drift, NOT introduced by this task. The guard still pins the older inventory task while `nextTask` had already advanced before this repair. Repair requires a guard repoint (or an Architect SoT decision) which is outside this task's authorized file scope. |
| R2 | `TmarDurableGuardTests` also pins `accessControlEvacuation.currentTask` to `TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001`. | Same pre-existing drift class; left untouched. |
| R3 | No dedicated `Tooba.AddressBook.Tests` project. | Explicitly non-blocking and out of scope; the durable guard lives in `Tooba.Host.Tests` on the AccessControl precedent. |
| R4 | Host `Customer/CustomerPanelComposer.cs` still consumes `IAddressBookDirectory` directly for `CountAsync`. | Explicitly non-blocking and out of scope; only its `using` was repointed. |
| R5 | `Infrastructure/Development/` has no other certified-module precedent. | Explicitly non-blocking and out of scope. |

## 19. Exact next task

`TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001` — AddressBook ARCH-COMPLETE-002 structure certification.
Not started automatically.
