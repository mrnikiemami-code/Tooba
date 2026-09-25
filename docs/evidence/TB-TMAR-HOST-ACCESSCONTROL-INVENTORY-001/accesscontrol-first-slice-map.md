# TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001 — AccessControl Host first bounded inventory slice

Mode: AUDIT / MAP ONLY. No production code changed.
Track: HOST_FIRST_ACCESSCONTROL (HOST-FIRST / FOLDER-BY-FOLDER / FILE-BY-FILE).
Covered host folder: `src/backend/Host/Tooba.Host/AccessControl`.

## 0. Folder contents at inspection time

| File | Status |
| --- | --- |
| `AccessControlEndpoints.cs` | COVERED (mapped) |
| `AccessControlDemoSnapshot.cs` | COVERED (mapped) |
| `HostPlatformEffectiveAccessReader.cs` | COVERED (mapped) |
| `AccessControlDevelopmentSeed.cs` | **DEFERRED** to next slice (only `Publish(demo)` call-site recorded) |

## 1. `AccessControlEndpoints.cs` — member-level Content Disposition Map

File: `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` (~983 lines).
Transport: minimal APIs, **no MediatR/`ISender`**, no validators. Direct calls into
`IAccessControlDirectory`.

### 1.1 Route groups

| Route group | Endpoint family | Handlers |
| --- | --- | --- |
| `/v1/admin/access-control` | Admin platform | `AdminListCatalogAsync`, `AdminListRolesAsync`, `AdminCreateRoleAsync`, `AdminGetRoleAsync`, `AdminUpdateRoleAsync`, `AdminCloneRoleAsync`, `AdminArchiveRoleAsync`, `AdminGetRolePermissionsAsync`, `AdminSetRolePermissionsAsync`, `AdminListAssignmentsAsync`, `AdminAssignAsync`, `AdminRemoveAssignmentAsync`, `AdminSearchUsersAsync`, `AdminEffectiveAsync`, `AdminDemoPreviewAsync`, `AdminBootstrapAsync`, `AdminMeCapabilitiesAsync`, catalog/scope handlers |
| `/v1/admin/sellers/{sellerId:guid}/access-control` | Admin seller-scoped | `AdminGetCeilingAsync`, `AdminSetCeilingAsync`, `AdminSeller*Async` (roles/assignments/effective) |
| `/v1/seller/access-control` | Seller self | `Seller*Async` and seller-scope-resource handlers |
| scope-resources (`categories`, `brands`, `products`) | Catalog-backed lookup | `Admin/SellerListCategoriesAsync`, `...BrandsAsync`, `...ProductsAsync` |
| scope-resources (`warehouses`, `stores`, `order-segments`) | Deferred in-memory stub | `Admin/SellerDeferredScopeAsync` |

### 1.2 Member ownership classification

