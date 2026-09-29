# TB-TMAR-HOST-SELLER-AMC-001-R4 — Migration

## Scope

Evacuate the seller dashboard HTTP surface (`GET /v1/seller/dashboard`) and its presentation
composition from `Host/Seller` into Order, and delete the now zero-consumer dashboard residue.

Evacuated:

- `GET /v1/seller/dashboard` route;
- `SellerDashboardSummary` view;
- `SellerPanelComposer` display-name composition;
- Host dependency on `Order.Application` dashboard query;
- Host dependency on `Party.Application` for dashboard display.

Untouched: `dev-contexts`, `SellerDevActorBootstrap`, frontend, R5 work.

## Accepted baseline

- Latest accepted implementation: `TB-TMAR-HOST-SELLER-AMC-001-R3`
- Commit: `5ab8bdc7f5e75c599313d8b444d18a4f6c80ffdf`
- Host/Seller files = 4
- Host-owned Seller routes = 2 (`GET /v1/seller/dashboard`, `GET /v1/seller/dev-contexts`)

## Pipeline

```text
HTTP -> ISender -> Order Application query/handler
     -> Order-owned abstractions + Party.Contracts -> ApiResponseFactory
```

## Order Endpoints (transport)

- New: `src/backend/Modules/Order/Tooba.Order.Endpoints/Seller/SellerDashboardEndpoints.cs`
  - `MapGroup("/v1/seller")` + `MapGet("/dashboard", GetAsync)`.
  - `ISender`-only MediatR dispatch; `ApiResponseFactory` for the canonical `Result` envelope.
  - Reuses the existing `IOrderSellerAuthorizer` port (`auth.ResolveAsync`) for seller auth.
  - Zero `DbContext`, zero foreign Application, zero `Results.Json`, zero `ex.Message`.
- Registered once in `OrderEndpointModule.MapOrderEndpoints` (`SellerDashboardEndpoints.Map(app);`).

## Order Application (CQRS + composition)

- `SellerOrderModels.cs`: added `SellerDashboardView(Guid SellerPartyId, string SellerDisplayName, int ActiveOffers, int OpenOrders, int PaidOrders)`.
- `SellerOrderComposer.cs`: added `GetDashboardViewAsync(sellerPartyId, actorUserId, ct)`.
  - Resolves the seller via `IPartyLookup.FindByIdAsync` (Party.Contracts only).
  - Missing seller -> `SellerOrderErrors.SellerMissing` (`seller.missing`).
  - Reuses `GetDashboardSummaryAsync` for the Order-owned `openOrders` / `paidOrders` counts.
  - `ActiveOffers` is fixed `0` (Offer ownership is outside this wave).
- `GetSellerOrderDashboardSummaryQuery.cs`: query + handler retargeted to `Result<SellerDashboardView>`.

## Host final state

- `SellerPanelEndpoints.cs`: dashboard route and all dashboard helper methods removed; only
  `GET /v1/seller/dev-contexts` remains.
- `Program.cs`: `Tooba.Host.Seller.SellerPanelComposer` DI registration removed.
- DELETED (zero-consumer, not relocated):
  - `Host/Seller/SellerPanelComposer.cs`
  - `Host/Seller/SellerPanelModels.cs` (including the six Offer DTO global aliases)
- `Host.Tests/Baselines/tmar-host-write-files.json`: `Seller/SellerPanelComposer.cs` entry removed.

## Post-migration counts

| Metric | Before | After |
| --- | --- | --- |
| Host/Seller production files | 4 | 2 |
| Host-owned Seller routes | 2 | 1 |

Remaining Host/Seller files: `SellerPanelEndpoints.cs`, `SellerDevActorBootstrap.cs`.
Remaining Host-owned route: `GET /v1/seller/dev-contexts`.

## Boundaries

- Order Application consumes `Party.Contracts` only (`IPartyLookup`); no `Party.Application`,
  no foreign `DbContext`.
- Host/Seller has ZERO dashboard layer leakage, ZERO `Order.Application`, ZERO `Party.Application`.
- No schema change, no frontend change, no route-sink-folder regression.
