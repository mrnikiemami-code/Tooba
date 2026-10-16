# SHIPPING-ONE-LEVEL-UI-132 — Isolated multi-seller grouped shipping/checkout browser acceptance (observation-only)

- **Task:** `TB-SHIPPING-ONE-LEVEL-TABS-132` (parent `TB-SHIPPING-ONE-LEVEL-TABS-131`)
- **Program:** TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
- **Mode:** BOUNDED_ISOLATED_MULTISELLER_SHIPPING_CHECKOUT_BROWSER_ACCEPTANCE
- **Verdict:** **PARTIAL** (one blocking defect; grouped step is not customer-completable)
- **Evidence:** `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-132/`

## What was actually observed (isolated, real browser)

An isolated, disposable stack was used: a no-volume Postgres container on a unique loopback port
(`127.0.0.1:55455`), an isolated Host on `:5402` with a validated scratch tenant
(`tenant-132-scratch`, `isolated-132`) and the Development-only one-level catalog gate, and the real Next
frontend on `:3102` bound to that Host through the repository's own `TOOBA_HOST_ORIGIN` seam. The binding was
discriminated by reading `/v1/storefront/checkout-identity-policy` through the frontend origin and seeing the
isolated policy, not the shared one. Shared `:5088`, `:3000`, `tooba_alpha` and the shared containers were
never touched.

Real customer actions produced the cart: the PDP of `workspace-live-shirt` was opened, the primary seller
offer added to the cart, the alternate seller radio selected, and the second offer added. The result was a
real two-line, two-seller guest cart (`sellerCount=2`, `subtotalExclusiveOfTax=3,640,000`) with
`status=Active`.

`/fa/shipping` then rendered the TB-131 grouped card with correct, exact, disjoint membership:

| Group | Seller | Real cart line | Methods |
| --- | --- | --- | --- |
| 1 | دیجی‌استایل نمونه | `shipping-group-line-01a14330-f794-7000-bc63-8079c19eb318` | 7 authoritative codes |
| 2 | فروشگاه آرمان | `shipping-group-line-01a14331-5fdb-7000-ab24-c1f91265da2b` | 7 authoritative codes |

Two genuinely distinct methods were selected — `post:express` for group 1 and `tipax:standard` for group 2 —
and each group's fee came from the projection's authoritative rate (۲۰۰٬۰۰۰ and ۱۸۰٬۰۰۰ ریال), summed in
`data-testid="shipping-group-fees"`. Desktop and one mobile (390×844) viewport were captured; the grouped
markup reuses the page's existing card/surface tokens, radii, spacing and RTL grid, with no global CSS added.

## Blocking defect found (not repaired — out of scope for an observation task)

With the address fully completed and **both** group methods selected, the delivery-date step stays empty:

- `shipping-date-*` chips = **0**
- inline notice: «ابتدا روش ارسال را انتخاب کنید تا نزدیک‌ترین تاریخ مجاز فروشگاه نمایش داده شود.»
- top summary «حداقل روز تحویل» = **—**
- **«ادامه به پرداخت» stays `disabled`**

Consequence: `deliveryDate` can never be set, `isValid` never becomes true, `PUT /v1/storefront/shipping/selection`
is never issued, and the payment draft — the very surface this task was meant to prove — is unreachable.

### Root cause (isolated by counterfactual)

The server computes the delivery-date horizon only when a `methodCode` is supplied to the projection:

```text
POST /v1/storefront/shipping/projection {cartId, language:fa}
    -> deliveryDates=0, minimumDeliveryDate=absent, draft=null
POST /v1/storefront/shipping/projection {cartId, language:fa, methodCode:"post:express",  provinceName:تهران}
    -> deliveryDates=7, minimumDeliveryDate=2026-10-19
POST /v1/storefront/shipping/projection {cartId, language:fa, methodCode:"tipax:standard", provinceName:تهران}
    -> deliveryDates=7, minimumDeliveryDate=2026-10-20
```

`src/frontend/app/storefront/storefront-shipping.tsx` never sends a `methodCode` on the grouped path: the
grouped per-seller method click handler updates only `groupMethodBySeller` (and the derived fee rows) and does
not re-project, while the legacy single-method picker — whose `onSelectMethod` *does* re-project with a code —
is intentionally not rendered for grouped carts.

### Narrow proposed fix (for a future authorized task, not applied here)

Re-project with the currently selected group method (the first/earliest group selection, matching the legacy
single-method semantics) whenever a group method changes, and keep the resulting `deliveryDates` /
`minimumDeliveryDate` for the shared date/time picker. A per-group date horizon would be the architecturally
complete answer but requires an Order-owned contract decision, so the minimal behaviour-preserving change is a
re-projection driven by the grouped selection. No client-side date arithmetic and no invented horizon.

## Non-claims

- Multi-seller grouped **payment** acceptance, grouped payment summary and tax-applicable draft outcome were
  **not observed** (unreachable).
- **One-seller / multiple-method** customer UI support remains an **outstanding acceptance case**; grouping by
  seller alone is not evidence for it.
- Durable `shippingGroups` draft read-back (refresh / back navigation) was **not exercised** (`draft` was null
  because no selection could be saved).
- The durable group schema/migration was **not runtime-exercised**.
- The single-seller legacy path was **not re-run in the browser** this round; it is source-backed only.
- The applicable IR-NAT/IR tax rule remains **source-backed**, not runtime-proven.
- Commercial allocation stays **PendingAllocation** (untouched).
- Host launch #1 aborted on a *setup* prerequisite (the disposable scratch container lacked the second
  scratch database required by MassTransit SQL transport); launch #2 is the single observation launch. This
  is recorded honestly in the evidence and is not an application defect.

## Stop state

`STOP_FOR_ARCHITECT` — observation complete, cleanup verified (all task-owned PIDs stopped by recorded PID
after absolute-path verification; scratch container, temp dirs and secret removed; `:5402/:3102/:55455`
released; shared services untouched and un-restarted). No `src/**` file was modified.
