# 56 — Storefront live slice (Home / Listing / PDP)

Status: IN_PROGRESS (TB-P04-T008 REPAIR; TB-P04-T007 Architect ACCEPTED)

This slice mounts purchased Shopeiva chrome (header, mega menu, cards, home merchandising, PDP 3-column, footer) onto live Tooba Host composition. Frontend visual reuse is not domain-model reuse. Accent token is Tooba blue; semantic success/warning/danger stay.

## Routes

- `/` Home
- `/products` listing (query `q`, `categoryId`)
- `/products/{slug}` PDP

## Host composition

`StorefrontComposer` queries Catalog, Offer, Pricing, Inventory, Tax, and Party separately. No cross-schema SQL JOIN. Public contracts live under `/v1/storefront/*`.

Displayed amount comes from `AuthoredPrice` on an Offer. Availability is the sum of `OnHand - Reserved` on Inventory positions for that Offer. Seller names come from Party lookup. Tax is a classification label, not a stored Product tax amount.

## Primary Offer rule (temporary)

Until a canonical Buy Box exists, Host selects one display Offer with:

1. Active Offer + currently valid Active price
2. Prefer `available > 0`
3. Then lowest tax-exclusive amount
4. Then `OfferId`

The UI must not pick the first table row itself.

## Presentation media

Catalog stores opaque `MediaAssetId` values only. `/v1/storefront/media/{id}` returns a development SVG. That URL is not Product business truth.

## Cart

Guest cart HTTP is public under `/v1/storefront/cart*`. `CartMutationEnabled` is true. Line identity is OfferId. Presentation (title, seller, image) is composed in Host from separate Catalog/Party reads. Totals are tax-exclusive cart estimates from Pricing quotes, not Checkout snapshots.

Routes:

- `/cart` (noindex)
- `/checkout` (noindex shell; no fake payment success)


## SEO baseline

Home/listing/PDP send `title`, `description`, canonical on PDP, indexable robots, semantic headings, and a Product JSON-LD seam from the resolved Offer.

## Theme

Central `--color-primary` shifted from template teal to professional blue. Success/warning/danger tokens stay semantic. Layout was not redesigned for the color change.
