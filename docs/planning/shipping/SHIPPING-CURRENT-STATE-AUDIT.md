# Shipping — Current State Audit (`TB-SHIPPING-AUDIT-034`)

Mode: `READ_ONLY_SOURCE_AUDIT_AND_REPORT_ONLY`
Program: `TOOBA-SHIPPING-ARCHITECTURE-PLANNING`
Parent-Task: `TB-P0-CHECKOUT-DEV-LIVE-CUTOVER-033`
Stop-State: `STOP_FOR_ARCHITECT`
Automatic-Next-Implementation-Task: NONE

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected Base SHA | `d5f74da6cd820950dbea2c47d7e87929619d7f91` |
| Audited HEAD | `d5f74da6cd820950dbea2c47d7e87929619d7f91` |
| `origin/main` at audit | `d5f74da6cd820950dbea2c47d7e87929619d7f91` |
| Preflight drift handling | Local HEAD was `3d0ae6c53ffa02cd0777f3d8292fe9263e21e553` (one commit behind). `d5f74da6c` was added upstream by the Architect (`docs(shipping): record draft shipping architecture decisions and audit gate`). Verified `3d0ae6c53` is an ancestor of `d5f74da6c` and the only incoming change was the new `docs/architecture/SHIPPING-ARCHITECTURE.md`; recovered with `git merge --ff-only` (no rebase / reset / stash / force-push). Pre-existing unrelated dirty work was preserved untouched. |
| Inspection date | 2026-10-14 (UTC+03:30) |
| Timebox | 30 min HARD — respected; no overrun |

## 0. Methodology and scope discipline

- Read-only source inspection of `main` plus targeted `rg` searches. No writes outside this single report file.
- No service restart, no environment/migration/DB/seed action, no PSP or wallet transaction, no paid order, no deployed Host change, no secret exposure.
- Line numbers below are verified against the audited HEAD above. Anchors are given as `path` + line range.
- **Classification vocabulary** is strictly: `EXISTING` (implemented and wired), `PARTIAL` (implemented for only part of the intended capability), `MISSING` (no implementation found in source), `UNVERIFIED` (cannot be concluded from static inspection within the timebox).
- The supplied prior analysis in `docs/architecture/SHIPPING-ARCHITECTURE.md` was treated as a hypothesis to falsify, not as evidence. Where it is wrong or contradicted, this is stated explicitly in §5.
- `docs/architecture/SHIPPING-ARCHITECTURE.md` was **not** edited and is **not** certified by this report.

**Known gaps / limits of this audit:** runtime-evaluated behavior was not re-probed (read-only source audit only); no database rows were read in this task; NH/EF query translation for the catalog reader was read statically; "who is commercially authorized to choose shipping responsibility" is only answerable at the domain/config level, not the business-policy level, from source alone.

---

## 1. Evidence matrix

