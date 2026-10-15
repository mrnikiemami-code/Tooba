# TB-SHIPPING-FULL-OPERATIONS-078 — Development fixture ledger (read-only run)

## Provenance (positively identified before any call)

```
GET http://127.0.0.1:5088/__platform-commerce
=> edition=SingleStore, deploymentId=dev-singlestore, tenantId=store-alpha,
   connectionReference=tenant-alpha, defaultMarketReference=IR
Database: tooba_alpha
```

The existing Development Host was **already running** and was **not** restarted, rebuilt or
reconfigured by this task. No staging or Production endpoint was contacted, and no scratch database
was created.

## Mutation statement

This task performed **zero writes**. Mission A added no route and no backend change, and the
inspector exposes no create/edit/delete affordance for rates, coverage, packaging policies or
schedules. The live matrix consists only of `GET`s plus the authorized diagnostic `POST`
`/v1/admin/shipping-services/{serviceCode}/operational-quote`, which is read-only by construction
(it is modelled as a query and exposes no write seam).

Byte-level no-write proof (`log-live-operational-quote.txt`, phase 16):

```
rates identical before/after:      True
packaging identical before/after:  True
schedules identical before/after:  True
GET /v1/admin/shipping-services/tipax/operational-quote -> 405   (no read route exists)
```

## Pre-existing fixtures used (owned by earlier tasks, NOT created or changed here)

| Identity | State observed | Used for |
| --- | --- | --- |
| `tipax` / `standard` / Alborz (`1f8050a8-…`) rate policy `01a13d84-ca00-…` | `isActive=true`, `180000 IRR`, prep 1 / lead 3 | the `Quoted` freight component |
| `tipax` / `express` / Alborz rate policy `01a13d84-ca7f-…` | `isActive=true`, `260000 IRR` | the inactive-schedule branch |
| `tipax` / `standard` schedule `01a13deb-cd7c-…` | `isActive=true`, `Asia/Tehran`, Sunday 13:00–14:00 and 16:30–18:00, capacity 25 | `Available` / `Unavailable` / `NotEvaluated` |
| `tipax` / `express` schedule `01a13eaf-2710-…` | `isActive=false` | `InactiveNotEnforced` |
| `tipax` packaging policies (`express`, `standard`, `express`) | all three `isActive=false` | the explicit zero-fee fallback |
| `tipax` / `standard` weight tiers `01a13e5d-22a9-…` (0–2000) and `01a13e5d-4aa8-…` (2000–5000) | both `isActive=false` | see the boundary honesty note below |

## The one honest gap, stated rather than papered over

There is **no active weight-tier rate** anywhere in the current Development data, so the live
endpoint cannot exercise the `1999` vs `2000` winning-tier boundary. `phase 11` therefore shows two
successful live calls whose freight is the same unconditioned `180000 IRR` rule, and the boundary
itself is covered by the focused unit fixture in
`shipping-operational-quote-api.test.ts` / `shipping-operational-quote-model.test.ts` plus the exact
`parseKilogramsToGrams` contract owned by 074. **No 1999/2000 live tier boundary is claimed.**

Similarly, there is currently **no active packaging policy** for any service in Development, so the
charged-packaging branch (`packagingAmount > 0`, `isCharged=true`, `shipping.packaging.charged`) is
covered by the unit fixture built from the real 076 answer shape, not by a live call. Creating one
purely for UI cosmetics was explicitly avoided.
