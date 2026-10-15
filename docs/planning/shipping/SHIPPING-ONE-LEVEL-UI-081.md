# TB-SHIPPING-ONE-LEVEL-TABS-081 — one-level shipping methods and tabbed management: source-grounded implementation blueprint

**Mode:** BOUNDED_READ_ONLY_ARCHITECTURE_AUDIT_AND_IMPLEMENTATION_DESIGN. **No production code, schema,
migration, seed, or runtime process was changed by this task.** Everything below is grounded in files read
on the verified base, plus one read-only live Development catalog probe.

**Base:** `36bdcff729def1537cb9f76ea1b0c44622003846` (`HEAD == origin/main`, verified after `git fetch`).
Pre-existing dirty/untracked paths were preserved untouched; no `RECOVERY_CONFLICT`.

**Target of the read-only live probe:** `GET /__platform-commerce` on `http://127.0.0.1:5088` →
`edition=SingleStore`, `deploymentId=dev-singlestore`, `tenantId=store-alpha`,
`connectionReference=tenant-alpha` (Development, never staging/production). Host was **not** restarted and
**not** rebuilt. Raw log: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-081/log-live-catalog-identity.txt`.

**Scope ruling.** This document is a **proposal**. It does not authorize task 082, does not enable any gate,
and does not create the example names «پست معمولی» / «پست پیشتاز» / «تیپاکس معمولی» / «تیپاکس اکسپرس» as
live data. No automatic next implementation task is created.

---

## 0. Verdict in one line

The current design is **two merchant-visible levels (service → option) whose real pricing/schedule/packaging
identity is the pair `service:option`**, and that pair is already the identity the storefront, the legacy
checkout draft and the paid snapshot persist. A one-level merchant surface is achievable **without migrating
existing records** by promoting the pair to a first-class *delivery method* whose stable code stays
byte-identical to today's `service:option` string for all six existing identities — but the choice between
"new Fulfillment table" and "flattened projection of the existing pair" is a genuine architectural fork and
is reported as **STOP_FOR_ARCHITECT**, not decided here.

---

## A. Inventory of identifiers and references (existing structure)

### A.1 Fulfillment — owner of the service/option catalog

| Concern | File | Symbol / anchor |
| --- | --- | --- |
| Parent aggregate | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingService.cs` | `ShippingService` (`ShippingServiceId`, `Code`, `ProviderKind`, `IconKey`, `ColorKey`, `SortOrder`, `IsActive`) |
| Child aggregate | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingServiceOption.cs` | `ShippingServiceOption` (`ShippingServiceOptionId`, `ShippingServiceId`, `Code`, `SortOrder`, `IsActive`) |
| Translations | `.../Aggregates/ShippingServiceTranslation.cs`, `.../Aggregates/ShippingServiceOptionTranslation.cs` | name (+ optional description on the parent only) |
| Tables | `.../Infrastructure/Persistence/FulfillmentDbContext.cs` (schema `fulfillment`, line 82) | `shipping_services` (:167), `shipping_service_translations` (:178), `shipping_service_options` (:187, **unique** `(ShippingServiceId, Code)` :191), `shipping_service_option_translations` (:196) |
| Migration | `.../Persistence/Migrations/20260910170000_AddShippingServiceCatalog.cs` | creates the four tables |
| Contracts | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Shipping/ShippingCatalogContracts.cs` | `ShippingCatalogServiceSnapshot`, `ShippingCatalogOptionSnapshot`, `IShippingCatalogReader`, `IShippingCatalogSeedPort` |
| Seed | `.../Infrastructure/Shipping/ShippingServiceDirectory.cs` `EnsureSeedAsync` (:109–179) | `post` → options `express`,`standard`; `tipax` → `express`,`standard`; `snapp_courier`, `store_courier`, `in_person` → **no options** |
| Admin write | same file `CreateAsync` (:37), `UpdateAsync` (:66), `DeactivateAsync` (:100) | `UpdateAsync` **removes and re-adds all options** of the service (:88–92) |
| Admin API | `.../Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs` | `GET /`, `GET /{serviceId}`, `POST /`, `PUT /{serviceId}`, `POST /{serviceId}/deactivate`, `POST /ensure-seed` under `/v1/admin/shipping-services` |
| Read models | `.../Application/Shipping/ShippingServiceReadModels.cs` | `ShippingServiceListItemDto` carries `OptionCount` + `ActiveOptionCount`; `ShippingServiceDetailDto` carries `Options[]` |
| Write models | `.../Application/Shipping/ShippingServiceWriteModels.cs` | `ShippingServiceWriteModel(..., IReadOnlyList<ShippingServiceOptionWriteModel> Options)` |
| Enabled tree | `.../Application/Shipping/Queries/ListEnabledShippingMethodsTreeQuery.cs` → `.../Endpoints/Shipping/ShippingMethodsEndpoints.cs` | `GET /v1/admin/shipping-methods` (no admin auth gate, deliberately) |

