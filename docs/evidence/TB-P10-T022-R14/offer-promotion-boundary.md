# Offer vs Promotion Boundary

## What `Offer` (`SellerOffer`) is

- Seller’s **commercial listing** on a Catalog Variant + Channel.
- Owns commercial status, seller SKU, return policy, order qty limits.
- **Does not own price or stock** (explicit domain comments + separate modules).

## What `Promotion` (`PromotionDefinition`) is

- **Checkout discount evaluator** (percentage/fixed, stacking, coupons, eligibility filters).
- Optional single `OfferId` / category / seller filters — not a merchandising rail membership list.
- Explicitly does **not** replace authored Pricing / tax.

## Answers

| Question | Answer |
|---|---|
| Is Offer the seller sellable record? | **YES** |
| Does Offer own price? | **NO** — `AuthoredPrice` does |
| Does Offer own seller + variant + stock context? | Seller+variant **YES**; stock via Inventory keyed by OfferId |
| Can Offer participate in multiple promotions over time? | Checkout promotions: multiple definitions may match over time. Merchandising campaigns: **should allow** multi-campaign history via membership rows (not present yet). |
| Are discounts attached to Offer? | Checkout discounts via Promotion filters; no Offer discount columns |
| Is there a campaign membership model? | **NO** today |

## Boundary rule for Amazing

Introduce a **Merchandising / PromotionCampaign** concept **separate from** checkout `PromotionDefinition`. Reuse Offer + Pricing + Inventory truths; do not overload `promotion.promotions` rows as Digikala-style product rails (wrong semantics: coupons, stacking, single OfferId filter).
