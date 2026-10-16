# SHIPPING ONE-LEVEL UI — TB-137 (Bounded grouped payment summary display closure)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-137` (Parent `TB-SHIPPING-ONE-LEVEL-TABS-136`)
Mode: `BOUNDED_GROUPED_PAYMENT_SUMMARY_DISPLAY_CLOSURE`
Stop-State: `STOP_FOR_ARCHITECT`
Base SHA: `5c32619bec45fe1adafc50c000c820be696e8d35`

## Defect being closed (customer-visible, exact)

On a genuine multi-group checkout projection (one seller, two products, two distinct per-line
shipping methods), `/fa/payment` rendered:

```
روش ارسال:            <-- empty value
هزینه ارسال: ۳۸۰٬۰۰۰ ریال
قابل پرداخت: ۳٬۹۷۷٬۰۰۰ ریال
```

Cause is intentional and correct server behaviour, not a label bug: `StorefrontCheckoutService`
blanks the legacy scalar `shippingMethodCode` / `shippingMethodLabel` when
`shippingGroups.Count > 1`, because there is no single method that covers every line. The
frontend blindly printed that intentionally blank scalar. The repair therefore must NOT invent an
aggregate method; it must render the authoritative groups.

## Authority trace (read-only, before coding)

- `docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md`
- `docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md`
  (mandatory: per-group method identity + per-group fee, reconciled aggregate totals, no
  double-counting; visual implementation must reuse the existing storefront design)
- `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-136.md`
- `src/frontend/app/storefront/storefront-checkout-api.ts` — `StorefrontCheckoutShippingGroup`
  (`shippingGroupId`, `methodCode`, `methodLabel`, `shippingAmount`, `currency`, `lineCount`, …)
  and `StorefrontCheckoutPage.shippingGroups` already exist in the Order-owned projection.
- `src/backend/Modules/Order/Tooba.Application/.../StorefrontCheckoutService.cs:347-353` —
  `var multiGroup = shippingGroups is { Count: > 1 }` ⇒ `methodCode`/`methodLabel` = `string.Empty`.
- `src/frontend/app/storefront/storefront-shipping.tsx:1038-1061` — the accepted storefront summary
  convention: aggregate `هزینه ارسال` row once, plus dashed-divider per-group fee rows.

## Change (smallest targeted, frontend-only)

`src/frontend/app/payment/storefront-payment-handoff.tsx`, summary card only
(+25 / −3 lines):

