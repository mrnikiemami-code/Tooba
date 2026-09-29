# TB-TMAR-HOST-SELLER-AMC-001-R4 — Route Ownership

## Seller route ownership after R4

| Route | Owner | Transport |
| --- | --- | --- |
| `GET /v1/seller/dashboard` | `Tooba.Order.Endpoints.Seller.SellerDashboardEndpoints` | Order Application query + `ApiResponseFactory` |
| `GET /v1/seller/dev-contexts` | `Tooba.Host.Seller.SellerPanelEndpoints` | Host Development surface (retained) |

## Ownership transition

| Slice | Before R4 owner | After R4 owner |
| --- | --- | --- |
| Dashboard route | Host (`SellerPanelEndpoints`) | Order (`SellerDashboardEndpoints`) |
| Dashboard metrics | Host composer | `Tooba.Order.Application.Seller.Queries.GetSellerOrderDashboardSummary` |
| Display-name enrichment | Host composer | `SellerOrderComposer.GetDashboardViewAsync` over `Party.Contracts/IPartyLookup` |

## Duplicate ownership

`duplicateRouteState = ZERO`. The dashboard route is mapped exactly once (Order);
`SellerDashboardEndpoints.Map` is registered exactly once in `OrderEndpointModule`.

## Counts

- Host-owned Seller routes: 2 -> 1.
- Host/Seller production files: 4 -> 2.

## Authorization

The Order-owned dashboard endpoint reuses the existing `IOrderSellerAuthorizer` port. No second
seller-auth mechanism was introduced. The Host `Host/Security/Seller` R1A boundary is unchanged.

## Sink-folder regression

`NONE`. The evacuated surface lives in the Order module; no dashboard file was left in (or created
under) `Host/Seller`.
