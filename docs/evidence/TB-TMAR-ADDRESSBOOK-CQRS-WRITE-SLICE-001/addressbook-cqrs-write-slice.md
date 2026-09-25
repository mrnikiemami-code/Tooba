# AddressBook CQRS Write Slice (Create + Update)

Task: `TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001`
Parent: `TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001` at `3679505e35598dd1b76e3ade3a540caed6a1ab4d`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — one bounded write slice.
Recovery honesty: AddressBook remains **IN_PROGRESS**; **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Exact two commands

| # | Command | Signature | Output | Namespace / path |
| --- | --- | --- | --- | --- |
| 1 | `CreateCustomerAddressCommand` | `public sealed record CreateCustomerAddressCommand(Guid ActorUserId, CustomerAddressWrite Input) : IRequest<CustomerAddressRecord>` | `CustomerAddressRecord` | `Tooba.AddressBook.Application.Customer.Create` — `Customer/Create/CreateCustomerAddressCommand.cs` |
| 2 | `UpdateCustomerAddressCommand` | `public sealed record UpdateCustomerAddressCommand(Guid ActorUserId, Guid AddressId, CustomerAddressWrite Input) : IRequest<CustomerAddressRecord>` | `CustomerAddressRecord` | `Tooba.AddressBook.Application.Customer.Update` — `Customer/Update/UpdateCustomerAddressCommand.cs` |

Exactly two write requests exist. No `DeleteCustomerAddressCommand` and no `SetDefaultCustomerAddressCommand` was added (verified by grep — zero matches in the whole AddressBook module).

`CustomerAddressWrite` (the existing `Tooba.AddressBook.Application` model) is reused as the payload instead of inventing a duplicate business DTO. The user-controlled fields therefore remain exactly the current Host mapping surface: `RecipientName`, `ContactMobile`, `Country`, `ProvinceName`, `CityName`, `PostalCode`, `PostalAddress`, `BuildingUnit`, `Label`, `IsDefault`, `FirstName`, `LastName`. `ActorUserId` stays trusted server context (never part of the payload record).

## 2. Exact two handlers

| # | Handler | Primary constructor | Delegation |
| --- | --- | --- | --- |
| 1 | `CreateCustomerAddressCommandHandler` : `IRequestHandler<CreateCustomerAddressCommand, CustomerAddressRecord>` | `(IAddressBookDirectory addresses)` | `addresses.CreateAsync(request.ActorUserId, request.Input, cancellationToken)` (returned directly as `Task`) |
| 2 | `UpdateCustomerAddressCommandHandler` : `IRequestHandler<UpdateCustomerAddressCommand, CustomerAddressRecord>` | `(IAddressBookDirectory addresses)` | `addresses.UpdateAsync(request.ActorUserId, request.AddressId, request.Input, cancellationToken)` (returned directly as `Task`) |

Exactly two handlers exist. Both are MediatR 12.5 `IRequestHandler<TRequest, TResponse>` consistent with certified modules.

## 3. Handler → `IAddressBookDirectory` delegation proof

- Neither handler references `AddressBookDbContext`, EF Core, `IServiceProvider` or any Infrastructure type.
- Each handler has exactly one dependency: `IAddressBookDirectory`.
- `Create` forwards `request.ActorUserId`, `request.Input` and `cancellationToken` unchanged.
- `Update` forwards `request.ActorUserId`, `request.AddressId`, `request.Input` and `cancellationToken` unchanged.
- No HTTP status mapping, no error-envelope change, and no duplication of domain/business rules (ownership, length/format, default-flag semantics remain in `CustomerAddress`/`AddressBookDirectory`).

## 4. Exact validator files and rule classification

| File | Namespace | Rules |
| --- | --- | --- |
| `Tooba.AddressBook.Application/Validators/Customer/Create/CreateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.Customer.Create` | `Input.RecipientName`, `Input.ContactMobile`, `Input.CityName`, `Input.PostalCode`, `Input.PostalAddress` → non-blank transport shape |
| `Tooba.AddressBook.Application/Validators/Customer/Update/UpdateCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.Customer.Update` | `AddressId` → non-empty route id **plus** the same five non-blank input-shape rules |
| `Tooba.AddressBook.Application/Validators/AddressBookFluentRules.cs` | `Tooba.AddressBook.Application.Validators` | shared `RequireNonBlank` + `RequireId` helpers and `AddressBookValidationCodes` |

