# TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152 — runtime acceptance Result

Task: `TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152`
Parent: `TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151`
Program: `TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS`
Mode: `BOUNDED_ISOLATED_PAYMENT_DRAFT_AND_REAL_BROWSER_GROUP_UI_ACCEPTANCE`
Base SHA: `e57ef0639244be1ddd2bb6151456faf3436d76e7`
Status target: `PASS_OR_PARTIAL_OR_BLOCKED`

## Verdict

PARTIAL — gap (A) multi-seller same-method selection/persistence and gap (B) read-only payment draft are now
fully proven end to end, and the real private browser proof of two independent Tipax cards succeeded. The verdict
is PARTIAL and **not** PASS because gap (D), repeated-save `ShippingGroupId` continuity, reproduced again in this
run (a new group id is issued on every accepted save), and the Continue-gating assertion could not be produced
from a real unselected state (the window control is single-select and cannot be deselected), so one clause of
criterion C rests on the enabled-state observation only. Both limits are reported as OPEN below rather than
silently certified.

## What was run (real, private, disposable)

Private topology, all task-owned and loopback-only:

| Layer | Endpoint | Ownership |
| --- | --- | --- |
| Next frontend | `http://127.0.0.1:3108` (`TOOBA_HOST_ORIGIN=http://127.0.0.1:5413`) | task-owned `next dev` |
| Host | `http://127.0.0.1:5413` | task-owned `Tooba.Host.exe`, Edition `SingleStore` |
| Postgres | `127.0.0.1:55471` db `tooba_152_scratch` + `tooba_152_messaging` | container `tooba-tb152-pg` (removed after evidence) |

Isolation guard (`isolation-guard.json`, fail-closed) proves: one tenant (`store-alpha` → `tenant-152-scratch`),
no `tooba_alpha`, no `tenant-alpha`, no `Port=5432`, no prior scratch port, no `ShippingRateAuthority`, no
`ShippingOperationalShadow`, Edition `SingleStore` (no swap), `AllowResetAndSeed=false`. Credentials were read from
the environment and never written into evidence (`CREDENTIALS_WRITTEN_TO_EVIDENCE=false`).

Overlay is task-owned fixture data only: `SellerPreparationDaysByPartyId` = seller A `2`, seller B `3`, canonical
`tipax:standard` `LeadDays` `3`, `DeliveryHorizonDays` `7`. No production source was changed.

One minimum tax fixture was added **in scratch only**: `tax.rules` for the schema-derived seeded offer category
(`01a1460a-4a8b-7000-8a53-1b669c1b250f`) mirroring the shape of the pre-existing canonical rule
(`IR-NAT` / `IR` / `Percentage` / `0.09` / specificity `10` / `OverridePolicy Disabled` / `Active`). This is the
exact budget the task authorized; no tax code was touched and no zero/dummy quote was forced. Total rules in
scratch after the fixture: 2 (one canonical `standard` + one schema category).

## (A) Authenticated two-seller same-method HTTP acceptance — PASS

`journey-152.js` (unchanged TB-151 journey semantics, private port only) executed the full lifecycle and the
assertion matrix is `assertion-matrix-152.txt`: **32 checks — 30 PASS, 0 FAIL, 2 OBSERVED**.

Observed per-seller promises (both `tipax:standard`, both `LeadDays = 3`):

| Group | Seller | Prep days | Earliest (min) date | Selected date | Selected window | Fee |
| --- | --- | --- | --- | --- | --- | --- |
| `01a1461a-e3dd-7000-add7-4a7d9600829d` | A (`...49e3-7000-a074-73a5b67329cf`) | 2 | `2026-10-21` | `2026-10-21` | `12-15` | 180,000 |
| `01a1461a-e3dd-7000-8ca6-5c22def14aea` | B (`...4c8d-7000-80df-fdc458534eea`) | 3 | `2026-10-22` | `2026-10-22` | `9-12` | 180,000 |

