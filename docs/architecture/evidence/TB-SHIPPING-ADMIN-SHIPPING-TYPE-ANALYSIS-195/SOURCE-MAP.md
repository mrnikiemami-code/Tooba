# SOURCE-MAP — operational shipping TYPE ownership and reuse (TB-195)

Date: 2026-10-22 · Channel: tooba-main · Worker: tooba-worker-01
Base inspected: `4065fa89dba21097b9a47435c41d7f85b63a3a01` (= origin/main = HEAD at claim)
Analysis only. No build, test, DB, HTTP probe or code change was performed.

## 0. Method and limits

- Search terms used (finite): `ShippingType`, `ShippingCategory`, `ServiceType`, `ShipmentType`,
  `DeliveryType`, `FulfillmentType`, `heavy|fragile|bulky|oversized|grocery|perishable|shipping.?class`,
  `shipping.?responsib|inbound|responsible`, `weight|dimension` (Catalog/Offer Contracts),
  `MethodCode` (Cart/Order Contracts), `shipping type` / `نوع ارسال` in `src/frontend`,
  `SHIPPING-ADMIN-FLAT-METHOD-CONTRACT` references, `ILanguageLookup`, `authoring_drafts`,
  `shipping_methods`, `shipping_method_translations`.
- Not examined (out of bounded scope / UNKNOWN): full Catalog/Order/Offer module sweeps, carrier
  integration capability, checkout quote runtime, frontend behaviour beyond the design-preview
  fixtures named below, and any runtime/DB state.
- The 43-route / 30-request / 27-validator / 12-migration guard counts come from accepted TB-194
  evidence and were **not** re-executed here.

## 1. Capability inventory (classification from real routes/callers)

| Capability | Evidence | Applicability |
| --- | --- | --- |
| Fulfillment canonical shipping method catalog (service / option / method / translations) | `Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs:48-77` maps `admin/v1/shipping-services` CRUD + `shipping-methods/one-level` list/binding/translation/activation/create; `ShippingMethodsEndpoints.cs:16` maps `GET /v1/admin/shipping-methods` | `HTTP_OWNING` |
| Fulfillment one-level canonical identity read seam | `Tooba.Fulfillment.Contracts/Shipping/ShippingOneLevelCanonicalMethodContracts.cs` (`IShippingOneLevelCanonicalMethodReader`), `ShippingOneLevelCatalogContracts.cs` (`IShippingOneLevelCatalogReader`, gate default OFF) | `INTERNAL_ONLY` (Contracts read seams) |
| Shipping owner-local authoring draft aggregate (regions, rate rows, presentation metadata, weekly schedule, method settings) | `Tooba.Shipping.Endpoints/Admin/ShippingAuthoringDraftEndpoints.cs:26-61`, `.../Seller/ShippingSellerAuthoringDraftEndpoints.cs:27-54`, `Tooba.Shipping.Domain/Authoring/*` | `HTTP_OWNING` |
| Shipping legacy rate/packaging/schedule policies keyed by `(service_code, option_code, scope)` | `Tooba.Shipping.Infrastructure/Persistence/ShippingDbContext.cs` (`rate_policies`, `method_coverages`, `packaging_policies`, schedule tables) | `MIXED_OR_UNKNOWN` (legacy write paths retained; not re-audited here) |
| Carrier provider metadata JSON incl. `serviceType` | `Tooba.Fulfillment.Application/Shipping/ShippingProviderMetadata.cs:13,39,135,181` | `INTERNAL_ONLY` adapter metadata — **not** an operational taxonomy |
| Product weight/dimension attributes | Catalog demo seed EAV attributes only (`CatalogDemoMatrix.cs:159,175,...`); no typed shipping classification in `Tooba.Catalog.Contracts` | NOT_APPLICABLE to type ownership |
| Item/offer shipping classification | no `shipping` member matches in `Tooba.Offer.Contracts` | `UNKNOWN` (absent) |
| Method consumption downstream | `Order.Contracts/{Fulfillment,Settlement,Payments}` and Cart carry only `MethodCode` strings | read-only consumers |

## 2. Existing canonical type / classification — negative findings

- No entity, table, contract, enum or route named `ShippingType`/`ShippingCategory`/operational class
  exists under `src/backend`. The only `serviceType` occurrences are provider-metadata JSON fields in
  `ShippingProviderMetadata.cs` and unrelated `ServiceType` reflection members in tests/DI code.
- No `shipping_types`, `shipping_type_translations` or equivalent table exists in
  `FulfillmentDbContext` (`shipping_services`, `shipping_service_translations`,
  `shipping_service_options`, `shipping_service_option_translations`, `shipping_methods`,
  `shipping_method_translations` — `FulfillmentDbContext.cs:201-268`) or in `ShippingDbContext`.
- No item/offer → shipping-class binding exists. Weight is a free-form Catalog attribute in demo
  seed data, not an authoritative shipping classification.
- `src/frontend/app/admin/shipping-services/design-preview/preview-model.ts:37` hardcodes
  `PreviewShippingTypeId = "ordinary" | "heavy" | "extra-heavy" | "grocery"` for the **design
  preview** only (`preview-fixtures.ts`, `preview-method-list.tsx`). This is a mock, not authority,
  not a value set, and not a seed source.

## 3. Exact current seams relevant to a future slice

- Draft root identity and concurrency: `ShippingAuthoringDraft` (`Tooba.Shipping.Domain/Authoring/
  ShippingAuthoringDraft.cs:221-302`) holds `OwnerKind/OwnerTenantId/OwnerSellerPartyId`,
  `ServiceCode`, `OptionCode`, `Revision` (concurrency token), `Translations`, `WeeklySchedule`,
  `MethodSettings`. One draft per owner + canonical binding is enforced by
  `ux_authoring_drafts_owner_binding` (`ShippingDbContext.cs:429-442`, `NULLS NOT DISTINCT`).
- Existing root-level optional singleton patterns to mirror: `ShippingAuthoringMethodSettings`
  (TB-194) and `ShippingAuthoringWeeklySchedule` (TB-193) — nullable navigation = explicitly
  UNCONFIGURED, not a defaulted value.
- Owner/edition/language authority already implemented: `ShippingAuthoringContextView` +
  `IShippingAuthoringContextSource` (edition, default currency, active DB languages with
  `LanguageId`), `ILanguageLookup` in `Tooba.Localization.Contracts/Ports/LanguageLookupContracts.cs`.
- Fulfillment translation shape to mirror for shared display text: `shipping_method_translations`
  with `(shipping_method_id, language_id)` unique and `LanguageId` supplied by the caller
  (`FulfillmentDbContext.cs:254-266`).
- Legal existing project edge: `Tooba.Shipping.Infrastructure.csproj:22` already references
  `Tooba.Fulfillment.Contracts` (Contracts-only). Shipping is in schema `shipping`; Fulfillment in
  `fulfillment` — a cross-schema FK or join would be illegal.

## 4. Reused unchanged evidence

- TB-194 accepted scope/limits (`docs/architecture/evidence/TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194/
  ARCHITECT-REVIEW.md`): root-level independent settings, explicit unconfigured, owner authorizer
  before `ISender`, 409 on stale revision. Reused as shape precedent only.
- TB-187-W0 `MODEL-LEGACY-AND-HANDOFF.md` C11/§4.2/§6/§7-D2: shipping-type taxonomy is `ADDITIVE`,
  previously *proposed* as "owned by Fulfillment, referenced by Shipping policy" with value set,
  configurability and translation marked `UNKNOWN`. That proposal is treated here as an unaccepted
  prior hypothesis, not as a decision.
