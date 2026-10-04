# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Root Allowlist

`Root-Allowlist-State = ENFORCED`

## 1. Manifest declaration (`tmar-module-structure-manifests.json`, AddressBook entry)

| Project | `rootAllowlist` | `forbiddenRootFiles` | `forbiddenTopLevelFolders` |
| --- | --- | --- | --- |
| `Tooba.AddressBook.Contracts` | `[]` | `CustomerAddressContracts.cs` | `[]` |
| `Tooba.AddressBook.Domain` | `[]` | `[]` | `[]` |
| `Tooba.AddressBook.Application` | `[]` | `AddressBookContracts.cs` | `[]` |
| `Tooba.AddressBook.Endpoints` | `["AddressBookEndpointModule.cs"]` | `AddressBookCustomerReadEndpoints.cs`, `AddressBookCustomerWriteEndpoints.cs`, `AddressBookCustomerActorResolver.cs` | `[]` |
| `Tooba.AddressBook.Infrastructure` | `[]` | `AddressBookModule.cs`, `AddressBookDirectory.cs` | `Migrations` |

## 2. Current disk state vs allowlist

| Project | Root `.cs` on disk | Allowed? | Verdict |
| --- | --- | --- | --- |
| `Tooba.AddressBook.Contracts` | *(none)* | n/a | ✅ |
| `Tooba.AddressBook.Domain` | *(none)* | n/a | ✅ |
| `Tooba.AddressBook.Application` | *(none)* | n/a | ✅ |
| `Tooba.AddressBook.Endpoints` | `AddressBookEndpointModule.cs` | ✅ in `rootAllowlist` | ✅ |
| `Tooba.AddressBook.Infrastructure` | *(none)* | n/a | ✅ |

`Root-Allowlist-State = ENFORCED` — zero forbidden root files, zero forbidden top-level folders.

## 3. Forbidden-file absence proof

| Forbidden path | Present on disk? |
| --- | --- |
| `Tooba.AddressBook.Contracts/CustomerAddressContracts.cs` | ❌ absent |
| `Tooba.AddressBook.Application/AddressBookContracts.cs` | ❌ absent |
| `Tooba.AddressBook.Endpoints/AddressBookCustomerReadEndpoints.cs` | ❌ absent (correctly under `Customer/`) |
| `Tooba.AddressBook.Endpoints/AddressBookCustomerWriteEndpoints.cs` | ❌ absent (correctly under `Customer/`) |
| `Tooba.AddressBook.Endpoints/AddressBookCustomerActorResolver.cs` | ❌ absent (correctly under `Customer/`) |
| `Tooba.AddressBook.Infrastructure/AddressBookModule.cs` | ❌ absent (correctly under `DependencyInjection/`) |
| `Tooba.AddressBook.Infrastructure/AddressBookDirectory.cs` | ❌ absent (correctly under `Adapters/`) |
| `Tooba.AddressBook.Infrastructure/Migrations/` | ❌ absent (correctly under `Persistence/Migrations/`) |

Also absent (legacy non-canonical locations asserted by the durable guard):
`Tooba.AddressBook.Domain/CustomerAddress.cs`, `Tooba.AddressBook.Contracts/Customer/`,
`Tooba.AddressBook.Application/Customer/`, `Tooba.AddressBook.Infrastructure/Directories/`,
`Tooba.AddressBook.Infrastructure/Development/`.

## 4. Durable guard

`AddressBookPhysicalStructureGuardTests.AddressBook_production_files_live_under_approved_offer_style_folders`
enforces per-project top-folder allowlists at test time:

```text
Contracts      : Dtos, Ports, Errors
Domain         : Aggregates, Entities, ValueObjects, Policies, Events, Errors
Application    : Commands, Queries, Mappings, Ports, Validators, Dtos, ReadModels, Models, Policies
Infrastructure : Persistence, Repositories, Adapters, Outbox, Events, DependencyInjection
Endpoints      : Admin, Storefront, Seller, Customer, Errors, Resources   (root: AddressBookEndpointModule.cs)
```

## 5. Manifest drift to correct in W2/W3

The manifest's **allowlists are correct**; the drift is in the
`Tooba.AddressBook.Application.rootAllowlistJustification` text, which documents
`Commands/<UseCase>, Queries/<UseCase>, …` as the accepted shape (see `manifest-structure.md`).

The W2 target adds one new top-level Application folder (`Composition/`) and one new capability
folder (`Addresses/`). Both are already inside the durable guard's `AllowedApplicationFolders` set
(`Composition` is not currently listed — W2 must add it to the guard and to the manifest
justification honestly, mirroring `Offer`/`AccessControl`/`UserPreference`, which all place
`Composition/` under Application).