- `page.shippingGroups.length > 1` ⇒ render heading `روشهای ارسال` plus one row per authoritative
  group (`methodLabel` + `shippingAmount` in that group's currency, falling back to page currency).
  Row identity testids: `payment-shipping-groups`, `payment-shipping-group-{shippingGroupId}`.
- otherwise ⇒ the previous legacy `<p>روش ارسال: {shippingMethodLabel}</p>` markup, byte-identical.

Reused classes (copied from the shipping summary rows — no new styling surface):
`space-y-1.5`, `border-t border-dashed border-gray-200 pt-2.5`,
`flex justify-between gap-2 text-[11px] md:text-xs`, `text-gray-500 truncate`,
`font-bold text-gray-800 shrink-0`.

Explicitly NOT done: no new aggregate method/rate/origin/tax/ETA identity, no client-side pricing,
no change to the aggregate `هزینه ارسال` row (still rendered exactly once) nor to
`قابل پرداخت` (server `payableAmount`), no CSS/theme/token/global-class change, no other route,
no Order/Payment/Settlement financial policy change, no migration, no write path touched — the load
effect remains read-only (no submit/commit/reservation/cart-clear on open or reload).

## Targeted proof

- New focused test `src/frontend/app/payment/storefront-payment-grouped-summary.test.ts` (4/4 pass):
  grouped branch driven by `shippingGroups`, heading not blank, per-group label+amount rows, legacy
  scalar markup intact, `هزینه ارسال:` / `قابل پرداخت:` / `payment-payable-amount` each exactly once,
  reused classes, no inline style / CSS import, load effect performs no payment start call.
- Existing `app/payment/storefront-payment-page.test.ts` and
  `app/storefront/storefront-checkout-api.test.ts` still pass (18/18 in the combined run).
- `npm run typecheck` clean.
- Two guard failures observed are PRE-EXISTING at the base SHA and unrelated (unmodified file blobs,
  worktree hash == HEAD blob hash):
  - `FE-SIZE` `app/admin/category-admin-screen.tsx` 2230 > baseline 2215
  - `component-compliance` `app/storefront/storefront-landing-blocks.tsx:286 bg-white`
  Neither was touched or repaired (out of scope, fail-closed).

## One bounded real-browser validation (isolated, sanctioned TB-136 procedure)

Isolated scratch runtime only: scratch PostgreSQL container `tooba-tb137-pg` (databases
`tooba_137_scratch` / `tooba_137_messaging`, port `55459`), Host on `5406`, frontend on `3106`,
deployment `isolated-137`, tenant `store-alpha`. Shared `:5088` / `:3000` / `tooba_alpha` never
touched. Isolation guard JSON: single tenant, canonical gateway tenant present, no shared alpha
database/reference/port, fail-closed.

Journey (real UI, Development-only same-seller companion fixture from TB-136): added
`workspace-live-shirt` (Seller A `فروشگاه آرمان`, `۱٬۸۵۰٬۰۰۰` IRR) and `workspace-live-scarf`
(same Seller A, `۱٬۴۵۰٬۰۰۰` IRR) ⇒ one seller card with 2 line rows, per-line methods
`post:express` (`۲۰۰٬۰۰۰`) and `tipax:standard` (`۱۸۰٬۰۰۰`), one server-derived delivery date
(`2026-10-20`, window `9-12`), then `ادامه به پرداخت`.

Observed `/fa/payment` summary card after the fix (DOM readback):

```
خلاصهٔ سفارش
روشهای ارسال
پیشتاز      ۲۰۰٬۰۰۰ ریال
معمولی      ۱۸۰٬۰۰۰ ریال
گیرنده: حمید آزمون — تهران
هزینه ارسال: ۳۸۰٬۰۰۰ ریال
قابل پرداخت: ۳٬۹۷۷٬۰۰۰ ریال
وضعیت پرداخت: در انتظار پرداخت
این سفارش هنوز ثبت نشده است. سفارش و رزرو موجودی فقط با کلیک روی دکمهٔ پرداخت نهایی ساخته میشود.
```

No blank `روش ارسال:` value, no invented aggregate method, fee total once, payable once.
RTL preserved; mobile viewport `393x887`: grouped branch still rendered, card width 326,
no horizontal overflow, 0 CSS edits.

Database readback (authorized, scratch DB):

```
GROUPS=2   DISTINCT_SELLERS=1
GROUP|post:express|پیشتاز|200000.00|01a14382-016f-7000-80cf-4a5b73cad337
GROUP|tipax:standard|معمولی|180000.00|01a14382-353a-7000-abd4-4ce7974a95a8
FEE_SUM=380000.00
DRAFT_SCALAR_METHOD_CODE=[]  DRAFT_SHIPPING=380000.00  DRAFT_DATE=2026-10-20  DRAFT_WINDOW=9-12
```

The blank legacy scalar is therefore still blank server-side by design (legacy readback intact),
and the customer no longer sees an empty value.

Teardown: PID-verified Host/frontend stop, scratch container + volumes removed, listeners on
`5406` / `3106` / `55459` = 0; shared `5088` / `3000` listeners and shared containers intact.

## Remaining blockers (unchanged, NOT claimed closed)

Full payment/capture completion and the commercial Settlement allocation policy remain OPEN. No
claim of final Payment transaction, commercial split, production readiness or migration
certification is made by this task.
