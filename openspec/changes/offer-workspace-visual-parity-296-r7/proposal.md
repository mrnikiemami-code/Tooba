# Proposal — offer-workspace-visual-parity-296-r7

## Why
Owner visual review rejected TB-296-R6 output. Build success, tab count and HTTP 200 are not design acceptance. The approved OpenDesign 295 artifact is the visual authority; this change corrects the existing surface rather than designing a new one.

## What changes
- Offer lists (admin + seller) follow the Orders admin list standard: right-side heading/breadcrumb, left create action, product first in RTL, compact standard grid row actions at the logical end, seller name (admin), real product image/variant code when exposed, Jalali dates, Persian digits, understandable currency labels, standard badges/tokens. Shared grid infrastructure, Orders/Products/Campaigns pages remain untouched.
- Shared seven-tab workspace matches the approved artifact's header, sticky summary, numbered tab bar, card hierarchy, form density, control heights, spacing and mobile action bar using a scoped CSS module and the project font/registry.
- Content cleanup: no raw UUID in the primary user view, no internal vocabulary (Pricing/Inventory/Promotion/Contract/Stage-3), natural Persian copy; pricing displays Persian grouped amounts and never silently rewrites legacy decimals; inventory distinguishes empty/zero/error; shipping exposes only controls with a real save and participates in the unsaved guard; history/discounts show honest unavailable states.
- Backend read enrichment (additive, no new endpoint): seller display name, variant code, primary media id in offer lists; variant code, media id and fulfillment responsibility/shipping type in offer detail read.

## Impact
Frontend offer list pages, shared workspace + tabs + scoped CSS, Offer/Catalog read contracts; no migration, no live policy invention, no grid infrastructure change.
