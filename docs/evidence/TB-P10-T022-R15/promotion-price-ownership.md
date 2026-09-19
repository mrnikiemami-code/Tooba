# TB-P10-T022-R15 — Promotion Price Ownership

## Decision: DEFER storage (choice B deferred)

Campaign promotional override price is **not stored** in this foundation.

## Why not AuthoredPrice extension (choice A)

- `AuthoredPrice.QualifierKind` is currently **Base-only** (`PriceQualifierKind.Base`).
- Unique index includes `QualifierKind` but there is no Campaign qualifier and no campaign membership key.
- Extending QualifierKind without a full Pricing redesign would force ambiguous campaign-scoped rows or overload Base.

## Why not scalar PromoAmount

- Tooba pricing is multi-dimensional (Market / Channel / Currency).
- Currency-ambiguous scalar `PromoAmount` is forbidden (LOCK-SF-397).

## Extension point retained

- Domain documents the deferral on `IMerchandisingCampaignPromoPrice` (empty reserved interface).
- Future child table must align to the same canonical pricing dimensions when implemented.
- Until then, list/base price remains `AuthoredPrice`; selling override is out of scope.

## Unchanged

- `AuthoredPrice` table/columns
- `PriceQualifierKind`
- Checkout `PromotionDefinition` fixed/percentage discount fields