### A.2 Live Development catalog identity (read-only probe, `log-live-catalog-identity.txt`)

```
GET /v1/admin/shipping-services  -> 200
post           active  options 2  activeOptions 1   id 6e9a4cdf-…
tipax          active  options 2  activeOptions 2   id 3dc3d04d-…
snapp_courier  INACTIVE options 0  activeOptions 0  id 49ecbe67-…
store_courier  active  options 0  activeOptions 0   id 0ed329bb-…
in_person      active  options 0  activeOptions 0   id 92c06402-…

GET /v1/admin/shipping-methods?language=fa -> 200  (4 items only; snapp_courier absent because it is inactive)
post  -> options [standard]
tipax -> options [express, standard]
store_courier -> options []
in_person     -> options []
```

Two facts matter for the design and are easy to get wrong:

1. **`post` has two option rows but only `standard` is active** (`optionCount 2`, `activeOptionCount 1`). The
   storefront therefore currently offers only `post:standard` for post, never `post:express`.
2. **Three of five services have zero options** (`snapp_courier`, `store_courier`, `in_person`). Their
   Shipping rules are keyed with `optionCode = null`. A one-level model therefore **cannot** define the
   method identity as "the option row".

### A.3 Shipping — owner of rates / coverage / schedule / packaging / operational quote

The Shipping schema keys every rule on the pair `(service_code, option_code)` with `option_code` **nullable**,
and `null` is the documented *service-level* rule:

| Table (schema `shipping`) | Key columns | Uniqueness |
| --- | --- | --- |
| `rate_policies` | `service_code`, `option_code` (nullable), destination scope, weight/item tier | `ux_rate_policies_service_option_scope` (NULLS NOT DISTINCT) — `ShippingDbContext.cs` :143–158 |
| `coverage_grants` | `service_code`, destination scope | `ux_coverage_grants_service_scope` |
| `packaging_policies` | `service_code`, `option_code`, scope | `ux_packaging_policies_service_option_scope` — `ShippingDbContext.cs` :239–250 |
| `operating_schedules` | `service_code`, `option_code` | `ux_operating_schedules_service_option` — `ShippingDbContext.cs` :288–291 |

Grounding for the "pair, never composite" rule:

- `ShippingRatePolicy.cs` :55–63, :139–149 — `OptionCode` is an *identity*, and `MatchesOption` treats a
  `null` policy as the service-level fallback.
- `FulfillmentShippingMethodOptionValidator.cs` :13–18, :44–49 — **`"tipax:express"` is explicitly rejected as
  a service code** (`shipping.service_code.unknown`); the composite is never split by the engine.
- `ShippingRatePolicyEngine`, `ShippingPackagingFeeResolver`, `ShippingScheduleAvailabilityEvaluator` all
  select "option-specific first, service-level as explicit fallback".
- Endpoints are code-keyed, not id-keyed: `/v1/admin/shipping-services/{serviceCode}/rates|coverage-grants|
  packaging-policies|operating-schedules|operational-quote` (see `Tooba.Shipping.Endpoints/Admin/*.cs`).
- Migrations: `20261015030159_OptionLevelRatePolicy.cs`, `20261015063838_WeightAndCountRateTiers.cs`.

### A.4 Order — storefront projection, draft, paid snapshot, legacy checkout

| Concern | File | Anchor |
| --- | --- | --- |
| Method composition | `.../Order.Application/Storefront/Services/StorefrontShippingService.cs` | `LoadEligibleMethodsAsync` :370–417: `var code = $"{service.Code}:{option.Code}"` (:411), `var label = $"{serviceName} — {optionName}"` (:412); option-less services call `TryAddMethod(..., service.Code, …)` (:400) |
| Identity carried separately | same file `TryAddMethod` :420–449 | `optionCode` is passed as its own field; comment states the composite must never be read as a service code (:447–449) |
| View contract | `.../Storefront/Models/StorefrontOrderModels.cs` :88–102 | `StorefrontShippingMethodView(MethodCode, Label, ServiceCode, IconKey, PriceAmount, LeadDays, IsFree, EstimationLabel, OptionCode = null)` |
| Legacy rate resolution | `.../Storefront/Services/StorefrontShippingCalculator.cs` :39–56 | exact match on the composite first, then `code.Split(':', 2)[0]` as service fallback |
| Legacy config authority | `.../Fulfillment.Contracts/Shipping/ShippingMethodsOptions.cs` | `Tooba:ShippingMethods`: `EnabledCodes` = the five service codes; `Rates[]` contains **both** `post` and `post:express` / `post:standard`, etc. |
| Legacy registry labels | same file `ShippingMethodRegistry` :53–84 | five service-level labels only |
| Selection revalidation | `StorefrontShippingService.SaveSelectionAsync` :176–217 | `methods.FirstOrDefault(m => m.MethodCode == request.ShippingMethodCode)` else `ShippingMethodUnavailable` (:215–217) |
| Commit → snapshot | `StorefrontShippingService.CommitAsync` :309–173 | draft values are handed to `StorefrontCheckoutService` as `shippingMethodCode/Label/Amount` |
| Snapshot write | `.../Storefront/Services/StorefrontCheckoutService.cs` :256–292 | `DefaultShippingCode = "storefront-default"` (:27) when blank |
| Draft column | `OrderDbContext.cs` :143–144 | `cart_shipping_drafts.shipping_method_code varchar(64)`, label `varchar(128)` |
| Checkout column | `OrderDbContext.cs` :119–120 | `checkouts.shipping_method_code **varchar(32)**`, label `varchar(64)` |
| Migrations | `20260824120000_CheckoutShippingSnapshot.cs`, `20260910190000_StorefrontShippingDraftAndCheckoutFields.cs` | created those columns |
| Shipment carrier | `.../Fulfillment.Domain/Aggregates/Shipment.cs` :29–38 | `CarrierDisplayName` (128), `ShippingMethodCode` (64), `ShippingMethodLabel` (128) |
| Observation-only shadow | `.../Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs` :46–88 | builds candidates from `m.ServiceCode` + `m.OptionCode` separately |

