# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Path ↔ Namespace

`Path-Namespace-State = EXACT`

## 1. Rule

For every hand-written production `.cs` file, the declared namespace must equal the path-derived
namespace:

```text
<ProjectAssemblyName>.<RelativeFolderPathWithDots>
```

EF migrations and the model snapshot are exempt (block-scoped generated namespaces), as established
by `AddressBookPhysicalStructureGuardTests.CheckExactNamespaces`.

## 2. Verified mapping

| File (relative to project) | Declared namespace | Expected | Verdict |
| --- | --- | --- | --- |
| `Contracts/Dtos/CustomerAddressRecord.cs` | `Tooba.AddressBook.Contracts.Dtos` | same | EXACT |
| `Contracts/Errors/AddressBookErrorCodes.cs` | `Tooba.AddressBook.Contracts.Errors` | same | EXACT |
| `Contracts/Ports/IAddressBookCheckoutLookup.cs` | `Tooba.AddressBook.Contracts.Ports` | same | EXACT |
| `Contracts/Ports/IAddressBookCountPort.cs` | `Tooba.AddressBook.Contracts.Ports` | same | EXACT |
| `Domain/Aggregates/CustomerAddress.cs` | `Tooba.AddressBook.Domain.Aggregates` | same | EXACT |
| `Application/Commands/CreateCustomerAddress/CreateCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.CreateCustomerAddress` | same | EXACT |
| `Application/Commands/DeleteCustomerAddress/DeleteCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.DeleteCustomerAddress` | same | EXACT |
| `Application/Commands/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.SetDefaultCustomerAddress` | same | EXACT |
| `Application/Commands/UpdateCustomerAddress/UpdateCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.UpdateCustomerAddress` | same | EXACT |
| `Application/Queries/GetCustomerAddress/GetCustomerAddressQuery.cs` | `Tooba.AddressBook.Application.Queries.GetCustomerAddress` | same | EXACT |
| `Application/Queries/ListCustomerAddresses/ListCustomerAddressesQuery.cs` | `Tooba.AddressBook.Application.Queries.ListCustomerAddresses` | same | EXACT |
| `Application/Models/CustomerAddressWrite.cs` | `Tooba.AddressBook.Application.Models` | same | EXACT |
| `Application/Ports/IAddressBookDirectory.cs` | `Tooba.AddressBook.Application.Ports` | same | EXACT |
| `Application/Validators/AddressBookFluentRules.cs` | `Tooba.AddressBook.Application.Validators` | same | EXACT |
| `Application/Validators/CreateCustomerAddress/CreateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.CreateCustomerAddress` | same | EXACT |
| `Application/Validators/DeleteCustomerAddress/DeleteCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.DeleteCustomerAddress` | same | EXACT |
| `Application/Validators/GetCustomerAddress/GetCustomerAddressQueryValidator.cs` | `Tooba.AddressBook.Application.Validators.GetCustomerAddress` | same | EXACT |
| `Application/Validators/SetDefaultCustomerAddress/SetDefaultCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.SetDefaultCustomerAddress` | same | EXACT |
| `Application/Validators/UpdateCustomerAddress/UpdateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.UpdateCustomerAddress` | same | EXACT |
| `Endpoints/AddressBookEndpointModule.cs` | `Tooba.AddressBook.Endpoints` | same | EXACT |
| `Endpoints/Customer/AddressBookCustomerActorResolver.cs` | `Tooba.AddressBook.Endpoints.Customer` | same | EXACT |
| `Endpoints/Customer/AddressBookCustomerReadEndpoints.cs` | `Tooba.AddressBook.Endpoints.Customer` | same | EXACT |
| `Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` | `Tooba.AddressBook.Endpoints.Customer` | same | EXACT |
| `Endpoints/Errors/AddressBookErrorCatalogContributor.cs` | `Tooba.AddressBook.Endpoints.Errors` | same | EXACT |
| `Endpoints/Resources/AddressBookErrorResources.cs` | `Tooba.AddressBook.Endpoints.Resources` | same | EXACT |
| `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | `Tooba.AddressBook.Infrastructure.Adapters` | same | EXACT |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | `Tooba.AddressBook.Infrastructure.Adapters` | same | EXACT |
| `Infrastructure/DependencyInjection/AddressBookModule.cs` | `Tooba.AddressBook.Infrastructure.DependencyInjection` | same | EXACT |
| `Infrastructure/Outbox/AddressBookOutboxRegistration.cs` | `Tooba.AddressBook.Infrastructure.Outbox` | same | EXACT |
| `Infrastructure/Persistence/AddressBookDbContext.cs` | `Tooba.AddressBook.Infrastructure.Persistence` | same | EXACT |
| `Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs` | `Tooba.AddressBook.Infrastructure.Persistence.Configurations` | same | EXACT |
| `Infrastructure/Persistence/Migrations/*` (4 files) | generated, block-scoped | exempt | EXEMPT |

**31 hand-written files verified EXACT, 0 mismatches, 4 generated files exempt.**

## 3. Alias / shim checks

| Check | Result |
| --- | --- |
| `using X = Y;` namespace alias workaround | **none** |
| `TypeForwardedTo` | **none** |
| Duplicate compatibility type | **none** |
| Foreign-module global alias | **none** |

## 4. Physical-copy check

| Check | Result |
| --- | --- |
| Same type name in two live paths | **none** (`AddressBookPhysicalStructureGuardTests.AddressBook_has_no_stale_or_duplicate_physical_type_copies` passes) |
| Stale path after a previous move | **none** — legacy `Domain/CustomerAddress.cs`, `Contracts/Customer/`, `Application/Customer/`, `Infrastructure/Directories/`, `Infrastructure/Development/`, `Infrastructure/Migrations/` are all absent (asserted by `AddressBookPhysicalStructureGuardTests` and `AddressBookValidatorCoverageGuardTests`) |
| `.slnx` entry pointing at a deleted path | **none** |

`Physical-Copy-State = CLEAN`

## 5. Note

Namespace exactness is **not** a structural PASS on its own: it is verified here as `EXACT`, while
`Folder-Granularity-State = TECHNICAL_AXIS_FIRST`. The Structure skill states that
"Compiling, green tests, and manifest membership alone are not a structural PASS" and that
"Namespace/manifest is NOT proof of physical organization." W2 owns the folder-granularity repair.
