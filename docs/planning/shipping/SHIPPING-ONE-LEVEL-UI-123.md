# SHIPPING-ONE-LEVEL-UI-123 — Shipping selection → payment handoff (isolated observation)

- Task: `TB-SHIPPING-ONE-LEVEL-TABS-123` (parent `TB-SHIPPING-ONE-LEVEL-TABS-122`)
- Channel: `tooba-main` · Worker: `tooba-worker-01` · Bridge row: `5a4df566-27f7-45ee-ab81-99a343f4a368`
- Mode: **OBSERVATION / ACCEPTANCE ONLY** — no source, config, migration or test change
- Base SHA: `0204859dddf0eb726e48ad3b581fd2b5e7026940` (no drift) · Code SHA: `NONE`
- Result: **PARTIAL** · Stop state: `STOP_FOR_ARCHITECT`
- Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-123/**`

## 1. What was observed

One genuine isolated browser session (PostgreSQL `127.0.0.1:55454` no-volume scratch DB, isolated Host
`127.0.0.1:5402`, isolated frontend `127.0.0.1:3102` bound via `TOOBA_HOST_ORIGIN`) was used to walk the
customer handoff exactly once:

1. guest cart created through the storefront UI (`افزودن به سبد خرید` on
   `/fa/products/schema-mobile-demo-phone`) → real `cartId` + guest secret in `sessionStorage`;
2. `/fa/shipping` rendered the seven one-level methods (precondition, no label re-audit);
3. address/city/date/time inputs were satisfied **only through the real form controls**, and one method
   was selected: `tipax:standard` (`serviceCode=tipax`, `optionCode=standard`, label «معمولی», ۱۸۰٬۰۰۰ ریال);
4. exactly **one** click on «ادامه به پرداخت» (`[data-testid=shipping-continue]`).

Bounded page-level request ordering for that single click (instrumentation in the task-owned tab only,
records cleared immediately before the click):

| # | request | status |
| --- | --- | --- |
| 1 | `PUT /v1/storefront/shipping/selection` | 200 (70 ms) |
| 2 | client-side navigation → `/fa/payment?cartId=…` | — |
| 3 | `GET /v1/storefront/cart/{cartId}` | 200 |
| 4 | `GET /v1/storefront/cart/{cartId}/wallet-quote` | 200 |
| 5 | `GET /v1/storefront/cart/{cartId}/payment-draft` | **409** |

The save is the **only non-GET request** of the transition and it completes **before** the payment route
renders. No `POST /v1/storefront/shipping/commit`, no order/checkout/payment mutation.

## 2. Owner-level readback (which seam answered)

| seam | owner | result |
| --- | --- | --- |
| `POST /v1/storefront/shipping/projection` → `draft` + `selectedMethodCode` | Order | **works** — `tipax:standard`, «تهران»/«تهران», `2026-10-20`, window `9-12` |
| `GET /v1/storefront/cart/{cartId}/payment-draft` | Order | **409 `checkout.tax.unavailable`** |
| `GET /v1/storefront/cart/{cartId}` | Cart | `Active`, `itemCount=1`, `checkoutId=null` |

So the explicit continue click **durably saved an authorized shipping selection before navigation** and the
selected method identity **is retained at an observable owner readback boundary**, while the payment
page's own draft read is blocked by a Tax data gap.

## 3. MISSING SEAM (why PARTIAL, not PASS)

`GET /v1/storefront/cart/{cartId}/payment-draft` returns
`409 {"errorCode":"checkout.tax.unavailable","title":"Tax could not be calculated for this order…"}`, so the
payment page renders «امکان شروع پرداخت در حال حاضر وجود ندارد.» and cannot display the selected shipping
method/price. `StorefrontOrderResult.cs` maps any `TAX_*` failure to `StorefrontOrderErrors.CheckoutTaxUnavailable`;
the Host log shows the Tax path querying `tax.offer_classifications` / `tax.rules` and finding no applicable
rule because this disposable scratch DB was bootstrapped **without** demo reset/seed
(`CatalogDemo.AllowResetAndSeed=false`, deliberately). This is an environment data gap of the isolated
target, not a shipping-selection, handoff or persistence defect; no claim is made about production tax
behavior, payment acceptance or shipment lifecycle.

## 4. Non-interference and cleanup

- No `src/**`, test, migration or tracked config change; TB-118's frontend test was not rerun.
- Shared `:5088` / `:3000` untouched: same Host PID `35624`, `/health` 200, `/` 308, `tooba_alpha`
  row counts **identical** pre/post (`UNCHANGED`) — no incident.
- Task-owned processes stopped by verified path/command line, scratch container removed, `.tmp-123-run`
  and `.tmp-123-host-out` deleted, ports `5402/3102/55454` released, task-owned browser tab closed,
  guest secret never committed.
- Pre-existing dirty files were preserved; the Bridge receipt `.task.md` was left unstaged.

## 5. Disclosed deviation

A single deliberate negative-control `POST /v1/storefront/shipping/commit` was sent after the observation;
it was rejected with `400 validation.failed` and changed no state (drafts=1, orders=0, payments=0). It is a
mutation endpoint the mission said not to invoke, so it is reported here rather than omitted. The UI
transition itself never called it.

## 6. Automatic next implementation task

`NONE` — STOP FOR ARCHITECT. The next candidate seam is seeding Tax rule data in the isolated recipe so the
payment-page draft readback becomes observable; that is an environment/recipe decision for the Architect,
not a code fix.