### A.5 Frontend admin (the surface to be restructured)

`src/frontend/app/admin/shipping-services-screen.tsx` (≈32 KB) currently renders **one grid row per service**
with six icon-only row actions and a nested create/edit dialog:

- Row actions (`:324–388`): `edit`, `rates` «نرخ و پوشش», `schedule` «برنامه کاری ارسال`,
  `packaging` «هزینه بسته‌بندی و پرداخت کرایه», `quote` «بررسی هزینه و شرایط ارسال», `deactivate`.
- Grid columns (`:390–439`): icon, نام, کد, **«انواع سرویس» = `activeOptionCount / optionCount`** (:408–417),
  فعال, عملیات.
- Create/edit dialog (`:506–…`) contains a **second level editor** «انواع سرویس (سطح ۲)» (`:615–…`) with per-option
  code, per-language name, active and sort order.
- Dialogs live in `src/frontend/app/admin/shipping-services/`: `shipping-rates-coverage-dialog.tsx` (773 lines),
  `shipping-operating-schedule-dialog.tsx` (504), `shipping-packaging-dialog.tsx` (504),
  `shipping-operational-quote-dialog.tsx` (426), plus model/api pairs and focused tests (117 admin shipping
  tests were green in task 080).
- Each of the four feature dialogs independently re-reads the option list via
  `loadShippingMethodTree()` (`shipping-services-api.ts` :296) and offers `SERVICE_LEVEL_OPTION_VALUE = ""`
  as the explicit "service level" option (`shipping-rates-coverage-model.ts` :293–339).
- API/types: `shipping-services-api.ts` — `ShippingServiceWriteRequest` with `options: ShippingServiceOptionWrite[]`
  (:26–43), `ShippingServiceDetail.options` with stable `shippingServiceOptionId` (:69–74).
- Form validation: `shipping-services-form-model.ts` validates nested option codes and translations
  (`:154–205`); `SHIPPING_SERVICE_CODE_MAX_LENGTH = 64`.
- Storefront selection UI: `src/frontend/app/storefront/storefront-shipping.tsx` keys selection on
  `methodCode` (:116, :280, :317–325, :621–632) and `iconFor` sniffs `methodCode.includes("express")` (:97).
- Design-system tab primitive: `src/frontend/design-system/primitives/overlays.tsx` `Tabs` (:227–254) —
  `role="tablist"`/`role="tab"`/`role="tabpanel"` + `aria-selected`, but **no `aria-controls`/`id` wiring and no
  arrow-key navigation**. The in-repo precedent for a hand-rolled accessible tab strip is
  `category-admin-screen.tsx` :1018–1035 (`role="tablist"` + `aria-label`).

### A.6 Summary of the identity actually in use

```text
service_id (uuid)  ── Fulfillment catalog, merchant-visible today
service_code       ── "post" | "tipax" | "snapp_courier" | "store_courier" | "in_person"
option_id (uuid)   ── Fulfillment catalog, merchant-visible today (2nd level)
option_code        ── "express" | "standard" | (none)
Shipping rule key  ── (service_code, option_code?)          ← internal, already flat-compatible
Storefront method  ── methodCode = "service:option" or "service"   ← customer + persisted identity
Order persistence  ── cart_shipping_drafts.shipping_method_code (64) / checkouts.shipping_method_code (32)
```

---

## B. One logical level in the merchant UI — identity proposal

### B.1 What "one level" must mean (and must not mean)

**Must mean:** the merchant sees and configures a list of independently named, independently activatable
delivery methods. There is no "service → then pick an option" step anywhere in the merchant surface. The
names are free text per language (so «پست معمولی» / «پست پیشتاز» / «تیپاکس معمولی» / «تیپاکس اکسپرس» are
expressible).

**Must not mean:** erasing the provider grouping from the database, conflating `post` with `post:standard`,
rewriting historical order identities, or silently re-pricing anything.

### B.2 Recommended model (proposal, requires Architect confirmation)

Introduce a **delivery-method** entity in **Fulfillment** (catalog owner) that is the merchant-visible and
storefront-visible unit, while keeping the provider grouping as **hidden internal metadata**:

```text
shipping_methods                        (proposed, Fulfillment schema)
  shipping_method_id      uuid  PK
  code                    text  UNIQUE          ← stable identity (see B.4)
  provider_kind           text  NOT NULL        ← HIDDEN internal grouping; never rendered as a 2nd level
  icon_key, color_key, sort_order, is_active
  legacy_service_code     text  NULL            ← compatibility link, internal only
  legacy_option_code      text  NULL            ← compatibility link, internal only
