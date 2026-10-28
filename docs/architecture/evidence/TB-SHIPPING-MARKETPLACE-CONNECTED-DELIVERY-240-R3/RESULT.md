# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3 — worker execution evidence

Status: **PASS (bounded)** for the schema/uniqueness gate and the connected mixed-owner vertical; the requested
coupon/immutability/UI-payment acceptance is NOT delivered and is recorded as such below.

Base: `307e7bc90c4e1fbe9d05487d4f1f0415249c5009` (issue commit, fast-forward only; no amend/force/reset).

Task Host: `D:\temp\tb240r3-build\out`, Development / Marketplace / Tenant NULL / `tb239-marketplace`,
DB `tooba_marketplace_tb239`, `http://127.0.0.1:5190` (old PID 23944 stopped by verified port owner; new PID from
the same launch env). Task FE `http://127.0.0.1:3102`. Task browser profile `D:\temp\tb240r3-review-chrome`
(CDP 9341), Chrome installed, own profile, no user profile/session touched.

## A — ONE forward Order migration: BOTH group uniqueness gates are now LEGACY-SCOPED

`20261028140000_AddCapturedGroupLegacyScopedUniqueIndexes` (plain `DROP INDEX` + `CREATE UNIQUE … WHERE
accepted_snapshot_id IS NULL` on the same existing keys, same index names, one migration transaction):

```
CREATE UNIQUE INDEX ux_cart_shipping_groups_cart_seller_method
    ON "order".cart_shipping_groups (cart_id, seller_party_id, method_code)
    WHERE (accepted_snapshot_id IS NULL)
CREATE UNIQUE INDEX ux_order_shipping_groups_checkout_seller_method
    ON "order".order_shipping_groups (checkout_id, seller_party_id, method_code)
    WHERE (accepted_snapshot_id IS NULL)
```

Runtime EF mappings now match the migrated shape:
`OrderDbContext` lines for `CartShippingDraftGroup` and `OrderShippingGroup` use
`.IsUnique().HasDatabaseName("<same name>").HasFilter("accepted_snapshot_id IS NULL")`.
No owner column, no method suffix, no JSON copy, no new table, no relaxation of the legacy rule, no edit to
applied 20261016/231/232/236 migrations, no EF baseline regeneration (pre-existing stale snapshot retained).

Applied by the NORMAL runner (`MarketplaceDevelopmentBootstrap` migration step) on the verified task DB only.
Actual per-target Order history is `…20261027230000_AddCapturedSelectionCouponContext` **plus**
`20261028140000_AddCapturedGroupLegacyScopedUniqueIndexes` — i.e. 231/232/236 ARE applied on this target
(the "shared NOT APPLIED" statement belongs to the shared `tooba_alpha` target, not here).

Legacy control (real DB, inside a transaction that was rolled back): inserting two `accepted_snapshot_id IS NULL`
rows with the same `(cart_id, seller_party_id, method_code)` still fails with
`23505 duplicate key value violates unique constraint "ux_cart_shipping_groups_cart_seller_method"`, and the
transaction left 0 rows behind. Lawful captured control: the SAME seller appears twice on the SAME
cart+method with complete evidence and both rows persist.

## B — planner seam tightened: a captured obligation key is authoritative, never a hint

`CartShippingGroupPlanner.ResolveMethod`: when the request carries a non-blank `ObligationKey` there is **no
fallback**. Exactly one eligible row of THAT seller must match the trimmed key exactly; zero ⇒
`shipping.group.method.not_eligible`, several ⇒ `shipping.group.method.identity_mismatch`, a row of a foreign
seller ⇒ `method.identity_mismatch`, a contradictory declared service/option ⇒ `method.identity_mismatch`.
Declared-identity checks now run BEFORE returning the keyed row. Legacy requests without a key keep the historic
`one group per seller+method` collapse (the R2 relaxation was re-scoped to keyed commitments only).
Duplicate/unknown/missing line refusals are unchanged.

