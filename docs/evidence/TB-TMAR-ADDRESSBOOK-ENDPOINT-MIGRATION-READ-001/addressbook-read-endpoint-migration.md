# AddressBook Read Endpoint Migration (List + Get)

Task: `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001`
Parent: `TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1` at `5a083e2a516f2c6b10f79872107a00fbb6059c72`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — one bounded read-endpoint slice.
Recovery honesty: AddressBook remains **IN_PROGRESS**. Host residue remains **NON-ZERO** (four write routes + seed). **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Exact two routes migrated to the module

| # | Route | Handler | Dispatch |
| --- | --- | --- | --- |
| 1 | `GET /v1/customer/addresses` | `AddressBookCustomerReadEndpoints.ListAsync` | `ISender.Send(new ListCustomerAddressesQuery(actor))` |
| 2 | `GET /v1/customer/addresses/{addressId:guid}` | `AddressBookCustomerReadEndpoints.GetAsync` | `ISender.Send(new GetCustomerAddressQuery(actor, addressId))` |

Files added:

| File | Namespace |
| --- | --- |
| `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerReadEndpoints.cs` | `Tooba.AddressBook.Endpoints.Customer` |
| `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerActorResolver.cs` | `Tooba.AddressBook.Endpoints.Customer` |

`AddressBookEndpointModule.MapAddressBookModuleEndpoints` now maps exactly the group `app.MapGroup("/v1/customer/addresses")` + `AddressBookCustomerReadEndpoints.MapReads(group)`; the write routes are not mapped there.

## 2. Exact four routes still Host-owned

`src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` now maps only:

| # | Route | Handler |
| --- | --- | --- |
| 1 | `POST /v1/customer/addresses` | `CreateAsync` |
| 2 | `PUT /v1/customer/addresses/{addressId:guid}` | `UpdateAsync` |
| 3 | `DELETE /v1/customer/addresses/{addressId:guid}` | `DeleteAsync` |
| 4 | `POST /v1/customer/addresses/{addressId:guid}/default` | `SetDefaultAsync` |

`ListAsync` and `GetAsync` members were deleted from the Host file. The Host class/file is **kept** because those four write routes still live there. `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `SetDefaultAsync`, `CustomerAddressWriteRequest`, `CustomerAddressWriteRequestExtensions` and `ToWrite` were **not** touched. `AddressBookDevelopmentSeed.cs` and the `ProductWorkspaceDevelopmentBootstrap` seed calls were not touched.

## 3. Actor resolution seam

`AddressBookCustomerActorResolver.ResolveActor(HttpContext, ICurrentAuthenticatedUser, IHostEnvironment)` preserves the exact current precedence, using the established neutral platform seam instead of the Host session type:

1. `currentUser.IsAuthenticated && currentUser.UserId` → authenticated session user.
2. Not `Development` and not `Testing` → `null` → endpoint returns `401 { title = "Unauthorized", errorCode = "customer.session.required" }`.
3. `X-Tooba-Dev-Actor-User-Id` header when it parses to a non-`Guid.Empty` guid.
4. `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`.

This mirrors the already-certified `FulfillmentCustomerAuthorizer.ResolveCustomerActor` precedent (same seam, same dev header, same guest fallback constant). `AddressBook.Endpoints` has **no** `Tooba.Host` reference: it depends only on `Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser` (already registered in Host at `Program.cs` as `HostCurrentAuthenticatedUser`) plus `IHostEnvironment`. No Host adapter was needed and no general auth refactor was performed.

## 4. Guest actor authority

The module uses `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` (`aaaaaaaa-aaaa-4aaa-8aaa-000000000009`), the stable contract constant — **not** `Order.Application`'s `StorefrontCheckoutService.StorefrontGuestActorId`. The Host file's own fallback was also switched from the `Order.Application` projection to the same `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` constant, removing that pre-existing leak from the remaining residual Host code without changing its value.

## 5. Dependency boundary proof

- `Tooba.AddressBook.Endpoints` project references now: `Tooba.AddressBook.Application`, `Tooba.BuildingBlocks`, `Tooba.Order.Contracts` (the last one added as a Contract-only reference, explicitly allowed by the task).
- `grep` for `Order.Application` / `Tooba.Host` across `Tooba.AddressBook.Endpoints` `*.cs`/`*.csproj` returns **only documentation comment mentions**, no code or project reference.
- `Tooba.AddressBook.Endpoints → Tooba.Host = ZERO`; `→ Tooba.AddressBook.Infrastructure = ZERO`; `→ foreign Application/Domain/Infrastructure = ZERO`.
- One Host-side csproj change was required for compilation: `Tooba.Host.csproj` gained a reference to `Tooba.AddressBook.Endpoints` and `Program.cs` gained `using Tooba.AddressBook.Endpoints;` so the new map extension is callable.

## 6. ISender dispatch proof

`AddressBookCustomerReadEndpoints` injects `Microsoft.AspNetCore.Http.HttpContext`, `ICurrentAuthenticatedUser`, `IHostEnvironment`, `MediatR.ISender` and `CancellationToken` — and **no** `IAddressBookDirectory`. Both handlers dispatch through `sender.Send(...)`. A `grep` for `IAddressBookDirectory` under `AddressBook.Endpoints/Customer` finds no match.

## 7. Duplicate route ownership

**ZERO.** Host maps only 4 route calls (`MapPost`, `MapPut`, `MapDelete`, `MapPost .../default`); the module maps only 2 (`MapGet("")`, `MapGet("/{addressId:guid}")`). `MapAddressBookModuleEndpoints` is called exactly once in `Program.cs` (line 508, directly after the retained `app.MapAddressBookEndpoints();` at line 507). No `GET` route remains Host-owned and no write route is module-owned.

## 8. Response parity for List and Get

| Route | Behavior | Before (Host) | After (module) |
| --- | --- | --- | --- |
| `GET /v1/customer/addresses` | unauthorized | `401 { title="Unauthorized", errorCode="customer.session.required" }` | identical |
| | success | `Results.Json(addresses.ListAsync(actor))` | `Results.Json(sender.Send(ListCustomerAddressesQuery(actor)))` — same `IReadOnlyList<CustomerAddressRecord>` payload |
| `GET /v1/customer/addresses/{addressId:guid}` | unauthorized | same 401 body | identical |
| | success | `Results.Json(item)` | `Results.Json(item)` — same `CustomerAddressRecord` |
| | missing/foreign | `404 { title="Not Found", errorCode="customer.address.missing" }` | identical |

Actor semantics, route templates, status codes and JSON shapes are unchanged.

## 9. Focused build and test results

| Command | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Application.csproj` | **Build succeeded** |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host.csproj` | **Build succeeded**, 0 errors, 3 pre-existing warnings |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests` | **Passed! Failed: 0, Passed: 8, Skipped: 0, Total: 8** |

