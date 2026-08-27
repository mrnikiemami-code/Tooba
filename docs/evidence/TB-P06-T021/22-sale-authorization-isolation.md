# 22 — Sale authorization / isolation (TB-P06-T021)

## Tests & code

Primary: `src/backend/Host/Tooba.Host.Tests/SellerOfferSaleWriteTests.cs`  
Access: `SellerPanelAccess.RequireAuthorizedAsync` on all `/v1/seller/offers*` writes  
Create: `SellerPartyId` **only** from authorized context — never from body (`05-seller-offer-flow.md`)

## Cases

| Case | Expected | Evidence |
|---|---|---|
| Seller A own Offer create / price / inventory | Allow | `SellerOfferSaleWriteTests` own write allow |
| Seller B mutate Seller A Offer (PATCH/price/inventory) | Deny (404 `seller.offer.missing` or access denied) | Foreign deny assertions; owner quantities unchanged |
| Seller pricing scope | Own OfferId only | `RequireOwnedOfferAsync` before `IPriceDirectory` |
| Seller inventory scope | Own OfferId only | `RequireOwnedOfferAsync` before `IInventoryDirectory` |
| Seller order scope | Own `SellerPartyId` orders only | Existing `SellerPanelComposer` order isolation |
| Customer own order | Allow | Customer panel Host scope |
| Customer foreign order | Denied | Customer order get fail-closed |
| Admin authorized access | Allow with `AdminPanelAccess` | Admin products/orders/fulfillments |
| Tenant isolation | Cross-tenant deny | Platform tenant context |
| Single-Store behavior | Remains valid | Settlement N/A flags unchanged |

## Host route registration proof

`Host_registers_seller_offer_price_inventory_write_routes` asserts:

- `MapPost("/offers"`
- `/offers/{offerId}/price`
- `/offers/{offerId}/inventory`
- `RequireAuthorizedAsync`
- Admin `MapPost("/")` on `/v1/admin/products`

## Verdict

```text
SALE_AUTHORIZATION_ISOLATION = PROVEN (tests + access gates)
```