| # | Capability | Current owner (verified) | Exact path + lines | Effective authority (DB / config / constant) | Status | Risk |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Storefront shipping projection UI | Frontend/Order | `src/frontend/app/storefront/storefront-shipping.tsx` L41, L97, L120 | `loadShippingProjection` | `EXISTING` | — |
| 2 | Shipping HTTP surface | **Order** module endpoints | `src/backend/Modules/Order/Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs` L37 (POST `shipping/projection`), L38 (PUT `shipping/selection`), L39 (POST `shipping/commit`), L41 (GET `geography/provinces`) | Order module endpoint group | `EXISTING` | Shipping HTTP surface lives in Order, not a Shipping module |
| 3 | Client API wrappers | Frontend/Order | `src/frontend/app/storefront/storefront-shipping-api.ts` L280, L306, L374 | — | `EXISTING` | — |
| 4 | Projection orchestration | Order Application | `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs` L59 `ProjectAsync`, L70 `maxPrep`, L72 `LoadEligibleMethodsAsync`, L210–L220 (selection path), L370–L415 `LoadEligibleMethodsAsync`, L420–L445 `TryAddMethod` | DI-resolved `IShippingCatalogReader` + `ShippingMethodsOptions` | `EXISTING` | Single service owns catalog read + rate resolve + province gate + label localization |
| 5 | Service/option catalog (names, icons, active, order) | **Fulfillment** persistence, read by Order | Reader: `.../Tooba.Fulfillment.Infrastructure/Shipping/ShippingCatalogReader.cs` L20, L54, L78–L92; table mapping `.../Persistence/FulfillmentDbContext.cs` L167 (`shipping_services`) | DB tables `shipping_services` / `shipping_service_options` (+ translations) | `EXISTING` | Catalog authority physically lives in the Fulfillment module while being consumed by Order → cross-module read coupling |
| 6 | Admin write surface for services | Fulfillment | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs` L17; `.../Application/Shipping/Ports/IShippingServiceDirectory.cs` L7, L10 | Write model = `ShippingServiceOptionId`, `Code`, `IsActive`, `SortOrder` + translations | `PARTIAL` | **No price, lead time, coverage, payment terms, carrier or policy fields are admin-writable.** Rates are not editable from the Admin UI |
| 7 | Service activation filter | Fulfillment config → Order behavior | `.../Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs` L46–L96 (L48–L51 `EnabledCodes`, L52–L63 `Rates`, L64–L74 `SellerPreparationDaysByPartyId`, L79–L88 defaults, L90–L93 `Rates` only applied when non-empty); filter applied at `StorefrontShippingService.cs` L378–L391 | `Tooba:ShippingMethods` config section | `EXISTING` | Effective enablement is a **config** intersection (`IsActive` DB ∧ `EnabledCodes`), not a DB-driven store setting |
| 8 | Rate pricing (base price / lead days / free threshold / province allow-list) | **Constants in Fulfillment Contracts**, optionally overridden by config | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Shipping/ShippingMethodsOptions.cs` L10–L17 (rate shape), L36–L47 (hard-coded default rate table: `post` 150,000/3d, `tipax` 220,000/2d, `snapp_courier` 90,000/0d, `store_courier` 75,000/1d, `in_person` 0/0d, plus `post:*` / `tipax:*` variants); quote/eligibility at `StorefrontShippingCalculator.cs` L38–L69 (`ResolveRate`, `IsDestinationAllowed`, `QuotePrice`) | C# class defaults; overridable only by `Tooba:ShippingMethods:Rates` | `PARTIAL` | Tariffs are compiled-in defaults; no zone/weight/carrier-API/manual-quote rating, no effective dates, no versioning |
| 9 | Config reality check | Host config | `src/backend/Host/Tooba.Host/appsettings.json` — **no `ShippingMethods` section at all**; `src/backend/Host/Tooba.Host/appsettings.Development.json` L83–L91 (`EnabledCodes`, `DefaultSellerPreparationDays: 1`, `DeliveryHorizonDays: 7`, `SellerPreparationDaysByPartyId` for two party IDs) | — | `EXISTING` | Because `Rates` is absent, `FulfillmentModule.cs` L90–L93 leaves the **C# hard-coded** rate table in force. Rates are therefore not merely "appsettings-owned"; they are code-owned by default |
| 10 | Seller preparation lead time | Fulfillment config, aggregated in Order | `ShippingMethodsOptions.cs` L33, L49; `StorefrontShippingCalculator.cs` L19–L36 `MaxSellerPreparationDays`; applied `StorefrontShippingService.cs` L70 | `DefaultSellerPreparationDays` + `SellerPreparationDaysByPartyId` | `PARTIAL` | Per-seller values exist in **config**, not on the Offer/seller entity. `MaxSellerPreparationDays` collapses all sellers in the cart group to one value (`.Max()` at L36) |
| 11 | Minimum delivery date | Order Application | `StorefrontShippingCalculator.cs` L88–L92 `ComputeMinimumDeliveryDate(today, maxSellerPreparationDays, methodLeadDays)` → `today + maxPrep + leadDays` | Pure function | `PARTIAL` | Calendar-unaware: no working days, no blackout dates, no holidays; uses the cart-wide max preparation |
| 12 | Delivery date horizon | Order Application / config | `StorefrontShippingCalculator.cs` L94+ `BuildDeliveryDates(minimum, horizonDays)`; horizon from `ShippingMethodsOptions.cs` L34 | `DeliveryHorizonDays` (7) | `EXISTING` | Offered dates are synthesized from a constant horizon, not from capacity |
| 13 | Delivery **time** windows | Order Application constant | `StorefrontShippingCalculator.cs` L11–L17 `DayTimeWindows`; L155 returns them | Hard-coded `9-12`, `12-15`, `15-18`, `18-21` | `PARTIAL` | Static, service-agnostic, capacity-free; no per-service or per-carrier slots |
| 14 | Geography (province/city) | Order Application constant | `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Geography/StorefrontIranGeography.cs` L7–L23 | C# static list: 12 provinces, city names as **raw Persian strings** | `PARTIAL` | Not a geography model: 12 of 31 provinces; no country level; no sub-city/service area; no stable IDs; identity is localized text; no translations; no polygons or postal rules. An architecture guard pins this symbol (`src/backend/Host/Tooba.Host.Tests/Architecture/HostStorefrontAmcR3GuardTests.cs` L83) |
| 15 | Address destination | AddressBook + Order snapshot | `src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Models/CustomerAddressWrite.cs` L9–L10 (`ProvinceName`, `CityName`); persisted as strings in AddressBook migrations L84/L37; snapshot copy in `.../Tooba.Order.Domain/Aggregates/CheckoutGroup.cs` L96–L106 | Free-text `ProvinceName` / `CityName` / `PostalAddress` | `PARTIAL` | Destination identity is free text on both the address and the order snapshot; no FK to any geography entity |
| 16 | Province eligibility gate | Order Application | `StorefrontShippingCalculator.IsDestinationAllowed(rate, provinceName)`; applied `StorefrontShippingService.cs` L430–L434 | `AllowedProvinces` array on the rate (empty by default in the shipped defaults) | `PARTIAL` / risk | The guard is applied **only when `provinceName` is non-blank** (`&& !string.IsNullOrWhiteSpace(provinceName)` at L431–L432) → **fails open** for a blank destination, the opposite of the fail-closed intent in §9 of the proposal |
| 17 | Package/order-line grouping | Order aggregates | `CheckoutGroup.cs` L116–L141 (`ShippingMethodCode`, `MinimumDeliveryDate`, `RequestedDeliveryDate`, `RequestedDeliveryTimeWindow`), L222–L233 | Committed on the checkout group | `PARTIAL` | Selection and the date/time promise are snapshotted per checkout group (no `Label` property was found on this aggregate, unlike `Shipment`) |
| 18 | Delivery-group concept | — | `rg` over `src/backend` for `DeliveryGroup` / `delivery_group` / `deliveryGroup` → **no matches** | — | `MISSING` | The proposed architecture's central grouping primitive does not exist. Today the only grouping is seller order |
| 19 | Product/SKU physical attributes and logistics category | — | `rg` in `.../Tooba.Catalog.Domain` for `WeightKg` / `ShippingWeight` / `LogisticsCategory` / `RestrictedShipping` → **no matches**. `QuantityPolicy.cs` L6 `UnitOfMeasureDimension` is a **unit-of-measure** enum, not package dimension. `weight` appears only as a *seeded demo product attribute* (`.../Development/CatalogDemo/CatalogDemoMatrix.cs` L159, L175) | — | `MISSING` | No structured weight, dimensions or `NORMAL/LARGE_HEAVY/EXTRA_HEAVY` category exists in the Catalog domain, so no weight/dimensional rating or package-compatibility check is currently possible |
| 20 | Offer seller preparation / dispatch origin / shipping responsibility | — | `rg` in `src/backend/Modules/Offer` for `PreparationDays` / `DispatchOrigin` / `Origin` / `ShippingResponsib` / `SupplyingPartyId` → **no matches** | — | `MISSING` | Offer has no preparation promise, origin or fulfillment-responsibility field; preparation lives in Host config keyed by seller party id (§1 row 10) |
| 21 | Free shipping | Order Application (threshold only) | `StorefrontShippingCalculator.cs` L64 (`rate.FreeAboveSubtotal is decimal threshold && cartSubtotalExclusive >= threshold`) | `FreeAboveSubtotal` on the rate (unset in shipped defaults) | `PARTIAL` | Only a per-method cart-subtotal threshold. No policy engine, no seller-scope, no delivery-group scope, no subsidy cost bearer, no precedence/combining rules, no caps |
| 22 | Cart amount / weight / item-count limits, packing fee, capacity | — | `rg` for `PackingFee` / `packing_fee` / `WorkingDays` / `CapacityPerWindow` / `Blackout` → **no matches** | — | `MISSING` | No packing fee, no working-day calendar, no per-window capacity, no cart weight/count constraint |
| 23 | Paid-order fulfillment creation | Fulfillment | `.../Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.cs` L60–L85 `CreateFromPaidSellerOrdersAsync` (L73 `foreach (var sellerOrderId in sellerOrderIds.Distinct())`) | One `FulfillmentUnit` per distinct seller order | `EXISTING` | Grouping is per seller order; there is no delivery-group aggregate to attach to |
| 24 | Fulfillment unit identity / shipment ownership | Fulfillment Domain | `.../Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs` L25 `SellerOrderId`, L28 `CheckoutId`, L364 `ShipmentCreatedDomainEvent` | Nested shipments inside the unit | `EXISTING` | Because a unit carries `CheckoutId` + `SellerOrderId` and owns its shipments, **cross-customer consolidation is structurally impossible** — a favourable property to preserve |
| 25 | Physical shipment record | Fulfillment Domain | `.../Tooba.Fulfillment.Domain/Aggregates/Shipment.cs` L20–L56 (`CarrierDisplayName` L29, `ShippingMethodCode` L32, `ShippingMethodLabel` L35, `ProviderMetadataJson` L38, `ProviderMetadataVersion` L41, `TrackingReference` L44, `DispatchedAt` L50, `DeliveredAt` L53) | Persisted per shipment | `PARTIAL` | A shipment-level carrier **display name** and free-form `ProviderMetadataJson` exist, but there is no carrier entity, contract, tariff reference or carrier payable. This is presentation/telemetry metadata, not commercial liability |
| 26 | Shipping charge at payment | Payment | `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs` L175–L190 (L176 `shipping`, L184–L189 `StoreShipping` allocation); `.../Payment.Domain/Aggregates/PaymentAllocation.cs` L15 `StoreShippingTargetId`, L68–L69 `CreateStoreShipping`; `.../Payment.Domain/ValueObjects/PaymentAllocationTargetKind.cs` L14 | `StoreShipping = 1` bucket | `EXISTING` | Shipping is a **single store-level bucket**; merchandise goes to `SellerOrder` allocations (L180–L183). The code comment at L179 explicitly avoids attributing shipping to the first seller |
| 27 | Order-side shipping amount | Order | `.../Tooba.Order.Domain/Aggregates/CheckoutGroup.cs` L126 `ShippingAmount`, L228; `.../Tooba.Order.Contracts/Payments/CartPaymentQuoteContracts.cs` L43; `.../Integrations/Payment/CheckoutPaymentAccessBridge.cs` L56; `.../StorefrontCheckoutService.cs` L357–L358; `.../Integrations/Payment/OrderPaymentBridge.cs` L78; `.../Events/Payment/OrderPaymentSucceededHandler.cs` L81 | Decimal on the group snapshot | `EXISTING` | One scalar for the whole checkout group; no per-seller or per-service breakdown |
| 28 | Carrier payable / cost bearer / collection party / subsidy ledger | — | `rg` over `src/backend/Modules/Settlement` (109 `.cs` files) for `Carrier` / `carrier` / `Payable` / `Shipping` → **no matches** | — | `MISSING` | Settlement has **no** carrier-payable, shipping-cost or shipping-subsidy concept. `PaymentAllocationTargetKind` has exactly two kinds (`SellerOrder`, `StoreShipping`); no carrier payee exists |
| 29 | Collect-on-delivery / prepaid payment terms | — | `rg` over `src/backend` for `CollectOnDelivery` / `collect_on_delivery` / `CashOnDelivery` / `Prepaid` → **no matches** | — | `MISSING` | All shipping is implicitly prepaid-online; no COD flow, no partial collection, no multi-party authorization |
| 30 | Per-service distinct delivery calendar | Order Application | `StorefrontShippingCalculator.cs` L11–L17, L88–L95, L155 | Shared constants | `MISSING` | Every method shares one date derivation and one window list, regardless of provider |
| 31 | Localization of shipping strings | Mixed | Catalog names come from DB translations (`StorefrontShippingService.cs` L392–L398, L405–L410); but service text is hard-coded Persian: `ShippingMethodsOptions.cs` L55–L60 `LabelFa`; `StorefrontShippingService.cs` L442 (`"آماده امروز"`, `حدود … روز`) and L447–L452 `ToPersianLeadDays` | — | `PARTIAL` | Presentation copy and numeral conversion for shipping are hard-coded in an Order **application service**, bypassing the canonical localization mechanism |

