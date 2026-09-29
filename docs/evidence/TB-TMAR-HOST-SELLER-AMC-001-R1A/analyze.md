# Host/Seller — Seller-R1A (foreign Application leakage repair) — Analyze

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R1A
**Parent task:** TB-TMAR-HOST-SELLER-AMC-001-R1 (`docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R1/`)
**Parent implementation commit:** `3c13e4bcbf2b0a32ef4a701b48e8782a41d6b51f`
**Skills:** `tooba-architecture-analyze` → `tooba-architecture-migrate` → `tooba-architecture-certify`
**Scope:** Repair ONLY the R1 foreign-Application leakage inside `Host/Security/Seller` and reconcile Recovery/SoT. Seller-R2 is **not** started. Routes, headers, status codes, DTOs, schema, and frontend are unchanged.

## 1. Why R1A exists (Architect blocker on R1)

R1 rehomed the Host seller platform security boundary to `Host/Security/Seller`, but the boundary still referenced **foreign Application layers**, which violates the strict Contracts-only / no foreign Application-Domain-Infrastructure-Persistence rule for a Host security boundary. R1 had recorded these three references as an "allowlisted residual" — R1A removes that allowance and drives the boundary to **ZERO**.

| # | Offending file | Foreign reference (pre-R1A) | Violation class |
| --- | --- | --- | --- |
| 1 | `Security/Seller/HostSellerOrderViewAccessReader.cs` | `Tooba.Order.Application.Seller.Ports` (+ implemented the Order Application port) | Host implementing a foreign Application port |
| 2 | `Security/Seller/HostOrderSellerAuthorizer.cs` | `Tooba.Order.Application.Seller.SellerOrderErrors` | Host consuming foreign Application error codes |
| 3 | `Security/Seller/HostSupportSellerAuthorizer.cs` | `Tooba.Support.Application.Errors.SupportErrorCodes` | Host consuming foreign Application error codes |

Because a Host security adapter is a transport/security boundary, it must not depend on any module's business **Application** layer. Persistence/Domain coupling was already absent.

## 2. Boundary inventory (10 files under `Host/Tooba.Host/Security/Seller`)

All 10 files carry the exact path-derived namespace `Tooba.Host.Security.Seller`.

| # | File | Post-R1A disposition |
| --- | --- | --- |
| 1 | `SellerPanelAccess.cs` | Host-owned gate; error codes now `SellerSecurityErrorCodes` |
| 2 | `SellerSecurityErrorCodes.cs` | **New** Host-boundary-owned stable code surface |
| 3 | `HostSellerPanelAccess.cs` | DI adapter, unchanged seam |
| 4 | `HostOfferSellerAuthorizer.cs` | unchanged (no foreign module layer) |
| 5 | `HostOrderSellerAuthorizer.cs` | foreign `SellerOrderErrors` removed → Host code |
| 6 | `HostReturnSellerAuthorizer.cs` | unchanged |
| 7 | `HostSettlementSellerAuthorizer.cs` | unchanged |
| 8 | `HostNotificationSellerAuthorizer.cs` | unchanged |
| 9 | `HostSupportSellerAuthorizer.cs` | foreign `SupportErrorCodes` removed → Host code |
| 10 | `HostPromotionSellerAuthorizer.cs` | unchanged |

`HostSellerOrderViewAccessReader.cs` was **deleted** from Host and re-implemented Order-owned (§3).

## 3. Architecture decision — Order view access

The Order Application port `Tooba.Order.Application.Seller.Ports.ISellerOrderViewAccessReader` is a **module-owned** port. Host must not implement it.

- Host implementation **deleted**: `Host/Security/Seller/HostSellerOrderViewAccessReader.cs` (ABSENT).
- Order-owned implementation **created**: `Modules/Order/Tooba.Order.Infrastructure/Seller/SellerOrderViewAccessReader.cs`, implementing the port intra-module and consuming only the neutral `Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader`.
- DI registration moved: `Program.cs` no longer registers the port; `OrderModule.cs` registers `Application.Seller.Ports.ISellerOrderViewAccessReader → SellerOrderViewAccessReader`.
- Host has no `Tooba.Order.Application.Seller.Ports` reference remaining.

## 4. Architecture decision — Host seller security error codes

Created `Security/Seller/SellerSecurityErrorCodes.cs` (`namespace Tooba.Host.Security.Seller`) holding only the shared Host security codes actually needed:

- `seller.actor.missing`
- `seller.identity.missing`
- `seller.authorization.denied`
- `seller.authorization.unavailable`

No module-specific business error codes are duplicated into Host.

## 5. Neutral seam preserved

`Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader` remains the single neutral platform effective-access seam (implemented AccessControl-side, registered in `Program.cs`). No service locator (`RequestServices`) exists in the boundary.

## 6. Behavior-preservation invariants (must hold after R1A)

- Seller routes/verbs unchanged.
- Headers unchanged: `X-Tooba-Seller-Party-Id`, `X-Tooba-Dev-Actor-User-Id`.
- Status codes unchanged: `401 seller.actor.missing`, `400 seller.identity.missing`, `403 seller.authorization.denied`, `503 seller.authorization.unavailable`.
- `order.view` semantics preserved: `DeniedByCeiling` exclusion, `GlobalWithinOwner` scope, category-scope projection, denied-snapshot semantics.
- No schema/migration change. No DTO change. No frontend change.