The 8 focused tests still pass because the Host source-text guard only asserts the surviving Actor/401/dev-environment/route-group markers plus the absence of owner-authority tokens — all still true for the four write routes, and both Host `AddressBook` files remain present.

## 10. Production code scope

**7 files touched** (5 code, 2 docs):

1. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerReadEndpoints.cs` — new.
2. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerActorResolver.cs` — new.
3. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/AddressBookEndpointModule.cs` — maps the read slice.
4. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj` — added `Tooba.Order.Contracts` reference.
5. `src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` — removed the two read routes/handlers; guest fallback switched to the Order.Contracts constant; unused `using Tooba.Host.Storefront;` removed.
6. `src/backend/Host/Tooba.Host/Program.cs` — added `using Tooba.AddressBook.Endpoints;` and `app.MapAddressBookModuleEndpoints();`.
7. `src/backend/Host/Tooba.Host/Tooba.Host.csproj` — added the `Tooba.AddressBook.Endpoints` project reference.

No test code changed. Checkout state unchanged. No frontend changes.

## 11. Residual defects

None introduced. Deliberately deferred and still IN_PROGRESS: the four write routes (`Create`/`Update`/`Delete`/`SetDefault`) still Host-owned, `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs` still present, `Host → AddressBook.Infrastructure` and `Host → AddressBook.Domain` composition edges still present, AddressBook capability/root restructuring still pending, and certification still pending.

## 12. Exact next recommended slice

**`TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001`** — migrate **Create + Update only**:

1. Add a Customer write endpoint file in `Tooba.AddressBook.Endpoints/Customer/` mapping `POST /v1/customer/addresses` and `PUT /v1/customer/addresses/{addressId:guid}` through `ISender` to `CreateCustomerAddressCommand`/`UpdateCustomerAddressCommand`, returning `201`/`200` with the same `CustomerAddressRecord` payload and the same 401 body.
2. Move `CustomerAddressWriteRequest` + `ToWrite` mapping into the module Endpoints project (the Host file no longer owns the read DTO concerns) and switch the Host write handlers to `ISender` too, or remove just those two routes from Host.
3. Still no Delete/SetDefault migration, no seed work, no root refactor.