shipping_method_translations
  (shipping_method_id, language_id) UNIQUE → name, description?
```

- `provider_kind` is the **only** surviving trace of the old parent level, and it must be labelled in code and
  docs as *internal provider metadata*, not as a merchant level. It must never be rendered as a group header
  the merchant configures.
- The existing `shipping_services` / `shipping_service_options` tables are **not deleted and not migrated in
  081**. They become the seed source for the new table (backfill mapping below) and stay readable for the
  Shipment `CarrierDisplayName` / `ProviderKind` metadata path.

### B.3 The alternative (must be explicitly rejected or chosen by the Architect)

**Alternative A — no new table: project the existing pair as the one-level list.** Each *active option* becomes
a method row, and option-less services become a single method row. Cost: three of five services have no option
row, so their method identity would have to be synthesized; and the merchant could not name «پست معمولی»
independently of the option name. It is the cheaper option and needs **no migration at all**.

**Alternative B — the new `shipping_methods` table above.** Cost: one additive migration, a backfill mapping,
and a compatibility layer; benefit: genuine independent naming/activation/ordering, and the provider grouping
is hidden rather than implied.

**This is the STOP/BLOCK item for this task:** both alternatives are defensible, they differ in permanent
schema and in who owns the new identity, and the task forbids guessing. This document recommends **B** (it is
the only one that literally satisfies "independently named" and "hidden grouping") but does **not** treat it
as authorized.

### B.4 Stable identity and safe transformation of the six existing identities

The storefront/persisted identity today is the composite string. **Keep it byte-identical** — this is the
single most important compatibility decision, because it is already written into `cart_shipping_drafts`,
`checkouts` and `shipments` and is compared with `OrdinalIgnoreCase` at selection time.

| Existing identity | Today's `methodCode` (frozen) | Proposed method `code` | Hidden `provider_kind` | Legacy link |
| --- | --- | --- | --- | --- |
| post / standard (active) | `post:standard` | `post:standard` | `post` | service `post`, option `standard` |
| post / express (**inactive**) | `post:express` | `post:express` | `post` | service `post`, option `express` |
| tipax / standard | `tipax:standard` | `tipax:standard` | `tipax` | service `tipax`, option `standard` |
| tipax / express | `tipax:express` | `tipax:express` | `tipax` | service `tipax`, option `express` |
| store_courier (no option) | `store_courier` | `store_courier` | `store_courier` | service only |
| in_person (no option) | `in_person` | `in_person` | `in_person` | service only |
| snapp_courier (**inactive**, no option, no rules) | `snapp_courier` | `snapp_courier` | `courier` | service only |

Rules that make this safe:

1. **Never conflate.** `post:standard` is *not* `post`. A service-level Shipping rule (`option_code IS NULL`)
   is the documented *fallback*, not the same rule. The one-level UI must show this as an explicit inheritance
   or as a per-method duplicate — never as a silent merge.
2. **Never erase historical order identity.** Existing `cart_shipping_drafts.shipping_method_code`,
   `checkouts.shipping_method_code` and `shipments.shipping_method_code` values are immutable evidence. No
   `UPDATE` on them in 081 or in the migration task. New readers must accept both the composite and the flat
   form.
3. **Inactive stays inactive.** `post:express`, `snapp_courier` and the deactivated 079 fixtures must render
   as *inactive/not offered*, never as a bookable method.
4. **Identity is the code, not the uuid.** Shipping keys on `(service_code, option_code)`; Fulfillment option
   uuids are re-created by `UpdateAsync` (`ShippingServiceDirectory.cs` :88–92). Any design that anchors the
   new method identity on `shippingServiceOptionId` would inherit that churn.
5. **Code length constraint.** New method codes must satisfy the **narrowest** persisted column:
   `checkouts.shipping_method_code` is `varchar(32)`. Either cap new codes at 32 characters, or widen that
   column in a dedicated migration (a decision for 082, not 081).

---

## C. The five-tab interface on `/admin/shipping-services`

### C.1 Structure

```text
/admin/shipping-services                     (existing route — URL preserved)
  ┌ method context bar ─────────────────────────────────────────────────────┐
  │ [ روش ارسال: ▾ ]  (one-level combobox of methods, incl. inactive)        │
  │ shows: name · code · فعال/غیرفعال · providerKind as read-only metadata   │
  └─────────────────────────────────────────────────────────────────────────┘
  ┌ tabs (role=tablist, RTL-first) ─────────────────────────────────────────┐
  │ ۱ سرویسها  ۲ تعرفه و پوشش  ۳ بستهبندی و کرایه  ۴ برنامه کاری و ظرفیت  ۵ بررسی هزینه │
  └─────────────────────────────────────────────────────────────────────────┘
  [ tabpanel for the selected tab, scoped to the selected method ]
