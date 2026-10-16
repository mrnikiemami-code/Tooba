# TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151 — isolated live proof of two same-method seller commitments

Program: `TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS`
Parent: `TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150`
Mode: `BOUNDED_ISOLATED_REAL_HTTP_WRITE_READ_AND_PAYMENT_HANDOFF_PROOF`
Expected base SHA: `e56a0e67b92aa3421017aa69c7ee67d342b0e1d2`
Verdict: **PARTIAL** — real authenticated private HTTP save/read of a two-seller, same-method cart is proven
end to end (`JOURNEY=PASS`, 25/25 runnable assertions), but two required proof elements are **not** closed:
the in-browser UI screenshots and the read-only payment-draft seam (blocked by a scratch tax-fixture gap).
No source change was made: no defect in 149/150 blocked a customer-visible assertion.

## 1. Environment actually used (task-owned, disposable, destroyed afterwards)

| Item | Value |
| --- | --- |
| Database container | `tooba-tb151-pg` (`postgres:16-alpine`), `127.0.0.1:55470`, databases `tooba_151_scratch` / `tooba_151_messaging`, role `scratch151` |
| Password | taken from the environment only; never written to evidence; the temp secret file was deleted |
| Isolated Host | `.tmp-151-host-out` (published from current `main` at the base SHA above), `http://127.0.0.1:5412` |
| Tenant | one `Active` tenant `store-alpha → tenant-151-scratch`, `StoreCommerce.SalesChannel = Marketplace`, `Edition = SingleStore` (no Edition swap) |
| Isolation guard | `isolation-guard.json` — `failClosed=true`: one tenant, canonical gateway tenant present, no `tooba_alpha`, no `tenant-alpha`, no `Port=5432`, no prior scratch port, no `ShippingRateAuthority`, no `ShippingOperationalShadow` |
| Launches | **2** launches of the task-owned Host on port 5412. Launch 1 bootstrapped the schema/seed and was stopped; launch 2 carried the final overlay. Reason: the seller-specific preparation days this proof needs are keyed by seller party id, and the seeded seller ids only exist after the first bootstrap. Both launches are the disposable scratch host; the shared `:5088`/`:3000`, `tooba_alpha`, the shared Postgres `:5432` and shared RabbitMQ were never touched or reconfigured. |
| Identity | sanctioned Development OTP fixture customer (fixed Development mobile + fixed Development code); the raw mobile/code/token are not reproduced here and all captured tokens are `REDACTED` in `journey-151-trace.txt` |
| Cart | the customer's own authenticated cart (`GET /v1/storefront/cart/current`), two lines of two different sellers, both selecting the canonical `tipax:standard` |
| Cleanup | `cleanup-151.txt` — isolated Host stopped by recorded PID **and** executable-path match, container `tooba-tb151-pg` removed, `0` LISTENING sockets on 55470, shared `:5088` still listening, shared `postgres-db` container preserved |

Task-owned overlay deltas (nothing else): scratch ports/names, and
`ShippingMethods.SellerPreparationDaysByPartyId = { sellerA: 2, sellerB: 3 }` with
`DefaultSellerPreparationDays = 1`, `ShippingOneLevelCatalog.Enabled = true` (TB-141/143 lineage).

## 2. Scenario under proof

| Fact | Value |
| --- | --- |
| Seller A | `01a145fd-fdf9-7000-b907-982ab18acc35`, offer `01a145fd-fe25-7000-8667-69e0b6b8530d`, preparation **2** days |
| Seller B | `01a145fe-00af-7000-911c-ada7bd1621c7`, offer `01a145fe-00b4-7000-8c42-4f0307b65d72`, preparation **3** days |
| Canonical method | `tipax:standard` for **both** lines (same method code, same service/option, same lead days = 3) |
| Scratch state reset before the decisive run | the task-owned draft tables (`order.cart_shipping_drafts`, `order.cart_shipping_groups`, `order.cart_shipping_group_lines`) were emptied so the pre-save assertions start from a genuinely draft-free cart. This is disclosed scratch-state control inside this task's own disposable database; the cart and catalog were not touched. |