---

## 2. Verified source-to-sink trace

```
Catalog / Offer                     Cart                    Order (checkout)                 Payment                Fulfillment
────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
structured weight /                 cart lines;             StorefrontShippingService        PaymentDirectory        FulfillmentUnit
dimensions / logistics              subtotal only           .ProjectAsync                    L175-190                per seller order
category  ── ABSENT ──►             (no package data)       L59                              StoreShipping bucket    L60-85
                                                                                                    │
Offer preparation / origin /                                StorefrontShippingCalculator          │                 Shipment
responsibility ── ABSENT ──►                                · MaxSellerPreparationDays L19-36     │                 CarrierDisplayName
                                                            · ResolveRate / QuotePrice L38-69     │                 ShippingMethodCode
                                                            · ComputeMinimumDeliveryDate L88-92   │                 TrackingReference
                                                            · BuildDeliveryDates L94+             │                 L20-56
                                                            · DayTimeWindows L11-17               │
                                                                                                  │
                                                            ┌─────────────────────────────┐       │
shipping_services /                                         │ catalog read (cross-module) │       │
shipping_service_options  ──►  IShippingCatalogReader  ──►  │ ShippingCatalogReader       │       │
(FF module tables)                                          │ (Fulfillment Infrastructure)│       │
                                                            └─────────────────────────────┘       │
                                                                                                    │
                                                            Tooba:ShippingMethods config           │
                                                            (EnabledCodes, Rates*,               │
                                                             prep days, horizon)                 │
                                                                                                  │
                                                            CheckoutGroup snapshot                 │
                                                            ShippingMethodCode /                   │
                                                            MinimumDeliveryDate /                  │
                                                            RequestedDeliveryDate /                │
                                                            RequestedDeliveryTimeWindow /          │
                                                            ShippingAmount                         │
                                                            (L96-141, L126)                        │
```

