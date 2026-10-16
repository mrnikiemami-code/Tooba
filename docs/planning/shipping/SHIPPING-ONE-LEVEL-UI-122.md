# TB-SHIPPING-ONE-LEVEL-TABS-122 — Isolated storefront shipping DOM label parity

Parent: TB-SHIPPING-ONE-LEVEL-TABS-121 · Channel: `tooba-main` · Worker: `tooba-worker-01`
Mode: **BOUNDED_ISOLATED_STOREFRONT_SHIPPING_DOM_LABEL_PARITY_PROOF** (observation only — no code change)

## 1. Question this task answers

TB-121 proved, at the API layer, that a freshly booted isolated Development Host populates the
enabled one-level catalog and that the authorized storefront projection returns 7/7 labels sourced
from it. It explicitly did **not** prove anything about the rendered UI. TB-122 asks the remaining
question: does the actual storefront `/fa/shipping` page, rendered in a real browser against an
isolated Host with a genuine scratch guest cart, display those same seven methods and labels?

## 2. The binding (one minimal, tracked-file-free seam)

The repo already owns the seam: `src/frontend/next.config.ts` rewrites `/v1/:path*` to
`process.env.TOOBA_HOST_ORIGIN ?? "http://127.0.0.1:5088"`. The isolated frontend was therefore
started **once** in dev mode on `http://127.0.0.1:3101` with `TOOBA_HOST_ORIGIN=http://127.0.0.1:5401`.
No tracked file was edited: the binding is process environment only, and the repo's own committed
`.env.local` (which points at the shared `:5088`) did **not** override the process value.

Binding provenance is independently checkable, because the two hosts differ observably:

| Probe | Result |
| --- | --- |
| `GET http://127.0.0.1:3101/v1/storefront/checkout-identity-policy` (via the isolated frontend proxy) | `{"policy":"GuestAllowed", …}` |
| `GET http://127.0.0.1:5401/v1/storefront/checkout-identity-policy` (scratch host) | `{"policy":"GuestAllowed", …}` |
| `GET http://127.0.0.1:5088/v1/storefront/checkout-identity-policy` (shared host) | `{"policy":"AuthenticatedOnly", …}` |

`GuestAllowed` was set on the scratch host only, through the canonical
`PUT /v1/admin/settings/checkout-identity` seam with the Development admin actor. The browser's `/v1`
traffic therefore demonstrably resolved to the scratch target, and the checkout auth gate stayed
open for a guest — which is what makes a genuine guest shipping page reachable at all.

## 3. Isolated runtime (same guarded recipe as TB-121)

| Item | Value |
| --- | --- |
| Scratch PostgreSQL | `127.0.0.1:55453` (unique; never `5432` or a prior scratch port), disposable, no volume |
| Scratch databases | `tooba_122_scratch`, `tooba_122_messaging` (task-owned) |
| Isolated Host | `.tmp-122-host-out\Tooba.Host.exe` on `http://127.0.0.1:5401`, watchdog 180 s |
| Isolated frontend | `next dev` on `http://127.0.0.1:3101`, external bound 600 s, task-owned PID |
| Tenant | `store-alpha:Active:tenant-122-scratch` (exactly one, active) |
| Gate | `Tooba:ShippingOneLevelCatalog:Enabled=true` in the disposable overlay only |
| `RunLegacyBootstraps` / `AllowResetAndSeed` | `true` / `false` |
| `ShippingRateAuthority` / `ShippingOperationalShadow` | absent / off |
| Shared `:5088`, `:3000`, `tooba_alpha`, containers | read-only, never started/stopped/migrated |
| Offline guard | `isolation-guard.json` → `failClosed=true`, no shared database/reference/port |

Both process launches were single, absolute-path, PID-recorded and bounded; the scratch password came
from the environment and is not committed.

## 4. Genuine cart session — no seeding, no forgery

The guest cart used for the proof was created by **the real storefront UI**: one click on the PDP
add-to-cart button (`schema-mobile-demo-phone`) on `/fa/products/schema-mobile-demo-phone`. That left
`sessionStorage["tooba.storefront.cartId"]` plus a 64-character guest secret in the tab — the app's
own session mechanism. No URL seeding, no `bootstrapCartSessionFromQuery`, no synthetic headers, no
manual SQL, no mocked projection, no substituted HTML.

## 5. DOM proof (one observation pass, one dedicated browser tab)

Rendered page: `http://127.0.0.1:3101/fa/shipping`, `document.documentElement.lang = "fa"`.

Structure flags — the page rendered the **projection branch**, not the no-projection fallback:

