# AddressBook CQRS Read Slice (List + Get)

Task: `TB-TMAR-ADDRESSBOOK-CQRS-READ-SLICE-001`
Parent: `TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001` at `089b6a8b386ca3eb01adddf2421bee74bab4cd26`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — one bounded read slice.
Recovery honesty: AddressBook remains **IN_PROGRESS**; **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Exact two requests

| # | Request | Signature | Output | Namespace / path |
| --- | --- | --- | --- | --- |
| 1 | `ListCustomerAddressesQuery` | `public sealed record ListCustomerAddressesQuery(Guid ActorUserId) : IRequest<IReadOnlyList<CustomerAddressRecord>>` | `IReadOnlyList<CustomerAddressRecord>` | `Tooba.AddressBook.Application.Customer.List` — `Customer/List/ListCustomerAddressesQuery.cs` |
| 2 | `GetCustomerAddressQuery` | `public sealed record GetCustomerAddressQuery(Guid ActorUserId, Guid AddressId) : IRequest<CustomerAddressRecord?>` | `CustomerAddressRecord?` | `Tooba.AddressBook.Application.Customer.Get` — `Customer/Get/GetCustomerAddressQuery.cs` |

Exactly two read requests exist. No create/update/delete/set-default command was added.

## 2. Exact two handlers

| # | Handler | Primary constructor | Delegation |
| --- | --- | --- | --- |
| 1 | `ListCustomerAddressesQueryHandler` : `IRequestHandler<ListCustomerAddressesQuery, IReadOnlyList<CustomerAddressRecord>>` | `(IAddressBookDirectory addresses)` | `addresses.ListAsync(request.ActorUserId, cancellationToken)` (returned directly as `Task`) |
| 2 | `GetCustomerAddressQueryHandler` : `IRequestHandler<GetCustomerAddressQuery, CustomerAddressRecord?>` | `(IAddressBookDirectory addresses)` | `addresses.GetAsync(request.ActorUserId, request.AddressId, cancellationToken)` (returned directly as `Task`) |

Exactly two handlers exist. Both are MediatR 12.5 `IRequestHandler<TRequest, TResponse>` implementations consistent with already-certified modules (e.g. `Settlement GetSellerSettlementBalanceQueryHandler`).

## 3. Handler → `IAddressBookDirectory` delegation proof

- Neither handler references `AddressBookDbContext`, EF Core, `IServiceProvider`, or any Infrastructure type. `grep` for `DbContext` across the two new `Customer/**` files returns nothing.
- Both handlers carry exactly one dependency — `IAddressBookDirectory` — matching the task's "handlers must depend only on `IAddressBookDirectory`" rule.
- `ListCustomerAddressesQueryHandler` forwards `request.ActorUserId` and `cancellationToken` unchanged.
- `GetCustomerAddressQueryHandler` forwards `request.ActorUserId`, `request.AddressId` and `cancellationToken` unchanged, and returns the directory result verbatim, so the existing **null-on-missing / null-on-foreign** behavior is preserved exactly. No HTTP status mapping, no new error envelope, no behavior normalization was introduced.

## 4. Folder / namespace paths created

| Path | Symbol |
| --- | --- |
| `Tooba.AddressBook.Application/Customer/List/ListCustomerAddressesQuery.cs` | query + handler, namespace `Tooba.AddressBook.Application.Customer.List` |
| `Tooba.AddressBook.Application/Customer/Get/GetCustomerAddressQuery.cs` | query + handler, namespace `Tooba.AddressBook.Application.Customer.Get` |
| `Tooba.AddressBook.Application/Validators/Customer/Get/GetCustomerAddressQueryValidator.cs` | namespace `Tooba.AddressBook.Application.Validators.Customer.Get` |

## 5. Validator classification

| Request | Classification | Rationale |
| --- | --- | --- |
| `ListCustomerAddressesQuery` | **`NO_VALIDATOR_REQUIRED`** | Its only input is `ActorUserId`, which is trusted server-side actor context and not a request payload. No validator file was created for it. |
| `GetCustomerAddressQuery` | **`VALIDATOR_REQUIRED`** | `AddressId` arrives from route input. The platform transport-shape pattern in already-certified modules validates route/payload identifiers: `Payment.Application/Validators/Storefront/GetStorefrontPaymentQueryValidator.cs` and `.../Admin/GetAdminPaymentQueryValidator.cs` both apply `PaymentFluentRules.RequireId`, i.e. `RuleFor(selector).NotEmpty().WithErrorCode(...)`. AddressBook follows the same pattern with a self-contained rule (no cross-module helper reference). |

Validator behavior: `RuleFor(x => x.AddressId).NotEmpty().WithErrorCode("customer.address.id_required")`. `ActorUserId` is deliberately **not** validated as untrusted payload.

Precedent inspected (explicit, not guessed):

- `src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Storefront/GetStorefrontPaymentQueryValidator.cs`
- `src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Admin/GetAdminPaymentQueryValidator.cs`
- `src/backend/Modules/Payment/Tooba.Payment.Application/Validators/PaymentFluentRules.cs` (`RequireId` → `.NotEmpty()`)