| Symbol / member | Responsibility | Current dependencies | Exact owner/destination | Migration risk |
| --- | --- | --- | --- | --- |
| `AccessControlEndpoints` (static) | route registration + HTTP composition | Host.Admin, Host.Seller, AccessControl.Application/Domain, BuildingBlocks, Catalog.Application, Identity.*, OperatorProfile.Application | **AccessControl.Endpoints** (`AccessControlEndpointModule`) | HIGH — the only real migration; splits into endpoint files per family |
| `MapAccessControlEndpoints` | `MapGroup` wiring | `WebApplication` | **AccessControl.Endpoints** `AccessControlEndpointModule.Map(IEndpointRouteBuilder)` | MEDIUM — call site is Host `Program.cs:520`; module registration order must be preserved |
| `EnsureCapabilityAsync` | per-handler permission gate | `IAuthorizationService`, `ICurrentTenant`, `AccessControlException`, `PlatformHttpException` | **AccessControl.Endpoints** (private helper) — decision logic itself is a thin adapter over the existing authz seam; do **not** push into Application | MEDIUM — fail-open branches are behavior; must move verbatim |
| `PlatformScope` / `SellerScope` | scope construction | `AccessOwnerScope`, `ICurrentTenant` | **AccessControl.Endpoints** (or `AccessControl.Contracts` if a shared scope factory is later extracted) | LOW |
| `Trace` (`X-Request-Id`) | request tracing | `HttpRequest` | **AccessControl.Endpoints** | LOW |
| `MapError` | exception → `IResult` | `AccessControlException` (Application/Domain), `PlatformHttpException` (BuildingBlocks) | **AccessControl.Endpoints** — mapping to HTTP status must stay at the transport edge | LOW |
| `AssignBody` (private record) | request body shape | none | **AccessControl.Contracts** (transport contract) | LOW |
| `CeilingBody`, `CeilingEntry` (private records) | request body shape | `AccessScopeKind` (Domain), `Guid?` | **AccessControl.Contracts** | LOW |
| `EnrichUserHitsAsync` | joins Access hits with Identity/OperatorProfile display identity | `IIdentityContactLookup`, `IIdentityAuthenticationService` (Identity.Application), `IOperatorProfileDirectory` (OperatorProfile.Application), `LoginIdentifierKind` (Identity.Domain) | **AccessControl.Application** (orchestration/read-model composition) or **AccessControl.Infrastructure** adapter — NOT Host. This is cross-module enrichment logic, not HTTP. | **HIGH** — introduces AccessControl → Identity/OperatorProfile dependency edges; requires `Contracts`-only rule review (see §5) |
| `FirstNonEmpty` | string helper | none | **AccessControl.Endpoints** or a shared primitive | LOW |
| `AdminDemoPreviewAsync` / `demo-preview` route | Development-only preview | `IHostEnvironment`, `AccessControlDemoSnapshot` | **Development-only surface** → `AccessControl.Endpoints` behind `IsDevelopment()`, driven by generic Development orchestration | MEDIUM — must remain DEV-gated |
| `AdminBootstrapAsync` / `bootstrap` route | bootstrap ensure | `IAccessControlDirectory.EnsureBootstrapAsync` | **AccessControl.Endpoints** + `AccessControl.Application` command if CQRS is introduced | MEDIUM |
| `Admin/SellerMeCapabilitiesAsync` | effective access for self | `IAccessControlDirectory.GetEffectiveAccessAsync` | **AccessControl.Endpoints** → **AccessControl.Application** query | LOW |
| deferred scope handlers | explicit deferred stub responses | none | **AccessControl.Endpoints** (temporary) — own `Development`/deferred marker | LOW |
| catalog scope handlers | category/brand/product lookup | `ICatalogLookupGateway` (Catalog.Application) | **AccessControl.Endpoints** → thin query into **AccessControl.Application**, gateway passed in | MEDIUM — Catalog.Application must be replaced with Catalog.Contracts-only surface |

### 1.3 Direct dependency edges observed (file `using`s)

- `Tooba.AccessControl.Application` → `IAccessControlDirectory`, `AccessOwnerScope`, `AccessOwnerScopeKind`, `CreateAccessRoleCommand`, `UpdateAccessRoleCommand`, `CloneAccessRoleCommand`, `RolePermissionGrant`, `AccessUserHitDto`, `AccessScopeKind`, `CeilingEntry`-adjacent types.
- `Tooba.AccessControl.Domain` → `AccessScopeKind`, `AccessControlException`.
- `Tooba.BuildingBlocks` → `PlatformHttpException`, `Authorization*`, `CurrentAuthenticatedSession`, `ICurrentTenant`, `ToobaEdition`.
- `Tooba.Catalog.Application` → `ICatalogLookupGateway` (foreign module, Application namespace).
- `Tooba.Host.Admin` → `AdminPanelAccess`.
- `Tooba.Host.Seller` → `SellerPanelAccess`.
- `Tooba.Identity.Application` → `IIdentityContactLookup`, `IIdentityAuthenticationService`.
- `Tooba.Identity.Domain` → `LoginIdentifierKind`.
- `Tooba.OperatorProfile.Application` → `IOperatorProfileDirectory`.

### 1.4 Minimum AccessControl project/layer structure required before migration