## 3. Authoritative before-save read (`projection-before-save-151.json`)

```text
groupKey  01a145fdfdf97000b907982ab18acc35|tipax:standard  seller A  prep=2  lead=3  earliest=2026-10-21  ids=null
groupKey  01a145fe00af7000911cada7bd1621c7|tipax:standard  seller B  prep=3  lead=3  earliest=2026-10-22  ids=null
```

* two **distinct** server-owned draft-stage group keys, both canonical `tipax:standard`, **no** `ShippingGroupId` yet;
* earliest dates differ by exactly the preparation delta (1 day) although the method and its lead days are identical;
* time windows are the current static generic set (`9-12`, `12-15`, `15-18`, `18-21`); no carrier timezone or per-slot
  promise was invented — the promise authority is seller preparation + method lead days + server `IClock`.

## 4. Save / re-read / edit / fail-closed (authenticated HTTP)

| Step | Request | Result |
| --- | --- | --- |
| save | `PUT /v1/storefront/shipping/selection` with **two** groups, each carrying its own date+window | 200; two distinct stable `ShippingGroupId`s; A `2026-10-21 / 9-12`, B `2026-10-22 / 9-12`; no scalar date/window accepted |
| re-read | `POST /v1/storefront/shipping/projection` | each route carries **its own** id, date and window (`projection-after-save-151.json`) |
| refresh/back | identical projection call again | byte-identical horizon payload (no client drift, no server recalculation) |
| edit A | `PUT` replacing A's window with `12-15`, B untouched | 200; A persisted as `12-15`; B's date, window and line membership unchanged |
| invalid date | A's date = earliest − 1 day | **400** `shipping.delivery.too_early` |
| invalid window | unknown window `23-24` | **400** `shipping.delivery.slot_unavailable` |
| after rejects | projection re-read | last accepted schedule still intact for A and B; no partial overwrite |

## 5. Persisted read-back (`scratch-readback-151.txt`)

| Invariant | Observed |
| --- | --- |
| Two draft groups, two distinct sellers, **same** `method_code = tipax:standard` | `draft_groups = 2` |
| Independent minimums | `2026-10-21` (seller A) and `2026-10-22` (seller B) |
| Independent selected schedules | `2026-10-21 / 12-15` and `2026-10-22 / 9-12` |
| Fee charged per group exactly once | `180000.00` + `180000.00` IRR (one row each, no shared/global fee row; draft header keeps an informational total `360000`) |
| Whole-line membership, each line once | `membership_rows = 2`, `distinct_lines = 2`, `groups_with_lines = 2` |
| No global schedule for the multi-group draft | `shipping_method_code = ''`, `selected_delivery_date = NULL`, `selected_delivery_time_window = NULL` in `order.cart_shipping_drafts` |
| No cart/order mutation before final checkout | `checkouts = 0`, `order_shipping_groups = 0`, `seller_orders = 0`; cart still holds exactly the two submitted offers |
| Same seller + same method stays one group | authenticated probe: seller B's line swapped for a second offer of seller A → projection returns exactly **1** canonical `tipax:standard` horizon (`sameSellerCanonicalHorizons = 1`, `probeCartSellerCount = 1`) |

## 6. Assertion matrix

`assertion-matrix-151.txt` — `CHECKS_TOTAL=28`, `CHECKS_PASS=25`, `CHECKS_FAIL=0`, `CHECKS_BLOCKED_ENV=1`, `CHECKS_OBSERVED=2`.

## 7. Not closed in this task (declared, not hidden)

1. **Browser UI screenshots — NOT captured.** A storefront frontend run pointed at `127.0.0.1:5412` (private port)
   was not started inside the 45-minute box. No screenshot is claimed, and none was fabricated. The UI rendering of the
   independent group cards therefore remains unproven in this task (the API/state authority it consumes is proven here).