## 6. Application structure and boundary audit

- New capability folders `Customer/List/` and `Customer/Get/` were created. The existing root contract file `AddressBookContracts.cs` was **not** moved (no root refactor in this task), and `AddressBookDirectory.cs`, `CustomerAddress.cs`, `CustomerAddressContracts.cs`, `AddressBookModule.cs` were untouched.
- The new query files use `Tooba.AddressBook.Contracts` only (for `CustomerAddressRecord`). `AddressBook.Domain` was not needed by the new files, though the Application project reference to Domain remains as pre-existing project shape.
- Application boundary: no `Tooba.Host` reference, no `Tooba.AddressBook.Infrastructure` reference, no foreign Application/Domain/Infrastructure reference.
- One csproj change was required for compilation: `Tooba.AddressBook.Application.csproj` gained the `Tooba.BuildingBlocks` project reference (relative path `..\..\..\BuildingBlocks\Tooba.BuildingBlocks\Tooba.BuildingBlocks.csproj`), because `MediatR` (12.5.0) and `FluentValidation` (11.11.0) are transitively provided only by `Tooba.BuildingBlocks` and the four new files use `MediatR`, `IRequest`, `IRequestHandler` and `AbstractValidator`. This is the established platform pattern: every already-certified module Application project (e.g. `Tooba.Settlement.Application`) references `Tooba.BuildingBlocks` transitively for MediatR/FluentValidation; no `PackageReference` was added, so there is still exactly one MediatR/validator registration pipeline.

## 7. Host route ownership unchanged

- `git diff -- src/backend/Host/Tooba.Host/AddressBook/` is empty — `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs` are byte-identical to the parent commit.
- `app.MapAddressBookEndpoints();` is untouched and Host still owns all six routes: `GET /v1/customer/addresses`, `GET /v1/customer/addresses/{addressId:guid}`, `POST /v1/customer/addresses`, `PUT /v1/customer/addresses/{addressId:guid}`, `DELETE /v1/customer/addresses/{addressId:guid}`, `POST /v1/customer/addresses/{addressId:guid}/default`.
- `AddressBookEndpointModule` route mapping was not modified and Program route ownership is unchanged.
- No HTTP route was moved or migrated in this task.

## 8. New Endpoints project route count

**Still ZERO.** `grep` for `MapGet|MapPost|MapPut|MapDelete|MapGroup|MapMethods` across `Tooba.AddressBook.Endpoints` returns no matches. No Customer endpoint file was added.

## 9. Focused build results

| Build | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Application.csproj` (restore needed for the new project reference; Build succeeded) | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors, 11 pre-existing warnings (Catalog/Promotion XML comments, `Program.cs` duplicate `using`, `ProductWorkspaceComposer` nullable) — none introduced by this task |

## 10. Focused tests

**None added, and none run.** Reason (documented per task instruction): there is no AddressBook test project at all (`src/backend/Modules/AddressBook/Tooba.AddressBook.Tests` does not exist), and adding one would require creating a new test project plus solution/test-infrastructure wiring — exactly the "broad infrastructure setup" the task says to skip. The only existing AddressBook tests live in `Tooba.Host.Tests/AddressBookFoundationTests.cs` and cover Host endpoint/seed behavior, not the new Application handlers. The direct delegation semantics are proven by source inspection (section 3) and by the focused builds.

## 11. Scope statement

- **Production-Code-Scope:** 5 files touched — 3 new Application files (`Customer/List/ListCustomerAddressesQuery.cs`, `Customer/Get/GetCustomerAddressQuery.cs`, `Validators/Customer/Get/GetCustomerAddressQueryValidator.cs`) plus one added `ProjectReference` line in `Tooba.AddressBook.Application.csproj`. Plus the two canonical docs artifacts.
- **Test-Code-Scope:** NONE.
- **Checkout-State:** UNCHANGED — checkout still consumes `IAddressBookCheckoutLookup`; the `Program.cs:188` alias is untouched.
- **Frontend-Production-Changes:** NONE.
- **Residual-Defects:** none introduced. Deliberately deferred (unchanged and still IN_PROGRESS): write CQRS (create/update/delete/set-default), route migration/endpoint evacuation, actor seam replacement of the `Order.Application` guest-actor leak, AddressBook capability/root restructuring, and certification.

## 12. Exact next recommended slice

**`TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001`** — the smallest bounded write subset:

1. Add `CreateCustomerAddressCommand`, `UpdateCustomerAddressCommand`, `DeleteCustomerAddressCommand`, `SetDefaultCustomerAddressCommand` + handlers in `Tooba.AddressBook.Application/Customer/{Create,Update,Delete,SetDefault}/`, delegating only to `IAddressBookDirectory` and preserving current behavior (201/200/204 outcomes, default-flag semantics, ownership enforcement in `AddressBookDirectory`).
2. Classify + add transport-shape validators for the write requests (mirroring the Payment/Settlement write-validator pattern) without moving business validation out of the directory/domain.
3. Still no route migration, no Host deletion, no Endpoints route mapping, no root refactor.