`OrderShippingGroupMaterializer` (the final Order-side twin of the same gate) applies the same scoping: a plan
carrying COMPLETE `AcceptedEvidence` is keyed by seller+method+whole-line membership, a plan without evidence
keeps the legacy seller+method rule. Repeated order lines are still refused.

Focused facts (new class `CapturedObligationKeyPlanningTests`, 7 facts) + the existing planner class: **20/20**
(`dotnet test … --filter CapturedObligationKeyPlanningTests|CartShippingGroupPlannerTests`). Covered: two
commitments of one seller on one method keep their own 95,000 / 62,000 prices; unknown key refuses with no
fallback even though a same seller+method row exists; duplicate key refuses; foreign seller key refuses;
contradictory declared service refuses; legacy no-key duplicate seller+method still refuses; repeated line still
refuses.

## C — connected flow from selection to a real mixed-owner Order

Same real 3-offer cart `01a181e2-0d5e-7000-a94c-87e9b826f94d` (cartVersion 7), real Platform/SellerA/SellerB
captures, real `post:standard` binding, existing APIs only, no fake/mock/interception:

1. `POST /v1/storefront/shipping/projection` → 200, three PRICED groups:
   `cap|platform|SellerA` goods 2,016,500 payable 100,000; `cap|seller|SellerA` goods 1,580,500 payable 65,000;
   `cap|seller|SellerB` goods 1,951,100 payable 49,500 — no refused group.
2. `PUT /v1/storefront/shipping/selection` → **200**, `selectionRevision=1`, three groups persisted with their own
   ids/owner/row/money/date/snapshot; the same seller appears in TWO groups on one binding and one method.
3. `GET /v1/storefront/cart/{cartId}/payment-draft` → 200, `selectionRevision=1`, shipping 214,500,
   payable 5,762,600, `paymentState=PendingPayment`, three groups with their own amount/date.
4. `POST /v1/storefront/cart/{cartId}/start-payment` (`providerCode=manual`, explicit text
   `expectedSelectionRevision="1"`) → **200**: checkout `01a18209-cd78-7000-a9b6-02dadf377e55`,
   `checkoutReplayed=false`, payment `01a18209-d01d-7000-a1f0-d59a53b1124f` `Pending`/`manual`, amount 5,762,600
   IRR, `reservationCount=3`, guaranteed until 12:24Z, redirect `/payment/result?...&awaitingManual=1`.

Persisted (DB readback, not prose): **3** `order.order_shipping_groups` with `accepted_snapshot_id` =
Platform `01a181ca-e8f8…`, SellerA `01a181de-0c80…`, SellerB `01a181de-0e59…`, owner kinds 1/2/2, own payables
100,000 / 65,000 / 49,500 and own dates 2026-10-31 / 2026-10-29 / 2026-10-31; **2** seller orders, **3** order
lines, **3** group lines; payment row `Pending`/`manual`; exactly **1** attempt; cart `Converted`.
The first attempt of the same command failed honestly at `order.shipping.group.seller_method.duplicate`
(the materializer, §B) with zero rows written — that is the measured RED that motivated the materializer fix.

