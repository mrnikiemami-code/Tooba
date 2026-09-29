# Host/Seller — Seller-R1A — Migration

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R1A
**Parent task:** TB-TMAR-HOST-SELLER-AMC-001-R1
**Parent implementation commit:** `3c13e4bcbf2b0a32ef4a701b48e8782a41d6b51f`
**Mode:** behavior-preserving boundary repair. No route/header/status/DTO/schema/frontend change.

## 1. A — Order view access port re-homed into Order Infrastructure

### Delete (Host no longer implements a foreign Application port)

- `src/backend/Host/Tooba.Host/Security/Seller/HostSellerOrderViewAccessReader.cs` — **DELETED**.

### Add (Order-owned implementation, exact path-derived namespace)

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Seller/SellerOrderViewAccessReader.cs`
namespace `Tooba.Order.Infrastructure.Seller`, `internal sealed class SellerOrderViewAccessReader(IPlatformEffectiveAccessReader access) : ISellerOrderViewAccessReader`.

Behavior preserved verbatim from the removed Host reader:

- `order.view` permission filter with `!x.DeniedByCeiling`.
- Empty grant → `Denied: true, GlobalWithinOwner: false, AllowedCategoryIds: []`.
- Any `PlatformAccessScopeKind.GlobalWithinOwner` → `Denied: false, GlobalWithinOwner: true`.
- Otherwise category scopes with non-null `ScopeResourceId` → allowed set; `Denied: allowed.Count == 0`.

It consumes only `Tooba.BuildingBlocks.Security` and the Order Application port (intra-module). No `Tooba.AccessControl` reference.

### DI rewiring

- `src/backend/Host/Tooba.Host/Program.cs`: removed the Host registration of `Tooba.Order.Application.Seller.Ports.ISellerOrderViewAccessReader → HostSellerOrderViewAccessReader`.
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs`: added
  `services.AddScoped<Application.Seller.Ports.ISellerOrderViewAccessReader, SellerOrderViewAccessReader>();`
  plus `using Tooba.BuildingBlocks.Security;`.

## 2. B — Host-owned seller security error codes

### Add

`src/backend/Host/Tooba.Host/Security/Seller/SellerSecurityErrorCodes.cs` — `internal static class SellerSecurityErrorCodes` in `namespace Tooba.Host.Security.Seller` with the four stable codes. Values are byte-for-byte the pre-existing literals, so wire behavior is unchanged.

### Edit

- `Security/Seller/HostOrderSellerAuthorizer.cs`: removed `using Tooba.Order.Application.Seller;`; `SellerOrderErrors.ActorMissing` → `SellerSecurityErrorCodes.ActorMissing`.
- `Security/Seller/HostSupportSellerAuthorizer.cs`: removed `using Tooba.Support.Application.Errors;`; `SupportErrorCodes.SellerAuthorizationDenied` → `SellerSecurityErrorCodes.AuthorizationDenied`.
- `Security/Seller/SellerPanelAccess.cs`: replaced the four raw string literals with `SellerSecurityErrorCodes.*` (values identical).

## 3. C — Final Host security boundary state

Every production file under `Host/Tooba.Host/Security/Seller` now has **ZERO** references to foreign `.Application` / `.Domain` / `.Infrastructure` / `.Persistence`, and no `DbContext` / `DbSet`. Allowed references are `Tooba.BuildingBlocks`, `Tooba.BuildingBlocks.Security`, module **Endpoints** ports where Host is the transport/security adapter, and ASP.NET/Host primitives.

## 4. Guards updated (never weakened)

- `src/backend/Host/Tooba.Host.Tests/Architecture/HostSellerAmcR1GuardTests.cs`: the R1 allowlist for foreign Application references was **removed** and replaced by a ZERO-tolerance regex over the whole boundary; added facts proving the Order port is Order-owned, Host no longer implements it, and the authorizers no longer reference foreign Application error codes; `SellerSecurityErrorCodes.cs` added to the boundary file set.
- `src/backend/Host/Tooba.Host.Tests/HostOrderReverseAuditGuardTests.cs` and `docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R7/host-order-reference-inventory.json`: reflect removal of `HostSellerOrderViewAccessReader.cs`.
- `src/backend/Modules/Order/Tooba.Order.Tests/Architecture/OrderSellerPanelArchitectureGuardTests.cs` and `OrderInfrastructureOrganizationGuardTests.cs`: point at the Order-owned reader path.
- `src/backend/Modules/Support/Tooba.Support.Tests/Architecture/SupportArchitectureGuardTests.cs`: assert `SellerSecurityErrorCodes.AuthorizationDenied` in `HostSupportSellerAuthorizer.cs`.

## 5. Recovery / SoT reconciliation

- `docs/architecture/tmar-current-state.json`: new `hostSellerAmcR1A` checkpoint.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`, `docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md`, `docs/ai/TOOBA-RECOVERY-CONTEXT.md`: current checkpoint = Seller R1A security-boundary repair; full Host/Seller remains OPEN; `workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R1A`; automatic next implementation task = NONE. Development closure lineage preserved as HISTORICAL.

## 6. Non-changes (explicit)

No route/verb/route-parameter change; no header change; no status/error-code value change; no DTO change; no schema/migration change; no frontend change; no Seller-R2 work; the 5 remaining Host/Seller business files untouched except where a moved type's namespace was consumed.
