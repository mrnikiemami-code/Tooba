# AddressBook Write Endpoint Migration 002 (Delete + SetDefault)

Task: `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-002`
Parent: `TB-TMAR-ADDRESSBOOK-ENDPOINT-MIGRATION-WRITE-001` at `2a17ae332d16fe1b5f4b8e31d1b194bdbde9c568`
Recovery SoT checkpoint: `21ae229bd54fa8ab7b6bf0a855df9a1bd8e177f9`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — final bounded AddressBook HTTP route slice.
Recovery honesty: AddressBook remains **IN_PROGRESS** because `AddressBookDevelopmentSeed.cs` still lives under Host, root/capability structure cleanup may remain, and ARCH-COMPLETE-002 certification is not done. **NOT** `COMPLETE_REFERENCE_PATTERN`; **NOT** `STRUCTURE_CERTIFIED`.

## 1. Exact two routes migrated to the module

| # | Route | Handler | Dispatch |
| --- | --- | --- | --- |
| 1 | `DELETE /v1/customer/addresses/{addressId:guid}` | `AddressBookCustomerWriteEndpoints.DeleteAsync` | `ISender.Send(new DeleteCustomerAddressCommand(actor, addressId))` |
| 2 | `POST /v1/customer/addresses/{addressId:guid}/default` | `AddressBookCustomerWriteEndpoints.SetDefaultAsync` | `ISender.Send(new SetDefaultCustomerAddressCommand(actor, addressId))` |

Both were added to the existing module-owned write composition file `Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` (`MapWrites` now registers all four write routes).

## 2. Route ownership end-state

- Module-owned route count = **6**: `GET ""`, `GET "/{addressId:guid}"`, `POST ""`, `PUT "/{addressId:guid}"`, `DELETE "/{addressId:guid}"`, `POST "/{addressId:guid}/default"`.
- Host-owned AddressBook route count = **0**.
- Duplicate ownership = **ZERO** — no route template is mapped by both owners.

## 3. Host route evacuation

`src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` became route-empty after moving both routes, so it was **deleted entirely** (`git rm`). Removed along with it: `MapDelete`, `MapPost …/default`, `DeleteAsync`, `SetDefaultAsync`, the Host-local `ResolveActor`, `Unauthorized()`, `DevActorHeader`, and the now-unused `using Tooba.AddressBook.Application;` / `IAddressBookDirectory` dependency of that file. Host `AddressBook` folder now contains only `AddressBookDevelopmentSeed.cs` (untouched).

The Host source-text guard `Endpoint_uses_session_and_rejects_missing_production_actor` was repaired accordingly: it now asserts the Host file is **absent**, and validates the neutral module seam markers (`currentUser.IsAuthenticated`, `environment.IsDevelopment()`), the module route group, the 401 markers in both module read and write endpoint files, absence of owner tokens in the write file, and absence of `CurrentAuthenticatedSession` in the module resolver.

## 4. Program mapping state

In `Program.cs`:

- `using Tooba.Host.AddressBook;` was removed (no longer referenced).
- `app.MapAddressBookEndpoints();` was removed.
- Exactly one `app.MapAddressBookModuleEndpoints();` remains (line 508).
- `builder.Services.AddAddressBookEndpointPresentation();` is retained once for the actor seam.

## 5. ISender-only + actor-seam reuse proof

`AddressBookCustomerWriteEndpoints` injects only `Guid`, `HttpContext`, `IAddressBookCustomerActorResolver`, `ISender`, `CancellationToken` and the transport DTO. It calls `actorResolver.ResolveActor(httpContext)`; no second resolver was created. A `grep` across `Tooba.AddressBook.Endpoints` for `currentUser|CurrentAuthenticatedSession|IAddressBookDirectory|Tooba.AddressBook.Infrastructure|Tooba.Host.|Order.Application` returns only:
- `currentUser`/`ICurrentAuthenticatedUser` inside `AddressBookCustomerActorResolver.cs` (the accepted neutral platform seam),
- one documentation-comment mention of `IAddressBookDirectory` and one of `Order.Application`.

