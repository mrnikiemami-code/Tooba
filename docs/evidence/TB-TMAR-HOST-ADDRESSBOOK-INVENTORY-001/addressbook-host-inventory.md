# AddressBook Host Inventory and Ownership Classification

Task: `TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001`
Parent: `TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1` at `227760882efe6ce3b5c1f81a481e5d212a836d4f`
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE — audit-only.
**Production code changes = NONE. Test code changes = NONE.**

## 1. Exact two-file Host inventory

| # | Host file | Lines | Namespace |
| --- | --- | --- | --- |
| 1 | `src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs` | 198 | `Tooba.Host.AddressBook` |
| 2 | `src/backend/Host/Tooba.Host/AddressBook/AddressBookDevelopmentSeed.cs` | 62 | `Tooba.Host.AddressBook` |

No other file exists in the Host `AddressBook` folder. Both files were read completely.

## 2. Member-level Content Disposition Map — `AddressBookEndpoints.cs`

| Member | Kind | Responsibility | Destination ownership |
| --- | --- | --- | --- |
| `AddressBookEndpoints` (static class) | host endpoint shell | HTTP boundary class | `AddressBook.Endpoints` |
| `DevActorHeader` = `"X-Tooba-Dev-Actor-User-Id"` | dev-only const | non-production actor header name | shared neutral security seam (dev actor header constant) — must not live in AddressBook production Application/Endpoints as an invented cross-module contract |
| `MapAddressBookEndpoints(this WebApplication)` | host route registration | `/v1/customer/addresses` group + 6 routes | `AddressBook.Endpoints` (`AddressBookEndpointModule`) |
| `ListAsync` → `GET /v1/customer/addresses` | endpoint | actor + `IAddressBookDirectory.ListAsync` | `AddressBook.Endpoints` |
| `GetAsync` → `GET /v1/customer/addresses/{addressId:guid}` | endpoint | actor + `GetAsync`, 404 `customer.address.missing` | `AddressBook.Endpoints` |
| `CreateAsync` → `POST /v1/customer/addresses` | endpoint | actor + `CreateAsync`, 201 | `AddressBook.Endpoints` |
| `UpdateAsync` → `PUT /v1/customer/addresses/{addressId:guid}` | endpoint | actor + `UpdateAsync`, 200 | `AddressBook.Endpoints` |
| `DeleteAsync` → `DELETE /v1/customer/addresses/{addressId:guid}` | endpoint | actor + `DeleteAsync`, 204 | `AddressBook.Endpoints` |
| `SetDefaultAsync` → `POST /v1/customer/addresses/{addressId:guid}/default` | endpoint | actor + `SetDefaultAsync`, 200 | `AddressBook.Endpoints` |
| `ResolveActor(HttpRequest, CurrentAuthenticatedSession, IHostEnvironment)` | actor resolution | session → dev header → guest actor fallback | split: session/dev-header gate = shared neutral security seam (`ICurrentAuthenticatedUser` + dev-actor seam); the guest fallback *value* must come from `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` — **NOT** from `Order.Application` |
| `Unauthorized()` | error helper | 401 `customer.session.required` | `AddressBook.Endpoints` |
| `CustomerAddressWriteRequest` (public record) | transport DTO | HTTP body shape, explicitly no owner id | `AddressBook.Endpoints` (or `AddressBook.Application` request shape if CQRS is introduced) |
| `CustomerAddressWriteRequestExtensions` | mapping helper | HTTP → `CustomerAddressWrite` | `AddressBook.Endpoints` (adjacent to the endpoint DTO) |
| `ToWrite()` | mapping method | `CustomerAddressWriteRequest` → `CustomerAddressWrite`, `FirstName/LastName` null-coalesced to `""` | `AddressBook.Endpoints` |
| `using Tooba.AddressBook.Application;` | dependency | `IAddressBookDirectory` + `CustomerAddressWrite` | acceptable temporary Host composition |
| `using Tooba.Host.Storefront;` | dependency | `CurrentAuthenticatedSession` lives in `Tooba.Host` (root) / `Tooba.Host.Storefront` | **architectural leak** to be replaced by a neutral seam |

