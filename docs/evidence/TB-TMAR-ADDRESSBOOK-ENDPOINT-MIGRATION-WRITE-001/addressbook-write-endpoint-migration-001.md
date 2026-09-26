# AddressBook Write Endpoint Migration (Create + Update)

Task: `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001`
Parent: `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-READ-001` at `e8761e244add7315dadc87ff511d5748bf07ff94`
Recovery SoT checkpoint: `f9626083094e1cb6a93cbb087c479d73acb88d03`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — one bounded write-endpoint slice (Create + Update only).
Recovery honesty: AddressBook remains **IN_PROGRESS**. Host residue remains **NON-ZERO** (Delete + SetDefault routes + seed). **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Exact two routes migrated to the module

| # | Route | Handler | Dispatch |
| --- | --- | --- | --- |
| 1 | `POST /v1/customer/addresses` | `AddressBookCustomerWriteEndpoints.CreateAsync` | `ISender.Send(new CreateCustomerAddressCommand(actor, body.ToWrite()))` |
| 2 | `PUT /v1/customer/addresses/{addressId:guid}` | `AddressBookCustomerWriteEndpoints.UpdateAsync` | `ISender.Send(new UpdateCustomerAddressCommand(actor, addressId, body.ToWrite()))` |

Files added:

| File | Namespace |
| --- | --- |
| `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` | `Tooba.AddressBook.Endpoints.Customer` |

`AddressBookEndpointModule.MapAddressBookModuleEndpoints` now maps the group `app.MapGroup("/v1/customer/addresses")` + `AddressBookCustomerReadEndpoints.MapReads(group)` + `AddressBookCustomerWriteEndpoints.MapWrites(group)`. Mapping stays idempotent with the read slice — `POST ""` and `PUT "/{addressId:guid}"` appear exactly once module-side.

## 2. Exact two routes still Host-owned

`src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` now maps only:

| # | Route | Handler |
| --- | --- | --- |
| 1 | `DELETE /v1/customer/addresses/{addressId:guid}` | `DeleteAsync` |
| 2 | `POST /v1/customer/addresses/{addressId:guid}/default` | `SetDefaultAsync` |

`CreateAsync`, `UpdateAsync`, `CustomerAddressWriteRequest`, `CustomerAddressWriteRequestExtensions` and `ToWrite` were **deleted from the Host file** because no remaining Host write route consumed them. `ResolveActor`, `Unauthorized()`, `DevActorHeader` and the `using Tooba.AddressBook.Application;` import were **retained** because `DeleteAsync`/`SetDefaultAsync` still use `IAddressBookDirectory` and the actor seam. `AddressBookDevelopmentSeed.cs` and the `ProductWorkspaceDevelopmentBootstrap` seed calls were **not** touched. The Host class/file is **kept** (two routes remain).

## 3. Request DTO / wire-shape parity

`CustomerAddressWriteRequest` and `CustomerAddressWriteRequestExtensions.ToWrite()` were moved verbatim into `AddressBookCustomerWriteEndpoints.cs` (namespace `Tooba.AddressBook.Endpoints.Customer`). Exact wire fields preserved, in order:

`RecipientName`, `ContactMobile`, `Country`, `ProvinceName`, `CityName`, `PostalCode`, `PostalAddress`, `BuildingUnit`, `Label`, `IsDefault`, `FirstName = null`, `LastName = null`.

Defaults preserved exactly as before: `FirstName ?? string.Empty` / `LastName ?? string.Empty` when materialising `Tooba.AddressBook.Application.CustomerAddressWrite` (whose own defaults stay `""`). No owner id is accepted from the client — neither in the Host file nor in the module DTO. No JSON/wire-shape change.

## 4. Actor resolver reuse

No second resolver was created. Both new handlers inject the existing `IAddressBookCustomerActorResolver` (registered once in `AddAddressBookEndpointPresentation()`) and call `actorResolver.ResolveActor(httpContext)`. Precedence is unchanged: authenticated `ICurrentAuthenticatedUser` → production (not Development/Testing) returns `null` → `X-Tooba-Dev-Actor-User-Id` → `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`. No Host session type and no `Order.Application` reference exists in the endpoint layer.

## 5. ISender-only proof

`AddressBookCustomerWriteEndpoints` injects only `CustomerAddressWriteRequest`, `Guid`, `HttpContext`, `IAddressBookCustomerActorResolver`, `ISender` and `CancellationToken`. A `grep` for `IAddressBookDirectory` under `Tooba.AddressBook.Endpoints` returns **only a documentation-comment mention** in `AddressBookCustomerWriteEndpoints.cs` ("this layer has no direct access to `IAddressBookDirectory`") — no code usage. `CreateAsync`/`UpdateAsync` dispatch through `sender.Send(...)`.

## 6. Duplicate route ownership

