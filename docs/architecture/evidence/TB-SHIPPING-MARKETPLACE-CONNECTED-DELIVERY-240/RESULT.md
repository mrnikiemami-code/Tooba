# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240 — RESULT

Status: **PARTIAL** (STOP_FOR_ARCHITECT, automaticNextImplementationTask: NONE)
Base source: `dc987ce4ee7feb34037d6424ccb3c3cf16fe8867` (issue commit containing this task)
Reviewed prior source: `3c726019449f1ccad0dadc93e783292ac8de276f` (239-R1)
Target: task Marketplace Host `http://127.0.0.1:5190` (PID 23444 at handoff), Development,
DeploymentId `tb239-marketplace`, Tenant NULL, DB `tooba_marketplace_tb239`.
No activation, no Visual ACCEPT, no PSP/payment-success claim.

## 0 — One-line outcome

The Marketplace sellable fixture now really exists and the mixed-owner *read* is real, but the
connected delivery still cannot be completed on this target: the fresh Marketplace database has
**zero Geography places and zero canonical content languages**, and neither has an existing
module-owned Development seam or any write endpoint in this repository. Every captured shipping
group therefore refuses with `storefront.captured_preview.destination_required` before any method,
selection, preview, coupon or Order stage can be reached.

## 1 — What was actually landed (source)

One opt-in Host composition seam, no new business authority:

- `src/backend/Host/Tooba.Host/Configuration/MarketplaceSellableSeedOptions.cs`
  Section `Tooba:Development:MarketplaceSellableSeed`, `Enabled` default **false**.
- `src/backend/Host/Tooba.Host/Development/MarketplaceDevelopmentBootstrap.cs`
  New `EnsureSellableFixturesAsync` runs INSIDE the already-assigned Tenant=NULL Marketplace
  `CommerceContext`, before `ISellerDevContextStore.EnsureAsync`, and calls ONLY the existing
  Catalog-owned Development interface `IWorkspaceDemoSeed.IsLiveProductSeededAsync` /
  `SeedNewProductAsync`. The existence check is the EXACT live fixture (never `Any(products)` — the
  566 Draft catalog rows are irrelevant), so a repeat/restart seeds nothing and consumed stock is
  never replenished. The seam body is testable with explicit inputs; no Datum/DbContext/SQL/entity
  work exists in Host.
- `src/backend/Host/Tooba.Host/Program.cs` binds the new options section.
- `src/backend/Host/Tooba.Host.Tests/Development/MarketplaceSellableSeedBootstrapTests.cs`
  4 focused facts: seeds only on Development + opt-in + absent fixture; never reseeds an existing
  fixture; disabled/Production/Staging seed nothing; checked-in `appsettings*.json` contains no
  `MarketplaceSellableSeed` key and the Host bootstrap owns no business data.

Focused run with the existing 239-R1 seam guard: **15/15 passed**, Host.Tests build 0 errors.

## 2 — Checkpoint (minute 8) — PASSED with real data

The exact module-owned seed (`WorkspaceDemoSeed.SeedNewProductAsync` → `WorkspaceDemoMarketplaceSeed`)
ran through existing Party/Offer/Pricing/Inventory/Tax Development Contracts gateways:

| fact | measured |
|---|---|
| real seller organizations | `party.parties = 2` (`01a181b0-5c87-7000-976e-ae5ce19864f6` = فروشگاه آرمان, `01a181b0-5c9a-7000-bf9b-8c8e1c47da72` = دیجی‌استایل نمونه) |
| sellable offers | `offer.offers = 3`, all `Active`: `ARM-LN-01` `01a181b0-5caf-7000-9c44-e82f2457a122`, `ARM-SC-01` `01a181b0-5e93-7000-bcc8-85f8532bd345` (same Seller A), `DGS-LN-01` `01a181b0-5cd7-7000-8ba1-a45b0df5425f` (Seller B) |
| prices / tax / stock | `pricing.prices = 3`, `tax.categories = 1`, `tax.rules = 1`, `tax.offer_classifications = 3`, `inventory.stock_positions = 4`, `inventory.locations = 3` |
| seller own scope | `GET /v1/seller/offers` with seller A token + own seller header → **200** (706 bytes, 2 offers) |
| seller foreign scope | same token + Seller B header → **403** |
| seller B own scope | own token + own header → **200** (355 bytes) |
| admin vs seller on catalog demo | admin **200**, seller **403** (239-R1 behaviour preserved after 2 restarts) |
| idempotence on restart | after a real Host restart the same four counts were unchanged (`parties=2 offers=3`), i.e. the create-only fixture was respected |

No 890-second 566-product reset was repeated; the bulk Draft catalog was left as-is.

## 3 — Mixed-owner read really works (new, real HTTP)

Normal buyer registered through the public API (`POST /v1/auth/register` → `01a181b6-0b20-7000-895e-bd938f048575`),
normal token login, normal guest cart (`POST /v1/storefront/cart` → `01a181b8-2640-7000-a27e-a9c926ca1cf1`,
market `IR`, currency `IRR`, channel `Marketplace`), three real offer lines added through
`POST /v1/storefront/cart/{cartId}/lines` with `X-Tooba-Cart-Version` + `X-Tooba-Guest-Secret`
(→ v7, 3 lines, subtotal 5,090,000 IRR, 2 sellers).

`POST /v1/storefront/shipping/projection` → **200**, `capturedPreview.enabled = true`,
`itemCount 3.0`, `sellerCount 2`, and exactly THREE per-line groups keyed on the real seller ids:

