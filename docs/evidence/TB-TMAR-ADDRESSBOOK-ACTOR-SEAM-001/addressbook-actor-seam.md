# AddressBook Neutral Actor-Authority Seam

Task: `TB-TMAR-ADDRESSBOOK-ACTOR-SEAM-001`
Parent: `TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1` at `5a083e2a516f2c6b10f79872107a00fbb6059c72`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — actor seam only, no route migration.
Recovery honesty: AddressBook remains **IN_PROGRESS**; Host still owns all six routes; **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Exact resolver type and path

| Item | Value |
| --- | --- |
| Interface | `IAddressBookCustomerActorResolver` — `Guid? ResolveActor(HttpContext httpContext)` |
| Implementation | `AddressBookCustomerActorResolver` — `sealed`, primary constructor `(ICurrentAuthenticatedUser currentUser, IHostEnvironment environment)` |
| Path | `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerActorResolver.cs` |
| Namespace | `Tooba.AddressBook.Endpoints.Customer` |

The seam is module-owned and lives in the endpoint layer, matching the certified `FulfillmentCustomerAuthorizer` precedent (module-local interface + implementation registered by the module's own presentation method). It is a small interface + implementation rather than a bare service so a later route migration and any focused test can substitute it.

## 2. Exact precedence preserved

| Step | Condition | Result |
| --- | --- | --- |
| 1 | `currentUser.IsAuthenticated && currentUser.UserId is { } authenticated` | returns `authenticated` (session authority) |
| 2 | neither `environment.IsDevelopment()` nor `environment.IsEnvironment("Testing")` | returns `null` → caller 401 |
| 3 | `X-Tooba-Dev-Actor-User-Id` parses to a non-`Guid.Empty` guid | returns that guid |
| 4 | Development/Testing otherwise | returns `StorefrontGuestActor.ActorId` |

This is a line-for-line semantic match of the previous Host `ResolveActor` (session → production refusal → dev header → guest fallback). Production still never trusts headers or fallbacks, and the request body still never creates identity.

## 3. Exact dev header

`public const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";` — the header name is unchanged and no new header was invented. The constant now lives on the module-side resolver, mirroring `FulfillmentCustomerAuthorizer`'s private `DevActorHeader`.

## 4. `ICurrentAuthenticatedUser` usage

The resolver consumes `Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser` (`IsAuthenticated`, `Guid? UserId`) — the existing neutral platform security seam. It does **not** consume `CurrentAuthenticatedSession` or any `Tooba.Host*` type, so the endpoint layer has no Host dependency. `ICurrentAuthenticatedUser` was already registered in Host (`Program.cs` → `HostCurrentAuthenticatedUser`); **no duplicate or second auth/session registration was added**.

## 5. Guest actor authority

Guest fallback uses `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` (`aaaaaaaa-aaaa-4aaa-8aaa-000000000009`) — the canonical contract constant — and **not** `Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId`. The `Order.Application` leak is therefore absent from the module seam.

## 6. Project references and boundary audit

`Tooba.AddressBook.Endpoints.csproj` references remain exactly:

| Reference | Status |
| --- | --- |
| `Tooba.AddressBook.Application` | allowed |
| `Tooba.BuildingBlocks` | allowed |
| `Tooba.Order.Contracts` | allowed (Contracts-only foreign edge; already added by the previous read-migration slice) |

`grep` for `Order.Application` and `Tooba.Host` across `Tooba.AddressBook.Endpoints` `*.cs` returns **zero code hits** (only explanatory documentation comments). Boundary state:

| Edge | Result |
| --- | --- |
| `AddressBook.Endpoints → Tooba.Host` | ZERO |
| `AddressBook.Endpoints → Order.Application` | ZERO |
| `AddressBook.Endpoints → AddressBook.Infrastructure` | ZERO |
| `AddressBook.Endpoints → foreign Application/Domain/Infrastructure` | ZERO |

## 7. DI registration

`AddAddressBookEndpointPresentation()` now registers the seam:

```csharp
services.AddScoped<IAddressBookCustomerActorResolver, AddressBookCustomerActorResolver>();
```

The method is invoked once in Host composition: `Program.cs` → `builder.Services.AddAddressBookEndpointPresentation();` (added directly after `AddFulfillmentEndpointPresentation()`, following the established per-module presentation registration style). `AddAddressBookEndpointPresentation` was previously a no-op with no call site; it now has exactly one production call site and one registration. No second MediatR/validator/auth pipeline was introduced.

The module read endpoints now consume the seam (`IAddressBookCustomerActorResolver actorResolver` instead of raw `ICurrentAuthenticatedUser` + `IHostEnvironment`), which keeps the endpoints thin and is the shape the upcoming route migration will reuse.

## 8. Host route ownership unchanged and zero new routes

- `src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` not modified in this task; `AddressBookDevelopmentSeed.cs` not modified; `app.MapAddressBookEndpoints()` and all six Host route mappings unchanged.
- This task adds **ZERO** route mappings. The only route mappings in `Tooba.AddressBook.Endpoints` are the two read routes already delivered by the parent read-migration work (`MapGroup("/v1/customer/addresses")`, `MapGet("")`, `MapGet("/{addressId:guid}")`); none were added or removed here.
- Evaluated against the mandated parent commit `5a083e2a` (see §11 note).

## 9. Focused build and test results

| Command | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors, 12 warnings (all pre-existing) |

Tests: **none added, none run.** No AddressBook module test project exists, and the only candidate host for actor-precedence tests would require creating a new test project plus infrastructure — the broad setup the task says to skip. Precedence is proven by source inspection (§2) and by the focused builds.

## 10. Scope, residual defects, next slice

- **Production-Code-Scope:** 4 files — rewritten `Customer/AddressBookCustomerActorResolver.cs`, updated `Customer/AddressBookCustomerReadEndpoints.cs` (consume the seam), updated `AddressBookEndpointModule.cs` (`AddAddressBookEndpointPresentation` registers the seam), one added line in `Host/Program.cs` (`AddAddressBookEndpointPresentation()`). Plus the two canonical docs artifacts.
- **Test-Code-Scope:** NONE.
- **Residual-Defects:** none introduced. Deferred and still IN_PROGRESS: four write routes remain Host-owned, `AddressBookEndpoints.cs` + `AddressBookDevelopmentSeed.cs` remain, capability/root restructuring and certification pending.
- **Exact next recommended slice:** **`TB-TMAR-ADDRESSBOOK-ENDPOINT-READ-MIGRATION-001`** — migrate `GET /v1/customer/addresses` and `GET /v1/customer/addresses/{addressId:guid}` to the module (dispatch via `ISender`, reuse this seam, preserve 401/404/200 shapes) and remove only those two mappings/handlers from the Host file while the four write routes stay Host-owned.
- **Note on parent commit:** the mandated `ACCEPTED-PARENT-COMMIT: 5a083e2a` is the Architect-accepted parent; the repository had already advanced to the accepted read-migration commit `e8761e24` (from the previously delivered `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001`) before this task, and this task intentionally builds on that already-accepted work without reverting it. This is disclosed rather than hidden.