2. **Read-only payment-draft seam — BLOCKED_ENV.** `GET /v1/storefront/cart/{cartId}/payment-draft` answered
   **409** `checkout.tax.unavailable`. Exact root cause from the scratch itself: this disposable database seeds
   `tax.rules = 1`, covering only category `01a145fe-00cc-7000-8967-741a833062a5`, while the two-seller cart spans
   categories `01a145fd-fea2-7000-b22f-60a08f3e6dd6` (seller A, **no** active rule) and
   `01a145fe-00cc-7000-8967-741a833062a5` (seller B). A two-seller cart therefore cannot be tax-priced in this
   scratch, so the "payment draft matches accepted selections" assertion could not be exercised. This is a
   seed-fixture coverage gap, **not** a 149/150 defect: the draft write path (`/shipping/selection`) and the
   authoritative projection both behave correctly. Safe next step: extend the Development tax seed with a national
   rule for the remaining seeded offer categories (or seed a per-category rule set) so a multi-seller draft is
   priceable, then re-run this same journey unchanged.
3. **Observation (no fix, no fabricated PASS): draft `ShippingGroupId` is re-issued on every save.**
   `CartShippingGroupPlanner.Plan` always allocates new ids (it has no existing-group reuse input), so the two
   groups saved first appear with new ids after the second `PUT`. Customer-visible schedule, fee and membership are
   stable across saves (the client re-reads the projection and reconciles `groupKey → ShippingGroupId`), so no
   customer-facing assertion fails; identity continuity across re-saves is simply **not** provided by 149/150.
   Recorded for the Architect as a scope decision, not repaired here (out of the narrow-fix budget and not a
   documented 149/150 contract).
4. **Compatibility scalar retained.** `StorefrontShippingProjection.minimumDeliveryDate` is still returned for a
   multi-group cart as the legacy single-group compatibility value (the later group minimum). Per-group promises are
   the `shippingGroupHorizons` rows, which are proven independent; the observation is recorded rather than read as a
   group claim.

## 8. Safety

No shared environment was written: `127.0.0.1:3000`, `:5088`, `tooba_alpha`/`store-alpha` on the shared Postgres,
and the shared RabbitMQ/Redis were never reconfigured, stopped or written. No real SMS, PSP or payment was called:
the journey never invokes final payment, never creates a real order/checkout (`checkouts = 0`), never bypasses
authentication/eligibility, and only Development fixtures were used. No shipping cost, origin or carrier timezone
was invented. No frontend/CSS/theme/admin file was touched. No source file was modified by this task — the commit
contains only the received task artifact and this task's evidence.

## 9. Reproduction

```powershell
$env:TOOBA_151_SCRATCH_PASSWORD = '<scratch db password>'
docker run -d --name tooba-tb151-pg -e POSTGRES_USER=scratch151 -e POSTGRES_PASSWORD=$env:TOOBA_151_SCRATCH_PASSWORD `
  -e POSTGRES_DB=tooba_151_scratch -p 127.0.0.1:55470:5432 postgres:16-alpine
docker exec -e PGPASSWORD=$env:TOOBA_151_SCRATCH_PASSWORD tooba-tb151-pg psql -U scratch151 -d postgres -c 'CREATE DATABASE tooba_151_messaging;'
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Debug -o .tmp-151-host-out
node docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151\prepare-isolated-target-151.js
powershell -File docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151\launch-isolated-host-151.ps1 -Port 5412
node docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151\journey-151.js
```

`.tmp-151-*` (build output, run overlay, journey state, scratch password) are gitignored and are **not** part of the
commit.

## 10. Evidence files

`isolation-guard.json`, `host-launch-151.txt`, `prepare-isolated-target-151.js`, `launch-isolated-host-151.ps1`,
`journey-151.js`, `journey-151-trace.txt`, `assertion-matrix-151.txt`, `projection-before-save-151.json`,
`selection-save-151.json`, `projection-after-save-151.json`, `payment-draft-151.json` (409 body),
`cart-after-flow-151.json`, `readback-151.sql`, `scratch-readback-151.txt`, `cleanup-151.txt`.

No focused test suite was run/modified, because this task changed no source code and ran no code path that the
already-green 149/150 suites do not cover; the proof is the live authenticated HTTP round trip documented above.