**REMOVE candidates: NONE.** Every member is live. `using Tooba.Host.Storefront` in `AddressBookEndpoints.cs` is currently *unused by symbol* (the file references the fully-qualified `Tooba.Order.Application...StorefrontGuestActorId` and `CurrentAuthenticatedSession` which is in namespace `Tooba.Host`, not `Tooba.Host.Storefront`) — it is syntactically tolerated but is not a member and must not be silently dropped without a compile check during migration.

## 3. Member-level Content Disposition Map — `AddressBookDevelopmentSeed.cs`

| Member | Kind | Responsibility | Destination ownership |
| --- | --- | --- | --- |
| `AddressBookDevelopmentSeed` (static class) | dev seed | deterministic demo addresses | **split**: data/logic → `AddressBook.Infrastructure/Development`; Host keeps only a thin trigger |
| `DefaultAddressId` = `aaaaaaaa-…-0000000000a1` | stable demo id | first demo address id | `AddressBook.Infrastructure/Development` (id constants travel with the seed) |
| `AlternateAddressId` = `aaaaaaaa-…-0000000000a2` | stable demo id | second demo address id | `AddressBook.Infrastructure/Development` |
| `ApplyAsync(IServiceProvider, CancellationToken)` | dev seed entry | idempotent insert of 2 demo rows | `AddressBook.Infrastructure/Development`; Host/bootstrap keeps a one-line call site |
| direct `AddressBookDbContext` use (`services.GetRequiredService<AddressBookDbContext>()`) | persistence coupling | Host resolves the module's own DbContext | acceptable only inside `AddressBook.Infrastructure/Development`; a Host-held `AddressBookDbContext` reference is a **composition leak** |
| `CustomerAddress.Create(...)` use | domain use | builds demo aggregates through the module aggregate | moves with the seed to `AddressBook.Infrastructure/Development` |
| `StorefrontGuestActorId` dependency | cross-module dependency | demo addresses are owned by the guest actor | replace `Order.Application…StorefrontGuestActorId` with `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` (or an AddressBook-owned dev-guest constant) — **must not** import `Order.Application` from module code |
| deterministic demo data (`گیرندهٔ نمایشی توبا`, `+989120000014`, `تهران`, `19199`, `aaaaaaaa-…` ids, createdAt `2026-08-25T14:00:00Z`) | demo fixture | reproducible storefront demo addresses | `AddressBook.Infrastructure/Development` |
| `using Tooba.Host.Storefront;` | unused import | not referenced by any symbol in this file | dead import — remove during migration (verify by build) |