**Link confidence**

| Link | Confidence | Evidence |
| --- | --- | --- |
| UI → `POST /v1/storefront/shipping/projection` | Proven | `storefront-shipping.tsx` L41/L120 → `storefront-shipping-api.ts` L280 → `StorefrontOrderEndpoints.cs` L37 |
| Endpoint → service | Proven | `StorefrontOrderEndpoints.cs` L144–L162 → `ProjectStorefrontShippingQuery` → `StorefrontShippingService.ProjectAsync` L59 |
| Service → catalog | Proven | `IShippingCatalogReader` injected L26/L38, used L381; implementation `ShippingCatalogReader.cs`, table `shipping_services` (`FulfillmentDbContext.cs` L167) |
| Service → price/date/window | Proven | `StorefrontShippingCalculator` calls L70, L429, L436 |
| Catalog/Offer → package weight or logistics category | Proven **absent** | §1 rows 19–20 |
| Checkout commit → `ShippingAmount` → `StoreShipping` | Proven | `CheckoutPaymentAccessBridge.cs` L56, `StorefrontCheckoutService.cs` L357–L358, `PaymentDirectory.cs` L176–L189 |
| Shipping charge → carrier payable | Proven **absent** | §1 row 28 (no Settlement match) |
| Paid event → fulfillment unit per seller order | Proven | `FulfillmentDirectory.cs` L60–L85 |
| Order promise → delivery-group aggregate | Proven **absent** | §1 row 18 (`rg` no matches) |
| Promotion/Pricing module involvement in shipping cost | `UNVERIFIED` | Not traced in this timebox; no shipping reference was found in the shipping trace itself |

