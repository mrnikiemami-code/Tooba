# TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166 — authenticated real-browser acceptance of method-owned rates + unavailable-method notices

Parent: `TB-SHIPPING-STOREFRONT-OWNED-RATE-BROWSER-165` (PARTIAL). Program: `TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS`.
Mode: `BOUNDED_REPRODUCIBLE_PRIVATE_SEED_AND_AUTHENTICATED_BROWSER_ACCEPTANCE`.
Base SHA: `200d904204c2f491659844ffa80199ed08bc2a5b`. Verdict: **PASS** (zero production source change; evidence commit only).

## What closed the TB-165 gap

TB-165 could not run the browser leg because the scratch database had no seller/catalog/tax/geography fixture.
TB-166 built the missing **reusable seed recipe** (`seed-recipe-166.md`) and executed the whole customer leg in a
real authenticated browser against a task-owned Host + Next + PostgreSQL.

## Proven legs

| Required leg | Result |
| --- | --- |
| Runnable reproducible scratch seed recipe | `seed-recipe-166.md` + `scratch-166-base-fixture.sql` + `scratch-166-extra-fixture.sql`; ran end-to-end in this task (~4 min to an authenticated page) |
| Two genuine seller groups, same canonical method, method-owned prices | Two cards with distinct persisted group ids `01a1480c-afb8…` / `01a1480c-afb9…`, both `tipax:standard`, **205000 IRR each**, summary `هزینه ارسال ۴۱۰٬۰۰۰ ریال` |
| Independent group schedules | Each card owns its date chips (2026-10-22…28) and windows (9-12/12-15/15-18/18-21) from the server horizon; no page-wide date control |
| Continue gating | `shipping-continue` disabled while a group lacked its own schedule/address, enabled only when complete |
| Save → payment preview → reload | Real save, `/fa/payment?cartId=…` preview with `هزینه ارسال ۴۱۰٬۰۰۰` and `قابل پرداخت ۱۶٬۰۵۱٬۵۰۰`; full document reload restored the **same** group ids and schedules |
| Unavailable-method UI against a real server negative | 5 methods (`post:express`, `post:standard`, `snapp_courier`, `store_courier`, `in_person`) rendered as read-only, per-method Persian notices (`shipping-group-unavailable-methods`), never selectable, no price/0/رایگان, no seller attribution |
| Negative write integrity | `PUT /v1/storefront/shipping/selection` with `post:standard` → **400 `shipping.method.unavailable`**; projection after the rejection unchanged (tipax 205000/480000 still eligible) |
| OFF parity | Host relaunched with overlay `Enabled=false` on the same cart/session → legacy rates (post 120000/200000, tipax 180000/260000, snapp 90000, store 75000, in_person 0) and `unavailableMethods` **absent** |
| No premature side effects | `checkouts 0`, `seller_orders 0`; one line per group (1/1); `DUP_GROUP_IDS 0` |
| Cleanup | Private Host PIDs stopped, `next dev` on 3116 stopped, container `tooba-tb166-pg` removed; ports 3116/5421/55486 closed (`cleanup-166.txt`) |

## Isolation matrix

| Concern | Value |
| --- | --- |
| Tenant | single Active `store-alpha -> tenant-166-scratch` (disposable DB) |
| Edition | `SingleStore` (no swap) |
| PostgreSQL | `127.0.0.1:55486`, db `tooba_166_scratch` + `tooba_166_messaging` |
| Host | `127.0.0.1:5421` |
| Frontend | `127.0.0.1:3116` (`TOOBA_HOST_ORIGIN=http://127.0.0.1:5421`) |
| Forbidden targets | `:3000`, `:5088`, `:5432`, `tooba_alpha`, `tenant-alpha`, shared sessions — untouched |
| Committed gate values | `appsettings.json` / `.Development.json` / `.Production.json` all `ShippingOwnedRateAuthority.Enabled=false` |
| Gate-ON location | scratch overlay only (`.tmp-166-run`, deleted after the run) |

## Fixture notes (disclosed)

- Shipping/Geography do **not** register runtime schema migrators by design, so their schemas were created with
  the canonical design-time context (`TOOBA_DESIGN_TIME_CONNECTION`), not by Host DDL.
- The storefront address form submits province/city **names**; name-based destination resolution in this chain
  stops at administrative-area scope, so the same two owned amounts (205000 / 480000) are also declared at
  area scope. Amounts and method ownership are unchanged; this is a scratch fixture detail, not a product change.
- Two extra active 9% tax rules were inserted for seller A's categories so the payment draft returns 200 instead
  of the TB-153-style 409 (same approach as TB-154).

## Method

Zero product source change: no frontend, Order, Shipping, Tax, Admin, migration, schema, DI or config modified.
Elapsed: ~55 minutes (seed-recipe discovery, live browser leg, OFF control, cleanup); the seed recipe is now
durable so later tasks skip that discovery cost.

Artifacts: `projection-on-166.json`, `projection-off-166.json`, `projection-on-166` trace (`http-trace-on-166.txt`),
`ui-dom-assertions-166.txt`, `ui-shipping-two-groups-on-166.png`, `db-readback-groups-on-166.txt`,
`db-readback-on-166.txt`, `isolation-guard-on-166.json`, `isolation-guard-off-166.json`, `cleanup-166.txt`,
`host-launch-166-on.txt`, `host-launch-166-off.txt`, `prepare-isolated-target-166.js`,
`launch-isolated-host-166.ps1`, `probe-projection-166.cjs`, `seed-recipe-166.md`.

STOP_FOR_ARCHITECT · Automatic-Next-Task: NONE
