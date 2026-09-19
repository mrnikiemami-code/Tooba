# TB-P10-T022-R15 — Quantity Ownership

## Decision: reuse Offer + Inventory; no campaign allocation fields

`MerchandisingCampaignOffer` stores only:

- CampaignId
- SellerOfferId
- SortOrder
- CreatedAt

No `AllocationQty`, `SoldCount`, or `sold_percentage`.

## Canonical owners

| Concern | Owner |
| --- | --- |
| Per-order min/max | `SellerOffer.MinimumOrderQuantity` / `MaximumOrderQuantity` |
| On-hand / reserved / available | `StockPosition` |
| Sold percentage (if ever shown) | Derived at read time from allocation + sales — not stored |

## Rationale

R14: campaign-specific allocation only if semantically distinct. Offer qty limits and Inventory already cover order and stock truth. Duplicating them on membership would diverge.
