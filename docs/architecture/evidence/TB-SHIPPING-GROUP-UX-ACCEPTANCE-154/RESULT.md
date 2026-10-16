# TB-SHIPPING-GROUP-UX-ACCEPTANCE-154 — real customer Shipping flow: Continue gating, independent group
# schedules, reload/back restoration and stable persisted group identity

Parent: `TB-SHIPPING-GROUP-ID-CONTINUITY-153` (PASS). Program: `TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS`.
Mode: `BOUNDED_PRIVATE_BROWSER_CONTINUE_GATING_AND_RELOAD_ACCEPTANCE`. Base SHA: `ab3f70b203056ff464cc963d33c2825cdb63ff29`.
Verdict: **PASS** (all five PASS criteria met; zero production source change).

## What was proven

1. **Two independent seller promises, same canonical method.** Private authenticated browser on
   `http://127.0.0.1:3109/fa/shipping` renders exactly two group cards for the same `tipax:standard`:
   seller A ("فروشگاه آرمان", preparation 2, first possible `1405/07/29` = 2026-10-21) and seller B
   ("دیجیاستایل نمونه", preparation 3, first possible `1405/07/30` = 2026-10-22), each with its own fee
   (۱۸۰٬۰۰۰ ریال), its own date chips and its own window chips. No global/ page-wide date or window control
   exists in the DOM.
2. **Continue disabled → enabled through a legitimate incomplete state.** Changing only seller A's line to a
   different canonical method (`post:standard`) left that group without a window selection and
   `[data-testid=shipping-continue]` observed `disabled=true`; after choosing that group's own server-provided
   date and window the button returned to `disabled=false`. Seller B was never touched and kept `9-12`.
3. **Persist + Payment preview + Back + full reload.** Clicking Continue performed the real authenticated
   selection save and navigated to `http://127.0.0.1:3109/fa/payment?cartId=…` (preview only, no purchase).
   After navigating back to `/fa/shipping` as a fresh document, both groups restored their persisted
   schedules and the **same** `ShippingGroupIds`.
4. **Window-only edit persisted with stable identity.** After restoring seller A's canonical method and
   changing only its window to `15-18`, the database holds
   `01a1463d-3852-7000-8f8a-88e19a4608a2 | seller A | tipax:standard | 2026-10-21 | 15-18 | 180000` and the
   untouched `01a1463d-3852-7000-bdba-93a120d0c9a5 | seller B | tipax:standard | 2026-10-22 | 9-12 | 180000`:
   both ids are the same ones allocated by the first save in this run — TB-153 continuity holds end-to-end
   through the real product surface.
5. **Read-only payment draft is HTTP 200 with correct per-group math.** The authenticated HTTP journey got
   `200` (the TB-153 409 fixture problem does NOT reproduce when the cart lines come from the seeded
   categories that carry the active tax rule): two groups, distinct sellers, both `tipax:standard`, identities
   equal to the latest accepted save, one line per group, per-group fees `180000 + 180000`,
   `shippingAmount 360000`, `taxAmount 327600` (authoritative 9%), `subtotalExclusiveOfTax 3640000`,
   `discount 0`, `payableAmount 4327600 = subtotal − discount + tax + shipping`, `checkoutId` absent
   (read-only seam).
6. **Zero premature mutation.** `checkouts 0`, `seller_orders 0`, `order_lines 0`, `payments 0`; group join
   rows 1:1 with `DUP_GROUP_IDS 0`, `LINES_IN_TWO_GROUPS 0`, `ORPHAN_JOIN_ROWS 0`.

## Verdict basis per PASS criterion

| Criterion | Result | Evidence |
| --- | --- | --- |
| One real private authenticated browser run, two seller cards, same method, separate server horizons, screenshots + sanitized DOM assertions | PASS | `ui-shipping-*.png`, `ui-dom-assertions-154.txt` |
| Continue observed disabled with a legitimately incomplete group, then enabled only when both groups valid; state restored after Payment→Back and reload | PASS | `ui-dom-assertions-154.txt` §B/§C/§F, `ui-payment-preview-154.png`, `ui-shipping-after-reload-154.png` |
| Repeated save keeps both stable `ShippingGroupIds`, independent date/window and line/fee membership in HTTP and DB; rejected invalid write does not corrupt data | PASS | `assertion-matrix-154.txt` (36 PASS / 0 FAIL), `selection-*-154.json`, `db-readback-154.txt` |
| Payment-draft 200 with per-group fee once and consistent totals | PASS | `payment-draft-154.json`, `assertion-matrix-154.txt` |
| Zero early checkout/order/payment/PSP mutation; test results and private cleanup trace | PASS | `db-readback-154.txt`, `cleanup-154.txt` |

