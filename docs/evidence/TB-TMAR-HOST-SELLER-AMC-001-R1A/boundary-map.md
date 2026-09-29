# Host/Seller — Seller-R1A — Boundary Map

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R1A
**Boundary:** `src/backend/Host/Tooba.Host/Security/Seller` — namespace `Tooba.Host.Security.Seller` (path↔namespace EXACT)

## 1. Dependency matrix (post-R1A)

| Boundary file | Allowed deps | Foreign App | Foreign Domain | Foreign Infra | Foreign Persistence | DbContext |
| --- | --- | --- | --- | --- | --- | --- |
| `SellerPanelAccess.cs` | BuildingBlocks, Security, ASP.NET, `SellerSecurityErrorCodes` | ZERO | ZERO | ZERO | ZERO | ZERO |
| `SellerSecurityErrorCodes.cs` | none (constants) | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostSellerPanelAccess.cs` | Security, Host panel seam | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostOfferSellerAuthorizer.cs` | Offer **Endpoints** port, panel seam | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostOrderSellerAuthorizer.cs` | Order **Endpoints** port, panel seam, `SellerSecurityErrorCodes` | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostReturnSellerAuthorizer.cs` | Returns **Endpoints** port, panel seam | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostSettlementSellerAuthorizer.cs` | Settlement **Endpoints** port, panel seam | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostNotificationSellerAuthorizer.cs` | Notification **Endpoints** port, panel seam | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostPromotionSellerAuthorizer.cs` | Promotion **Endpoints** port, panel seam | ZERO | ZERO | ZERO | ZERO | ZERO |
| `HostSupportSellerAuthorizer.cs` | Support **Endpoints** port, panel seam, `SellerSecurityErrorCodes` | ZERO | ZERO | ZERO | ZERO | ZERO |

`HostSellerOrderViewAccessReader.cs` → **ABSENT** from Host.

## 2. Moved / created surfaces outside the boundary

| Surface | Path | Namespace | Owner |
| --- | --- | --- | --- |
| Order view-access implementation | `Modules/Order/Tooba.Order.Infrastructure/Seller/SellerOrderViewAccessReader.cs` | `Tooba.Order.Infrastructure.Seller` | Order |
| Order DI registration | `Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs` | `Tooba.Order.Infrastructure` | Order |

## 3. Neutral seam

```
Host/Security/Seller adapters ──▶ Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader
                                        ▲
                                        └── implemented AccessControl-side (PlatformEffectiveAccessReader), registered in Program.cs
Order.Infrastructure.SellerOrderViewAccessReader ──▶ same neutral seam (intra-module, Order-owned)
```

No service locator. No foreign persistence. No cross-module EF join.

## 4. Allowed vs. forbidden reference summary

- **Allowed:** `Tooba.BuildingBlocks*`, ASP.NET/Host primitives, module **Endpoints** authorizer ports (Host acts as transport/security adapter), the Host-owned `SellerSecurityErrorCodes`.
- **Forbidden (now ZERO):** any foreign `Tooba.<Module>.Application`, `.Domain`, `.Infrastructure`, `.Persistence`; foreign `DbContext`/`DbSet`.

## 5. Sink-folder regression audit

| Destination | State | Verdict |
| --- | --- | --- |
| `Host/Development` | unchanged | NO_REGRESSION |
| `Host/Admin` | unchanged | NO_REGRESSION |
| Other closed Host folders | unchanged | NO_REGRESSION |
| New Host folders created | none | NO_REGRESSION |
| `Host/Seller` remaining business files | 5 files, unchanged except namespace consumption | NO_REGRESSION |