**Seed ownership decision:** `SPLIT — module `AddressBook.Infrastructure/Development` owns `AddressBookDevelopmentSeed` (ids, `ApplyAsync`, DbContext use, demo data, guest-actor value via Order.Contracts); Host keeps only the existing thin bootstrap trigger lines in `ProductWorkspaceDevelopmentBootstrap.cs`. It must **not** be retired: it has two live production composition call sites plus a live guard test asserting idempotency and both ids.

## 4. Six-route semantic parity table

| # | Route | Auth / actor source | Dev/Testing fallback | Request DTO | Success response | Not-found | Unauthorized | Directory method | Trace | Validation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `GET /v1/customer/addresses` | `CurrentAuthenticatedSession.UserId` | dev header → guest actor | none | `200` JSON list | n/a (empty list) | `401 customer.session.required` | `ListAsync(actor, ct)` | none | none at HTTP layer |
| 2 | `GET /v1/customer/addresses/{addressId:guid}` | same | same | route guid | `200` JSON record | `404 { title="Not Found", errorCode="customer.address.missing" }` | `401` | `GetAsync(actor, addressId, ct)` | none | route constraint `:guid` only |
| 3 | `POST /v1/customer/addresses` | same | same | `CustomerAddressWriteRequest` | `201` JSON record | n/a | `401` | `CreateAsync(actor, body.ToWrite(), ct)` | none | none at HTTP layer; `AddressBookDirectory`/`CustomerAddress.Create` own rules (mobile `+98…`, postal length, country `IR` default, `Guid.Empty` actor guard) |
| 4 | `PUT /v1/customer/addresses/{addressId:guid}` | same | same | route guid + `CustomerAddressWriteRequest` | `200` JSON record | domain/directory outcome (no explicit 404 branch) | `401` | `UpdateAsync(actor, addressId, body.ToWrite(), ct)` | none | as row 3 |
| 5 | `DELETE /v1/customer/addresses/{addressId:guid}` | same | same | route guid | `204` no content | domain/directory outcome (no explicit 404 branch) | `401` | `DeleteAsync(actor, addressId, ct)` | none | route constraint only |
| 6 | `POST /v1/customer/addresses/{addressId:guid}/default` | same | same | route guid | `200` JSON record | domain/directory outcome (no explicit 404 branch) | `401` | `SetDefaultAsync(actor, addressId, ct)` | none | route constraint only |

No trace/correlation/telemetry behavior exists in this Host file. Behavior must be preserved exactly; no normalization is performed here.

## 5. Actor authority classification

`ResolveActor` precedence:

1. `session.IsAuthenticated` → `session.UserId` (production authority).
2. If not `Development` **and** not `Testing` → `null` (401). Production never trusts headers or fallbacks.
3. `Development`/`Testing`: `X-Tooba-Dev-Actor-User-Id` header if it parses to a non-`Guid.Empty` guid.
4. `Development`/`Testing` fallback: `Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId` (value = `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`, `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`).

Classification:

- The **session + environment gate** is a legitimate Host concern; modules must not own it or reference `CurrentAuthenticatedSession`.
- The **dev-actor header** is a non-production testing seam; it is a Host/dev concern, not AddressBook business logic.
- The **guest fallback is a value dependency, not a business rule.** It is currently imported from `Order.Application`, which is an **architectural leak** (Host → foreign module Application). The canonical value already exists as a neutral contract constant `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`, and `Fulfillment.Endpoints` already consumes it that way — this is the accepted migration precedent.

**Correct neutral ownership decision for migration:**

- Do **not** move `Order.Application…StorefrontGuestActorId` into `AddressBook.Application` or `AddressBook.Endpoints`.
- AddressBook production code must consume only `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` if it needs the value at all.
- Preferred target shape: `AddressBook.Endpoints` resolves the actor through a neutral, module-owned seam built on `Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser` (already `IsAuthenticated` + `UserId`) plus a Host-registered dev/testing actor provider; the guest value then comes from `Order.Contracts`, never from `Order.Application`. The dev header name belongs to that shared dev seam, not to AddressBook production code.
- `AddressBook.Endpoints` must never reference `Tooba.Host*`.

## 6. Dependency-edge audit (current Host AddressBook)

| Edge | Evidence | Classification |
| --- | --- | --- |
| Host `AddressBook` → `AddressBook.Application` | `using Tooba.AddressBook.Application;`; `IAddressBookDirectory`, `CustomerAddressWrite` | ACCEPTABLE temporary Host composition |
| Host `AddressBook` → `AddressBook.Infrastructure.Persistence` | `AddressBookDevelopmentSeed.cs`: `using Tooba.AddressBook.Infrastructure.Persistence;` + `GetRequiredService<AddressBookDbContext>()` | **LEAK for the endpoint file's purposes**, but currently acceptable only as dev-seed composition; must move with the seed into `AddressBook.Infrastructure/Development` |
| Host `AddressBook` → `AddressBook.Domain` | `using Tooba.AddressBook.Domain;` + `CustomerAddress.Create` | ACCEPTABLE only inside the dev seed; moves with seed to the module |
| Host `AddressBook` → `Tooba.Host.Storefront` | `using Tooba.Host.Storefront;` in both files | MIXED — the `using` is unused-by-symbol in both files; `CurrentAuthenticatedSession` actually resolves from namespace `Tooba.Host`. Dead-import risk to clean during migration, not a member |
| Host `AddressBook` → `Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId` | `AddressBookEndpoints.cs:157`; `AddressBookDevelopmentSeed.cs:24` | **ARCHITECTURAL LEAK** — Host → foreign module Application. Replace with `Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` |
| Host `AddressBook` → `CurrentAuthenticatedSession` | `Endpoints` parameter type | ACCEPTABLE Host concern today; becomes a neutral `ICurrentAuthenticatedUser` seam at evacuation |
| Host `AddressBook` → `IHostEnvironment` | `Endpoints` parameter type | ACCEPTABLE Host concern (environment gate) |
| Host `AddressBook` → `IHostEnvironment` / ASP.NET types | `WebApplication`, `IResult`, `Results`, `StatusCodes` | ACCEPTABLE (becomes `AddressBook.Endpoints` with `FrameworkReference Microsoft.AspNetCore.App`, mirroring Payment/Settlement/Fulfillment) |
| `AddressBook.Application` → Host | none | CLEAN |
| `AddressBook.Application` → foreign Application/Domain | none | CLEAN |

## 7. Module readiness audit

### 7.1 Current root `.cs` files

| Project | Root `.cs` files |
| --- | --- |
| `Tooba.AddressBook.Application` | `AddressBookContracts.cs` |
| `Tooba.AddressBook.Contracts` | `CustomerAddressContracts.cs` |
| `Tooba.AddressBook.Domain` | `CustomerAddress.cs` |
| `Tooba.AddressBook.Infrastructure` | `AddressBookDirectory.cs`, `AddressBookModule.cs` |
| `Tooba.AddressBook.Endpoints` | **DOES NOT EXIST** |

Non-root structure: `Tooba.AddressBook.Infrastructure/Persistence/AddressBookDbContext.cs`, `Tooba.AddressBook.Infrastructure/Migrations/*` (4 files, namespace `Tooba.AddressBook.Infrastructure.Migrations` — note this deliberately differs from the persisted folder name `Migrations`, mirroring the Payment EF precedent). `artifacts` directories exist under Application/Domain/Infrastructure. No capability folders at all.

### 7.2 CQRS / MediatR

- `MediatR`, `IRequestHandler`, `ISender`: **ZERO occurrences** in the entire AddressBook module.
- All six HTTP routes call `IAddressBookDirectory` **directly** — there is no request/handler layer, no `ISender` dispatch.
- CQRS state = `ABSENT_NO_MEDIATR_NO_REQUESTS_NO_HANDLERS`.

### 7.3 Endpoints project

**ABSENT.** `src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints` does not exist. A proper `AddressBook.Endpoints` project **must be created before Host endpoint evacuation** (established ARCH-COMPLETE-002 reference pattern: `EndpointModule` with `AddXEndpointPresentation` + `MapXEndpoints`, Admin/Seller or Customer capability folders, `ISender`-only endpoints, `FrameworkReference Microsoft.AspNetCore.App`, references `Application` + `BuildingBlocks` only).

### 7.4 Validators

**ABSENT.** No `FluentValidation` reference and no validators anywhere in the module. Endpoint-reachable request inventory is currently 6 (the six routes), all direct directory calls with no request objects, so validator classification does not exist yet and would be derived during the CQRS/endpoints foundation task.

### 7.5 Contracts contents

`Tooba.AddressBook.Contracts/CustomerAddressContracts.cs`:

- `CustomerAddressRecord` (public DTO: `AddressId`, `RecipientName`, `ContactMobile`, `Country`, `ProvinceName`, `CityName`, `PostalCode`, `PostalAddress`, `BuildingUnit`, `Label`, `IsDefault`, `CreatedAt`, `UpdatedAt`, `FirstName`, `LastName`) — no owner id.
- `IAddressBookCheckoutLookup.GetAsync(Guid actorUserId, Guid addressId, CancellationToken)` — stable read seam for Order checkout shipping imaging.

`Tooba.AddressBook.Application/AddressBookContracts.cs`:

- `CustomerAddressWrite` (write input, no owner id).
- `IAddressBookDirectory : IAddressBookCheckoutLookup` with `CreateAsync`, `ListAsync`, `UpdateAsync`, `DeleteAsync`, `SetDefaultAsync`, `CountAsync`.

`IAddressBookDirectory.CountAsync` has **no production consumer** (only `AddressBookDirectory` implements it; the Host endpoint layer never calls it). It is candidate dead-surface residue for a later bounded hygiene decision — not a blocker, and not removed here.

### 7.6 Infrastructure structure

`AddressBookDirectory.cs` (implements `IAddressBookDirectory` only, uses `AddressBookDbContext`), `AddressBookModule.cs` (`IToobaModule`: `AddSingleton<IOutboxModuleRegistration, AddressBookOutboxRegistration>`, `AddScoped<IAddressBookDirectory, AddressBookDirectory>`, `AddDbContext<AddressBookDbContext>` with `ToobaNpgsql.ResolveForContext` + `OutboxSaveChangesInterceptor`), `AddressBookOutboxRegistration` (declares schema + `OutboxMessageMapping.TableName`, `Translate` → null, no integration events), `Persistence/AddressBookDbContext.cs` (schema `address_book`), `Migrations/` (2 migrations + designer + snapshot). **No capability folders** (`Directories`, `Messaging`, `Persistence` are missing/partial by ARCH-COMPLETE-002 standards).

### 7.7 Do Host routes call `IAddressBookDirectory` directly?

**YES.** All six routes inject `IAddressBookDirectory` and call its methods directly. There is no `ISender`/MediatR boundary today.

### 7.8 Does Application need CQRS request/handler extraction?

**YES** if AddressBook is to reach the accepted `COMPLETE_REFERENCE_PATTERN`/ARCH-COMPLETE-002 shape (`ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT`). Six requests + handlers must be derived from the six directory operations, keeping ownership rules in `AddressBookDirectory`. Whether AddressBook is declared HTTP-owning or stays a bounded internal/contracts-only module is an Architect applicability decision — evidence records that the current Host routes make it de-facto HTTP-owning.

### 7.9 Does the root structure already violate ARCH-COMPLETE-002?

**YES**, as measured against the certified reference pattern:

- Application root carries a capability file (`AddressBookContracts.cs`) — target root allowlist is `[]` with `Application` capability folders.
- Infrastructure root carries `AddressBookDirectory.cs` (target: `Directories/`) and `AddressBookModule.cs` (legitimate module root).
- Domain root carries `CustomerAddress.cs` (root allowlist `[]` expected for a domain project).
- Contracts root carries `CustomerAddressContracts.cs` (compare `Payment.Contracts`/`Settlement.Contracts` certified entries — root allowlists there are `[]` too, so this is a consistent, documented deviation rather than a spontaneous defect).
- No capability/integration folders, no Endpoints project, no Validators, no CQRS.
- `AddressBook` is currently `COMPLETE_REFERENCE_PATTERN`-**undeclared**: it is not in `docs/architecture/tmar-module-structure-manifests.json` and not in `structureLock.certifiedModules`, so it is not a certified-module contradiction — it simply is not yet on the certified path.

Structure is **not repaired** in this task.

## 8. Program / bootstrap call-site inventory

Production composition sites (unmodified):

| # | Call site | File:line |
| --- | --- | --- |
| 1 | `app.MapAddressBookEndpoints();` | `src/backend/Host/Tooba.Host/Program.cs:506` |
| 2 | `using Tooba.Host.AddressBook;` | `src/backend/Host/Tooba.Host/Program.cs:33` |
| 3 | `builder.Services.AddScoped<Tooba.AddressBook.Contracts.IAddressBookCheckoutLookup>(sp => sp.GetRequiredService<Tooba.AddressBook.Application.IAddressBookDirectory>());` | `src/backend/Host/Tooba.Host/Program.cs:188` |
| 4 | `new AddressBookModule()` | `src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs:72` (`using Tooba.AddressBook.Infrastructure;` at line 23) |
| 5 | `await AddressBookDevelopmentSeed.ApplyAsync(provider);` | `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs:161` |
| 6 | `await AddressBookDevelopmentSeed.ApplyAsync(provider, cancellation);` | `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceDevelopmentBootstrap.cs:287` |

Test call sites (evidence, not navigation):

| # | Call site | File:line |
| --- | --- | --- |
| 7 | `AddressBookDevelopmentSeed.ApplyAsync(provider)` ×2 (idempotency proof) | `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs:403-404` |
| 8 | `AddressBookDevelopmentSeed.DefaultAddressId` / `.AlternateAddressId` assertions | `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs:409-410` |
| 9 | Host source-text guard on `AddressBook/AddressBookEndpoints.cs` | `src/backend/Host/Tooba.Host.Tests/AddressBookFoundationTests.cs:62-69` |

Baseline registration: `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-host-write-files.json:4` lists `AddressBook/AddressBookDevelopmentSeed.cs` as a Host-write file.

## 9. Recommended bounded follow-up sequence

Derived from the evidence above; **not** created by this task.

**Task A — `TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001`** (module-side, largest, still bounded)
Create `Tooba.AddressBook.Endpoints` (Customer capability folder, `AddressBookEndpointModule` with presentation registration + `MapAddressBookEndpoints`) and the six CQRS requests/handlers in `Tooba.AddressBook.Application` (Commands/Queries/Errors/Models), moving `CustomerAddressWriteRequest`/`ToWrite` transport shape into the Endpoints project; add ADDRESSBOOK capability/root foldering (`Validators` classification, `Infrastructure/Directories`), fix `Contracts`/`Application`/`Domain`/`Infrastructure` root structure, and add a Host-independent actor seam. No routing change, no Host deletion.

**Task B — `TB-TMAR-HOST-ADDRESSBOOK-ENDPOINT-EVACUATION-001`**
Delete `src/backend/Host/Tooba.Host/AddressBook/AddressBookEndpoints.cs`, switch `Program.cs` to `MapAddressBookModuleEndpoints()` (single map call), register the module-owned presentation/authorizer seams Host-side, replace the `Order.Application` guest-actor leak with `Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId`, repair the Host source-text guard in `AddressBookFoundationTests` and the Host root/ownership guards for the removed file, and prove semantic parity for all six routes.

**Task C — `TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001`**
Move `AddressBookDevelopmentSeed` (ids, `ApplyAsync`, DbContext use, demo fixture, guest-actor value) to `Tooba.AddressBook.Infrastructure/Development`, keep only the two thin `ProductWorkspaceDevelopmentBootstrap` triggers in Host, update `tmar-host-write-files.json`, and remove the Host `AddressBook` folder entirely.

**Task D — optional later certification**: `TB-TMAR-ADDRESSBOOK-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001` mirroring the AccessControl closure (manifest certification, `structureLock.certifiedModules`, validator coverage, manifest/SoT guards).

Smallest safe sequence = A → B → C (3 tasks) with D only after B/C, and only if the Architect declares AddressBook HTTP-owning / certification-eligible.

## 10. Explicit scope statement

**Production code changes = NONE.**
**Test code changes = NONE.**
Files created by this task: `docs/ai/tasks/TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001.task.md`, `docs/evidence/TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001/addressbook-host-inventory.md`. Nothing under `src/backend/Modules/**`, `src/backend/Host/Tooba.Host/**`, or `src/backend/Host/Tooba.Host.Tests/**` was modified. No build, no tests, no SoT rewrite.