## Interrupted-claim note (disclosed)

The first Worker `GET /api/tasks/next` returned the full task body for Bridge row
`111cd8ec-de60-4fc9-84ea-f536a63931ab`, so the follow-up scripted claim returned no body; the artifact was
persisted from the received content (`docs/ai/tasks/TB-SHIPPING-GROUP-UX-ACCEPTANCE-154.task.md`) and the same
Bridge row was completed at the end of the lifecycle.

## Method / environment

Nothing in production code was changed (`Code-SHA` = evidence commit only): no frontend, no Order, no Tax, no
Admin, no migration, no schema, no DI, no config committed. The isolated Host is the current `Debug` publish
of `Tooba.Host` with a task-owned overlay (`isolation-guard.json`, fail-closed PASS): one Active tenant
`store-alpha -> tenant-154-scratch`, `Edition: SingleStore` (no swap), no `tooba_alpha`, no `tenant-alpha`,
no `Port=5432`, no prior scratch port (`55473` here), no rate-authority/shadow switch,
`AllowResetAndSeed=false`, `RunLegacyBootstraps=true`. Seller-specific preparation days (A=2, B=3) come from
the scratch overlay only; horizons are server-computed (`minimumDeliveryDate`), never invented.

Scratch bootstrap minted fresh ids per seed, so the two cart lines were taken from the seeded offers whose
categories carry the active `tax.rules` row
(`01a1463c-2b36-…`/`01a1463c-2c22-…` for seller `01a1463c-2b31-…`, `01a1463c-2b3b-…` for seller
`01a1463c-2b33-…`). No tax/locale/`tax.rules` production code was touched and no production DB was read.

Host launches: one bootstrap launch (which seeded the scratch DB and revealed that this seed mints different
seller ids than TB-152/153 held), then one launch with the correct seller preparation overlay. Single
frontend process on `:3109`. No shared service was restarted, purged or rebuilt.

## Evidence index

| File | Content |
| --- | --- |
| `isolation-guard.json` | fail-closed isolation guard for the private target |
| `host-launch-154.txt` | private Host launch (PID, health, watchdog) |
| `prepare-isolated-target-154.js`, `launch-isolated-host-154.ps1`, `launch-isolated-frontend-154.ps1`, `derive-154-scripts.ps1`, `claim-task.js` | task-owned copies/receipts of the proven recipe |
| `journey-154.js`, `journey-154-trace.txt`, `assertion-matrix-154.txt` | authenticated HTTP acceptance (37 checks: 36 PASS / 0 FAIL / 1 OBSERVED) |
| `ui-dom-assertions-154.txt` | real browser DOM assertions (gating, independence, reload, identity) |
| `ui-shipping-two-groups-complete-154.png`, `ui-payment-preview-154.png`, `ui-shipping-after-reload-154.png` | real browser screenshots |
| `selection-save-154.json`, `selection-edit-a-154.json`, `selection-repeat-save-154.json` | save / window-edit / repeat-save identity |
| `projection-before-save-154.json`, `projection-after-save-154.json` | per-seller horizons and restored selections |
| `payment-draft-154.json` | read-only payment draft (HTTP 200, per-group fees once) |
| `cart-after-flow-154.json` | cart integrity after the whole flow |
| `db-readback-154.txt` | persisted ids/schedules/joins and zero premature rows |
| `cleanup-154.txt` | private-resource teardown and port-cleanup trace |

## Remaining open (outside this task, NOT started)

Admin financial mode and per-method restrictions, real create-method API, multilingual save/write-roundtrip,
EF debug log flood/scale check, trusted transport calendar/origin authority, approved shipping fee
allocation, and the release blockers tracked in `docs/planning/MUST-FIX-BEFORE-RELEASE.md`. Worker PASS is not
Architect ACCEPT.
