# TB-P10-T022-R15 — AntiPattern Scan

| Anti-pattern | Status |
| --- | --- |
| Offer.IsAmazing | ABSENT |
| IsFlash/IsFeatured/IsClearance on Offer | ABSENT |
| Persisted PromotionType enum as business identity | ABSENT (table + Code) |
| Checkout PromotionDefinition as merchandising rails | ABSENT (separate entities) |
| Duplicate base price truth | ABSENT (promo price deferred) |
| Duplicate inventory truth | ABSENT |
| Scalar PromoAmount | ABSENT |
| Stored countdown timer | ABSENT |
| Stored discount percent when derivable | ABSENT |
| Stored sold percentage | ABSENT |
| Per-second activation job | ABSENT |
| Hardcoded FA in domain entity defaults | ABSENT (seed display names in directory method only) |
| Section-owned locale | ABSENT |
| Builder UI changes | ABSENT |
| Storefront changes | ABSENT |
| Frontend/Admin changes | ABSENT |
| New top-level module | ABSENT |
| User-work overwrite / stash destroy | ABSENT |
| P11 work | ABSENT |

## Positive controls

- `MerchandisingPromotionType.AmazingCode = "AMAZING"`
- System rename/delete protection methods
- Store scope check on `AddOfferAsync(expectedStoreId)`
- `IMerchandisingCampaignPromoPrice` extension stub documents deferral