```

Deep-link contract: `?tab=<services|rates|packaging|schedule|quote>&method=<code>`, so a tab is shareable and
a reload restores the exact context. Selection lives in the URL, not in a modal stack.

### C.2 Tab 1 «سرویسها» (replaces the nested dialog + the «انواع سرویس» column)

- One grid/list row per delivery method: icon, نام (per-language), کد (mono, `dir="ltr"`), وضعیت, ترتیب.
- «روش جدید» / ویرایش opens a **single-level** form: code, name per language (reusing the existing language
  tab strip from the current dialog), icon key, color key, active, sort order. **No** «انواع سرویس (سطح ۲)»
  editor.
- Provider grouping is displayed as read-only internal metadata, never as a configurable parent.
- Grid column «انواع سرویس» is removed; the saved-view key `admin-shipping-services` and the existing grid
  columns/filters/export behavior are otherwise preserved.
- Deactivate keeps the existing explicit confirmation text and the honest consequence wording.

### C.3 Tabs 2–5 (the four existing feature dialogs become tabs)

Each tab renders the *existing* functional content of the corresponding dialog, re-scoped to the method chosen
in the context bar:

| Tab | Source of behavior (must be preserved) | Identity handed to the API |
| --- | --- | --- |
| ۲ تعرفه و پوشش | `shipping-rates-coverage-dialog.tsx` + `shipping-rate-tier-model.ts` | `serviceCode` + `optionCode` (derived from the selected method's legacy link) |
| ۳ بستهبندی و کرایه | `shipping-packaging-dialog.tsx` + `shipping-packaging-model.ts` | same |
| ۴ برنامه کاری و ظرفیت | `shipping-operating-schedule-dialog.tsx` + `shipping-operating-schedule-model.ts` | same |
| ۵ بررسی هزینه | `shipping-operational-quote-dialog.tsx` + `shipping-operational-quote-model.ts` | same |

Explicit non-removals: the destination picker (`shipping-destination-picker.tsx`), the Geography reference
seam (`geography-admin-api.ts`), the tier/condition editors, the option-identity validation, the freight payer
mode, the timezone field, the capacity label «ظرفیت تنظیمشده (هنوز اعمال نمیشود)», the operational-quote
failure-code map, and every existing test assertion.

The **scattered icon-only entry points are eliminated as entry points, not as functionality**: the four row
actions become tabs, and only `ویرایش` (the method's own configuration) and `غیرفعالسازی` remain as row
actions in tab 1.

### C.4 Required states and behavior (per tab)

| Concern | Required behavior |
| --- | --- |
| Loading | Distinct skeleton/`در حال بارگذاری…` per tab; the context bar shows the method immediately from the list read, not after the tab read |
| Empty | Explicit, truthful empty state per tab (e.g. «هیچ سیاست نرخی ثبت نشده است»), never a zero-filled fake row |
| Error | Distinct from empty; retry offered; a failed write re-reads server state instead of leaving the failed desire on screen |
| Permission denied | `401` → «نشست ادمین شناسایی نشد؛ دوباره وارد شوید.»; `403` → dedicated access-denied state. Never bypass, never render a disabled-but-clickable form |
| Save / cancel | Save disabled while a request is pending **and** while the read state is not `ok`; cancel restores the last server state |
| Unsaved change + tab switch | Warn or preserve the draft per tab; never silently discard a pending edit |
| RTL / responsive | RTL-first layout, wrapping context bar and tab strip, no clipped save/close at narrow widths; tab strip horizontally scrollable rather than truncated |
| Accessibility | `role="tablist"` + `aria-label`, `role="tab"` + `aria-selected` + `aria-controls`, `role="tabpanel"` + `aria-labelledby`, **arrow-key/Home/End navigation with roving `tabindex`**, and visible focus. The existing `Tabs` primitive lacks the `aria-controls`/keyboard wiring, so either extend it or follow the `category-admin-screen.tsx` hand-rolled precedent |
| Testability | Stable `data-testid` per tab, per context selector and per save/empty/error state, so the 117 existing focused tests can be extended rather than replaced |

### C.5 Frontend architecture constraints (mandatory, from `.cursor/skills/tooba-frontend-feature`)

- Route composition stays in `shipping-services-screen.tsx`; feature behavior stays in the
  `shipping-services/*-model.ts` + `*-api.ts` pairs; shared UI stays in `design-system/`.
- No new parallel abstraction, no new state library, no new data-fetching pattern; reuse
  `loadAdminShippingServices`, `loadShippingMethodTree`, the existing `AdminResult` shape and
  `resolveAdminChromeLocale`.
- No hardcoded prices/stock/ratings; all Persian strings stay in the existing code→message maps.

---

## D. Impact map, migration/backward-compatibility and rollback

### D.1 Module-by-module impact

| Module | Impact | Direction |
| --- | --- | --- |
| **Fulfillment** | Owns the new method catalog + translations + hidden `provider_kind` + legacy link; keeps the old tables readable; seed/backfill mapping | new Contracts types + MediatR commands/queries + admin endpoints; **no change to existing `/shipping-services` behavior** |
| **Shipping** | **No contract change required if the internal key stays `(service_code, option_code)`.** The one-level method resolves to that pair through its legacy link; rates/coverage/packaging/schedule/quote endpoints and all four unique indexes stay untouched | none, if the pair is preserved |
| **Geography** | None — place identity is untouched | none |
| **Order (storefront)** | `StorefrontShippingService.LoadEligibleMethodsAsync` is the only composer of `methodCode`; a one-level catalog means the method list is read from Fulfillment instead of being composed from service+option. `TryAddMethod`, `StorefrontShippingMethodView`, selection revalidation and `StorefrontShippingCalculator.ResolveRate`'s `Split(':')` fallback must keep working for **both** forms during the transition | additive read seam; legacy config remains authoritative |
| **Order (paid snapshot)** | Unchanged writes; readers must tolerate the composite form forever | read tolerance only |
| **Legacy Checkout** | Unchanged. `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow` stay OFF; no cutover switch is created | none |
| **Payment / Settlement** | Untouched (COD is still a quote fact only) | none |
| **Frontend admin** | Screen restructured into context bar + 5 tabs; four dialogs become tab panels; nested option editor removed from the create/edit form | largest visible change; feature-gated |
| **Frontend storefront** | Selection stays keyed on `methodCode`; labels may change (e.g. «پست — معمولی» → «پست معمولی») — a **customer-visible label delta** that must be listed as an intentional change and visually reviewed | label-only, gated |

### D.2 Migration / backward-compatibility strategy (proposed, not executed)

1. **Additive only.** New table + new columns; no `DROP`, no column narrowing, no data rewrite of
   `cart_shipping_drafts` / `checkouts` / `shipments`.
2. **Backfill is a mapping, not a migration.** Seed the new table from the existing service/option rows using
   the table in B.4. Idempotent, keyed on the method `code`.
3. **Dual-read window.** New read seams return one-level methods; every consumer that still holds a composite
   code (a live cart draft, an old checkout, the legacy calculator) keeps resolving, because the composite is
   also the new method code for all existing identities.
4. **Feature gate.** `Tooba:ShippingOneLevelCatalog` (proposed, **default OFF**, no appsettings section). Gate
   OFF ⇒ the screen renders today's nested layout and the storefront path is byte-identical. Gate ON ⇒ the
   one-level screen. The gate is **UI/catalog-read only** and must not touch price authority.
5. **Two independent existing gates stay independent.** The one-level catalog gate must not read, imply, or
   flip `Tooba:ShippingRateAuthority` or `Tooba:ShippingOperationalShadow`.

### D.3 Rollback

- **Gate OFF** restores the previous screen and the previous read path with zero data change.
- Because 081 migrates nothing and the migration task is additive, rollback never requires a destructive
  down-migration; the new table may simply remain unread.
- If the new table must be removed later, dropping it cannot affect Order, Shipping, Payment or Settlement,
  because no other module owns a foreign key to it (Contracts-only seam).

---

## E. Reconciliation of unfinished capabilities and the ≤11-task roadmap

### E.1 The five unfinished capabilities — none of them is a UI problem

| # | Capability | True state | Owner | Can UI work close it? |
| --- | --- | --- | --- | --- |
| 1 | Trusted **packed weight / packed item-count** provenance | `ShippingMeasuredParcel(long? WeightGrams, int? ItemCount)` exists and is fail-closed, but Fulfillment Contracts publish **no** packed measurement; the storefront deliberately leaves it undeclared | Fulfillment + Order | **No.** Needs a measurement contract and an authoritative source. UI must keep showing "اندازه در دسترس نیست" |
| 2 | **Atomic capacity claim / release** | `ShippingCapacityEnforcement` has exactly one value `NotEnforced` (guard-tested). Configured capacity (e.g. 25) is not reserved, limited, or available stock | Order + Shipping + Settlement | **No.** Needs an order↔window link, idempotent reservation, expiry/cancellation and a ledger owner |
| 3 | **COD collection / settlement** | `CollectOnDelivery` is a rate fact only; no Payment/Settlement transaction or reconciled ledger exists | Payment + Settlement | **No** |
| 4 | **True customer delivery promise** | A carrier pickup window is not a promised delivery slot; seller preparation-day provenance and Order snapshot integration are unresolved | Product + Order + Fulfillment/Shipping | **No** |
| 5 | **Merchant-approved tariffs + commercial Checkout cutover** | All Development fixtures (Alborz area/city rules, tier rows, COD packaging) are synthetic and NOT approved for sale; the cutover is an explicit human decision | Merchant + Architect | **No.** UI may only present and validate |

Consequence for the roadmap: **no task in the list below may silently activate Checkout**, and the one-level UI
work (082–088) must not be presented as commercial readiness.

### E.2 Proposed delivery plan — 11 tasks total including 081

| Task | Scope | Owner | Evidence-based acceptance criteria | Hard dependency |
| --- | --- | --- | --- | --- |
| **081** | This read-only blueprint | Architect | Report + read-only live inventory committed; no source change | — |
| **082** | **Fulfillment one-level delivery-method catalog foundation.** Decide A vs B; additive schema; `code` uniqueness; hidden `provider_kind`; legacy link; backfill mapping from the six existing identities; Contracts + MediatR + admin endpoints; module tests; gate OFF | Fulfillment | New table/columns created additively; backfill idempotent and verified against the live catalog (post 2/1, tipax 2/2, three option-less services); `/v1/admin/shipping-services` behavior byte-identical; architecture guards green; gate OFF by default | **Architect decision on A vs B** |
| **083** | **Identity compatibility proof.** Prove Shipping's `(service_code, option_code)` key, all four unique indexes, the engine precedence, and Order's selection revalidation behave identically for composite and flat method codes; add regression tests; decide the `checkouts.shipping_method_code` 32-char question | Shipping + Order | Focused tests green; live read-only parity with gate OFF; explicit written ruling on the 32-char column | 082 |
| **084** | **Admin shell + tab 1 «سرویسها».** Context bar, accessible tab strip, deep-link contract, one-level create/edit form, removal of the «انواع سرویس» column; gate OFF | Frontend + Fulfillment | Focused frontend tests; typecheck; RTL/responsive check; denied/error/empty states; existing 117 tests still green | 082, 083 |
| **085** | **Tab 2 «تعرفه و پوشش»** moved from dialog to tab, scoped to the selected method | Frontend + Shipping | Same assertions as today's dialog tests plus the new context | 084 |
| **086** | **Tab 3 «بستهبندی و کرایه»** | Frontend + Shipping | idem | 084 |
| **087** | **Tab 4 «برنامه کاری و ظرفیت»** (still configuration-only; capacity label unchanged) | Frontend + Shipping | idem | 084 |
| **088** | **Tab 5 «بررسی هزینه»** | Frontend + Shipping | idem; the 078 browser-roundtrip assertions are re-proved in the tab context | 084 |
| **089** | **Trusted packed weight / item-count provenance.** Measurement contract in Fulfillment Contracts + authoritative source + fail-closed wiring; tier rates become usable for real | Fulfillment + Order | Provenance is authoritative, never synthesized from subtotal/quantity/client input; tier boundary proof at 1999/2000 g | 083 |
| **090** | **Atomic capacity claim / release.** Reservation model, order↔window link, idempotent claim, expiry/cancel release, ledger owner | Order + Shipping + Settlement | Concurrency proof (no double claim), release on cancel/expiry, no over-count; only then may `ShippingCapacityEnforcement` gain a second value | 083, 089 |
| **091** | **Commercial closure.** Merchant-approved tariffs/coverage/packaging per method; COD settlement owner in Payment/Settlement; delivery promise; **explicit user-authorized** Checkout cutover decision including the prepaid packaging case | Merchant + Architect + Payment/Settlement + Order | Written merchant approval per method/scope; reconciled COD ledger; promise field owned by Order; a separate, explicitly authorized cutover task with rollback proven | 089, 090 |

Tasks 089–091 are **not** UI work and must not be bundled into the 082–088 sequence as if they were.
Task 091 in particular must remain decomposed: COD settlement, delivery promise and the pricing cutover have
different owners and different authorization needs.

---

## F. Risks, blockers, minimum tests and the recommended scope for task 082

### F.1 Risks

| # | Risk | Severity | Mitigation |
| --- | --- | --- | --- |
| R1 | `checkouts.shipping_method_code` is `varchar(32)` while drafts are `varchar(64)`; a flat method code longer than 32 chars cannot be snapshotted | High | Cap new codes at 32, or widen the column in a dedicated migration — **explicit 082 decision, not a silent assumption** |
| R2 | `StorefrontShippingCalculator.ResolveRate` splits on `':'` and `Tooba:ShippingMethods.Rates` contains both `post` and `post:express`/`post:standard`; removing composite codes silently re-prices the storefront | High | Keep legacy config authoritative until the authorized cutover; add parity tests |
| R3 | `ShippingServiceDirectory.UpdateAsync` deletes and re-inserts options; a design anchored on `shippingServiceOptionId` inherits that churn | Medium | Anchor identity on `code`, not on the option uuid |
| R4 | Service-level Shipping rules (`option_code IS NULL`) currently act as fallback for option-level methods; a one-level UI could present them as a separate bookable method or silently merge them | High | Show inheritance explicitly; never merge; never invent a method for a bare fallback rule |
| R5 | `post:express` and `snapp_courier` are inactive/no-rule; a one-level list could make them look bookable | High | Render inactive/not-offered honestly; no auto-activation |
| R6 | Customer-visible label delta («پست — معمولی» → «پست معمولی») | Medium | List it as an intentional change; run the critical-storefront visual review for any shared storefront surface touched |
| R7 | Gate confusion — the new catalog gate must not imply price authority | High | Two independent gates stay independent; the new gate is UI/catalog-read only; guard tests |
| R8 | Provider grouping creeping back as a visible second level | Medium | `provider_kind` documented as internal metadata only; a guard test asserting it is not rendered as a configurable level |

### F.2 Blockers (explicit)

1. **B1 — STOP_FOR_ARCHITECT:** the choice between **Alternative A (flatten the existing pair, no new table)**
   and **Alternative B (new Fulfillment `shipping_methods` table, hidden provider grouping)** changes permanent
   schema and module ownership. It cannot be resolved from source and is not decided by this task.
2. **B2:** whether to widen `checkouts.shipping_method_code` from 32 to 64 (or cap codes) is a schema decision
   with a paid-snapshot compatibility surface. Needs an explicit ruling.
3. **B3:** the four capabilities in E.1 remain blocked with named owners and are unaffected by any amount of
   UI work.
4. **B4:** Checkout cutover remains unauthorized. `Tooba:ShippingRateAuthority` and
   `Tooba:ShippingOperationalShadow` must stay OFF.

### F.3 Minimum targeted tests (for 082, not run here)

- Fulfillment module: create/update/deactivate of a one-level method; `code` uniqueness; the hidden
  `provider_kind` is never exposed on any admin read; backfill mapping reproduces exactly the live inventory
  (post 2/1, tipax 2/2, three option-less services, snapp_courier inactive).
- Architecture guards: Contracts-only seam, no cross-module `DbContext`, no `Results.Json`, MediatR
  `ISender`, `Result`/`ApiResponseFactory`.
- Frontend (084): `node --test app/admin/shipping-services/*.test.ts` extended for the tab shell and context
  bar; `npm run typecheck`.
- Parity: with the new gate OFF, the storefront projection response is byte-identical to today's (the 079/080
  method already exists for this).

### F.4 Recommended scope for task 082 (bounded, single module)

**In scope:** Fulfillment-owned additive one-level delivery-method catalog (schema + Contracts + MediatR
commands/queries + admin endpoints + idempotent backfill mapping from the existing service/option rows,
without deleting or rewriting the old tables), a default-OFF feature gate, and module tests.

**Explicitly out of scope for 082:** the admin UI tabs (084–088); any Checkout/price-authority change; any
migration or rewrite of existing Shipping rate/coverage/packaging/schedule rows; capacity enforcement; packed
weight provenance; COD settlement; delivery promise; and any change to `cart_shipping_drafts` /
`checkouts` / `shipments` historical values.

**082 must not start** until the Architect resolves **B1** and **B2**.

---

## G. Guardrail compliance statement

- Read-only: the only live calls made by this task were authenticated `GET`s against Development, plus the
  unauthenticated `GET /__platform-commerce` identity check and `POST /v1/auth/login`. No service, option,
  rate, coverage, packaging, schedule or order row was created, edited or deleted.
- No production code, migration, seed or runtime process was changed; the host was not restarted or rebuilt.
- No new Checkout price authority, no paid-order/snapshot mutation, no shadow gate enabled.
- Module ownership preserved: Fulfillment owns the delivery-method catalog; Shipping owns
  rates/coverage/schedule/packaging/operational quote; Geography owns place identity; Order owns the customer
  promise and paid snapshots; Payment/Settlement own collections.
- Pre-existing worktree modifications were preserved unchanged.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` was **not** modified by this task.