- `Tooba.AccessControl.Contracts` — **does not exist**; required for `AssignBody`, `CeilingBody`/`CeilingEntry`, request/response DTOs and cross-module read shapes (`AccessUserHitDto` etc. once moved out of Application).
- `Tooba.AccessControl.Endpoints` — **does not exist**; required to own route groups, `EnsureCapabilityAsync`, scope/error/trace helpers, and `AccessControlEndpointModule`.
- `Tooba.AccessControl.Application` — exists, but is **not CQRS**; needs queries/commands + validators for role/assignment/ceiling/effective/catalog operations.
- `Tooba.AccessControl.Infrastructure` — exists (directory, DbContext, migrations, module, outbox, instrumentation); candidate owner for `HostPlatformEffectiveAccessReader` if the seam is judged module-owned (see §3).

## 2. `AccessControlDemoSnapshot.cs` — Content Disposition Map

| Symbol | Responsibility | Destination | Risk |
| --- | --- | --- | --- |
| `AccessControlDemoSnapshot` (static holder, `Gate` + `_current`) | process-local DEV snapshot state | **Development orchestration** (generic dev support) with AccessControl-typed payload; not production module state | LOW |
| `Current` (thread-safe getter) | read snapshot | same | LOW |
| `Publish(AccessControlDemoContext)` | write snapshot, called from `AccessControlDevelopmentSeed` (`:259`) and read from `AccessControlEndpoints` (`:359`) | same | LOW |
| `AccessControlDemoContext` record (23 fields) | demo scenario identifiers (actor ids, seller, categories, offers, orders, operator role) | **Development-only surface** — belongs with dev seed orchestration, not AccessControl.Domain/Application | LOW |

Decision: keep as **AccessControl development support under the generic Development orchestration
seam**. It is DEV-only, no production module should reference it. No move in this task.

## 3. `HostPlatformEffectiveAccessReader.cs` — Content Disposition Map

| Symbol | Responsibility | Destination | Reason |
| --- | --- | --- | --- |
| `HostPlatformEffectiveAccessReader` | maps AccessControl effective grants → neutral `PlatformPermissionGrant` seam | **Split**: keep the `IPlatformEffectiveAccessReader` implementation as **generic platform infrastructure** (currently `Tooba.BuildingBlocks.Security`), but it is AccessControl-backed so it must live where AccessControl types are reachable | The interface is a generic platform seam consumed by foreign modules (Fulfillment authorizer, tests). Only the adapter implementation is AccessControl-specific. |
| `GetEffectivePermissionsAsync` | owner-kind → `AccessOwnerScope`, then `directory.GetEffectiveAccessAsync` | Adapter body | LOW |
| `MapScope` | `AccessScopeKind` → `PlatformAccessScopeKind` mapping | Adapter body | LOW |

Recorded reason: the seam itself (`IPlatformEffectiveAccessReader`, `PlatformPermissionGrant`,
`PlatformAccessOwnerKind`, `PlatformAccessScopeKind`) is declared in
`src/backend/BuildingBlocks/Tooba.BuildingBlocks/Security/IPlatformAccessSeams.cs` and is a
**generic platform security seam**. Therefore the reader must **not** remain in Host
(`Tooba.Host.AccessControl`) long-term, because a foreign module (Fulfillment) already consumes
the interface and the Host implementation is only reachable via Host DI. The correct destination
is **`Tooba.AccessControl.Infrastructure`** (an AccessControl-owned adapter that references both
`Tooba.BuildingBlocks.Security` and AccessControl Application/Domain). This is a move, not a
split of the generic seam. Exact DI registration call site: `Program.cs:202`.

## 4. Usage / consumer evidence (targeted only)

| Symbol | Consumers / registrations |
| --- | --- |
| `MapAccessControlEndpoints` | `src/backend/Host/Tooba.Host/Program.cs:520` (`app.MapAccessControlEndpoints();`) |
| `AccessControlDemoSnapshot.Publish` | `AccessControlDevelopmentSeed.cs:259` (deferred file) |
| `AccessControlDemoSnapshot.Current` | `AccessControlEndpoints.cs:359` |
| `HostPlatformEffectiveAccessReader` | `Program.cs:202` DI registration as `IPlatformEffectiveAccessReader` |
| `IPlatformEffectiveAccessReader` (seam) | `Tooba.Fulfillment.Endpoints/Seller/FulfillmentSellerAuthorizer.cs:14`; `Tooba.Fulfillment.Tests/Behavior/FulfillmentAdminSellerAuthorizerParityTests.cs:130` (test stub); declaration `BuildingBlocks/.../IPlatformAccessSeams.cs:78` |

