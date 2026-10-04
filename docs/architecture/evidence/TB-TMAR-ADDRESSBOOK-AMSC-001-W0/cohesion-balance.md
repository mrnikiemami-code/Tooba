# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Cohesion Balance

`File-Cohesion-State = COHESIVE`
`Oversized/God-File-State = NONE`

## 1. Per-file cohesion audit

Every production file was read in full. Source-file LOC includes XML documentation.

| File | LOC | Responsibilities | Verdict |
| --- | --- | --- | --- |
| `Application/Commands/CreateCustomerAddress/CreateCustomerAddressCommand.cs` | 25 | request record + handler | `COHESIVE` |
| `Application/Commands/DeleteCustomerAddress/DeleteCustomerAddressCommand.cs` | 26 | request record + handler | `COHESIVE` |
| `Application/Commands/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommand.cs` | 24 | request record + handler | `COHESIVE` |
| `Application/Commands/UpdateCustomerAddress/UpdateCustomerAddressCommand.cs` | 25 | request record + handler | `COHESIVE` |
| `Application/Queries/GetCustomerAddress/GetCustomerAddressQuery.cs` | 24 | request record + handler | `COHESIVE` |
| `Application/Queries/ListCustomerAddresses/ListCustomerAddressesQuery.cs` | 23 | request record + handler | `COHESIVE` |
| `Application/Models/CustomerAddressWrite.cs` | 20 | one Application input record | `COHESIVE` |
| `Application/Ports/IAddressBookDirectory.cs` | 20 | one directory port | `COHESIVE` |
| `Application/Validators/AddressBookFluentRules.cs` | 51 | `AddressBookValidationCodes` + `AddressBookFluentRules` | `COHESIVE` (two same-concern types: codes + their rules) |
| `Application/Validators/*/…Validator.cs` (5 files) | 19–25 | one validator each | `COHESIVE` |
| `Contracts/Dtos/CustomerAddressRecord.cs` | 20 | one boundary DTO | `COHESIVE` |
| `Contracts/Errors/AddressBookErrorCodes.cs` | 12 | module stable codes | `COHESIVE` |
| `Contracts/Ports/IAddressBookCheckoutLookup.cs` | 14 | one boundary port | `COHESIVE` |
| `Contracts/Ports/IAddressBookCountPort.cs` | 9 | one boundary port | `COHESIVE` |
| `Domain/Aggregates/CustomerAddress.cs` | 258 | one aggregate (factory + update + default transitions + bounded-field rules + name composition) | `OVERSIZED_ONLY` — single cohesive responsibility, well under any guard |
| `Endpoints/AddressBookEndpointModule.cs` | 47 | route-group + presentation registration | `COHESIVE` |
| `Endpoints/Customer/AddressBookCustomerActorResolver.cs` | 56 | actor seam interface + implementation | `COHESIVE` |
| `Endpoints/Customer/AddressBookCustomerReadEndpoints.cs` | 62 | 2 read routes | `COHESIVE` |
| `Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` | 149 | 4 write routes + `CustomerAddressWriteRequest` + its extension | `COHESIVE` (one file per audience/capability write boundary) |
| `Endpoints/Errors/AddressBookErrorCatalogContributor.cs` | 35 | error descriptors | `COHESIVE` |
| `Endpoints/Resources/AddressBookErrorResources.cs` | 29 | `ResourceManager` + `IErrorResourceSet` | `COHESIVE` |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 220 | directory adapter (create/list/get/update/delete/set-default/count + own-default invariant) | `OVERSIZED_ONLY` — single cohesive responsibility |
| `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | 66 | development seed | `COHESIVE` |
| `Infrastructure/DependencyInjection/AddressBookModule.cs` | 38 | module composition | `COHESIVE` |
| `Infrastructure/Outbox/AddressBookOutboxRegistration.cs` | 30 | outbox registration | `COHESIVE` |
| `Infrastructure/Persistence/AddressBookDbContext.cs` | 50 | DbContext + design-time factory | `COHESIVE` |
| `Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs` | 34 | EF mapping | `COHESIVE` |

## 2. God-file / multi-responsibility checks

| Check | Result |
| --- | --- |
| File mixing endpoint + request models + infrastructure + business logic | **none** |
| File bundling unrelated DTOs/snapshots/command inputs (`*Contracts.cs` dump) | **none** |
| Duplicate CQRS command/query shape beside the authoritative request | **none** |
| File with multiple unrelated top-level types | only `AddressBookFluentRules.cs` (codes + rules, same concern) and the request+handler co-location pattern used repo-wide |
| Cosmetic over-splitting to game a size guard | **none** (no AddressBook entry exists in the size baseline) |
| `AddressBookEndpointModule.cs` at project root | allowed by manifest `rootAllowlist` and by `AddressBookPhysicalStructureGuardTests` (`allowRootFiles: ["AddressBookEndpointModule.cs"]`) |

## 3. Size guard status

`Baselines/tmar-source-size-baseline.json` contains **no** AddressBook entry — no module file is
near or above a size ceiling. The two `OVERSIZED_ONLY` files (258 and 220 LOC) are cohesive
single-responsibility units and are explicitly `WATCH`, not defects.

## 4. `OVERSIZED_ONLY` disposition

| File | LOC | Disposition |
| --- | --- | --- |
| `Domain/Aggregates/CustomerAddress.cs` | 258 | WATCH only. Splitting it would fragment the aggregate's invariant surface (bounded-field rules + default transitions belong to the aggregate). No split planned. |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 220 | WATCH only. Single adapter for one aggregate. No split planned. |

## 5. Note on F1/F2

The result-pattern and fault-type findings (`analyze.md` F1/F2) are **not** cohesion defects — the
files are cohesive; their *contract* is non-canonical. They are tracked separately and must not be
"fixed" by splitting files.