```
shipping-page          present
shipping-steps         present
shipping-methods       present
shipping-methods-empty ABSENT      <- the fallback/empty state is NOT rendered
shipping-summary       present
shipping-continue      present     <- NOT clicked
shipping-error         ABSENT
header cart badge      "۱"
```

Hero step semantics (`shipping-steps`): `shipping-step-1` = «سبد خرید» (cart),
`shipping-step-2` = «ارسال» (shipping), `shipping-step-3` = «پرداخت» (payment).

Rendered method cards — seven `data-testid="shipping-method-{methodCode}"` elements:

```
shipping-method-post:express     "پیشتاز ۲۰۰٬۰۰۰ ریال حدود ۲ روز"
shipping-method-post:standard    "معمولی ۱۲۰٬۰۰۰ ریال حدود ۴ روز"
shipping-method-tipax:express    "پیشتاز ۲۶۰٬۰۰۰ ریال حدود ۱ روز"
shipping-method-tipax:standard   "معمولی ۱۸۰٬۰۰۰ ریال حدود ۳ روز"
shipping-method-snapp_courier    "اسنپ / پیک آنلاین ۹۰٬۰۰۰ ریال آماده امروز"
shipping-method-store_courier    "پیک فروشگاه ۷۵٬۰۰۰ ریال حدود ۱ روز"
shipping-method-in_person        "تحویل حضوری رایگان آماده امروز"
```

## 6. 7/7 three-way comparison

| methodCode | one-level catalog (fa) | browser-origin projection | rendered DOM label | match |
| --- | --- | --- | --- | --- |
| `post:express` | پیشتاز | پیشتاز | پیشتاز | ✔ |
| `post:standard` | معمولی | معمولی | معمولی | ✔ |
| `tipax:express` | پیشتاز | پیشتاز | پیشتاز | ✔ |
| `tipax:standard` | معمولی | معمولی | معمولی | ✔ |
| `snapp_courier` | اسنپ / پیک آنلاین | اسنپ / پیک آنلاین | اسنپ / پیک آنلاین | ✔ |
| `store_courier` | پیک فروشگاه | پیک فروشگاه | پیک فروشگاه | ✔ |
| `in_person` | تحویل حضوری | تحویل حضوری | تحویل حضوری | ✔ |

Catalog: `gate=Enabled`, `ready=true`, 7 rows. Projection: 7 methods, `status 200`, fetched from the
**browser page context for the same cart**, so the compared baseline is not a separate server-side run.
Formatted prices in the DOM also match the projection for all seven (`۲۰۰٬۰۰۰ / ۱۲۰٬۰۰۰ / ۲۶۰٬۰۰۰ /
۱۸۰٬۰۰۰ / ۹۰٬۰۰۰ / ۷۵٬۰۰۰ / رایگان`). **mismatches = 0.**

## 7. Honest scope of the DOM evidence

The DOM carries the canonical `methodCode` in the test id and renders the label plus the formatted
price. `serviceCode` / `optionCode` are projection fields and are not emitted as DOM attributes, so
the DOM does not independently re-assert them; their mapping to these same seven `methodCode`s was
established at the projection layer (this task's browser-origin projection and TB-121's binding
reads). What the DOM proves is therefore: seven canonical `methodCode` identities, seven Persian
labels matching both the enabled one-level catalog and the projection, and matching formatted prices.

## 8. What was deliberately NOT done

Continue/payment was never clicked; `PUT shipping/selection` and `shipping/commit` were never
invoked; no order, checkout, payment, shipment or shipping-selection was created; no manual SQL; no
forged actor; no synthetic auth header. Selection-persistence semantics are out of scope and remain
covered only by TB-118's earlier focused source/test work.

## 9. Cleanup and non-interference

The isolated Host and the isolated frontend were stopped only by their recorded PIDs after verifying
their absolute paths; the task-owned container and the ignored `.tmp-122-run` / `.tmp-122-host-out`
directories were deleted; scratch ports have zero listeners and the raw cart secret was never written
to evidence. The one dedicated browser tab was closed; every pre-existing tab was left untouched.

Read-only shared parity is identical before and after: shared Host PID `35624` at
`.tmp-dev-host-out-5088-101\Tooba.Host.exe`, `:5088` `/health` 200 with `tenant-alpha` provenance,
`:3000` status 308, containers `postgres-db`+`rabbitmq` unchanged, and `tooba_alpha` counts
byte-identical.

## 10. Remaining non-claims

- This is a Development, scratch-tenant proof. It is not production acceptance and does not certify
  commercial readiness, checkout, payment, shipment lifecycle, or shipping-selection persistence.
- The one-level gate stays off outside the disposable overlay; nothing here changes shared
  configuration, feature flags or the locked architecture.
- PASS is not an Architect ACCEPT.
