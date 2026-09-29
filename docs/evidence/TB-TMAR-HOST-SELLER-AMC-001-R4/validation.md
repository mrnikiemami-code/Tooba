# TB-TMAR-HOST-SELLER-AMC-001-R4 — Validation

## Focused build

| Project | Result |
| --- | --- |
| `Tooba.Order.Endpoints` | PASS — 0 errors |
| `Tooba.Host` | PASS — 0 errors |
| `Tooba.Host.Tests` | PASS — 0 errors |

Solution-wide tests were intentionally NOT run (focused validation only).

## Focused tests

| Filter / suite | Result |
| --- | --- |
| `HostSellerAmcR4GuardTests` (new) | PASS |
| `HostSellerAmcR3GuardTests` | PASS |
| `HostSellerAmcR2GuardTests` | PASS |
| `HostSellerAmcR1GuardTests` | PASS |
| `SellerPanelCompositionTests` | PASS |
| `SellerOfferSaleWriteTests` | PASS |
| `HostOrderReverseAuditGuardTests` | PASS |

Aggregate: `Passed! - Failed: 0, Passed: 47, Skipped: 0, Total: 47`.

## Durable guard — `HostSellerAmcR4GuardTests`

Proves:

- dashboard absent from Host;
- dashboard Order.Endpoints-owned (exactly one route mapping, `ISender`-only, `DbContext` ZERO);
- duplicate route ZERO (module registration exactly once);
- Order dashboard CQRS returns `Result<SellerDashboardView>` with `ActiveOffers: 0`;
- Party boundary = Contracts only (`IPartyLookup`; no `Party.Application`, no `PartyDbContext`);
- `SellerPanelComposer` absent; `SellerPanelModels` absent;
- Host Seller route count = 1; Host/Seller file count = 2;
- Host/Seller dashboard layer leakage ZERO; Host `Order.Application` = ZERO; Host `Party.Application` = ZERO;
- R1A panel gate, R2 Catalog, R3 Party settings and `HostOrderSellerAuthorizer` invariants preserved;
- no sink-folder regression.

## Behavior parity

| Aspect | State |
| --- | --- |
| Path + verb | `GET /v1/seller/dashboard` preserved |
| Seller auth | Reuses `IOrderSellerAuthorizer`; unchanged semantics |
| `seller.missing` | Preserved (404) for unknown seller |
| Response fields | `sellerPartyId`, `sellerDisplayName`, `activeOffers`, `openOrders`, `paidOrders` |
| `activeOffers` | Fixed `0` |
| Status/error semantics | Unchanged canonical `Result` → `ApiResponseFactory` |
| Schema | No change |
| Frontend | Unchanged |

## Boundary checks

- Order Endpoints: `ISender`-only, `DbContext` ZERO, no `PlatformHttpException`, no `Results.Json`,
  no raw `ex.Message`.
- Host/Seller: `Order.Application` = ZERO, `Party.Application` = ZERO, dashboard layer leakage = ZERO.
- `SellerDevActorBootstrap.cs` retains its legitimate Development Party/Identity seams (out of scope).
