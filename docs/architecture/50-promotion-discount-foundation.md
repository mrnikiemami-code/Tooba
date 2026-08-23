# Tooba — Promotion & Discount Foundation

Status:

```text
IN_PROGRESS — TB-P03-T009 awaiting Architect ACCEPT
```

Task:

```text
TB-P03-T009
```

## Explicit statements

```text
Promotion != Base Price
Promotion does not rewrite Pricing truth
Commercial order = Pricing → Promotion → Tax → Order Snapshot
```

Promotion is a conditional commercial adjustment. It consumes an authored tax-exclusive Pricing result. It does not become the price book. It does not calculate tax. It does not capture payment.

P00 document `docs/architecture/22-promotion-discount.md` remains the broader architecture. This foundation implements a bounded first slice.

## Aggregate

`PromotionDefinition` owns identity, status, priority, effective dating, stacking policy, a typed discount action (`PercentageOff` or `FixedAmountOff`), optional coupon, and optional eligibility selectors.

Selectors are stored as opaque identifiers/strings (`OfferId`, `CatalogVariantId`, `CategoryId`, `SellerPartyId`, Market, SalesChannel, Currency, customer/org, minimum quantity/subtotal). Promotion never queries Pricing/Order/Catalog DbContexts.

## Discount actions

Percentage uses decimal rates in `(0, 1]` and `PromotionRounding` (same scale rules as tax: IRR/JPY/KRW 0 dp, otherwise 2, AwayFromZero).

Fixed amount requires Currency. A mismatched currency is a rejection (`CURRENCY_MISMATCH`), not a silent convert.

Discount is clamped so the tax-exclusive remainder never goes negative.

## Coupon seam

Optional normalized code (trim + invariant upper + whitespace collapsed). Possession of a code is not authorization; eligibility and dating still apply. Issuance quotas and concurrent redemption counts are deferred via `IPromotionRedemptionLedger` (currently always allows redeem).

## Stacking

Deterministic order: `Priority` descending, then `PromotionId` ascending. Database row order is irrelevant.

If any Exclusive candidate matches, only the first Exclusive after that sort is applied. Otherwise all matching Stackable promotions apply in that order.

## Evaluation

`IPromotionEvaluator` / `IPromotionDirectory.EvaluateAsync` returns applied promotions, discount amount, post-discount tax-exclusive amount, and rejection reasons.

Checkout re-evaluates at submit. Cart quotes are not source of truth. If `QuotedDiscountAmount` is supplied and differs, checkout fails with `PROMOTION_CHANGED`.

Tax receives `PostDiscountTaxExclusiveAmount` with quantity 1 so VAT/GST math uses the discounted exclusive base.

Order lines persist immutable promotion snapshots. Later expire/change of the live Promotion row does not rewrite those snapshots or `GrandTotalSnapshot`.

## Tenant / edition

Marketplace data lives in the marketplace database; SingleStore data in the tenant database, via existing commerce connection resolution. No Host parsing inside Promotion. Tenant A rows cannot be read from Tenant B's database.

## Authorization / events

No final admin permission matrix. `IPromotionUseCaseGuard` is the existing open seam.

Events: `promotion.created.v1`, `promotion.activated.v1`, `promotion.changed.v1`, `promotion.expired.v1`. No MassTransit/Authzed types in Domain or Application.

## Out of this task

Loyalty, gift cards, campaign UI, affiliate, personalization, real payment/PSP, T010, P03 Gate.
