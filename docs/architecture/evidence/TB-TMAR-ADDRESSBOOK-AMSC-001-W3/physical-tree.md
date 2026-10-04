# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — physical-tree

Production `.cs` files: **32** across **5** projects (excluding `bin/`, `obj/`, EF `Migrations/` and
`*ModelSnapshot.cs`).

```text
src/backend/Modules/AddressBook/
  Tooba.AddressBook.Contracts/                                    4 files
    Dtos/CustomerAddressRecord.cs
    Errors/AddressBookErrorCodes.cs
    Ports/IAddressBookCheckoutLookup.cs
    Ports/IAddressBookCountPort.cs
  Tooba.AddressBook.Domain/                                       1 file
    Aggregates/CustomerAddress.cs
  Tooba.AddressBook.Application/                                 15 files
    Addresses/Commands/CreateCustomerAddressCommand.cs
    Addresses/Commands/DeleteCustomerAddressCommand.cs
    Addresses/Commands/SetDefaultCustomerAddressCommand.cs
    Addresses/Commands/UpdateCustomerAddressCommand.cs
    Addresses/Queries/GetCustomerAddressQuery.cs
    Addresses/Queries/ListCustomerAddressesQuery.cs
    Addresses/Validators/CreateCustomerAddressCommandValidator.cs
    Addresses/Validators/DeleteCustomerAddressCommandValidator.cs
    Addresses/Validators/GetCustomerAddressQueryValidator.cs
    Addresses/Validators/SetDefaultCustomerAddressCommandValidator.cs
    Addresses/Validators/UpdateCustomerAddressCommandValidator.cs
    Composition/AddressBookOperation.cs
    Models/CustomerAddressWrite.cs
    Ports/IAddressBookDirectory.cs
    Validators/AddressBookFluentRules.cs
  Tooba.AddressBook.Infrastructure/                               6 files
    Adapters/AddressBookDevelopmentSeed.cs
    Adapters/AddressBookDirectory.cs
    DependencyInjection/AddressBookModule.cs
    Outbox/AddressBookOutboxRegistration.cs
    Persistence/AddressBookDbContext.cs
    Persistence/Configurations/CustomerAddressConfiguration.cs
    (+ Persistence/Migrations/* EF-generated, excluded)
  Tooba.AddressBook.Endpoints/                                    6 files
    AddressBookEndpointModule.cs                                  (root allowlist)
    Customer/AddressBookCustomerActorResolver.cs
    Customer/AddressBookCustomerReadEndpoints.cs
    Customer/AddressBookCustomerWriteEndpoints.cs
    Errors/AddressBookErrorCatalogContributor.cs
    Resources/AddressBookErrorResources.cs
```

## Capability-first verification

- `Application/Addresses/` is the capability (the module's single real business capability).
- `Commands` / `Queries` / `Validators` are **secondary** axes under the capability.
- `Application/Commands/` and `Application/Queries/` do **not** exist at the Application root.
- `Application/Validators/` exists **only** as the cross-cutting rules folder holding one structural
  file (`AddressBookFluentRules.cs`), not as a request tree.
- Depth is `project → capability → technical axis → file` at its deepest.
- Zero single-file use-case leaf folders.
- Zero empty/ceremonial folders.

Enforced by `AddressBookPhysicalStructureGuardTests`:
`AddressBook_application_is_capability_first_with_no_technical_axis_root` and
`AddressBook_has_no_single_file_use_case_leaf_folders` (both PASS).

## Root files

| Project | Root `*.cs` |
| --- | --- |
| Contracts | *(none)* |
| Domain | *(none)* |
| Application | *(none)* |
| Endpoints | `AddressBookEndpointModule.cs` (allowlisted composition entry) |
| Infrastructure | *(none)* |

`Root-Dump-State = ZERO`.

## Structure gate consumption

`Folder-Granularity-State = PROFESSIONAL_SHALLOW`, `Solution-Explorer-State = CANONICAL`,
`Path-Namespace-State = EXACT`, `Physical-Copy-State = CLEAN`, `Root-Allowlist-State = ENFORCED` —
all read from the W2 Structure gate for this same surface and re-verified against current disk here.