**ZERO.** Module-owned routes = **4** total (`GET ""`, `GET "/{addressId:guid}"`, `POST ""`, `PUT "/{addressId:guid}"`). Host-owned routes = **2** total (`DELETE "/{addressId:guid}"`, `POST "/{addressId:guid}/default"`). No route template is mapped by both owners.

## 7. Program dual-map state

`Program.cs` intentionally keeps **both** calls in this slice, because two Host write routes remain:

```csharp
app.MapAddressBookEndpoints();
app.MapAddressBookModuleEndpoints();
```

`AddAddressBookEndpointPresentation()` is registered once (line 89). The stale duplicate `using Tooba.AddressBook.Endpoints;` was collapsed to one directive, removing the `CS0105` duplicate-using warning.

## 8. HTTP semantic parity

| Route | Case | Before (Host) | After (module) |
| --- | --- | --- | --- |
| `POST /v1/customer/addresses` | unauthorized | `401 { title="Unauthorized", errorCode="customer.session.required" }` | identical body |
| | success | `201` `Results.Json(created)` — `CustomerAddressRecord` | `201` `Results.Json(sender.Send(CreateCustomerAddressCommand))` — same payload |
| `PUT /v1/customer/addresses/{addressId:guid}` | unauthorized | same 401 body | identical body |
| | success | `200` `Results.Json(updated)` | `200` `Results.Json(sender.Send(UpdateCustomerAddressCommand))` — same payload |

No new error mapping was introduced. `CreateCustomerAddressCommandHandler`/`UpdateCustomerAddressCommandHandler` still delegate to `IAddressBookDirectory.CreateAsync`/`UpdateAsync` with the same actor/address/input arguments, so directory/domain behaviour (foreign-address rejection, mobile/postal validation, `IR` default) is unchanged.

## 9. CQRS / validation inventory (unchanged)

No command, handler or validator was modified. Inventory remains: **6** requests total, **5** validator-required / **5** present, **1** no-validator-required — gap **ZERO**.

## 10. Focused build and test results

| Command | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors (pre-existing warnings + the now-removed duplicate-using warning) |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests` | **Passed! Failed: 0, Passed: 8, Skipped: 0, Total: 8** |

The Host source-text guard `Endpoint_uses_session_and_rejects_missing_production_actor` was repaired for the moved DTO: it now reads the Host file for the retained session/401/dev-environment/route-group markers and additionally reads the module actor resolver + read endpoints for the neutral seam markers. All 8 focused tests pass.

## 11. Production code scope

**5 production/architecture files touched:**

1. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` — **new** (routes, handlers, transport DTO, `ToWrite`).
2. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/AddressBookEndpointModule.cs` — maps the write slice; doc comments updated.
3. `src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` — removed the two create/update routes, their handlers and the now-unused DTO/extensions; retained delete/set-default + actor seam.
4. `src/backend/Host/Tooba.Host/Program.cs` — collapsed the duplicate `using Tooba.AddressBook.Endpoints;`.
5. `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs` — repaired the Host source-text guard for the moved DTO.

**2 test-project files touched (evidence, not navigation):**

6. `src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj` — added the `Tooba.AddressBook.Endpoints` project reference required for the repaired guard.
7. `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs` (same as #5).

No frontend changes. Checkout state unchanged. No seed work. No root/capability foldering change. No CQRS/validator change. Host is **not** deleted; its `AddressBook` folder still contains `AddressBookEndpoints.cs` (delete/set-default) and `AddressBookDevelopmentSeed.cs`.

## 12. Residual defects

None introduced. Deliberately deferred and still IN_PROGRESS: the two remaining Host write routes (`Delete`/`SetDefault`), `AddressBookEndpoints.cs` and `AddressBookDevelopmentSeed.cs` still present, `Host → AddressBook.Infrastructure`/`Domain` composition edges still present, AddressBook capability/root restructuring still pending, and certification still pending.

## 13. Exact next recommended slice

**`TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002`** — migrate **Delete + SetDefault only**:

1. Add (or extend) a Customer write endpoint file in `Tooba.AddressBook.Endpoints/Customer/` mapping `DELETE /v1/customer/addresses/{addressId:guid}` and `POST /v1/customer/addresses/{addressId:guid}/default` through `ISender` to `DeleteCustomerAddressCommand` (→ `204 NoContent`) and `SetDefaultCustomerAddressCommand` (→ `200` JSON `CustomerAddressRecord`), reusing `IAddressBookCustomerActorResolver` and the same 401 body.
2. After that slice the Host `AddressBook/AddressBookEndpoints.cs` becomes route-empty, so collapse `Program.cs` to a single `app.MapAddressBookModuleEndpoints()` call and retire the Host map call + residual actor seam in a following, separately-authorized evacuation slice.
3. Still no seed work, no root/capability refactor, no certification.
