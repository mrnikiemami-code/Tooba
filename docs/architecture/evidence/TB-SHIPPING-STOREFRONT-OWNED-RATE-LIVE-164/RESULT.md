# TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164 — RESULT

MODE: BOUNDED_ISOLATED_AUTHENTICATED_STRICT_OWNED_RATE_ACCEPTANCE
VERDICT: **PASS** (authenticated ON journey, fail-closed negatives, OFF parity, and one narrow owner-correct DI fix)

## Baseline
- Claimed at `HEAD == origin/main == 8960eaabf438a7e4bdc3e4b3e81ffb4a82e4ca90` (task Expected-Base-SHA `44d604fd9…` plus the task-owned TB-163 result-artifact commit; no divergence, no rebase, no force-push).
- Pre-existing unrelated dirty paths (task files, evidence artifacts from earlier tasks) were left untouched and not staged.

## Isolation (fail-closed guard)
- Postgres `tooba-tb164-pg` → `127.0.0.1:55484` only, scratch dbs `tooba_164_scratch` / `tooba_164_messaging`, user `scratch164`.
- Host `http://127.0.0.1:5419`; scratch tenant `store-alpha -> tenant-164-scratch`; `Edition: SingleStore`.
- Only authority flag enabled anywhere: `Tooba:ShippingOwnedRateAuthority:Enabled=true` **in the disposable overlay** (`.tmp-164-run/appsettings.Development.json`). Committed defaults remain `false` in `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` (asserted by the guard).
- Shadow gates (`Tooba:ShippingRateAuthority`, `ShippingOperationalShadow`) never set.
- Shared `:5432`/`tooba_alpha`, `:3000`, `:5088`, RabbitMQ, Redis and other workers were never started, migrated, written or restarted.
- Guard artifacts: `isolation-guard-on-164.json`, `isolation-guard-off-164.json` → `failClosed=true`.

## Narrow owner-correct fix (real seam defect found)
The scratch Host failed startup DI validation because the Shipping module registered only
`IShippingRateCoverageDirectory`, while the TB-161/162 command handler requires
`IShippingMethodCoverageDirectory` (implemented by the same adapter).

`src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs`:
one `AddScoped<IShippingMethodCoverageDirectory, ShippingRateCoverageDirectory>()` registration.
No behavior, contract, schema or policy change; no Host edit.

This also fixed a BOM accidentally introduced into the three committed `appsettings*.json` files in
TB-163 (stripped, content unchanged) — otherwise the config loader produced an invalid JSON document for
tooling.

## Fixture (SCRATCH ONLY)
`scratch-164-fixture.sql`: real Geography chain (IR → THR → THR-C → THR-C-01) and
- `tipax:standard` = **205000 IRR**, `tipax:express` = **480000 IRR** (exact option, city scope, unconditioned)
- provider/service-level row `tipax` `option_code NULL` = **99000 IRR** (must never leak)
- explicit `Unrestricted (0)` method-coverage declarations for both strict options
- plus transactional tax rules (9%) for every classified offer category (required for the read-only draft to be calculable).

## Authenticated ON journey (real customer session)
`/v1/auth/register` (Phone) + real OTP login (`Development` fixture) as customer `09111111111`, two-seller cart
(sellers `…e0a9` and `…eb2b`, offers `…e169` and `…eb3c`), then:

| Check | Result | Evidence |
| --- | --- | --- |
| Projection `tipax:standard` = 205000 (owner exact option) | PASS | `projection-on-164.json` |
| Legacy configured 180000 never reaches the customer quote | PASS | `projection-on-164.json` |
| Sibling `tipax:express` = 480000 (own option), never standard/provider | PASS | `projection-on-164.json` |
| Provider-only 99000 row never prices a strict option | PASS | `projection-on-164.json` |
| No strict method presented as free/zero | PASS | `projection-on-164.json` |
| Two-group selection save accepted; each persisted group = 205000 IRR | PASS | `selection-save-164.json` |
| Re-read draft: two groups at 205000, stable `ShippingGroupId`s, one disjoint line each | PASS | `projection-on-reread-164.json` |
| Read-only payment draft HTTP 200: two groups 205000 each, `shippingAmount = 410000` exactly | PASS | `payment-draft-on-164.json` |
| Payment arithmetic `payable = subtotal − discount + tax + shipping` | PASS | `payment-draft-on-164.json` |
| No checkout id, no order/payment rows | PASS | `payment-draft-on-164.json`, `db-readback-164.txt` |

DB readback (`db-readback-164.txt`): `checkouts 0`, `seller_orders 0`, `order_lines 0`, `payments 0`,
`cart_shipping_drafts 1`, `cart_shipping_groups 2` → `…e45da7e8c527 tipax:standard 205000.00`,
`…304d9d2eb04b tipax:standard 205000.00`.

## Fail-closed negatives (same scratch, same authenticated customer)
1. Owned express rule deactivated → projection excludes `tipax:express`; reason
   `shipping.quote.owned_option_not_priced`; remaining standard still 205000; selecting express returns
   **HTTP 400 `shipping.method.unavailable`**; the previously saved draft stays intact at 205000/205000.
2. Express rule re-activated but method declaration set to `Restricted (1)` with no allowed grant →
   projection excludes express with **`shipping.coverage.method_restricted`**.

Artifacts: `checks-negative-164.json`, `projection-negative-164.json`,
`projection-negative-restricted-164.json`, `trace-negative-164.txt`.

No quote error ever fell back to the legacy price or to 0/FREE.

## OFF parity (private scratch only)
Host restarted with `Tooba:ShippingOwnedRateAuthority:Enabled=false` (same overlay file, gate flipped),
same tenant/customer/cart:

- `tipax:standard` = **180000**, `tipax:express` = **260000**, full legacy list including
  `post:*`, `snapp_courier`, `store_courier`, `in_person` (0/FREE as before) — `projection-off-164.json`.
- `unavailableMethods` absent from the payload (`false`), i.e. the gate did not evaluate and no owner
  quote/unavailable mapping happened.
- Health 200, no owner/geography call path exercised (resolver returns `Disabled` before any lookup; unit-tested
  zero-call parity in TB-163).

## Tests
- `dotnet build Tooba.Host` (after the DI fix) → 0 errors.
- TB-163 focused suite remains the behavioral guard for the OFF zero-call and fail-closed mapping; no test
  changed in this task.

## Isolation/cleanup
`cleanup-164.txt`: ON Host PID stopped, OFF Host PID stopped, container `tooba-tb164-pg` removed, port 5419 no
listener, shared services untouched. No PSP/SMS call, no final purchase, no order.

## Honest unverified / not claimed
- No browser/DOM screenshot was captured; proof is HTTP + DB provenance only.
- Frontend rendering of `unavailableMethods` was not exercised (contract only).
- Tax fixture rows were seeded directly in the scratch DB (tax policy authoring is out of this task's scope).
- Worker PASS is not Architect ACCEPT.
