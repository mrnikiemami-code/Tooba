# Host/Seller — Seller-R1 — Migration

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R1
**Slice:** Seller-R1 — Host seller platform security boundary rehome.

## 1. Final physical tree (post-R1)

`src/backend/Host/Tooba.Host/Security/Seller/` (10 files, namespace `Tooba.Host.Security.Seller`):

| File | LOC | Responsibility |
| --- | --- | --- |
| `SellerPanelAccess.cs` | 104 | Host platform seller authorization core (Actor resolve + `user → party#view` gate) |
| `HostSellerPanelAccess.cs` | 22 | `ISellerPanelAccess` adapter resolving dependencies from DI |
| `HostSellerOrderViewAccessReader.cs` | 41 | `ISellerOrderViewAccessReader` adapter over neutral `IPlatformEffectiveAccessReader` |
| `HostSupportSellerAuthorizer.cs` | 47 | `ISupportSellerAuthorizer` adapter; capability check via neutral seam |
| `HostOfferSellerAuthorizer.cs` | 17 | `IOfferSellerAuthorizer` adapter |
| `HostOrderSellerAuthorizer.cs` | 26 | `IOrderSellerAuthorizer` adapter |
| `HostReturnSellerAuthorizer.cs` | 14 | `IReturnSellerAuthorizer` adapter |
| `HostSettlementSellerAuthorizer.cs` | 15 | `ISettlementSellerAuthorizer` adapter |
| `HostNotificationSellerAuthorizer.cs` | 14 | `INotificationSellerAuthorizer` adapter |
| `HostPromotionSellerAuthorizer.cs` | 15 | `IPromotionSellerAuthorizer` adapter |

`src/backend/Host/Tooba.Host/Seller/` (5 files remain — deferred to R2–R6):

- `SellerPanelEndpoints.cs`, `SellerSettingsEndpoints.cs`, `SellerPanelComposer.cs`, `SellerPanelModels.cs`, `SellerDevActorBootstrap.cs`.

No file is deleted outside the moved set; no new Host folder introduced; no sink folder used.

## 2. Path ↔ namespace equality

Every file under `Security/Seller/` declares `namespace Tooba.Host.Security.Seller;` — path-derived namespace is exact. No alias workaround, no `TypeForwardedTo`.

## 3. Dependency-seam replacement

| File | Before | After |
| --- | --- | --- |
| `HostSupportSellerAuthorizer.cs` | `IAccessControlDirectory` (AccessControl.Application) + `AccessControl.Domain` scopes | `IPlatformEffectiveAccessReader.GetEffectivePermissionsAsync(actor, PlatformAccessOwnerKind.Seller, sellerPartyId)` + `PlatformAccessScopeKind.GlobalWithinOwner` + `DeniedByCeiling` |
| `HostSellerOrderViewAccessReader.cs` | `IAccessControlDirectory` + `AccessControl.Domain` scopes | same neutral seam; `order.view` global vs `PlatformAccessScopeKind.Category` projection |
| `SellerPanelAccess.cs` | service-locator-resolved dependencies | `IAuthorizationGuard` + `CurrentAuthenticatedSession` + `IHostEnvironment` resolved via DI |

`HostSellerPanelAccess` is registered as the neutral `Tooba.BuildingBlocks.Security.ISellerPanelAccess` port; the thin module adapters consume `ISellerPanelAccess` and (Support/Order) `IPlatformEffectiveAccessReader`.

## 4. Registration map (`Program.cs`)

All nine Host seller adapters are registered under their module-owned Endpoints ports from the new boundary namespace:

```
Tooba.BuildingBlocks.Security.ISellerPanelAccess               -> Tooba.Host.Security.Seller.HostSellerPanelAccess
Tooba.Promotion.Endpoints.Seller.IPromotionSellerAuthorizer    -> Tooba.Host.Security.Seller.HostPromotionSellerAuthorizer
Tooba.Offer.Endpoints.Seller.IOfferSellerAuthorizer            -> Tooba.Host.Security.Seller.HostOfferSellerAuthorizer
Tooba.Settlement.Endpoints.Seller.ISettlementSellerAuthorizer  -> Tooba.Host.Security.Seller.HostSettlementSellerAuthorizer
Tooba.Order.Endpoints.Seller.IOrderSellerAuthorizer            -> Tooba.Host.Security.Seller.HostOrderSellerAuthorizer
Tooba.Order.Application.Seller.Ports.ISellerOrderViewAccessReader -> Tooba.Host.Security.Seller.HostSellerOrderViewAccessReader
Tooba.Returns.Endpoints.Seller.IReturnSellerAuthorizer         -> Tooba.Host.Security.Seller.HostReturnSellerAuthorizer
Tooba.Notification.Endpoints.Seller.INotificationSellerAuthorizer -> Tooba.Host.Security.Seller.HostNotificationSellerAuthorizer
Tooba.Support.Endpoints.Seller.ISupportSellerAuthorizer        -> Tooba.Host.Security.Seller.HostSupportSellerAuthorizer
```

`using Tooba.Host.Seller;` remains in `Program.cs` alongside `using Tooba.Host.Security.Seller;` because the still-Host-owned seller business endpoints (`MapSellerPanelEndpoints`) live in `Host/Seller` until R2–R6.

## 5. Consumer updates

| Consumer | Change |
| --- | --- |
| `Story/StoryEndpoints.cs` | `using Tooba.Host.Seller;` → `using Tooba.Host.Security.Seller;` |
| `Reviews/ReviewEndpoints.cs` | same |
| `Seller/SellerPanelEndpoints.cs` | added `using Tooba.Host.Security.Seller;` |
| `Seller/SellerSettingsEndpoints.cs` | added `using Tooba.Host.Security.Seller;` |

No route body, handler, or response shape was changed in any consumer.

## 6. Durable guard (new)

`src/backend/Host/Tooba.Host.Tests/Architecture/HostSellerAmcR1GuardTests.cs` locks:

1. boundary files exist under `Security/Seller` and not under `Seller/`;
2. no foreign module `Application`/`Domain` reference in the boundary (explicit allowlist for module-owned port namespaces);
3. panel gate resolves dependencies from DI, not the service locator; stable codes preserved;
4. thin adapters contain no `RequestServices`, no `Tooba.AccessControl`, no `DbContext`; Support/Order adapters consume the neutral seam;
5. `Program.cs` registers the boundary and contains no stale `Host/Seller/Host*` implementation;
6. no foreign seller DTO `global using` alias remains in Host production.

## 7. Updated existing guards (moved-path alignment)

Module architecture guards were updated to the new physical path only (no assertion weakening):

- `Tooba.Settlement.Tests/Architecture/SettlementArchitectureGuardTests.cs`
- `Tooba.Support.Tests/Architecture/SupportArchitectureGuardTests.cs`
- `Tooba.Returns.Tests/Architecture/ReturnsArchitectureGuardTests.cs`
- `Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs`
- `Tooba.Notification.Tests/Architecture/NotificationArchitectureGuardTests.cs`
- `Tooba.Host.Tests/HostOrderReverseAuditGuardTests.cs` + `docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R7/host-order-reference-inventory.json`

Pre-existing path drift unrelated to R1 but blocking these focused guards was corrected in the same pass (the real current paths are `Admin/Access/Authorizers/...` and `Admin/Panel/AdminPanelComposer.cs`); no assertion was weakened.