---

## 3. Current payment behaviour and carrier liability

- **Everything is prepaid online.** `PaymentDirectory.cs` L175–L190 builds the amount as `merchandise + shipping` and routes the shipping portion to the single `PaymentAllocationTargetKind.StoreShipping` bucket (`PaymentAllocationTargetKind.cs` L14). There is exactly one shipping bucket for the entire payment; no per-seller, per-service, per-shipment or per-carrier split.
- **No collect-on-delivery exists.** No `CollectOnDelivery` / `CashOnDelivery` / `Prepaid` symbol anywhere in `src/backend` (§1 row 29). The property name `ShippingAmount` and the allocation kind describe *collection from the buyer*, not terms of delivery.
- **No carrier payable exists.** The Settlement module (109 `.cs` files) contains no `Carrier`, `Payable` or `Shipping` reference (§1 row 28). `PaymentAllocationTargetKind` exposes only `SellerOrder` and `StoreShipping`; the `StoreShippingTargetId` constant (`PaymentAllocation.cs` L15) is a synthetic fixed GUID, i.e. an internal bucket, not a carrier identity.
- **Explicit absence of carrier-liability evidence at the inspected boundaries.** The only carrier-shaped data anywhere is on `Shipment`: `CarrierDisplayName` (L29), `ProviderMetadataJson` (L38) and `TrackingReference` (L44). These are presentation/telemetry fields. **No inspected boundary associates a monetary amount with a carrier.** Per the audit's own instruction, no generic `StoreShipping` allocation is equated with a carrier liability here.
- **`ProviderMetadataJson` is unverified as a commercially meaningful structure** — no reader/writer contract for a carrier tariff was found; treat any future claim about its contents as `UNVERIFIED`.
- **Free-shipping accounting:** the only discount mechanism is `FreeAboveSubtotal` (`StorefrontShippingCalculator.cs` L64), which reduces the *buyer charge only*. Nothing records who funds the discount, so if it were enabled today the carrier-payable question would be unanswerable from the current schema (§1 rows 21, 28).