No Host dependency, no `AddressBook.Infrastructure` dependency, and no foreign Application/Domain/Infrastructure dependency exist in `Tooba.AddressBook.Endpoints`. `Tooba.Order.Contracts` remains the only already-accepted cross-module contract reference.

## 6. HTTP semantic parity

| Route | Case | Before (Host) | After (module) |
| --- | --- | --- | --- |
| `DELETE /v1/customer/addresses/{addressId:guid}` | unauthorized | `401 { title="Unauthorized", errorCode="customer.session.required" }` | identical body |
| | success | `204` `Results.NoContent()` after `DeleteAsync` | `204` `Results.NoContent()` after `ISender.Send(DeleteCustomerAddressCommand)` |
| `POST /v1/customer/addresses/{addressId:guid}/default` | unauthorized | same 401 body | identical body |
| | success | `200` `Results.Json(SetDefaultAsync(...))` | `200` `Results.Json(ISender.Send(SetDefaultCustomerAddressCommand))` — same `CustomerAddressRecord` |

No new error mapping was introduced and no business/domain exception is caught or translated — `DeleteCustomerAddressCommandHandler`/`SetDefaultCustomerAddressCommandHandler` still delegate directly to `IAddressBookDirectory.DeleteAsync`/`SetDefaultAsync`.

## 7. CQRS / validation inventory (unchanged)

No request, handler or validator was modified. Inventory remains **6** endpoint-reachable requests, **5** `VALIDATOR_REQUIRED` / **5** present, **1** `NO_VALIDATOR_REQUIRED`, validator gap **ZERO**.

## 8. Focused build and test results

| Command | Result |
| --- | --- |
| `dotnet build …/Tooba.AddressBook.Endpoints.csproj --no-restore` | **Build succeeded**, 0 warnings, 0 errors |
| `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore` | **Build succeeded**, 0 errors, 11 warnings (all pre-existing) |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests` | **Passed! Failed: 0, Passed: 8, Skipped: 0, Total: 8** |

## 9. Seed untouched

`src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs` and its `ProductWorkspaceDevelopmentBootstrap` call sites were **not touched**.

## 10. Production code scope

1. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Customer/AddressBookCustomerWriteEndpoints.cs` — added `DeleteAsync` + `SetDefaultAsync` and their two `MapDelete`/`MapPost` registrations.
2. `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/AddressBookEndpointModule.cs` — ownership doc comment updated to the six-route end-state.
3. `src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` — **deleted** (route-empty).
4. `src/backend/Host/Tooba.Host/Program.cs` — removed the legacy using and `app.MapAddressBookEndpoints();`.
5. `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs` — repaired the focused Host source-text guard for the removed Host file.

No frontend changes. Checkout state unchanged. No root/capability refactor. No CQRS/validator change.

## 11. Residual defects

None introduced by this slice. Still IN_PROGRESS and deferred: `AddressBookDevelopmentSeed.cs` still under Host, the `Host → AddressBook.Infrastructure`/`Domain` composition edges still present in `Program.cs`, AddressBook root/capability structure cleanup, and final ARCH-COMPLETE-002 certification. The durable guards still carry pre-slice SoT expectations (`TmarDurableGuardTests` asserts `nextTask`/`currentHostEvacuation.currentTask` as the inventory task); that SoT advance is an Architect-owned commit and was not rewritten here.

## 12. Exact next recommended bounded cleanup slice

**`TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001`** — move `AddressBookDevelopmentSeed` (ids, `ApplyAsync`, DbContext use, demo fixture, guest-actor value) into `Tooba.AddressBook.Infrastructure/Development`, keep only the thin `ProductWorkspaceDevelopmentBootstrap` triggers in Host, shrink `tmar-host-write-files.json` accordingly, and remove the Host `AddressBook` folder entirely so Host AddressBook residue becomes ZERO. No root/capability refactor and no certification in that slice.
