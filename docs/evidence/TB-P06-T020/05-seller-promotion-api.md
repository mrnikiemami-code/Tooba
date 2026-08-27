# 05 — Seller / Admin promotion HTTP API (TB-P06-T020)

## Seller (`SellerPanelAccess` fail-closed)

| Method | Path |
|---|---|
| GET | `/v1/seller/promotions` |
| POST | `/v1/seller/promotions` |
| GET | `/v1/seller/promotions/{id}` |
| PUT | `/v1/seller/promotions/{id}` |
| POST | `/v1/seller/promotions/{id}/activate` |
| POST | `/v1/seller/promotions/{id}/deactivate` |

Host files:

- `src/backend/Host/Tooba.Host/Promotion/PromotionEndpoints.cs`
- `src/backend/Host/Tooba.Host/Promotion/PromotionPanelComposer.cs`
- Registered: `Program.cs` → `MapPromotionEndpoints()`

Create body (`UpsertSellerPromotionBody`): name, couponCode, discountKind, discountValue, effectiveFrom/To, currency, minimumSubtotal.

## Admin (`AdminPanelAccess` fail-closed)

| Method | Path |
|---|---|
| GET | `/v1/admin/promotions` (`?sellerPartyId=` optional) |
| GET | `/v1/admin/promotions/{id}` |
| POST | `/v1/admin/promotions/{id}/deactivate` |

## Authorization

- Seller: SpiceDB `user → party#view` via `SellerPanelAccess.RequireAuthorizedAsync`
- Ownership: directory enforces `SellerPartyId` match; foreign → 404 / mutation reject
- Admin: `AdminPanelAccess.RequireAuthorizedAsync` for list/get/deactivate oversight