---

## 4. Verified current behaviour summary (proven facts only)

1. Shipping catalog **availability** is DB-driven (`shipping_services` / `shipping_service_options`, `IsActive`, `SortOrder`, translation rows) and read across the Fulfillment → Order module boundary through `IShippingCatalogReader`.
2. Shipping **enablement** is a config intersection: DB `IsActive` **AND** `Tooba:ShippingMethods:EnabledCodes` (`StorefrontShippingService.cs` L378–L391).
3. Shipping **tariffs and lead times** are C# constants in `Tooba.Fulfillment.Contracts` (`ShippingMethodsOptions.cs` L36–L47) and are only overridden if `Tooba:ShippingMethods:Rates` is present and non-empty (`FulfillmentModule.cs` L90–L93). It is absent in both `appsettings.json` and `appsettings.Development.json`, so the compiled defaults are the effective rates today.
4. Shipping **delivery dates** = `today + max(all sellers' preparation) + method lead days` (`StorefrontShippingCalculator.cs` L36, L88–L92), with a synthesized horizon of 7 days.
5. Shipping **time windows** are the four hard-coded strings `9-12`, `12-15`, `15-18`, `18-21` (`StorefrontShippingCalculator.cs` L11–L17), shared by every method.
6. Destination eligibility is province-string based, with a 12-province hard-coded Persian list (`StorefrontIranGeography.cs` L7–L23), and **fails open** on a blank province (`StorefrontShippingService.cs` L430–L432).
7. The order snapshot stores the method code, minimum date, requested date, requested time window and one `ShippingAmount` scalar (`CheckoutGroup.cs` L96–L141, L126).
8. At payment, shipping becomes one `StoreShipping` allocation (`PaymentDirectory.cs` L184–L189).
9. Paid checkout creates exactly one `FulfillmentUnit` per distinct seller order, and shipments are nested inside it with carrier display metadata (`FulfillmentDirectory.cs` L60–L85; `FulfillmentUnit.cs` L25, L28; `Shipment.cs` L20–L56).

---

## 5. Most important contradictions to `docs/architecture/SHIPPING-ARCHITECTURE.md`