No broad Host module-completion scan performed.

## 5. AccessControl module readiness (as-is, not repaired)

| Project | Exists | Files (non-bin/obj) | Namespace state | Notes |
| --- | --- | --- | --- | --- |
| `Tooba.AccessControl.Domain` | YES | `AccessControlDomain.cs` (root) | `Tooba.AccessControl.Domain` | No capability folders; single root file |
| `Tooba.AccessControl.Application` | YES | `AccessControlContracts.cs` (root), `PermissionCatalog.cs` (root) | `Tooba.AccessControl.Application` | **Not CQRS**: no `Commands`, `Queries`, `Validators` folders; contracts live inside Application rather than a `Contracts` project |
| `Tooba.AccessControl.Infrastructure` | YES | `AccessControlDirectory.cs`, `AccessControlInstrumentation.cs`, `AccessControlModule.cs`, `AccessControlOutboxRegistration.cs` (root) + `Persistence/` (DbContext + 4 migration files) | `Tooba.AccessControl.Infrastructure`, `.Infrastructure.Persistence[.Migrations]` | Has module registration + outbox; integration folders not yet split |
| `Tooba.AccessControl.Contracts` | **NO** | — | — | must be created |
| `Tooba.AccessControl.Endpoints` | **NO** | — | — | must be created |
| `Tooba.AccessControl.Tests` | **NO** | — | — | not requested; note for later certification |

- **CQRS state**: no MediatR anywhere under `Modules/AccessControl` (verified: no `MediatR` package
  reference in any AccessControl csproj). Endpoints call `IAccessControlDirectory` directly.
- **Validator state**: none — no FluentValidation references, no `Validators` folder.
- **Folder/namespace state**: Application/Domain/Infrastructure have no capability folders
  (`Aggregates`, `Commands`, `Queries`, `Validators`, `Endpoints`, `Infrastructure` integration
  folders). AccessControl is therefore **not** ARCH-COMPLETE-002 certified; it remains in
  `uncertifiedHttpOwningModules`.

## 6. Next bounded implementation slice (recommended, <= 15 minutes)

**One** slice only — do not migrate the whole folder:

> `TB-TMAR-HOST-ACCESSCONTROL-ENDPOINTS-SLICE-001` — create
> `Tooba.AccessControl.Endpoints` + `Tooba.AccessControl.Contracts`, move the **largest shared
> transport family** (`AssignBody`, `CeilingBody`, `CeilingEntry` request shapes + the
> `AccessControlEndpointModule` skeleton with `MapAccessControlEndpoints` wiring and the
> `PlatformScope`/`SellerScope`/`Trace`/`MapError` helpers), and re-point `Program.cs:520`.
> Keep all handlers and `EnsureCapabilityAsync` in Host for this slice (behavior verbatim).

Rationale: this first slice is mechanical, keeps behavior identical, creates the two missing
projects, and unblocks subsequent family-by-family handler migration without touching
`EnrichUserHitsAsync` cross-module enrichment (the highest-risk item) yet.

Residual risks / open decisions for the Architect:
1. `Catalog.Application` (`ICatalogLookupGateway`) must become `Catalog.Contracts` before the
   catalog scope-resource family can migrate (Contracts-only foreign dependency rule).
2. `EnrichUserHitsAsync` creates AccessControl → Identity/OperatorProfile edges; whether
   AccessControl.Application may consume those `Application` namespaces or only `Contracts`
   needs an Architect ruling.
3. `AccessControlDevelopmentSeed.cs` remains in Host and depends on several modules; it is the
   next slice after the endpoint module skeleton.

## 7. Compliance

- Production code changes: **none**.
- Tests: none required, none run (map-only).
- Protected surfaces: Fulfillment certified state untouched; Checkout paused; frontend frozen;
  no other Host folder inspected.
