# 06 — Promotion checkout integration (TB-P06-T020)

## Path

Order already evaluates `SubmitCheckoutCommand.CouponCode` via `IPromotionEvaluator`.

Storefront previously hard-nulled coupon in `StorefrontCheckoutComposer.BuildCommand`.

## Changes

1. `StorefrontSubmitCheckoutRequest` adds optional `CouponCode`
2. Preview accepts `couponCode` query param
3. `BuildCommand` passes trimmed coupon into `SubmitCheckoutCommand.CouponCode`
4. Frontend cart stores code in `sessionStorage` and calls preview/submit with `couponCode`
5. No Product.Price mutation; discount snapshots remain on Order lines

## Multi-seller cart semantics

Evaluation is **per line** with that line’s `SellerPartyId`. A seller-scoped coupon (`SellerPartyId` set on definition) applies only to matching seller lines; other sellers’ lines get no discount from that code.

## FE honesty

- Cart coupon field enabled; apply runs Host preview
- UI shows Host-reported `discountAmount` only — no client-side fake discount math