| Architecture draft claim | Contradicting verified fact | Severity |
| --- | --- | --- |
| §1 / §0: shipping authority is proposed as a module, and existing contracts must not be duplicated. | The shipping HTTP surface, projection orchestration, rate resolution, date/time derivation, free-shipping threshold and destination gate all live in **Order**; the catalog **data** lives in **Fulfillment**; enablement and tariffs live in **Fulfillment config**. There is no Shipping capability to extract or extend today — only two modules sharing one concern. The extraction boundary is therefore a **cross-module decoupling** problem, not a green-field build. | High |
| §2: Country → City → Sub-city with **stable IDs, translations**, identity never localized text. | `StorefrontIranGeography.cs` L7–L23 is a hard-coded 12-province C# constant with **localized Persian city strings as identity**; `AddressBook` and the order snapshot store `ProvinceName` / `CityName` as **free text** (`CustomerAddressWrite.cs` L9–L10; `CheckoutGroup.cs` L96–L106). No country level, no sub-city, no IDs, no translations. | Critical |
| §3: per-service rate sources (`ZoneRate`, `WeightOrDimensionalRate`, `CarrierApiRate`, `ManualQuotation`) with version/effective dates; "no single global BasePrice". | The effective rating is literally one `BasePrice` + `LeadDays` per method code, compiled into `ShippingMethodsOptions.cs` L36–L47, plus one `FreeAboveSubtotal` threshold. Zone support is only a province allow-list. No version, no effective dates, no tariff history. | Critical |
| §3: separate `CarrierCost` / `CustomerShippingCharge` / `ShippingSubsidy` / `PricingAuthority` / `CollectionParty` / `CostBearer` / `FulfillmentOwner`. | Exactly one monetary concept exists: `ShippingAmount` → `StoreShipping` allocation. No carrier cost, no subsidy, no cost bearer, no fulfillment-owner. | High |
| §4: shipping policy engine scoped Tooba-global or Seller, with precedence/combining/caps/audit. | Only a scalar `FreeAboveSubtotal` cart-subtotal threshold. No policy entity, no scope, no precedence, no cap, no subsidy bearer. | High |
| §5: quote per **eligible delivery group**; do not use a single max preparation across all sellers; group keys not based on service name. | `MaxSellerPreparationDays` (L19–L36) collapses every seller to one value via `.Max()`, and `ComputeMinimumDeliveryDate` (L88–L92) applies it cart-wide. There is **no** `DeliveryGroup` concept anywhere (`rg`: no matches). Confirmed: the current design does what §5 forbids. | Critical |
| §5: "offer only services and windows that can genuinely be fulfilled (no universal fabricated slot promise)". | Every method shares the same four hard-coded windows (L11–L17) and the same synthesized 7-day horizon; no capacity, calendar or blackout input exists. The universal fabricated slot promise is the **current** behaviour. | High |
| §5: "Delivery Group is a promise/operational grouping; Shipment is a physical package". | No delivery group exists; the operative grouping is the seller order (`FulfillmentDirectory.cs` L73). | High |
| §5: "Never physically consolidate different customers' items in the same customer shipment." | Already structurally guaranteed: a `FulfillmentUnit` carries `CheckoutId` + `SellerOrderId` and owns its shipments (`FulfillmentUnit.cs` L25, L28). This is an **existing invariant to preserve**, not a gap to close. Conversely, §9's open question about consolidating multiple groups **within the same customer order** is currently impossible across sellers because each seller order gets its own unit. | Medium (positive) |
| §6: model preparation lead time separately from carrier transit; support working days, blackouts, per-window capacity; distinct packing fee; estimated range. | Lead days are a single `LeadDays` int per method; preparation is config-only and cart-wide; no working days, blackouts, capacity or packing fee exist (`rg`: no matches). | High |
| §1: Catalog owns actual/dimensional weight, dimensions and logistics category. | Absent from the Catalog domain (`rg` in `Tooba.Catalog.Domain`: no matches). `weight` exists only as a seeded demo *product attribute*, which is presentation data, not a shippable physical property. | Critical |
| §1: Offer owns supplying/dispatch origin, preparation promise and fulfillment responsibility. | Absent from the Offer module (`rg`: no matches). Preparation is Host config keyed by a seller party GUID. | High |
| §1 / §9: Fulfillment module boundaries are described as existing to be respected. | `CarrierDisplayName` / `ProviderMetadataJson` / `TrackingReference` already sit on `Shipment` (`Shipment.cs` L29–L44), i.e. Fulfillment already holds a carrier-shaped surface. Any future carrier modelling must reconcile with these fields rather than add a parallel concept. | Medium |
| §7: Tooba admin governs tariffs, provider contracts, coverage, calendars, subsidies. | The Admin write surface accepts only `Code`, `IsActive`, `SortOrder` and translations (`ShippingServiceEndpoints.cs` L17; `IShippingServiceDirectory.cs` L7, L10). It cannot edit price, lead time, coverage or payment terms. Any "govern tariffs" requirement implies new admin contracts. | High |
| §3: "infrastructural switches and credentials remain in configuration/secrets". | Consistent with current reality (`Tooba:ShippingMethods`), but note the draft's §0 statement that checkout shipping projection "is reported to mix DB catalog availability, appsettings rates/lead days and hard-coded delivery slots" is **partly inaccurate**: rates/lead days are not currently in appsettings at all — they are compiled C# defaults, with appsettings only able to *override* them. | Medium (factual correction) |

---

## 6. Priority-ordered confirmed architecture blockers (max 5)

