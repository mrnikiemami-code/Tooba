# TB-STOREFRONT-PERF-PUBLISHED-SCALE-182 — authorized scratch publication + two published sizes

**Status: PASS (two true Published storefront sizes, 4.33x)**
Base SHA: `dbbfa4c3b997b5cab000763d1efb70c49557907d` (HEAD == origin/main before work)

## 1. What was proven

| | Size A | Size B |
|---|---|---|
| Published products in DB | 3 | 13 |
| Distinct product IDs in Home payload | 3 | 13 |
| Home payload bytes | 33,964 | 52,084 |
| Warm-up + 3 measured responses | HTTP 200 | HTTP 200 |
| Measured wall time (3 requests) | 72.9 ms (first of A run) | 93.0 / 75.1 / 68.2 ms |
| Body SHA-256 (3 requests) | — | identical `5C61FF1A…` in all three |
| Ratio | 1.0 | **4.33x** |

Both sizes come from the **same binary, same scratch database, same tenant, same
locale (`fa`), and the Warning-default EF log level**; the only change between the
two legs is the published-catalog population, produced by real owner-authorized
publication (section 3). This is the first TB-178..182 measurement where the
storefront-visible item count — not the Draft row count — actually grew ≥4x.

## 2. Fixture and starting point (size A)

Disposable loopback PostgreSQL 16 (`127.0.0.1:55502`, db `tooba_182_scratch`) and a
private Host (`127.0.0.1:5453`), started by the task's own publish output, guarded by
`prepare-isolated-target-182.js` (`isolation-guard-182.json`: PASS; no
`tooba_alpha`/`store-alpha`/`:5432`/`:3000`/`:5088` contact).

Starting state after Host-owned Development bootstrap:

```
products=5 published=3 draft=1 archived=1 variants=4 offers=6 prices=7 stock=7
```

Size A measurement (`home-size-a.json`, 33,967 B file / 33,964 B HTTP body):
`featuredProducts=3, specialOffers=1, campaignProducts=0, newArrivals=3`, distinct
payload product IDs = **3**.

## 3. Authorized publication provenance (no bypass)

Every size-B item entered the storefront through four **owner** HTTP routes with
real validators and real Development actors/memberships:

1. `POST /v1/seller/offers` — Offer module CQRS (`CreateOfferCommand`), authorized by
   `ISellerOfferAuthorizer` → `SellerPanelAccess` → `user#member@party` tuples of the
   genuine Development memberships in `party.memberships`
   (`seller-actor-a@…` → فروشگاه آرمان, `seller-actor-b@…` → دیجی‌استایل نمونه, both
   `status=Active, relation_code=member`). No grant was injected or forged.
2. `PUT /v1/seller/offers/{id}/price` — Pricing through the Offer owner boundary
   (`SetOfferPriceCommand`), Active Base price in `IRR`/`IR`.
3. `PUT /v1/seller/offers/{id}/inventory` — Inventory through the Offer owner
   boundary (`SetOfferInventoryCommand`), `onHand=25`.
4. `POST /v1/admin/products/{id}/publish` — ProductWorkspace CQRS →
   `PublishProductCommand` → `ProductLifecycleDirectory.PublishAsync`, which requires
   `ProductPublishReadiness.IsReady` (category / identity / attributes / variants /
   media / SEO) before `product.Publish(...)`.

No SQL write to `catalog.products.status`, no row cloning, no seeder change, no new
route, no lifecycle-rule change, no production data.

Honest acceptance ratio: of 38 attempted demo Draft products, **13 products ended
Published** (3 baseline + 10 newly published). The publish route returns
`200` with the `ApiResponse` envelope, so HTTP status alone is not success; the DB
transition is the criterion. The rejected products failed genuine publish-readiness
checks (demo Draft fixtures are not uniformly publish-ready) — validation was
**not** bypassed to reach the 4x size.

Demo seeder provenance: one bounded `POST /v1/admin/catalog/demo/seed-only`
(Development-only, `AllowResetAndSeed`, authorized admin actor) grew the scratch
catalog to `products=198, draft=194, categories=122, variants=645`; it is the only
row source besides the 5 pre-existing Host-bootstrap products.

## 4. Request-scoped performance comparison

| Leg | Home requests | Payload bytes | Wall time | Published items |
|---|---|---|---|---|
| A | 1 warm-up + 3 measured | 33,964 | 72.9 ms (warm measured set, all 200) | 3 |
| B | 1 warm-up + 3 measured | 52,084 | 93.0 / 75.1 / 68.2 ms | 13 |

Latency did **not** degrade proportionally to the 4.33x published growth; size B
stayed in the same sub-100 ms band on the same binary and same EF Warning default.
The payload is byte-deterministic across the three size-B requests (same SHA-256),
so the difference between A and B is a population difference, not run-to-run noise.

This task did not recreate the TB-178 logging fix and did not re-run a diagnostic
`EF=Information` leg; per TB-179/TB-180 the request-scoped command count for this
Home path was already separated from startup commands (37 request-scoped commands
per request window at size A). Because size B changes only the number of Published
products and their offers/prices/stock, a disproportionate command growth at size B
would show up as latency; it did not. Any formal size-B command count remains an
open evidence item, explicitly labelled as not measured here.

## 5. Payload integrity

`payload-integrity-182.json` records, per card: productId, title, sellerName,
`offerAmountExclusiveOfTax`, currency, availableUnits, slug. Checks performed:

- every size-B card carries a real productId, title, seller name, IRR amount and
  `availableUnits=25` from the offer/pricing/inventory writes above;
- no card lacks price or seller (a card cannot be composed without an Active Base
  price and a resolvable primary offer — `ComposeProductAsync` returns `null`);
- the three baseline Published products remain present and their substantive fields
  are unchanged between `home-size-a.json` and `home-size-b.json`;
- single tenant (`store-alpha`) and single scratch database: no cross-tenant rows can
  exist in this fixture.

## 6. Release gate

PERF-CRIT-001 stays **OPEN**. This task closed the missing *two true Published
sizes* evidence, not the register's full acceptance: Architect review, the formal
register decision, and the user's real-browser responsiveness verdict are still
outstanding, and no size-B EF command count was captured.

## 7. Isolation and cleanup

- Task-owned only: container `tooba-tb182-pg`, db `tooba_182_scratch` +
  `tooba_182_messaging`, ports `55502` (PG) and `5453` (Host). No shared service,
  port, database, Next cache, Redis, RabbitMQ, or Host was touched or restarted.
- The pre-existing user Host process was left running and untouched.
- Cleanup verified: container/volume removed, `.tmp-182-*` artifacts deleted,
  `5453`/`55502` no longer listening, no scratch Host process remaining.
