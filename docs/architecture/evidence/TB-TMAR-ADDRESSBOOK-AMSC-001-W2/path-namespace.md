# TB-TMAR-ADDRESSBOOK-AMSC-001-W2 — path-namespace

`PATH_NAMESPACE_ALIGNMENT` is verified by `AddressBookPhysicalStructureGuardTests.CheckExactNamespaces`
(asserts `namespace == <ProjectName>[.<folder>…]` for every production `.cs`, excluding EF
`Migrations/` + `ModelSnapshot.cs`) and independently by
`TmarCompleteReferenceStructureGateTests.AssertNamespaceAlignment` for every certified module.

## Namespace moves performed by W2

| File (new path) | Namespace before | Namespace after |
| --- | --- | --- |
| `Application/Addresses/Commands/CreateCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.CreateCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Commands` |
| `Application/Addresses/Commands/DeleteCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.DeleteCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Commands` |
| `Application/Addresses/Commands/SetDefaultCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.SetDefaultCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Commands` |
| `Application/Addresses/Commands/UpdateCustomerAddressCommand.cs` | `Tooba.AddressBook.Application.Commands.UpdateCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Commands` |
| `Application/Addresses/Queries/GetCustomerAddressQuery.cs` | `Tooba.AddressBook.Application.Queries.GetCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Queries` |
| `Application/Addresses/Queries/ListCustomerAddressesQuery.cs` | `Tooba.AddressBook.Application.Queries.ListCustomerAddresses` | `Tooba.AddressBook.Application.Addresses.Queries` |
| `Application/Addresses/Validators/CreateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.CreateCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Validators` |
| `Application/Addresses/Validators/DeleteCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.DeleteCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Validators` |
| `Application/Addresses/Validators/GetCustomerAddressQueryValidator.cs` | `Tooba.AddressBook.Application.Validators.GetCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Validators` |
| `Application/Addresses/Validators/SetDefaultCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.SetDefaultCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Validators` |
| `Application/Addresses/Validators/UpdateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.UpdateCustomerAddress` | `Tooba.AddressBook.Application.Addresses.Validators` |

No alias/`using` workaround is used to hide the move — every file's namespace equals its path.

## `using` sites updated

| Consumer | Old using | New using |
| --- | --- | --- |
| `Endpoints/Customer/AddressBookCustomerReadEndpoints.cs` | `…Application.Queries.GetCustomerAddress`, `…Application.Queries.ListCustomerAddresses` | `Tooba.AddressBook.Application.Addresses.Queries` |
| `Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` | `…Application.Commands.{Create,Delete,SetDefault,Update}CustomerAddress` | `Tooba.AddressBook.Application.Addresses.Commands` |
| `Addresses/Validators/*Validator.cs` (5 files) | `…Application.Commands.<UseCase>` / `…Application.Queries.<UseCase>` | `…Application.Addresses.Commands` / `…Application.Addresses.Queries` |
| `Host/Tooba.Host.Tests/Architecture/AddressBookValidatorCoverageGuardTests.cs` | 11 per-use-case usings | 3 usings (`Addresses.Commands`, `Addresses.Queries`, `Addresses.Validators`) |

Namespaces that did **not** move and therefore kept their consumers untouched:

- `Tooba.AddressBook.Application.Ports` (referenced by `Host/Tooba.Host/Program.cs` line 188 for the CQRS
  assembly scan, and line 212 for the `IAddressBookCheckoutLookup` → `IAddressBookDirectory` alias)
- `Tooba.AddressBook.Application.Models` (referenced by `AddressBookDirectory`, write endpoints,
  `AddressBookFoundationTests`)
- `Tooba.AddressBook.Application.Validators` (shared `AddressBookFluentRules`)
- `Tooba.AddressBook.Application.Composition`

## Namespace-collision hazard check

Before flattening, 11 files were merged into 3 namespaces. Verified that no **type name** collides within
the new namespaces (each file contributes its own unique request + handler + validator type names) and that
no file declared two namespaces. The `AddressBook_has_no_stale_or_duplicate_physical_type_copies` guard
(file-name uniqueness across the module) passes.

## Result

`Path-Namespace-State = EXACT` (unchanged from before W2 — exactness held before and after; the defect was
shape, not alignment). Confirmed by:

```text
AddressBookPhysicalStructureGuardTests.AddressBook_namespaces_equal_path_derived_namespaces_exactly  PASS
TmarCompleteReferenceStructureGateTests (AddressBook project loop)                                   PASS (no AddressBook entry in the failure set)
```