Proven without client intersection: distinct GroupKeys before the first save; distinct `ShippingGroupId`s after
save; the earliest dates differ exactly by the preparation delta (2 vs 3 days) with the same method lead;
independent selected dates/windows survive projection re-read and a second re-read; editing group A persists A's
new window while B's date, window and single line membership stay byte-identical; an early (past) date for A fails
closed with 4xx and an out-of-window slot fails closed with `shipping.delivery.slot_unavailable`, and neither
rejected write overwrites the last accepted selection; cart line membership is still exactly the two submitted
offers at the end of the flow (no premature checkout/charge).

Live proof found no client-side intersection: group A's eligible date chips start `2026-10-21`, group B's start
`2026-10-22` (see `ui-dom-assertions-152.txt`).

## (B) Read-only payment draft — PASS (this closes the TB-151 409)

`GET /v1/storefront/cart/{cartId}/payment-draft` answered **HTTP 200** (no longer `409 checkout.tax.unavailable`)
and, in the same response, carried two groups mapped to the two sellers on the same canonical method:

| Field | Value |
| --- | --- |
| groups | 2, distinct sellers, both `tipax:standard`, identities equal to the latest accepted save |
| line mapping | one cart line per group, no shared line |
| preserved schedules | A `2026-10-21 / 15-18` (last accepted), B `2026-10-22 / 9-12` |
| per-group fees | 180,000 + 180,000 |
| `shippingAmount` | 360,000 = sum of the two group fees (charged once per group) |
| `subtotalExclusiveOfTax` | 14,350,000 |
| `discountAmount` | 0 |
| `taxAmount` | 1,291,500 (authoritative Tax owner: 9% of subtotal) |
| `payableAmount` | 16,001,500 = subtotal − discount + tax + shipping (exact) |
| `checkoutId` | `null` — the draft is a read-only seam, nothing was created |

`payment-draft-152.json` holds the full sanitized payload. No financial share allocation was invented and no
non-existent field was asserted: the contract exposes `MinimumDeliveryDate`, `SelectedDeliveryDate`,
`SelectedDeliveryTimeWindow`, `LeadDays`, `LineCount`, `CartLineIds` per group, and all of them agree with the
persisted draft (`db-readback-152.txt`).

## (C) Real private browser acceptance — PASS with one noted limitation

Authenticated through the private frontend only (real OTP fixture session for the sanctioned Development mobile);
the two-seller cart was prepared and the persisted draft was restored by the app itself. Captured from the actual
app (no synthetic UI, no mock images):

- `ui-shipping-two-groups-152.png` — two distinguishable «تیپاکس معمولی» cards, each with its own seller label
  («فروشنده schema موبایل» / «فروشگاه آرمان»), its own item, its own fee (۱۸۰٬۰۰۰ ریال), its own earliest date
  (`2026-10-21` vs `2026-10-22`) and its own date + window controls.
- `ui-shipping-independent-windows-152.png` — the same page after changing **only** group A's window.
- `ui-dom-assertions-152.txt` — DOM reads:
  - initial load: A `tipax:standard / 2026-10-21 / 12-15`, B `tipax:standard / 2026-10-22 / 9-12`;
  - real UI action on A (window → `15-18`): A changed, **B unchanged** (`2026-10-22 / 9-12`);
  - full page reload: A restored to the server-persisted `12-15` (the unsaved local edit is not persisted) and B
    still `2026-10-22 / 9-12`; the header showed the same cart (۲ کالا / ۱۴٬۳۵۰٬۰۰۰ ریال).
  - `data-testid=shipping-continue` («ادامه به پرداخت») was enabled while both groups carried
    method+date+window.

Limitation (reported, not hidden): the window chips are single-select with no deselect, so a real
"one group unselected" state cannot be produced from this cart through the UI; the gating clause is therefore
evidenced by the enabled state only, not by an observed disabled→enabled transition.

## (D) Repeated-save ShippingGroupId continuity — OPEN / MUST FIX

Reproduced in this run (`selection-repeat-save-152.json`, OBSERVED entry in the assertion matrix):