1. **No destination identity model (critical).** Country/City/Sub-city do not exist; destination is free-text Persian on both `AddressBook` and the order snapshot, with a 12-province constant used for eligibility. Every downstream concern — coverage, zone rating, serviceability, address translation, correct failure behaviour — is blocked on this. The eligibility gate also **fails open** for blank destinations (`StorefrontShippingService.cs` L430–L432).
2. **Cart-wide delivery promise derived from the worst-case seller (critical).** `MaxSellerPreparationDays` + `ComputeMinimumDeliveryDate` impose one date for the entire cart, and there is no delivery-group aggregate. Until a grouping primitive exists, group-level quoting, per-group promises and honouring faster sellers are impossible.
3. **Shipping authority is structurally split across Order, Fulfillment and Host config (critical).** Endpoint surface, projection logic, rating, dates/windows, free threshold and destination gate are in Order; catalog tables are in Fulfillment; enablement/tariffs/lead-day overrides are in `Tooba:ShippingMethods`. Neither a Shipping module nor a single authority exists, and the Fulfillment↔Order catalog read is a live cross-module coupling.
4. **No physical/logistics attributes and no rate model beyond one flat price (high).** Catalog has no structured weight, dimensions or `NORMAL/LARGE_HEAVY/EXTRA_HEAVY` category; rating has no zone tiers, weight/dimensional tiers, carrier API or manual quotation, and no version/effective dates. Weight-based rating and package-compatibility checks are unimplementable today.
5. **No commercial shipping ledger (high).** Nothing distinguishes carrier cost, customer charge, subsidy or cost bearer; Settlement has no carrier payable; COD does not exist. Consequently even the existing `FreeAboveSubtotal` threshold cannot be accounted for, and any future free-shipping or subsidy feature would create unattributable cost.

*Runner-up (not in top five): hard-coded Persian presentation copy and numeral conversion inside an Order application service (`StorefrontShippingService.cs` L442, L447–L452) plus `LabelFa` in `Fulfillment.Contracts` (`ShippingMethodsOptions.cs` L55–L60), which bypasses the canonical localization mechanism.*

### Recommended next bounded slice — for Architect review only (no execution)

**`TB-SHIPPING-GEOGRAPHY-IDENTITY-001` (design/ADR-first, read-mostly):** resolve blocker #1 in isolation by producing a destination-identity ADR and a bounded plan covering (a) a persisted Country/Province/City/ServiceArea hierarchy with stable IDs and translation rows, (b) how `AddressBook` free-text and the `CheckoutGroup` snapshot migrate without adding a module or breaking the pinned `StorefrontIranGeography.Provinces` architecture guard, and (c) the fail-closed destination-eligibility rule. Explicitly out of scope: rating, delivery groups, carrier payable, UX, any code move.

Rationale: the geography decision is the only blocker whose resolution is a prerequisite for *every* other item (§6 rows 2–5 all depend on a resolvable destination), it is cheap to decide, and it can be settled without touching live checkout behaviour or the certified module boundaries — keeping the Cart-Active-until-final-Pay behaviour and current Order/Fulfillment shipping commitments intact.

---

## 7. Non-actions (explicit)

- No feature implementation; no endpoint, DTO, contract or business-model edits.
- No test suite was executed; no test that mutates persistent data was created or run.
- No migration, database change, seeding or DB write; no DB row was read.
- No runtime intervention: no service restart, no environment creation, no Host redeploy.
- No PSP, wallet or payment transaction; no paid order.
- No code refactor and no Shipping module was created.
- `docs/architecture/SHIPPING-ARCHITECTURE.md` was read only and **not** modified.
- No edits to `AGENTS.md`, Host, frontend, Order, Fulfillment, Payment, Settlement, Catalog, Offer, AddressBook or any other module file.
- No new tasks generated; no follow-on execution; no indefinite polling.

## 8. Remaining unknowns

- Whether `Promotion` / `Pricing` participates in shipping cost or shipping discounts (`UNVERIFIED`; no reference found in the traced shipping path).
- Whether `Shipment.ProviderMetadataJson` is consumed anywhere as a structured carrier contract, and by whom (`UNVERIFIED`).
- Current live enablement in the running development host (config for the DB `IsActive` state was not read; runtime was not probed).
- Whether any seller-facing or operator-facing UI implies fulfillment responsibility or carrier selection beyond the display metadata on `Shipment` (`UNVERIFIED`).
- Whether the 12-province constant is duplicated anywhere else in the frontend (`UNVERIFIED`).

## 9. Stop report

- Audited HEAD `d5f74da6cd820950dbea2c47d7e87929619d7f91` == `origin/main` == Expected-Base-SHA. Preflight drift was a single upstream Architect docs commit, recovered by a verified fast-forward; unrelated dirty work was preserved.
- Sole repository write in this task: this report.
- Status: `PASS` (audit delivered) → Final state `STOP_FOR_ARCHITECT`.
- `automaticNextImplementationTask = NONE`. No next task was started or authorized.