```
cap|unassigned|01a181b05caf70009c44e82f2457a122  seller 01a181b0-5c87-…-ae5ce19864f6
cap|unassigned|01a181b05e937000bcc885f8532bd345  seller 01a181b0-5c87-…-ae5ce19864f6
cap|unassigned|01a181b05cd770008ba1a45b0df5425f  seller 01a181b0-5c9a-…-8c8e1c47da72
```

Every group refuses `storefront.captured_preview.destination_required`; `methods`/`deliveryDates` are
empty. Supplying the legacy `provinceName`/`cityName` text DOES NOT satisfy the captured path (same
refusal), which matches the accepted 232/233 decision that only trusted Geography identities may price.

Shipping administration authorization itself is proven working: after
`POST /v1/admin/access-control/bootstrap` the configured DEV admin holds `fulfillment.view` +
`fulfillment.manage` capability tuples, `GET /v1/admin/shipping-services/authoring-drafts` → **200**,
and `POST /v1/admin/shipping-services/authoring-drafts` reaches business validation with
**400 `shipping.service_code.unknown`** (not 403).

## 4 — Exact blockers (source-confirmed, measured)

1. **Geography has no rows and no way to create them.**
   `geography.countries = 0`, `geography.administrative_areas = 0`, `geography.cities = 0`.
   `Tooba.Geography.Endpoints` exposes ONLY reads
   (`/v1/admin/geography/countries|…/administrative-areas|…/cities|…/service-areas` and the
   `/v1/storefront/geography/places/*` equivalents); a repository-wide search finds no Geography
   Development seed, no importer and no write command. The store-alpha dataset used by earlier
   sibling tasks was therefore authored out-of-band; reproducing it here by direct SQL or by copying
   another database is explicitly forbidden by this task.
2. **No canonical content languages.**
   `localization.languages = 0`. Consequently a canonical shipping method cannot even be created:
   `POST /v1/admin/shipping-services/shipping-methods/one-level` requires at least one REAL
   registered language name (`ShippingMethodNameRequired` / `ShippingServiceLanguageInvalid`), so no
   publication can be authored for Platform / Seller A / Seller B.
   An EXISTING module-owned Development seam does exist and is the correct next step:
   `Tooba.Localization.Infrastructure.Bootstrap.LanguageBootstrapDevelopmentSeed.PostMigration.EnsureAsync(provider, ct)`
   (idempotent, Development-gated, no hardcoded ids) — it is simply not yet composed by the
   Marketplace bootstrap.
3. **Empty shipping catalog.** `fulfillment.shipping_services = 0`, `shipping_methods = 0`,
   `shipping_types = 0` (hence `shipping.service_code.unknown`).
4. **AccessControl capability tuples need a second bootstrap pass on a fresh owner.** Observed:
   first `POST /v1/admin/access-control/bootstrap` → 204 but the subsequent authoring POST still
   returned `403 admin.authorization.denied`; the same POST again (also 204) made the SAME request
   reach business validation. Not repaired here (out of this task's seam); recorded because it is
   the difference between "no permission" and "no data".

Because of (1), none of the remaining required stages were reachable — no publication on one shared
binding, no per-group method/date choice, no selection save, no payment preview, no StartPayment /
Order / manual Pending, no coupon read/selection/final parity, no duplicate-submit or immutable Order
history proof, and no desktop/mobile PAYMENT captures. They are NOT claimed.

## 5 — Budget accounting (no new clock)

The user's four-hour envelope is cumulative. 239-R1 reported ≈53 focused minutes; this task used
≈70 focused minutes (12:10→13:20 local) including 2 real Host restarts and the reference-data
investigation. Cumulative ≈123 min of the 240-minute upper estimate; ≈117 minutes remain. No fresh
clock was started. The remaining work cannot be completed inside that remainder: commissioning
Geography + languages + method, three owner publications, coupon, Order/manual Pending, duplicate
submit, immutable Order read and three payment viewports is a multi-hour slice.

## 6 — Preservation

- Checked-in preview/selection/checkout defaults stay **false**; only the named non-production task
  runtime env enables them.
- No schema/migration/baseline edit; 231/232/236 remain applied on this task DB; stale EF snapshot
  gap retained.
- The USER-visible review window, FE 3000 / Host 5088, the 238 handoff FE 3100 / Host 5188 and
  `D:\temp\tb238-handoff-chrome` were never enumerated, read, automated, reloaded or closed; no HMR.
- The 230 DEV publication, the user's original draft, rates, week, pointer, assignments and the
  handed-off cart were not touched. No DB reset/reseed/drop, no direct business SQL, no cross-DB
  copy.
- TB185 stays paused/default-OFF/PendingAllocation. 238's `SQLSTATE 22001` stays UNCLASSIFIED.

## 7 — Next architectural decision (not an automatic task)

To make the connected delivery reachable on a fresh Marketplace installation, the missing
reference-data commissioning has to exist as module-owned Development seams (or as an explicit
decision to point this task at a Marketplace database that already contains Geography + languages):

1. compose the existing Localization language seed in the Marketplace bootstrap;
2. add ONE Geography-owned Development seed (Iran + a bounded province/city set) with the same
   Development + opt-in guards, OR decide that Geography is provisioned out-of-band and provide that
   dataset;
3. then this task's remaining flow (3 owner publications on one binding, coupon partial/invalid/
   clear, payment preview + StartPayment manual Pending, duplicate UI+HTTP, immutable Order read,
   1440/390/360 payment captures) becomes executable.
