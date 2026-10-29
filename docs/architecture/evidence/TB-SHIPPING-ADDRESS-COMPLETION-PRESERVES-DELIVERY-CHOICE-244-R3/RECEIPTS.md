# RECEIPTS — TB-SHIPPING-ADDRESS-COMPLETION-PRESERVES-DELIVERY-CHOICE-244-R3

## A. Claim / base

- Bridge row: `ae45a987-b507-44fb-b2fc-2ce6b493046b` (channel tooba-main)
- Expected-Base / Base: `e9495b0388dae6e878cb8349bb58e25be8a69e18`
- Parent: TB-SHIPPING-USER-ENVIRONMENT-AUTHORITATIVE-APPROVED-DATA-DELIVERY-244-R2

## B. Source fix

1. `reconcileCapturedChoicesAgainstPreview(prev, capturedPreview)` — for each groupKey, keep choice iff
   methodId matches and date exists and (no slots ⇒ slot ticks null/ok) or ticks ∈ that day's slots.
2. Context-key effect: `setCapturedChoices(prev => reconcile…)` instead of `setCapturedChoices({})`.
3. Tests cover: recipient/contact/postal/notes not in context key; retention across reconcile;
   invalid-slot prune; Continue blockers without wipe; dest/coupon/cart still change key.

## C. Focused verification

```
npx vitest run app/storefront/storefront-shipping-captured-selection.test.ts
  app/storefront/storefront-shipping-continue-state.test.ts
  app/storefront/storefront-shipping-destination-refresh.test.ts
→ 41 passed
npm run typecheck → 0 errors
```

## D. Live canonical cart (FE3000 / Host5088 / tooba_alpha)

- Buyer `01a0996c-b8d9-7000-8f5e-…` (session from prior R2), cart `01a17b2d-e219-7000-b3b2-5bcdbe6810c5`
- After first/last/mobile/addressLine/postal/notes: `__date0`/`__date1` and `__slot0`/`__slot1` = 14:00–15:00;
  `__projCount=0` (no destination re-projection from those fields)
- After Geography province+city Tehran: one pending refresh; choices restored; Continue enabled
- Continue → `/fa/payment?cartId=01a17b2d-e219-7000-b3b2-5bcdbe6810c5`
- Payment UI: shipping 250,000; payable 81,661,464.16; «این سفارش هنوز ثبت نشده است»
- DB: cart Active v48; checkouts 0; draft selection_revision 3 shipping 250000;
  2 groups accepted_receiving_date 2026-11-02 window 14:00-15:00 freight 120000 each

## E. Runtime

- Host 5088 PID 30328 (tb244r2-host; gates StorefrontCapturedPreview ON local)
- FE 3000 PID 28396 (src/frontend)
- No Order/payment submission; no merchant data rewrite beyond R2 approved config
