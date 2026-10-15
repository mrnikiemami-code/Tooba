# SHIPPING-ONE-LEVEL-UI-102 — isolated storefront label projection (BLOCKED)

Task `TB-SHIPPING-ONE-LEVEL-TABS-102` (parent `TB-SHIPPING-ONE-LEVEL-TABS-101`) · Channel `tooba-main`
Mode `BOUNDED_ISOLATED_STOREFRONT_LABEL_PROJECTION_PROOF` · Stop-State `STOP_FOR_ARCHITECT`
**Status: BLOCKED — `BLOCKED_ISOLATED_CART_PROOF`**

## 1. Preflight

| Check | Result |
| --- | --- |
| Claim | exactly one Pending task on `tooba-main` (`Working` heartbeat → `GET /api/tasks/next`); receipt preserved **UNSTAGED** |
| Base SHA | `Expected-Base-SHA` `09870abc0…` == `HEAD` == `origin/main`; zero `src/**` drift; only the pre-existing dirty frontend screens and prior untracked evidence remained, untouched |
| Shared stack | `:5088` healthy (pid 37200, gate `ShippingOneLevelCatalog=true`), `:3000` untouched, `tooba_alpha` counts captured read-only **before** the task (7 methods / 14 translations / 50 carts / 78 lines / 14 drafts / 26 seller orders) |
| Minimal path chosen | `POST /v1/auth/register` → `POST /v1/auth/login` → `POST /v1/storefront/cart` → `POST /v1/storefront/cart/{id}/lines` (the canonical Development customer flow proven in task 067), then `POST /v1/storefront/shipping/projection` |

## 2. Isolation (before any scratch DML)

- New disposable container `tooba-102-scratch-pg` (`postgres:17`, loopback `127.0.0.1:55442`, no volume) with
  databases `tooba_102_scratch` and `tooba_102_messaging` and a task-generated user; the password was generated
  for this task and kept only in an untracked ephemeral file (never in evidence, never in git).
- The guarded overlay (`isolated/prepare-isolated-target-102.js`) declares exactly one tenant (`store-alpha`,
  Active) and exactly two connection references, both pointing at the scratch port — the effective merged config
  judged by the launcher is the config that was actually launched (`isolated-102`, reference `tenant-102-scratch`).
- Isolated Host on `http://127.0.0.1:5192` from a task-owned output directory built from current code; one-level
  gate only ever set as a process environment value; `ShippingRateAuthority` / `ShippingOperationalShadow` never set.
- Provenance after launch: `{"edition":"SingleStore","deploymentId":"isolated-102","tenantId":"store-alpha",
  "connectionReference":"tenant-102-scratch",…}`.
- Inside the container only `tooba_102_scratch` / `tooba_102_messaging` (plus templates) exist — there is no
  `tooba_alpha` in the scratch server, so no scratch statement can touch the shared database.

## 3. What blocked Mission A/B — no sellable offer in the scratch catalog

Two supported configurations were tried on the empty scratch database:

1. `Tooba:CatalogDemo:RunLegacyBootstraps=true` — the host **terminates during migration** with an unhandled
   `InvalidOperationException: محصول برای انتشار آماده نیست` from
   `CatalogDevelopmentSeed.PostMigration.EnsureAsync` → `CatalogAttributeSchemaDevelopmentSeed.ApplyAsync` →
   `CatalogDirectory.PublishProductAsync`. The Catalog-owned post-migration attribute step tries to publish a
   product whose prerequisites do not exist on a genuinely empty database. Verbatim stack:
   `legacy-bootstrap-crash-excerpt.txt`.
2. `Tooba:CatalogDemo:RunLegacyBootstraps=false` — the documented schema-only path: migrations apply, the host is
   healthy and fully isolated, but **no catalog demo product/offer is created**, which is exactly what the shared
   Development database got from its own seed history.

The only remaining way to obtain an offer would have been hand-built raw catalog/offer/price/inventory fixtures —
the shortcut the task explicitly forbade ("Do NOT invent raw schema fixtures that evade normal cart access rules"),
and one that would make the resulting cart an artificial construct rather than a genuine storefront cart. Per the
task's own instruction for this case, the work stopped with `BLOCKED_ISOLATED_CART_PROOF` instead of expanding scope
or editing production source.

## 4. Genuine proof that WAS obtained on the isolated stack

| Step | Result | Meaning |
| --- | --- | --- |
| `GET /__platform-commerce` | `200` `isolated-102 / tenant-102-scratch` | the isolated target, not shared |
| projection, anonymous | `401 checkout.authentication_required` | identity gate is live on the isolated stack |
| `POST /v1/auth/register` (customer) | `201` (repeat call `500 platform.unexpected` duplicate identifier) | real identity creation, no bypass |
| `POST /v1/auth/login` | `200` with a real session/token | real credential login, not a fabricated actor header |
| `POST /v1/storefront/cart` | `200` with a cart and its guest secret | API-created owned cart |
| projection with that owned cart | **`400 shipping.cart.empty`** | the identity gate **and** the cart access rules were satisfied legitimately — the request reached the Order projection handler |
| `POST …/cart/{id}/lines` with an offer | `400 cart.offer.unavailable` | the only missing piece is a sellable catalog offer |

Because `RequireCartAsync` rejects an empty cart before any method list is built, no `methods[]` payload exists in
either gate state, so the gate-OFF vs gate-ON label parity (MISSION B) could not be attempted. No test was used as
a substitute for HTTP proof, and no claim is made from the 100 unit tests.

## 5. Cleanup and shared-state parity

- Isolated Host stopped; `5192` listeners after stop: **0**; scratch container removed (only the pre-existing
  `rabbitmq` and `postgres-db` remain); task-owned run/output directories and untracked credential files deleted.
- Shared stack untouched and operational: `:5088` still pid 37200 `/health` 200, provenance
  `dev-singlestore / store-alpha / tenant-alpha`, gate unchanged, `:3000` never restarted.
- Shared `tooba_alpha` read-only parity after cleanup: 7 methods, 14 translations, `post:express`/`snapp_courier`
  still inactive, 50 carts, 78 lines, 14 drafts, 26 seller orders — identical to preflight. **No shared DML.**

## 6. For the Architect

1. The 102 proof needs a source of a genuine sellable offer in an isolated environment. Options: (a) authorize a
   task to fix/parameterize the Catalog Development bootstrap so it can seed an empty database; (b) authorize
   defined raw SQL catalog/offer fixtures for scratch-only use as an explicit exception; (c) authorize
   `POST /v1/admin/catalog/demo/reset-and-seed` on an isolated host with an admin actor, if that path does not hit
   the same failing prerequisite.
2. The observed crash is an unimplemented-order/seed-ordering defect of the Development bootstrap on an empty
   database; it was **not** repaired here (out of scope) and does not affect the shared Development environment,
   whose catalog was seeded previously.
3. Minor observation for triage: a duplicate customer identifier returns `500 platform.unexpected`
   (`IdentityDuplicateIdentifierFault`) rather than a typed conflict.
4. Elapsed: claimed ≈03:12 local, finished ≈03:45 local ≈ **33 minutes**, i.e. a disclosed overrun of the 30-minute
   HARD timebox; every artifact was cleaned up and the state was left coherent rather than abandoning cleanup.