5. `GET /v1/storefront/checkout/{checkoutId}?cartId=…` (buyer's own token) → 200: 2 seller orders with
   1,850,000+1,450,000 and 1,790,000 line bases, taxes 297,000 / 161,100, shipping 214,500, payable 5,762,600,
   `canInitiatePayment=true`.

## D — payment idempotency: winner replay now uses the SAME lawful replay path

`PaymentDirectory`: the detector is exact (SQLSTATE 23505 + `ix_payments_idempotency_key` on table `payments`;
no `Contains("idempotency_key")`), and the losing branch no longer hand-builds a first-row response. After
detaching only its own graph it requires the winner to be visible and then re-enters the EXISTING same-key replay
path once (`idempotencyRaceResolved: true`), which re-applies `EnsureActorCanSeeAsync`, the
checkout+canonical-provider identity check, already-succeeded and failed-retry rules. A second conflict is a
typed conflict, never a loop.
**Not claimed:** `gateway.InitiateAsync` still happens BEFORE the contested `SaveChanges`, so an
external provider can still be called by both racing branches. Only the persisted single payment/attempt is
proven; there is no PSP/provider exactly-once claim, no real-money call.

## E — UI (existing surfaces only) and what it honestly shows

Task FE 3102 → Host 5190, real login through the existing BFF (`GET /api/auth/csrf` + `POST /api/auth/login`
with the `X-Tooba-Csrf` header; 200, `edition=Marketplace`), then the existing payment route
`/fa/payment?checkoutId=…`. Screenshots at widths 1440/390/360 are native
(`innerWidth == clientWidth == scrollWidth == visualViewport.width` at each width, no horizontal overflow):
`screens/payment-{1440,390,360}.png`.
The page does NOT show the payment form for this order: it renders the existing access guard
«دسترسی به اطلاعات پرداخت این سفارش تأیید نشد…». The reason is real and recorded: the committed-checkout
access proof (`resolveCommittedCheckoutAccess`) lives in the browser that drove shipping→payment, while this
order was created through the documented APIs, so the browser holds no proof for the checkout id and the guard
fails closed instead of exposing payment data. No bypass, no injected payload, no fake login was used to force
the form. The authenticated payment FORM at 1440/390/360 therefore remains **unproven** for this order.

## F — Acceptance ledger

| Item | Status |
| --- | --- |
| Forward migration, BOTH tables, partial legacy-scoped unique indexes, same names/keys | DONE (applied on task DB by normal runner) |
| Legacy uniqueness preserved (DB 23505 control, rolled back, 0 rows) | DONE |
| Captured same-seller/same-method/same-binding coexistence | PROVEN (3 real rows + order rows) |
| Planner: no fallback on supplied obligation key (zero/dup/foreign/contradictory) + legacy control | DONE 20/20 focused |
| 3-group selection 200 → payment draft → StartPayment → real Order/manual Pending | DONE |
| Own order read after order exists | DONE (200, 2 seller orders) |
| Order immutability after changing ONLY a task DEV publication | **NOT RUN** |
| Coupon: partial applicability on ONE eligible seller/group | **NOT RUN** |
| Coupon: invalid and cleared | **NOT RUN** |
| Foreign-owner denial on this order | **NOT RUN** (240-R2 seller 403 `seller.authorization.denied` remains the last proof) |
| UI double-click / failed-refresh retry | **NOT RUN** |
| Payment form at 1440/390/360 | **NOT REACHED** (access guard; see §E) |
| PSP / provider exactly-once, real payment, activation, user Visual ACCEPT | NOT CLAIMED |

## G — Deadline ledger (no new clock)

Previous: 53 + 70 + 62 + 31 = 216 min. This continuation claimed 2026-10-28T10:03:07Z and reports at
≈10:31Z → **≈27 min** against the 24 remaining, i.e. cumulative **≈243 min** and ≈3 min over the original
240-minute contract. Two extra source corrections were needed inside §B/§C (materializer twin, planner
re-scope) in addition to the planned migration; both were required for the flow to reach the real Order and are
recorded, not hidden. No extension is claimed and no automatic 241 is issued.

## H — Preservation and remaining known gaps

No reset/drop/manual schema mutation on shared targets; `store-alpha`/user DB untouched; no seed re-run; the
task cart's own data changed only by the lawful flow above. Checked-in gates stay OFF (task-local env only);
TB185 paused/default OFF/PendingAllocation. USER runtime `/fa` on 3000/5088, handoff 3100/5188 profiles and every
user tab/unsaved work were never enumerated, read, automated, reloaded or closed. Retained, unfixed: 238
unclassified `22001`, 227 mobile empty-note, unrecovered 225-R1 local edits, and the acknowledged
provider-side exactly-once limit in §D.
