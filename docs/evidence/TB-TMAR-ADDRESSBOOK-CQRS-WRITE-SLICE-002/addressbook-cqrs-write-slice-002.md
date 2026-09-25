# AddressBook CQRS Write Slice 002 (Delete + SetDefault)

Task: `TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002`
Parent: `TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-001` at `3f7562865f4d6b3dc51fec3046223cc3598fd37a`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — one bounded write slice.
Recovery honesty: AddressBook remains **IN_PROGRESS**; **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Exact two commands

| # | Command | Signature | Output | Namespace / path |
| --- | --- | --- | --- | --- |
| 1 | `DeleteCustomerAddressCommand` | `public sealed record DeleteCustomerAddressCommand(Guid ActorUserId, Guid AddressId) : IRequest<Unit>` | `Unit` | `Tooba.AddressBook.Application.Customer.Delete` — `Customer/Delete/DeleteCustomerAddressCommand.cs` |
| 2 | `SetDefaultCustomerAddressCommand` | `public sealed record SetDefaultCustomerAddressCommand(Guid ActorUserId, Guid AddressId) : IRequest<CustomerAddressRecord>` | `CustomerAddressRecord` | `Tooba.AddressBook.Application.Customer.SetDefault` — `Customer/SetDefault/SetDefaultCustomerAddressCommand.cs` |

`Unit` return for the delete use-case follows the certified AccessControl precedent (`SetSellerCeilingCommand`/`SetRolePermissionsCommand`/`RemoveAssignmentCommand`/`ArchiveRoleCommand` all use `IRequest<Unit>` + `IRequestHandler<T, Unit>` with `return Unit.Value;`). No payload equivalent was invented.

Both commands take `ActorUserId` from trusted server context and `AddressId` from route input.

## 2. Exact two handlers

| # | Handler | Primary constructor | Delegation |
| --- | --- | --- | --- |
| 1 | `DeleteCustomerAddressCommandHandler` : `IRequestHandler<DeleteCustomerAddressCommand, Unit>` | `(IAddressBookDirectory addresses)` | `await addresses.DeleteAsync(request.ActorUserId, request.AddressId, cancellationToken); return Unit.Value;` |
| 2 | `SetDefaultCustomerAddressCommandHandler` : `IRequestHandler<SetDefaultCustomerAddressCommand, CustomerAddressRecord>` | `(IAddressBookDirectory addresses)` | `addresses.SetDefaultAsync(request.ActorUserId, request.AddressId, cancellationToken)` (returned directly as `Task`) |

Exactly two handlers added.

## 3. Exact two validators

| File | Namespace | Rule |
| --- | --- | --- |
| `Tooba.AddressBook.Application/Validators/Customer/Delete/DeleteCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.Customer.Delete` | `AddressBookFluentRules.RequireId(this, x => x.AddressId, AddressBookValidationCodes.AddressIdRequired)` |
| `Tooba.AddressBook.Application/Validators/Customer/SetDefault/SetDefaultCustomerAddressCommandValidator.cs` | `Tooba.AddressBook.Application.Validators.Customer.SetDefault` | `AddressBookFluentRules.RequireId(this, x => x.AddressId, AddressBookValidationCodes.AddressIdRequired)` |

Both reuse the existing `AddressBookFluentRules.RequireId` helper and the existing stable code `customer.address.id_required`. No new helper, no new code constant, and no new business error code was introduced. `ActorUserId` is **not** validated in either command.

## 4. AddressId-only validation proof

- Each validator contains exactly one `RuleFor`-equivalent call (`RequireId`), targeting `x => x.AddressId` only.
- No rule references `ActorUserId`, no rule references any other property, and no length/format/regex/business bound was invented.
- Domain/business behavior is untouched: `AddressBookDirectory.DeleteAsync` / `SetDefaultAsync` still own existence, ownership and any thrown `InvalidOperationException`; the handlers neither catch nor translate exceptions and add no HTTP status mapping.

## 5. Handler → `IAddressBookDirectory` delegation proof

- Neither handler references `AddressBookDbContext`, EF Core, `IServiceProvider` or any Infrastructure type.
- Each handler has exactly one dependency: `IAddressBookDirectory`.
- `Delete` forwards `request.ActorUserId`, `request.AddressId` and `cancellationToken` unchanged, then returns `Unit.Value`.
- `SetDefault` forwards `request.ActorUserId`, `request.AddressId` and `cancellationToken` unchanged and returns the directory result verbatim.

