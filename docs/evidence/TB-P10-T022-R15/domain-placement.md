# TB-P10-T022-R15 — Domain Placement

## Decision

Place merchandising campaign foundation **inside the existing Promotion module** (`schema promotion`), as a **separate write model** from checkout `PromotionDefinition`.

## Why Promotion module

- R14 recommended a Merchandising Campaign aggregate distinct from checkout discounts.
- Promotion module already owns schema `promotion` and checkout promotion definitions.
- Avoids a new top-level module while keeping clear conceptual separation via distinct entities + `IMerchandisingCampaignDirectory`.

## Explicit non-placements

| Candidate | Rejected because |
| --- | --- |
| Checkout `PromotionDefinition` | Coupon/stacking/discount evaluator — not Storefront rails |
| `SellerOffer` flags (`IsAmazing`) | Boolean explosion; Offer remains listing identity only |
| Pricing / `AuthoredPrice` | Canonical base price; no safe campaign qualifier yet |
| Inventory / `StockPosition` | Canonical stock; campaigns must not duplicate |
| New top-level module | Unnecessary; Promotion schema already owns promo concerns |
| PageComposition / Builder | Integration deferred; no Builder source this task |

## Boundary

- `IPromotionDirectory` — checkout discount create/evaluate (unchanged path)
- `IMerchandisingCampaignDirectory` — campaign CRUD, membership, resolve active/members