| Save step | Group A id | Group B id |
| --- | --- | --- |
| first accepted save | `01a1461a-e2d6-7000-a25e-0c1cb0b9447d` | `01a1461a-e2d7-7000-bdd0-208d5c3eb2a3` |
| after editing A | `01a1461a-e370-7000-b3b3-d89b8a08649a` | `01a1461a-e370-7000-b14d-9362ac531794` |
| identical repeated save | `01a1461a-e3dd-7000-add7-4a7d9600829d` | `01a1461a-e3dd-7000-8ca6-5c22def14aea` |

Every accepted save re-issues both draft `ShippingGroupId`s, so the locked "stable ShippingGroupId" requirement is
still unmet. Schedules, line membership and fees remain correct and consistently mapped to the latest ids (the
payment draft in (B) references exactly the latest save), but identity is not stable across saves. This is
documented as `OPEN / MUST FIX`; no schema rewrite or opportunistic id overhaul was attempted, per the task
boundary.

## Other Open items (not this task's scope)

- authoritative carrier calendar / capacity / timezone;
- same-seller same-method with incompatible origins;
- Admin monetary modes / create-method;
- release blockers tracked in `docs/planning/MUST-FIX-BEFORE-RELEASE.md`.

## No premature mutation

`db-readback-152.txt`: `checkouts = 0`, `seller_orders = 0`, `order_lines = 0`, `order_shipping_groups = 0`,
`payments = 0`, `payment_attempts = 0`, while `cart_shipping_groups = 2` and `cart_shipping_group_lines = 2` with
one line per group. The whole shipping navigation and group editing never created a checkout, order or payment.

## Reproduction

```powershell
# 1. disposable scratch cluster (task-owned port/name)
docker run -d --name tooba-tb152-pg -e POSTGRES_USER=scratch152 -e POSTGRES_PASSWORD=<env-only> `
  -e POSTGRES_DB=tooba_152_scratch -p 127.0.0.1:55471:5432 postgres:16-alpine
# 2. publish + isolate
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Debug -o .tmp-152-host-out
$env:TOOBA_152_SCRATCH_PASSWORD='<env-only>'; node docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152\prepare-isolated-target-152.js
# 3. minimal scratch tax fixture (both seeded offer categories must resolve a rule)
docker exec -e PGPASSWORD=$env:TOOBA_152_SCRATCH_PASSWORD tooba-tb152-pg psql -U scratch152 -d tooba_152_scratch -f /tmp/seed-tax.sql
# 4. private Host + private Next (never :5088 / :3000)
powershell -NoProfile -File docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152\launch-isolated-host-152.ps1 -Port 5413
$env:TOOBA_HOST_ORIGIN='http://127.0.0.1:5413'; npx next dev -p 3108
# 5. real journey + real browser
$env:TOOBA_152_HOST='http://127.0.0.1:5413'; node docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152\journey-152.js
```

## Evidence index

| File | Content |
| --- | --- |
| `isolation-guard.json` | fail-closed isolation guard for the private target |
| `host-launch-152.txt`, `host-launch-history-152.txt` | private Host launch, PID, health, launch history |
| `journey-152.js`, `journey-152-trace.txt`, `assertion-matrix-152.txt` | authenticated HTTP acceptance + 32 assertions |
| `projection-before-save-152.json`, `projection-after-save-152.json` | two-seller independent horizons |
| `selection-save-152.json`, `selection-edit-a-152.json`, `selection-repeat-save-152.json` | save / edit-A / repeated-save identity |
| `payment-draft-152.json` | read-only payment draft (HTTP 200) with per-group fees and authoritative totals |
| `db-readback-152.txt` | group ids, schedules, line mapping, fee, zero checkout/order/payment rows |
| `ui-shipping-two-groups-152.png`, `ui-shipping-independent-windows-152.png` | real private browser screenshots |
| `ui-dom-assertions-152.txt` | DOM-level per-group assertions, independence, reload restore |
| `cart-after-flow-152.json` | cart integrity after the whole flow |
