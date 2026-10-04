# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — path-namespace

`Path-Namespace-State = EXACT`. `AliasWorkaround = NONE`.

## Namespace ↔ path for every production file (32/32)

| File | Namespace | Path-derived | State |
| --- | --- | --- | --- |
| `Contracts/Dtos/CustomerAddressRecord.cs` | `Tooba.AddressBook.Contracts.Dtos` | same | EXACT |
| `Contracts/Errors/AddressBookErrorCodes.cs` | `Tooba.AddressBook.Contracts.Errors` | same | EXACT |
| `Contracts/Ports/IAddressBookCheckoutLookup.cs` | `Tooba.AddressBook.Contracts.Ports` | same | EXACT |
| `Contracts/Ports/IAddressBookCountPort.cs` | `Tooba.AddressBook.Contracts.Ports` | same | EXACT |
| `Domain/Aggregates/CustomerAddress.cs` | `Tooba.AddressBook.Domain.Aggregates` | same | EXACT |
| `Application/Addresses/Commands/CreateCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Addresses.Commands` | same | EXACT |
| `Application/Addresses/Commands/DeleteCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Addresses.Commands` | same | EXACT |
| `Application/Addresses/Commands/SetDefaultCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Addresses.Commands` | same | EXACT |
| `Application/Addresses/Commands/UpdateCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Addresses.Commands` | same | EXACT |
| `Application/Addresses/Queries/GetCustomerAddressQuery.cs` | `Tooba.AddressBook.Application.Addresses.Queries` | same | EXACT |
| `Application/Addresses/Queries/ListCustomerAddressesQuery.cs` | `Tooba.AddressBook.Application.Addresses.Queries` | same | EXACT |
| `Application/Addresses/Validators/CreateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Addresses.Validators` | same | EXACT |
| `Application/Addresses/Validators/DeleteCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Addresses.Validators` | same | EXACT |
| `Application/Addresses/Validators/GetCustomerAddressQueryValidator.cs` | `Tooba.AddressBook.Application.Addresses.Validators` | same | EXACT |
| `Application/Addresses/Validators/SetDefaultCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Addresses.Validators` | same | EXACT |
| `Application/Addresses/Validators/UpdateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Addresses.Validators` | same | EXACT |
| `Application/Composition/AddressBookOperation.cs` | `Tooba.AddressBook.Application.Composition` | same | EXACT |
| `Application/Models/CustomerAddressWrite.cs` | `Tooba.AddressBook.Application.Models` | same | EXACT |
| `Application/Ports/IAddressBookDirectory.cs` | `Tooba.AddressBook.Application.Ports` | same | EXACT |
| `Application/Validators/AddressBookFluentRules.cs` | `Tooba.AddressBook.Application.Validators` | same | EXACT |
| `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | `Tooba.AddressBook.Infrastructure.Adapters` | same | EXACT |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | `Tooba.AddressBook.Infrastructure.Adapters` | same | EXACT |
| `Infrastructure/DependencyInjection/AddressBookModule.cs` | `Tooba.AddressBook.Infrastructure.DependencyInjection` | same | EXACT |
| `Infrastructure/Outbox/AddressBookOutboxRegistration.cs` | `Tooba.AddressBook.Infrastructure.Outbox` | same | EXACT |
| `Infrastructure/Persistence/AddressBookDbContext.cs` | `Tooba.AddressBook.Infrastructure.Persistence` | same | EXACT |
| `Infrastructure/Persistence/Configurations/CustomerAddressConfiguration.cs` | `Tooba.AddressBook.Infrastructure.Persistence.Configurations` | same | EXACT |
| `Endpoints/AddressBookEndpointModule.cs` | `Tooba.AddressBook.Endpoints` | same | EXACT |
| `Endpoints/Customer/AddressBookCustomerActorResolver.cs` | `Tooba.AddressBook.Endpoints.Customer` | same | EXACT |
| `Endpoints/Customer/AddressBookCustomerReadEndpoints.cs` | `Tooba.AddressBook.Endpoints.Customer` | same | EXACT |
| `Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` | `Tooba.AddressBook.Endpoints.Customer` | same | EXACT |
| `Endpoints/Errors/AddressBookErrorCatalogContributor.cs` | `Tooba.AddressBook.Endpoints.Errors` | same | EXACT |
| `Endpoints/Resources/AddressBookErrorResources.cs` | `Tooba.AddressBook.Endpoints.Resources` | same | EXACT |

## Exemptions applied (repository locks, not workarounds)

| Exemption | Reason |
| --- | --- |
| `Persistence/Migrations/*` | EF-generated; block-scoped namespaces by tooling convention |
| `AddressBookDbContextModelSnapshot.cs` | EF-generated snapshot |

No `GlobalUsings` file exists in this module.

## Alias / shim proof

| Mechanism | Count |
| --- | --- |
| `using X = Y;` namespace alias | 0 |
| `global using` alias | 0 |
| `TypeForwardedTo` | 0 |
| duplicate compatibility type | 0 |
| foreign-module global alias | 0 |

Verified by `AddressBookPhysicalStructureGuardTests.AddressBook_namespaces_equal_path_derived_namespaces_exactly`
(PASS) and independently by `TmarCompleteReferenceStructureGateTests.AssertNamespaceAlignment` for the
AddressBook project loop (no AddressBook entry in the failure set).
