TB-SHIPPING-ONE-LEVEL-TABS-117 — STAGE-BY-STAGE ISOLATED STARTUP PROOF
=====================================================================

One launch only: ISOLATED_HOST_PID=15724, absolute exe `.tmp-117-host-out\Tooba.Host.exe`,
ASPNETCORE_URLS http://127.0.0.1:5398, external watchdog limit 180 s, LAUNCH_COUNT=1, no retry,
no HTTP mutation endpoint, no manual SQL write, no reset-and-seed / 545-product expansion.

Publish source: HEAD 709e474951c0aa6f5481e2ff4658d24c02533dea (current main with the TB-116
commerce-context repair), `dotnet publish -c Debug`, 0 errors.
Scratch: fresh no-volume `tooba-117-scratch-pg` (postgres:16-alpine) on loopback 127.0.0.1:55450 with
task-owned `tooba_117_scratch` / `tooba_117_messaging`; per-run credentials only via the
TOOBA_117_SCRATCH_PASSWORD environment variable, kept solely in the deleted ignored run directory.
Offline fail-closed guard (`isolation-guard.json`, `failClosed=true`): tenantCount 1, activeTenantCount 1,
canonicalGatewayTenantPresent true, tenants [store-alpha:Active:tenant-117-scratch], references
[tenant-117-scratch, messaging-117-scratch], no `tooba_alpha`, no `tenant-alpha`, no Port=5432, no prior
scratch port, Development, RunLegacyBootstraps=true, AllowResetAndSeed=false, ShippingRateAuthority /
ShippingOperationalShadow / ShippingOneLevelCatalog absent.

Stage table
-----------
| # | Stage | Result |
| --- | --- | --- |
| 1 | Host builder / DI container build | OK — no wiring or scope-validation error |
| 2 | `DevelopmentSchemaMigrator.ApplyAsync(app.Services)` (Program.cs:367) | COMPLETED — 112 EF migration events; ordered two-phase migration for catalog/inventory/localization/offer/party/pricing/promotion/tax |
| 3 | Catalog attribute schema + workspace demo seed inside the migrator's own assigned scope | COMPLETED — includes store_landing_pages inserts (first at stdout line 1455) and the workspace chain up to admin-r3-archived-hat |
| 4 | Legacy Catalog bootstrap gateway `DevelopmentTenantCommerceContext.RunForDevelopmentTenantAsync` (Program.cs:374, TB-116) | ENTERED — scope created and trusted CommerceContext assigned |
| 5 | `FashionTemplateCatalogSeed.ApplyAsync` → `CatalogDbContext` options → `ToobaNpgsql.ResolveForContext` | **SUCCEEDED** — no `platform.edition.unconfigured`, no root-provider or DI-lifetime error |
| 6 | All six legacy Catalog bootstraps | **COMPLETED** — template inserts begin at stdout line 1659 (450 `template_products`, 30 `store_templates` statements); landing-page and store-menu seeds idempotent after the migrator scope |
| 7 | Kestrel listen | **REACHED** — `Now listening on: http://127.0.0.1:5398` (line 1795), `Application started` (1796), `Hosting environment: Development` (1797) |
| 8 | `GET /health` | **200** `{"status":"ok"}` at ELAPSED_SECONDS=45 of the 180 s budget |
| 9 | `GET /__platform-commerce` | **200** `{"edition":"SingleStore","deploymentId":"isolated-117","tenantId":"store-alpha","connectionReference":"tenant-117-scratch","resolvedHost":"127.0.0.1","themeReference":"theme-alpha","defaultMarketReference":"IR"}` — proves ISOLATED scratch provenance, not the shared store |

Marker counts over the whole 1861-line stdout: `platform.edition.unconfigured` = 0,
`Cannot resolve scoped service` = 0, `from root provider` = 0, `catalog.attribute.code.duplicate` = 0,
`catalog-dev.attribute.incompatible-reuse` = 0, `Development seed requires Active tenant` = 0.
stderr = empty (no unhandled exception). The former TB-115 barrier is **eliminated in real runtime**.

Only non-information lines: two pre-existing EF query-shape warnings (Skip/Take without OrderBy;
First/FirstOrDefault without OrderBy) and two OutboxDispatcher warnings after the Host was already
healthy — `Outbox claim failed for one module. Schema=returns` and `Schema=settlement (42P01 relation
does not exist)`. The `returns` and `settlement` schemas are simply not part of this Development scratch
schema set; the dispatcher logs a warning and continues. Not a startup blocker.

Scratch read-back (row presence only — NOT a storefront-eligibility proof)
-------------------------------------------------------------------------
Migration history: catalog 29, inventory 5, localization 1, offer 3, party 2, pricing 1, promotion 2, tax 1.
Definitions: color 1, ram 1, screen_size 1, storage 1 — exactly ONE color definition.
Options: color/black 1, color/blue 1, ram/8gb 1, storage/128gb 1, storage/256gb 1 — no duplicate option.
Single-identity proof: PHONE-BLK-128, PHONE-BLU-256 and LIVE-SHIRT-BLK all store the SAME color
definition_id `01a1428e-cc3f-7000-abb5-7c7600a2640d`, and PHONE-BLK-128 shares LIVE-SHIRT-BLK's canonical
black value — the legacy seed reuses the schema-owned definition at real startup.
Products: schema-mobile-demo-phone Published with exactly 2 variants and exactly 1 default
(PHONE-BLK-128); workspace-live-shirt Published; admin-r3-draft-scarf Draft; admin-r3-archived-hat Archived.
Sellable chain: PHONE-BLK-128 (default) 2 offers / 2 active / 1 active Base price / available 5.000000;
PHONE-BLU-256 1 offer / 1 active / 1 active Base price / available 5.000000. Totals: offers 5,
active_offers 5, prices 6, active_base_prices 4, stock_positions 6, positive on_hand 5, seller_parties 4
(party.parties, no 42P01), tax_rules 1.
LEGACY TEMPLATE SEEDS NOW REACHED (0 in TB-115): template_products **150**, template_variants 150,
template_categories 560, template_brands 60, store_templates **10**. Landing/menu seeds: store_landing_pages 3
with 16 sections, store_menus 1 with 6 items.
Scratch stays commerce-free: carts 0, seller_orders 0, shipping_methods 0. No cart/order created, no
shipping flag activated.

Cleanup / non-interference
--------------------------
Host PID 15724 verified not alive; listeners on 5398 = 0. `docker rm -f tooba-117-scratch-pg` removed the
only task-owned container; listeners on 55450 = 0 afterwards; no 113/115 container was touched and the
pre-existing containers (sarv-jaeger, sarv-redis, rabbitmq, postgres-db) are untouched. Ignored
`.tmp-117-run` (incl. the per-run credential file) and `.tmp-117-host-out` deleted. No name/wildcard
termination; the only process ever stopped was the recorded isolated PID (after health, bounded).
Read-only shared postflight parity: :5088 PID 35624 `/health` 200, :3000 PID 31580 -> 308 `/fa`,
`/__platform-commerce` unchanged (store-alpha / tenant-alpha / dev-singlestore / theme-alpha / IR), and
tooba_alpha counts byte-identical (methods 7, translations 14, carts 50, cart_lines 78, drafts 14,
seller_orders 26, products 567, offers 3223). No INCIDENT.
