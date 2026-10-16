# SHIPPING-ONE-LEVEL-UI-119 — Isolated Storefront Shipping Projection & One-Level Label Proof

Parent task: `TB-SHIPPING-ONE-LEVEL-TABS-119` (Parent-Task `TB-SHIPPING-ONE-LEVEL-TABS-118`).
Mode: `BOUNDED_ISOLATED_STOREFRONT_SHIPPING_PROJECTION_AND_LABEL_PROOF`.
Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-119/**`.

## Result

`PARTIAL` — the **isolated authorized storefront shipping projection is PROVEN** (7 real methods with
labels, 12 provinces, under verified scratch provenance). The **one-level display-label half is BLOCKED
by a canonical missing dependency**: the one-level catalog is readable but has zero rows on a fresh
database, and no sanctioned runtime path exists to populate it.

## 1. Proven live (single isolated Host launch, PID 23440, `127.0.0.1:5399`)

- Provenance: `GET /__platform-commerce` → `deploymentId=isolated-119`,
  `connectionReference=tenant-119-scratch`, `tenantId=store-alpha` — isolated scratch, not shared.
- `/health` 200 in 40 s of the 180 s watchdog; `LAUNCH_COUNT=1`; a 110 s bounded keep-alive window for
  the proof probes; stopped afterwards **only** by the recorded PID (`STOPPED_BY_RECORDED_PID=True`).
- Authorized **`POST /v1/storefront/shipping/projection` → 200** with a real scratch cart:
  | methodCode | label | price | lead |
  | --- | --- | --- | --- |
  | `post:express` | پست — پیشتاز | 200 000 | 2 |
  | `post:standard` | پست — معمولی | 120 000 | 4 |
  | `tipax:express` | تیپاکس — پیشتاز | 260 000 | 1 |
  | `tipax:standard` | تیپاکس — معمولی | 180 000 | 3 |
  | `snapp_courier` | اسنپ / پیک آنلاین | 90 000 | 0 |
  | `store_courier` | پیک فروشگاه | 75 000 | 1 |
  | `in_person` | تحویل حضوری | 0 (رایگان) | 0 |

  12 provinces, 4 delivery time windows, `maxSellerPreparationDays=1`, subtotal 12 500 000 IRR.
  A second projection with `provinceName=تهران` returned the same 7 methods.
- Authorization was obtained **only** through canonical seams: `GET /v1/admin/dev-context` for the
  Development admin actor, then `PUT /v1/admin/settings/checkout-identity {"policy":"GuestAllowed"}` →
  re-read `checkoutAuthenticationRequired:false`. No fake actor, no auth bypass, no manual SQL.

## 2. Blocked half — the one-level label mapping

With `Tooba:ShippingOneLevelCatalog:Enabled=true` set **only** in the disposable isolated overlay:
`GET /v1/admin/shipping-services/shipping-methods/one-level` → **`{"gate":"Enabled","ready":true,
"languageId":"01a1429f-af2a-7000-8f70-71ecb0870e3d","methods":[]}`** — the gate/read plumbing is healthy
and the catalog is empty. Consequently every projected label above is the **legacy** composed label
(`service — option`), i.e. `ApplyOneLevelDisplayNamesAsync` overrode nothing and invented no name.

Canonical missing dependency (static proof):

| Seam | Behaviour |
| --- | --- |
| `20261015120000_AddShippingOneLevelCatalog` | DDL only — no data |
| `ShippingOneLevelCatalogActivation` | `MethodNotFound`, never creates a row |
| `ShippingOneLevelCatalogAdminWrite` | `MethodNotFound`, never creates a row |
| `ShippingOneLevelBackfillStore` / `ShippingMethodBackfillPlanner` | the only row creator; DI-registered at `FulfillmentModule.cs:117` but **has no production/Host caller** |
| `IShippingCatalogSeedPort` (`ensure-seed`) | seeds the legacy services/options only |

So populating the catalog would require direct SQL DML or new production code — both forbidden here.

## 3. UI parity

Not attempted: the isolated frontend could not be bound to the isolated Host within the budget, so **no UI
claim is made** (no DOM, no screenshot). The rendering surface itself remains as certified in TB-118
(`/fa/shipping` renders the 3-step hero; option rows are client-side and appear only once the projection
resolves). Marked `PARTIAL`, per the acceptance clause.

## 4. Selection preservation

Not re-issued: TB-118's source proof plus its 8/8 focused frontend test remain the evidence, and this task
explicitly forbids issuing a PUT/commit merely to demonstrate navigation. No PUT/commit was sent.

## 5. Shared parity / non-interference

- `:5088` PID 35624 `/health` 200, `/__platform-commerce` unchanged; `:3000` PID 31580 → 308.
- `tooba_alpha` counts byte-identical: methods 7, translations 14, carts 50, cart_lines 78, drafts 14,
  seller_orders 26, products 567, offers 3223. No INCIDENT.
- All scratch writes went to the task-owned disposable database only, through canonical application APIs.

## 6. Cleanup

Host PID 23440 stopped after absolute-path verification; `:5399` and `:55451` no longer listening;
`docker rm -f tooba-119-scratch-pg` removed the only task-owned container; ignored `.tmp-119-run` (with the
per-run credential file) and `.tmp-119-host-out` deleted. Credentials never written to evidence.

## 7. Rollback

Docs/evidence only — nothing to roll back.

## 8. Stop state

`STOP_FOR_ARCHITECT`; `automaticNextImplementationTask = NONE`. No shipping commercial-readiness, checkout
cutover or payment claim.
