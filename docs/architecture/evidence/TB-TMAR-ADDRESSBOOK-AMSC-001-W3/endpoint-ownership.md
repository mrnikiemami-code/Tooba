# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — endpoint-ownership

`Endpoint-Ownership-State = MODULE_OWNED`. `HostHttpOwnership = ZERO`. Route count = **6**.

## Module-owned routes

| # | Method | Route | Endpoint file | Handler | Validator |
| --- | --- | --- | --- | --- | --- |
| 1 | `GET` | `/v1/customer/addresses` | `Customer/AddressBookCustomerReadEndpoints.cs` | `ListCustomerAddressesQuery` | `NO_VALIDATOR_REQUIRED` |
| 2 | `GET` | `/v1/customer/addresses/{addressId:guid}` | `Customer/AddressBookCustomerReadEndpoints.cs` | `GetCustomerAddressQuery` | `GetCustomerAddressQueryValidator` |
| 3 | `POST` | `/v1/customer/addresses` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `CreateCustomerAddressCommand` | `CreateCustomerAddressCommandValidator` |
| 4 | `PUT` | `/v1/customer/addresses/{addressId:guid}` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `UpdateCustomerAddressCommand` | `UpdateCustomerAddressCommandValidator` |
| 5 | `DELETE` | `/v1/customer/addresses/{addressId:guid}` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `DeleteCustomerAddressCommand` | `DeleteCustomerAddressCommandValidator` |
| 6 | `POST` | `/v1/customer/addresses/{addressId:guid}/default` | `Customer/AddressBookCustomerWriteEndpoints.cs` | `SetDefaultCustomerAddressCommand` | `SetDefaultCustomerAddressCommandValidator` |

Route count is asserted by `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_owns_its_http_surface_with_zero_host_http_ownership`
(counting `group.Map{Get,Post,Put,Delete,Patch}(` in the Endpoints project) — **6**.

## Composition entry

`Tooba.AddressBook.Endpoints/AddressBookEndpointModule.cs` (the only allowlisted root file):

- `AddAddressBookEndpointPresentation()` — registers the presentation stack
  (`IErrorCatalogContributor, AddressBookErrorCatalogContributor` and
  `IErrorResourceSet, AddressBookErrorResourceSet`).
- `MapAddressBookModuleEndpoints()` — maps the `/v1/customer/addresses` group once and delegates to
  `MapReads(group)` / `MapWrites(group)`.

No duplicate mapping: the route group is created exactly once and the two endpoint classes each register
their own routes inside it.

## Host HTTP ownership

```text
src/backend/Host/Tooba.Host/AddressBook/   ABSENT
```

Asserted by the W3 cert guard (`Assert.False(Directory.Exists(...Host/Tooba.Host/AddressBook))`) and by
`AddressBookValidatorCoverageGuardTests`. The legacy Host `AddressBook/AddressBookEndpoints.cs` is gone.

## Host references to AddressBook — all legitimate composition

| Host file | Reference | Classification |
| --- | --- | --- |
| `Composition/ToobaModuleComposition.cs` | `new AddressBookModule()` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` | `AddAddressBookEndpointPresentation()`, `MapAddressBookModuleEndpoints()`, CQRS assembly scan on `Tooba.AddressBook.Application.Ports.IAddressBookDirectory`, `IAddressBookCheckoutLookup` → `IAddressBookDirectory` alias | `ALLOWED_COMPOSITION_ROOT` |
| `Development/DevelopmentSchemaMigrator.cs` | development seed invocation | `ALLOWED_COMPOSITION_ROOT` |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | migration descriptor | `ALLOWED_COMPOSITION_ROOT` |

Zero `ILLEGAL_ENDPOINT_OWNERSHIP`, zero Host business or persistence authority.

## Endpoint layer discipline

`AddressBookValidatorCoverageGuardTests.All_six_requests_are_real_mediatr_requests_and_endpoints_use_ISender`
asserts, for both endpoint files:

- `ISender sender` present;
- no `IAddressBookDirectory` reference (no direct directory/persistence call);
- no `IValidator` reference;
- no `ValidateAsync` call.

And `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_endpoints_use_only_the_canonical_result_factory`
asserts the Endpoints project contains no `Tooba.AddressBook.Domain` / `Tooba.AddressBook.Infrastructure`
import (no Domain/Infrastructure reach-through).
