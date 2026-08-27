# 04 — Promotion domain boundary (TB-P06-T020)

## Owner

**EXTEND** existing `Tooba.Promotion` — no new Coupon module.

| Layer | Path |
|---|---|
| Domain | `src/backend/Modules/Promotion/Tooba.Promotion.Domain/PromotionDomain.cs` |
| Contracts | `src/backend/Modules/Promotion/Tooba.Promotion.Application/PromotionContracts.cs` |
| Infrastructure | `src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/PromotionDirectory.cs` |

## Concepts used

- `PromotionDefinition` with optional `CouponCode`, `SellerPartyId`, dates, discount kind (percentage / fixed), `MinimumSubtotal`
- Status: `Draft` → `Active` → `Expired` (deactivate = Expire)
- Money via existing decimal + `PromotionRounding` (no float shortcuts)
- **No** `Product.Price` mutation; Pricing remains authored-base only
- Evaluation still via `IPromotionEvaluator` consumed by Order checkout

## Seller-scoped API (directory)

Added on `IPromotionDirectory`:

- `ListBySellerAsync` / `GetForSellerAsync`
- `CreateForSellerAsync` (forces `SellerPartyId`)
- `UpdateForSellerAsync` (Draft/Expired only via `UpdateEditableFields`)
- `ActivateForSellerAsync` / `DeactivateForSellerAsync`
- Admin: `ListForAdminAsync` (optional seller filter), `GetForAdminAsync`, `DeactivateForAdminAsync`

Foreign seller mutations throw; foreign get returns null.

## Explicit non-goals

- Redemption ledger / max-uses enforcement (still deferred stub)
- Embedding coupons into Offer or Catalog schema