Classification per rule:

- **Transport-shape only.** Non-blank checks assert presence of untrusted input; no length, no format, no regex, no business bounds were invented.
- `ActorUserId` is **never** validated as untrusted payload in either command.
- `AddressId` in the Update command is treated purely as route input (`RequireId` → `.NotEmpty()`), not an ownership/business check.
- Domain/business rules remain untouched in `CustomerAddress.Create`/mutators (`RequireBounded`/`OptionalBounded`: recipient 1..max, mobile min/max, country 2..max + `IR` default, city, postal length, postal address, optional building-unit/label bounds) and in `AddressBookDirectory`.

Helper consolidation: the previously inline `candidate.address`-style rule in the read-slice `GetCustomerAddressQueryValidator` was refactored to use the same shared helper (single small helper, no over-abstraction, no behavior change — identical rule and error code). Any other candidate AddressBook rule can now reuse it.

## 5. Application structure

- New capability folders `Customer/Create/` and `Customer/Get/` and `Customer/Update/` (this slice added `Create` and `Update`).
- Existing root `AddressBookContracts.cs` was **not** moved, and `AddressBookDirectory.cs`, `CustomerAddress.cs`, `CustomerAddressContracts.cs`, `AddressBookModule.cs` were untouched (no root refactor).
- `Tooba.AddressBook.Application.csproj` was not modified in this slice (its `Tooba.BuildingBlocks` reference already exists from the foundation/read slices).
- Application boundary: no `Tooba.Host`, no `Tooba.AddressBook.Infrastructure`, no foreign Application/Domain/Infrastructure reference in any new file.

## 6. Host route ownership and Endpoints route count

- `git diff -- src/backend/Host/Tooba.Host/AddressBook/` is empty — `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs` are byte-identical to the parent commit.
- `app.MapAddressBookEndpoints();` untouched; Host still owns all six routes: `GET /v1/customer/addresses`, `GET /v1/customer/addresses/{addressId:guid}`, `POST /v1/customer/addresses`, `PUT /v1/customer/addresses/{addressId:guid}`, `DELETE /v1/customer/addresses/{addressId:guid}`, `POST /v1/customer/addresses/{addressId:guid}/default`.
- `AddressBookEndpointModule` route mapping unmodified; Program route ownership unchanged.
- `Tooba.AddressBook.Endpoints` still maps **ZERO** routes (`grep Map*` → no matches).
- No HTTP route was moved in this task.

## 7. Focused build results

| Build | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Application.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors, 11 pre-existing warnings — none introduced by this task |

## 8. Focused tests

**None added, none run.** No AddressBook test project exists (`src/backend/Modules/AddressBook/Tooba.AddressBook.Tests` absent); creating one plus wiring would be the broad infrastructure setup the task says to skip. Delegation and rule shape are proven by source inspection (sections 3–4) and the focused builds.

## 9. Scope statement

- **Production-Code-Scope:** 5 files touched — 2 new commands+handlers, 2 new validators, 1 new shared rules helper, plus a small in-place refactor of `GetCustomerAddressQueryValidator` to use the shared helper. Plus the two canonical docs artifacts.
- **Test-Code-Scope:** NONE.
- **Checkout-State:** UNCHANGED.
- **Frontend-Production-Changes:** NONE.
- **Residual-Defects:** none introduced. Deliberately deferred and still IN_PROGRESS: Delete + SetDefault CQRS, endpoint migration/evacuation, the `Order.Application` guest-actor leak replacement, AddressBook capability/root restructuring, and certification.

## 10. Exact next recommended slice

**`TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002`** — Delete + SetDefault only:

1. Add `DeleteCustomerAddressCommand` + handler and `SetDefaultCustomerAddressCommand` + handler under `Customer/Delete/` and `Customer/SetDefault/`, delegating only to `IAddressBookDirectory.DeleteAsync` / `SetDefaultAsync` and preserving current behavior.
2. Add transport-shape validators (route `AddressId` non-empty only, `ActorUserId` not validated) reusing `AddressBookFluentRules`.
3. Still no route migration, no Host deletion, no Endpoints route mapping, no root refactor.
