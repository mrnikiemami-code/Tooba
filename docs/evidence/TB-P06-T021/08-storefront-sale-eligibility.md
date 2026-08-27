# 08 — Storefront sale eligibility (TB-P06-T021)

## Eligibility (current architecture)

A listing/PDP offer is purchasable only when composition finds **all** of:

1. **Catalog Product** status = Published (with Variant)
2. **Offer** status = Active (seller-owned listing)
3. **Pricing** Active tax-exclusive base price for market/channel/currency (defaults IR / Marketplace / IRR)
4. **Inventory** available units > 0 (`OnHand - Reserved`)

Saving a UI form alone does **not** make an item purchasable until the above are true.

## Explicit non-shortcuts

- No price/stock on Product identity
- Catalog publish ≠ Offer Active ≠ price Active ≠ stock available
- Advanced variant matrix remains deferred; default/empty-axis Variant is supported for demo

## Authoring path that reaches eligibility

1. Admin: `POST /v1/admin/products` (title/slug/category → Published + default Variant with one current axis option)
2. Seller: `POST /v1/seller/offers` with `status=Active`
3. Seller: PUT price → Active authored price
4. Seller: PUT inventory → onHand > 0
5. Storefront composes listing/PDP with `availableUnits > 0` purchasable hint

Variant note: Catalog requires ≥1 variant axis value (no advanced matrix redesign; reuses/creates a simple `default_option` axis when none exists).

## Seed path

Development bootstrap already seeds eligible Offers; panel writes now allow the same chain without DB mutation.
