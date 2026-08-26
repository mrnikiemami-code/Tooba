# 21 — PDP Data Map

Task: `TB-P05-T017`

```text
/products/{slug}
  → StorefrontShopeivaPdp
  → loadStorefrontDetail → GET /v1/storefront/products/{slug}
       Catalog (title, media, short/full description, attributes/specs, variants)
       Offer + Pricing + Inventory (primary + other sellers)
       Reviews aggregate (Published only)
       related products composition

Tabs (distinct bodies — no generic flatten):
  intro  → shortDescription + honest trust tiles
  full   → fullDescription
  specs  → specifications[] attribute cards
  reviews→ StorefrontPdpReviews → Reviews endpoints
  qa     → StorefrontPdpQa
           GET  /v1/storefront/products/{slug}/questions
           POST /v1/customer/product-questions
           schema product_qna (ProductQnA)
  bulk   → StorefrontPdpBulk
           POST /v1/storefront/products/{slug}/bulk-inquiries
           schema bulk_inquiry (BulkInquiry) — no price fields
```

Authority: Product ≠ Variant ≠ Offer ≠ Price ≠ Inventory. No Product.Price / Product.Stock.
