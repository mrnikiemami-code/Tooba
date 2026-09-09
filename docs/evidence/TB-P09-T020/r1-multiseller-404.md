# R1 Multi-Seller 404 — TB-P09-T020-R1

Storefront `cart.missing` 404 is a catch-all for any exception containing `پیدا نشد` (including `Offer از قرارداد Lookup پیدا نشد` and `سیاست مقدار گونه از Catalog پیدا نشد`). Not a cart or fulfillment regression.

| Probe | Result |
| --- | --- |
| B_OFFER fixture `01a03826-a21a-7000-ac2a-b44156838b10` | 404 `cart.missing` / سبد پیدا نشد |
| Storefront product listing | 200; listing offer `01a030d1-40f1-7000-95f6-b8efc58e2619` (Arman `01a030d1-40cb-7000-8abe-6d31739956c5`) |
| Catalog products | Draft/Archived; storefront rails empty |
| Other-seller Active offers | almost all orphaned `catalog_variant_id` (no `catalog.variants` row) |

Fixture repair (data, not a second fulfillment path): Arman offer `catalog_variant_id` set to live KG variant `01a05387-fcf1-7000-8974-16c163961338`. Listing add then 200. KG + Arman two-line cart succeeded.
