# 19 — Real product sale E2E (TB-P06-T021)

Proven via Host APIs (no direct DB mutation). Artifact: `e2e-sale-api.json`.

## Flow

1. Admin `POST /v1/admin/products` → Published Catalog Product + default Variant  
2. Seller `POST /v1/seller/offers` + tax coverage auto-assign  
3. Seller `PUT .../price` (Pricing) + `PUT .../inventory` (Inventory)  
4. Storefront listing discovers slug; PDP resolves Offer  
5. Guest cart add line → checkout → PendingPayment  
6. Sandbox payment → **Succeeded**  
7. Seller order detail → **Paid** (`TB-20260827094827-01-8fb91c`)

## Ownership

Admin Catalog Product + Seller Offer (Catalog RO on seller UI).

## Claims

`SELLABLE_PRODUCT_FLOW_LIVE` (demo/sandbox)  
Not `PRODUCTION_GO_LIVE_READY`