## 6. Full CQRS inventory — now 6 use-cases total

| # | Use-case | Request | Kind | Return | Capability folder |
| --- | --- | --- | --- | --- | --- |
| 1 | List customer addresses | `ListCustomerAddressesQuery` | read | `IReadOnlyList<CustomerAddressRecord>` | `Customer/List/` |
| 2 | Get customer address | `GetCustomerAddressQuery` | read | `CustomerAddressRecord?` | `Customer/Get/` |
| 3 | Create customer address | `CreateCustomerAddressCommand` | write | `CustomerAddressRecord` | `Customer/Create/` |
| 4 | Update customer address | `UpdateCustomerAddressCommand` | write | `CustomerAddressRecord` | `Customer/Update/` |
| 5 | Delete customer address | `DeleteCustomerAddressCommand` | write | `Unit` | `Customer/Delete/` |
| 6 | Set default customer address | `SetDefaultCustomerAddressCommand` | write | `CustomerAddressRecord` | `Customer/SetDefault/` |

Counts verified by grep: 6 × `: IRequest<` declarations and 6 × `IRequestHandler<` handlers in `Tooba.AddressBook.Application`. This matches the six currently Host-owned HTTP routes one-to-one. Validators: 5 files (`Create`, `Get`, `Delete`, `SetDefault`, `Update`) — `ListCustomerAddressesQuery` remains `NO_VALIDATOR_REQUIRED` (trusted-only input).

## 7. Application structure and boundary audit

- New capability folders `Customer/Delete/` and `Customer/SetDefault/` added. Existing root `AddressBookContracts.cs` was **not** moved; `AddressBookDirectory.cs`, `CustomerAddress.cs`, `CustomerAddressContracts.cs`, `AddressBookModule.cs` untouched (no root refactor).
- `Tooba.AddressBook.Application.csproj` was not modified in this slice.
- Boundary: new files reference `MediatR` (transitive via `Tooba.BuildingBlocks`), `Tooba.AddressBook.Contracts` (SetDefault only) and the existing `Tooba.AddressBook.Application` model. No `Tooba.Host`, no `Tooba.AddressBook.Infrastructure`, no foreign Application/Domain/Infrastructure.

## 8. Host route ownership and Endpoints route count

- `git diff -- src/backend/Host/Tooba.Host/AddressBook/` is empty — `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs` are byte-identical to the parent commit.
- `app.MapAddressBookEndpoints();` untouched; Host still owns all six routes.
- `AddressBookEndpointModule` route mapping and Program route ownership unmodified.
- `Tooba.AddressBook.Endpoints` still maps **ZERO** routes (`grep Map*` → no matches).
- No route was moved.

## 9. Focused build results

| Build | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Application.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors, 11 pre-existing warnings — none introduced by this task |

## 10. Focused tests

**None added, none run.** No AddressBook test project exists (`src/backend/Modules/AddressBook/Tooba.AddressBook.Tests` absent) — creating one plus wiring would be the broad infrastructure setup the task says to skip. Delegation, `Unit` semantics and AddressId-only rule shape are proven by source inspection (sections 1–5) plus the focused builds.

## 11. Scope statement

- **Production-Code-Scope:** 4 files touched — 2 new command+handler files and 2 new validator files. Plus the two canonical docs artifacts.
- **Test-Code-Scope:** NONE.
- **Checkout-State:** UNCHANGED.
- **Frontend-Production-Changes:** NONE.
- **Residual-Defects:** none introduced. Deliberately deferred and still IN_PROGRESS: endpoint migration/evacuation (Host still owns all six routes), the `Order.Application` guest-actor leak replacement with a neutral seam, AddressBook capability/root restructuring, and certification.

## 12. Exact next recommended slice

**`TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-SLICE-001`** — first small endpoint slice, not a big-bang:

1. Add a Customer endpoint file in `Tooba.AddressBook.Endpoints/Customer/` mapping only the two read routes (`GET /v1/customer/addresses`, `GET /v1/customer/addresses/{addressId:guid}`) through the existing read queries via `ISender`.
2. Introduce the Host-independent actor seam based on `Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser` plus a Host-registered dev/testing actor provider, sourcing the guest value from `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` (never `Order.Application`).
3. Add `AddAddressBookEndpointPresentation` real registration only if needed; keep `MapAddressBookModuleEndpoints` route switch out of this slice, or switch only the migrated routes if the Architect allows — leaving the remaining four routes Host-owned for a following slice.
