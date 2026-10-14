# Shipping — Geography Owner / Contracts ADR (`TB-SHIPPING-GEOGRAPHY-BOUNDARY-ADR-036`)

> **STATUS: PROPOSED / NOT CERTIFIED.**
> This is a decision-candidate document for Architect acceptance. It is **not** an
> implementation order, **not** an accepted ADR, and **not** a certification of
> `docs/architecture/SHIPPING-ARCHITECTURE.md` (still DRAFT). No schema, code, contract,
> endpoint, config, seed or runtime state is authorized or changed by this document.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected / Base / Audited SHA | `a814b8cb924e0c591b36cd99e561ad9bf2b020eb` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-DESIGN-035` (DESIGN ACCEPT as planning evidence only) |
| Upstream evidence | `docs/planning/shipping/SHIPPING-CURRENT-STATE-AUDIT.md`, `docs/planning/shipping/SHIPPING-GEOGRAPHY-DESIGN-035.md` |
| Mode | `DESIGN_ONLY_SOURCE_GROUNDED_ADR` |
| Stop-State | `STOP_FOR_ARCHITECT` |
| Timebox | 30 min HARD |

## 0. Purpose and non-goals

035 left three decisions unresolved: **D3** (who owns the geography reference catalogue),
**D8** (how the pinned legacy geography surface is retired without breaking its guard), and
**D9** (where display-label translations live). 036 closes D3 with one recommendation grounded
in existing source, and defines D8/D9 as bounded strategies plus a **D10** (administrative-level
modelling) status. It also adds the operational safety gates that must exist before any
fail-closed switch — the single largest risk of an architecture-wide shipping outage.

**Non-goals:** no geography module is created, no delivery groups, no carrier payable, no pricing
refactor, no Shipping module extraction, no checkout/Order/Fulfillment/Payment behaviour change.

---

## 1. Source-verified evidence set

| # | Fact | Exact path + lines |
| --- | --- | --- |
| E1 | The repo already contains **32 certified modules** (`AccessControl, Identity, Media, OperatorProfile, ProductQnA, BulkInquiry, Wishlist, UserPreference, PageComposition, Catalog, Party, Localization, Order, Cart, StoreContext, Offer, Payment, Settlement, Story, Fulfillment, AddressBook, Content, CustomerProfile, Inventory, Notification, Pricing, ProductWorkspace, Promotion, Returns, Support, Tax, Wallet`), each with `structureCertified: true`, `lockVersion: "ARCH-COMPLETE-002"`; structural rules are `APPLICATION_CAPABILITY_FOLDERS`, `ENDPOINTS_CAPABILITY_FOLDERS`, `INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS`, `PATH_NAMESPACE_ALIGNMENT`, `ROOT_ALLOWLIST`, `NO_NAMESPACE_ALIAS_WORKAROUND`; `uncertifiedHttpOwningModules: []`, `preCertModules: []` | `docs/architecture/tmar-module-structure-manifests.json` (JSON keys `modules[].module`, `modules[].structureCertified`, `modules[].lockVersion`, `rules`, `uncertifiedHttpOwningModules`, `preCertModules`) |
| E2 | A small **neutral capability module** with only Contracts + Infrastructure already exists as an accepted shape — `StoreContext` consists of exactly three files and exposes Contracts-only read/assign seams | `src/backend/Modules/StoreContext/Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs` L18–L34 (`StoreCommerceContext` record + `ICurrentStoreCommerceContext`, `IStoreCommerceContextAssigner`, `IWorkerStoreCommerceContextFactory`); `.../Tooba.StoreContext.Infrastructure/Current/StoreCommerceContextAccessor.cs`; `.../DependencyInjection/StoreContextModule.cs` |
| E3 | The pinned guard asserts **two literal strings** live in Order endpoints and states the intended ownership line (`geography → Order.Endpoints`) | `src/backend/Host/Tooba.Host.Tests/Architecture/HostStorefrontAmcR3GuardTests.cs` L5–L8 (doc comment) and L73–L85 (`Order_owns_geography_provinces_route`: asserts `MapGet("/geography/provinces"` **and** `StorefrontIranGeography.Provinces`) |
| E4 | The route serves the constant directly; response is unversioned, unlocalized, country-parameterless | `.../Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs` L41–L42 |
| E5 | The catalogue itself: 12 provinces, cities as raw Persian strings, no country level, no sub-city, no city ids, no translation rows | `.../Tooba.Order.Application/Storefront/Geography/StorefrontIranGeography.cs` L6–L24 |
| E6 | Wire DTO shape: province has a `Code`; `Cities` is `IReadOnlyList<string>` (no city codes) | `.../Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs` L110 |
| E7 | Projection/draft DTOs carry destination as **strings** plus `SavedAddressId` | same file L114–L131 (`StorefrontShippingDraftView`), L134–L150 (`StorefrontShippingProjection`) |
| E8 | Frontend selects by **localized label**, so `Code` is effectively unused on the wire | `src/frontend/app/storefront/storefront-shipping.tsx` L170–L172, L442–L446, L460–L467, L758–L759, L770–L775 |
| E9 | AddressBook owns the customer address; destination is free text with length bounds only; no membership check against any catalogue | `.../Tooba.AddressBook.Domain/Aggregates/CustomerAddress.cs` L21 (`DefaultCountry = "IR"`), L42–L79 (`Country` L60, `ProvinceName?` L63, `CityName` L66), L199–L209 (`ApplyFields`) |
| E10 | The address contract is a flat DTO read through a Contracts-only port | `.../Tooba.AddressBook.Contracts/Dtos/CustomerAddressRecord.cs` L4–L18; `.../Contracts/Ports/IAddressBookCheckoutLookup.cs` L9–L13 |
| E11 | Order **persists** the destination as plain columns (expand pattern already proven: two blocks of `ProvinceName/CityName/PostalAddress/PostalCode` with max lengths 64/64/512/16) | `.../Order.Infrastructure/Persistence/OrderDbContext.cs` L115–L118 and L139–L142 |
| E12 | Order's immutable handoff to Fulfillment carries `ProvinceName`/`CityName` strings as part of a frozen snapshot record | `.../Tooba.Order.Contracts/Fulfillment/OrderFulfillmentContracts.cs` L6–L21 (L14 `ProvinceName`, L15 `CityName`); reader interface L33+ |
| E13 | Order + cart draft hold the promise and the destination strings | `.../Tooba.Order.Domain/Aggregates/CheckoutGroup.cs` L96 (ProvinceName), L101 (CityName), L106 (PostalAddress), L116, L126, L131, L136, L141; `.../Tooba.Order.Domain/Checkout/CartShippingDraft.cs` L40, L43, L52, L145–L149 |
| E14 | Localization owns **language**, not place: `Language` aggregate keyed by `LanguageId` with `Code`, `NativeName`, `Direction`, `IsActive`; Contracts expose only language listing/activation/guard | `.../Tooba.Localization.Domain/Aggregates/Language.cs` L18–L26; `.../Application/Ports/ILanguageDirectory.cs` L6–L17; `.../Contracts/Ports/LanguageLookupContracts.cs` L4–L18 (`LanguageLookupSnapshot`, `ILanguageLookup`); `.../Contracts/Ports/ILanguageReferenceGuard.cs` L7–L11; `.../Contracts/Ports/ILanguageActivationPort.cs` |
| E15 | The **translation-row precedent** is established twice, with a unique index on `(entityId, LanguageId)` | `.../Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs` L176–L184 (`shipping_service_translations`, unique index L183) and L194–L201; `.../Catalog.Infrastructure/Persistence/CatalogDbContext.cs` L258 (`category_translations`), L426, L594 |
| E16 | Shipping **catalogue data** physically lives in Fulfillment and is read by Order across the module boundary | `.../Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs` L167 (`shipping_services`); `.../Fulfillment.Infrastructure/Shipping/ShippingCatalogReader.cs` L20, L54, L78–L92 |
| E17 | Party owns seller organizations but has **no structured dispatch origin** — seller settings expose only `AddressLine` free text | `.../Tooba.Party.Application/Seller/Models/PartySellerSettingsModels.cs` L10–L31; port `.../Tooba.Party.Contracts/Ports/IPartySellerSettings.cs` |
| E18 | The serviceability gate compares **province name strings**, and the caller **fails open** for a blank province (the pure function would deny it) | `.../Storefront/Services/StorefrontShippingCalculator.cs` L72–L83; `.../Storefront/Services/StorefrontShippingService.cs` L429–L433 |
| E19 | The shipped defaults leave `AllowedProvinces` **empty for every method**, and empty means "no restriction" | `.../Fulfillment.Contracts/Shipping/ShippingMethodsOptions.cs` L10–L17 (L16), L36–L47; `StorefrontShippingCalculator.cs` L72–L76 |
| E20 | Effective enablement is a config ∩ DB intersection, and the effective rate table is compiled, not configured | audit 034 F3/F4 (findings restated there; re-verified in 035 §1) |

**`UNVERIFIED` items carried forward:** live DB `IsActive` state of `shipping_services`; whether any
other frontend surface duplicates the 12-province list; whether a generic cross-module translation
abstraction exists (it does **not** appear in Localization Contracts, E14).

---

## 2. D3 — Ownership: comparison and recommendation

### 2.1 Candidate comparison

| Candidate owner | Source-grounded argument **for** | Source-grounded argument **against** | Verdict |
| --- | --- | --- | --- |
| **Order** (current de-facto owner, E3/E4/E5) | Already owns the route, the constant and the guard; zero new module cost | Order is a **state/transaction** module owning cart→checkout→seller orders→immutable promises (E13). A global place-catalogue is reference data with no order state. Keeping it here forces AddressBook, a future Shipping capability and any origin-side consumer to depend on **Order** for a non-order concern, and it preserves the very conflation 035 was created to fix. Guard E3 pins the *current* surface, not the *correct* home. | Reject as owner |
| **AddressBook** | Already holds country/province/city fields and validates them (E9) | It owns **customer PII addresses** (E10). Making it the global catalogue turns a customer-data module into a platform dependency; ownership rules forbid other modules reading its DB, so every consumer would add a Contracts edge to a PII module. Address validation is a *consumer* of geography, not its authority. | Reject as owner |
| **Catalog** | Repo's reference-data powerhouse (categories, brands, attributes, units) with translation-row precedent (E15) and admin grids | Catalog is the **product taxonomy** owner. Geography is not product; hosting it here makes AddressBook/Order/Shipping depend on the product module for a shipping concern, the same dependency-inversion the Offer/Catalog split was designed to avoid. | Reject as owner |
| **Localization** | Owns `LanguageId`-keyed reference data with a clean Contracts port (E14); geography needs translated labels | Localization owns **language/locale**, not **place**. Conflating administrative geography with UI culture would make every geography read depend on localization semantics and would couple place identity to language availability. Its Contracts expose language listing/activation/guard only (E14) — nothing geography-shaped. | Reject as owner; **reuse the translation-row pattern** |
| **Fulfillment** | Already physically stores the shipping catalogue consumed by storefront (E16) | Audit 034 flagged the Fulfillment→Order catalogue read as a live cross-module coupling (B3), and Fulfillment's shipping-catalogue ownership is itself an open redesign question. Adding geography would entrench the wrong boundary and mix an operational module with platform reference data. | Reject as owner |
| **Party** | Owns organizations/sellers that have origins | Seller side has only free-text `AddressLine` (E17) — no geography capability to extend; Party is organizational identity, not place reference data. | Reject as owner (Party stays a *consumer* for origin data) |
| **StoreContext** | Proven precedent for a tiny neutral capability with Contracts-only seams (E2); already models store-wide effective context | It models **effective commerce context for a request scope** (Market/Currency/SalesChannel), not a persistent hierarchy with translations and admin lifecycle. It has no DbContext. | Reject as host; **cite as the shape precedent** |
| **Dedicated neutral `Geography` capability** (recommended) | It is a **leaf** reference catalogue: every other module may depend on it and it depends on none (except BuildingBlocks), which is the only arrangement that satisfies "Host composes only / Contracts-only cross-module / no parallel catalogues". It has a natural home for identity + translation rows with the established `(entityId, LanguageId)` unique-key pattern (E15), and matches the repo's one-concern-per-module convention (E1) — the same convention that already produced a 3-file neutral module (E2). | Cost: a new module must be created and later certified under `ARCH-COMPLETE-002`, and added to the structure manifest (E1). Seed data must start **minimal and explicit**, never a blanket nationwide import. | **RECOMMENDED** |

### 2.2 Recommendation (one)

**D3 — RECOMMENDED: a dedicated neutral geography reference capability owns the catalogue**
(suggested project prefix `Tooba.Geography.*`, naming subject to Architect approval), exposing
**Contracts-only read ports**. It owns *place identity and place labels*. It owns **no** commercial
policy, **no** coverage decisions, **no** customer addresses, **no** order state.

**Responsibility split (no duplicate authority):**

| Concern | Owner | Rule |
| --- | --- | --- |
| Country / AdministrativeArea / City / ServiceArea identity + codes + hierarchy | **Geography** capability | Identity is an ID/code; labels are translations |
| Display labels for a `LanguageId`, with fallback | **Geography** capability (translation rows) | Uses `ILanguageLookup` (E14) to know the language set and default; never owns the language list |
| Which language is default/active | **Localization** (unchanged, E14) | No geography duplication of language data |
| Customer address row (recipient, phone, line, postal code, chosen ids) | **AddressBook** (unchanged owner, E9/E10) | Consumes Geography via Contracts; never a second catalogue |
| Seller dispatch origin / operating region | **Party** (seller settings, E17) + catalogued place ids | Origin becomes an id reference in a later slice; not in this ADR's scope |
| Immutable order destination snapshot | **Order** (unchanged, E11/E12/E13) | Freezes ids **and** rendered labels; never re-resolves |
| Carrier/service origin→destination **coverage policy** | **Shipping** capability (future) | Consumes Geography ids; geography must not encode carrier rules |
| Composition / DI / route registration | **Host** | Composition only; no business logic |
| Language reference integrity (is a language still referenced?) | **Localization** Contracts (E14 `ILanguageReferenceGuard`) | Geography registers its own reference guard if needed; no cross-module DB reads |

### 2.3 Dependency direction (text diagram)

```
                      ┌──────────────────────────────────────────────┐
                      │  GEOGRAPHY capability  (RECOMMENDED, leaf)   │
                      │  Domain: Country, AdministrativeArea, City,  │
                      │          ServiceArea (+ translation rows)    │
                      │  Contracts: read-only lookup ports +         │
                      │             resolution outcomes              │
                      │  Infrastructure: own DbContext + adapters    │
                      │  Depends on: BuildingBlocks, Localization.   │
                      │              Contracts (language set only)   │
                      └──────────────────────────────────────────────┘
                          ▲              ▲               ▲
        ids + labels      │              │               │
                          │              │               │
        ┌─────────────────┴──┐  ┌────────┴────────┐  ┌───┴──────────────────────┐
        │ AddressBook        │  │ Order (checkout,│  │ Shipping capability      │
        │ (owns address row, │  │  snapshot,      │  │ (future: coverage policy │
        │  consumes lookup)  │  │  handoff)       │  │  origin×destination)     │
        └────────────────────┘  └─────────────────┘  └──────────────────────────┘
                 │                       │                      │
                 └───────────── Host composes (DI only) ───────┘

        Party ──(seller origin, later slice)──► Geography.Contracts
        Fulfillment ──(receives frozen strings in handoff, E12)── Order snapshot
        Localization ──(language set/default; no place data)──► Geography

  FORBIDDEN: cross-module DbContext reads · a second geography catalogue ·
             Geography depending on Order/Catalog/Fulfillment/Party ·
             Shipment/Order schema changes driven by geography design
```

---

## 3. Contracts-only boundary specification (PROPOSED)

All types below are **proposals only** — none exists today.

### 3.1 Geography capability — `Tooba.Geography.Contracts`

```text
DestinationLevel            enum { Country, AdministrativeArea, City, ServiceArea }
GeoNodeKind / level discriminator        (feeds OPEN decision D10)

CountrySnapshot              (CountryId, CountryCode, IsActive)
AdministrativeAreaSnapshot   (AreaId, CountryId, ParentAreaId?, LevelCode?, Code, IsActive)
CitySnapshot                 (CityId, CountryId, AreaId?, Code, IsActive)
ServiceAreaSnapshot          (ServiceAreaId, CityId, Code, IsActive)

DestinationIdentifiers       (CountryId? , AreaId? , CityId? , ServiceAreaId?)
DestinationResolutionOutcome enum { Resolved, Ambiguous, Unresolved }
DestinationResolution        (Outcome, DestinationIdentifiers?, CandidateCount)

IGeographyLookup             // read-only; the ONLY cross-module seam
    Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(ct)
    Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAreasAsync(Guid countryId, ct)
    Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(Guid countryId, Guid? areaId, ct)
    Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(Guid cityId, ct)
    Task<DestinationIdentifiers?> GetByIdsAsync(DestinationIdentifiers ids, ct)
    Task<DestinationResolution> ResolveLegacyAsync(string? country, string? areaName, string? cityName, string? serviceAreaName, ct)

IGeographyLabelLookup        // labels are separate from identity
    Task<IReadOnlyDictionary<Guid,string>> GetCityLabelsAsync(IReadOnlyCollection<Guid> cityIds, string? languageCode, ct)
    Task<IReadOnlyDictionary<Guid,string>> GetAreaLabelsAsync(IReadOnlyCollection<Guid> areaIds, string? languageCode, ct)
```

**Boundary rules (binding on any future implementation):**

1. `ResolveLegacyAsync` never creates identity; it only maps text → ids or returns
   `Ambiguous`/`Unresolved`. It must be pure with respect to order state.
2. No `IGeographyWritePort` is exposed to other modules. Catalogue administration lives in the
   Geography capability's own endpoints/authorizers, following repo conventions (E1).
3. Geography never returns a "serviceability" verdict. Verdicts belong to Shipping.
4. No other module may add a `ProvinceName`-style string field to its schema without an ADR
   exception; new destination fields are ids + frozen labels.

### 3.2 AddressBook boundary

- `CustomerAddress` (E9) keeps `Country` / `ProvinceName?` / `CityName` as **transitional**
  display/carry fields and gains **nullable** `CountryId?`, `AreaId?`, `CityId?`, `ServiceAreaId?`.
- `CustomerAddressRecord` (E10) gains the same nullable ids; `IAddressBookCheckoutLookup`
  signature is unchanged (address resolution stays an AddressBook concern).
- AddressBook must **not** re-export geography lists to other modules; consumers use
  `IGeographyLookup` directly.

### 3.3 Order boundary

- `CheckoutGroup` (E13) and `CartShippingDraft` (E13) gain nullable ids alongside the existing
  strings; both strings **and** ids are frozen at commit (rendered labels are the strings already
  stored, so historical rendering is preserved byte-for-byte).
- `OrderFulfillmentHandoffSnapshot` (E12) gains nullable ids **append-only**; existing positional
  fields L6–L21 are not reordered or removed, so Fulfillment adapters keep compiling.
- `OrderDbContext` (E11) gains nullable columns in the same two blocks (L115–L118, L139–L142);
  no drop, no rename, no nullability tightening.

### 3.4 Shipping coverage boundary

- Serviceability is expressed as `origin ids × destination ids × serviceCode ⇒ ALLOWED|DENIED`
  and is owned by the (future) Shipping capability's Contracts, **not** Geography.
- The existing `AllowedProvinces` string array on `ShippingMethodRateOptions`
  (`ShippingMethodsOptions.cs` L10–L17) remains readable and is bridged, not deleted, and only
  after Phase 3 gates pass (§4.2). The **empty array's current meaning — "unrestricted"** (E19
  L72–L76) — must be preserved until explicit coverage rows exist (see Gate G2/G3).

---

## 4. Rollout, D8 legacy compatibility, and D9 localization

### 4.1 D8 — guard-safe legacy migration strategy

**Source facts:** the guard asserts exactly two literal strings (E3 L73–L85); the route serves the
constant (E4 L41–L42); the constant itself has no database (E5); the wire DTO's `Code` is unused by
the UI, which submits labels (E6/E8).

**Strategy (additive, never remove/bypass/alter during this planning):**

| Step | Action | Guard impact |
| --- | --- | --- |
| D8.1 | Introduce the new catalogue and a **new, additional** read route inside the geography capability (e.g. `/v1/geography/catalog`) — no change to the existing route | None |
| D8.2 | `GET /v1/storefront/geography/provinces` **stays as-is**, still returning `StorefrontIranGeography.Provinces` | None (guard passes unmodified) |
| D8.3 | Add **nullable** ids to AddressBook/Order fields; strings remain authoritative for rendering | None |
| D8.4 | Add legacy-text resolution with `Resolved/Ambiguous/Unresolved`; surfaces may *additionally* return ids | None |
| D8.5 | Frontend begins submitting **ids** while still displaying labels; label-valued controls become code/id-valued | Behaviour-affecting → requires its own task + visual review |
| D8.6 | The legacy route becomes a thin **compatibility shim** that projects the new catalogue back into `StorefrontProvinceOption` shape | Guard still passes: route name + symbol retained |
| D8.7 | Only if/when the Architect decides to retire the shim: **amend the guard under explicit authority** | **PENDING — D8-OPEN** |

Explicit prohibitions: do not edit `HostStorefrontAmcR3GuardTests`, do not delete
`StorefrontIranGeography`, do not change the route name, do not change `StorefrontProvinceOption`'s
existing shape in this phase.

### 4.2 D9 — localization ownership and fallback

- **SOURCE-VERIFIED:** translation rows keyed by `LanguageId` with unique `(entityId, LanguageId)`
  are already the repo pattern (E15); the language set/default lives in Localization and is
  reachable through a Contracts port (E14 `ILanguageLookup`).
- **RECOMMENDED:** geography label translations are stored by the **Geography** capability, keyed
  by `LanguageId`, resolved via `ILanguageLookup` for the language set and default. Fallback chain:
  *requested language → default language (`IsDefault`) → stable code*. IDs/codes are never
  localized.
- **OPEN (D9-OPEN):** there is **no generic cross-module translation service** in Localization
  Contracts today (E14 exposes language listing, activation and reference-guard only). Whether to
  introduce a shared translation port/abstraction is **not decided here** and must not be invented.
  Until decided, geography owns its own translation rows, exactly as Fulfillment and Catalog do for
  their own reference data (E15).

### 4.3 Phased sequence (corrected order)

```
Phase 0a  Catalogue structure + MINIMAL, EXPLICIT region data
          (no blanket nationwide import; nothing enabled by default)
Phase 0b  Service-coverage bootstrap: explicit coverage rows per enabled service
          (never infer an unrestricted grant from an empty AllowedProvinces — E19)
Phase 1   Nullable ids on AddressBook / cart draft / Order snapshot  (FUTURE task)
Phase 2   Legacy text resolution + reconciliation states
          (Resolved / Ambiguous / Unresolved), shadow-compared, no decision change
Phase 3   Fail-closed gate enabled ONLY after 0b + Phase 2 gates pass
Phase 4   Legacy AllowedProvinces bridge + shim retirement discussion (Architect only)
```

### 4.4 Operational safety checklist — preventing a checkout-wide shipping outage

| Gate | Requirement before Phase 3 can be enabled | Why (source) |
| --- | --- | --- |
| **G1 — no-regression resolution** | Every destination that resolves today still resolves: shadow-compare the legacy province-name path (E18 L72–L83) against `ResolveLegacyAsync` across the current 12-province set (E5) plus representative free-text city values | Prevents turning resolvable carts into `Unresolved` |
| **G2 — coverage completeness** | Explicit coverage rows exist for **every** service in the effective enabled set — the intersection of `Tooba:ShippingMethods:EnabledCodes` and DB `IsActive` (audit 034 F3, E20) — before default-deny applies to that service | Default-deny with zero coverage rows denies **all** shipping for that service |
| **G3 — empty-array semantics preserved** | Empty `AllowedProvinces` today means **unrestricted** (E19 L72–L76). That meaning must survive the bridge; it must not be silently inverted into "deny all". Inversion requires an explicit Architect ruling **and** completed G2 | Highest-severity outage risk in this design |
| **G4 — shadow mode + kill switch** | New resolution runs alongside legacy; differences are logged, and **legacy still decides** until G1–G3 pass. A single config flag disables the new gate | Rollback without data migration |
| **G5 — additive rollback** | All schema changes are additive/nullable; rollback = flip the flag, not a data restore | E11 shows the repo's expand pattern |
| **G6 — history and non-checkout flows never blocked** | Historical orders are never re-evaluated (E11/E12/E13 snapshots are frozen); `Unresolved`/`Ambiguous` must never block address create/update/list or order viewing | Preserves committed promises and address CRUD |
| **G7 — fail-open caller fix is part of Phase 3** | The caller at `StorefrontShippingService.cs` L429–L433 only enforces the allow-list for a non-blank province (E18). Fix it **together with** Phase 3 as one reviewed change, with a blank-destination regression scenario — not earlier (a premature fix is a no-op only while `AllowedProvinces` stays empty, and a silent behaviour change the moment someone sets it) | Corrects the verified defect at the right gate |

---

## 5. Decision register

| ID | Decision | Status |
| --- | --- | --- |
| **D3** | Dedicated neutral **Geography reference capability** owns country/area/city/service-area identity + labels; Contracts-only read ports; AddressBook owns addresses, Order owns the frozen snapshot, Shipping owns coverage, Host composes only | **RECOMMENDED** (evidence E1–E20; §2) |
| **D8** | Keep the legacy route + `StorefrontIranGeography.Provinces` + guard **unmodified**; add a new additive catalogue read; convert the legacy route to a compatibility shim later; guard amendment is a separate explicit Architect ruling | **RECOMMENDED + D8-OPEN** (the guard-amendment timing is OPEN) |
| **D9** | Geography owns its own `LanguageId`-keyed label translations; language set/default from Localization Contracts; fallback = requested → default → code; **no** generic translation service exists and none is invented | **RECOMMENDED + D9-OPEN** (whether to add a shared translation port) |
| **D10** | Whether `AdministrativeArea` carries an explicit level discriminator (province/state/region/emirate) or an arbitrary-depth self-referencing hierarchy | **OPEN** — no current requirement evidence; blocks nothing before Phase 0b |
| **D13** | Geography capability must later be registered in the structure manifest and certified under `ARCH-COMPLETE-002` (E1) | **SOURCE-VERIFIED requirement** (cost accepted if D3 is approved) |
| **D14** | Whether the geography capability's HTTP surface is needed at all in Phase 0 (catalogue admin) or whether Phase 0 is data+ports only | **OPEN** — recommend data+ports only until G1–G3 exist |
| **D15** | Seller **dispatch origin** structured identity (Party currently free text, E17) | **DEFERRED** — separate slice, not required for destination identity |

**Unresolved facts / risks:** live DB `IsActive` state of `shipping_services` (`UNVERIFIED`);
existence of other duplicated province lists in the frontend (`UNVERIFIED`); cost/lead time of a
new module's `ARCH-COMPLETE-002` certification (not estimated here — no authority); the
`AllowedProvinces`→coverage bridge may be impossible to express losslessly if a future zone model
needs polygons (risk, not a blocker for Phase 0).

---

## 6. Bounded recommendation — exactly ONE next task (NOT created, NOT executed)

**`TB-SHIPPING-GEOGRAPHY-MODULE-FOUNDATION-037`** (Architect authorization required)

- **Scope:** if and only if D3 is accepted — create the **Geography capability foundation**: the
  project set following the repo pattern (E1), the **Contracts-only read ports** of §3.1, the
  domain identities of §2.3 (Country / AdministrativeArea / City / ServiceArea + translation
  entities, **no commercial policy**), internal DI registration, and the structure-manifest entry
  plan. Design of Phase 0b coverage bootstrap is **not** in this task.
- **Explicit exclusions:** no endpoints or routes; no production data or seeds; **no change** to
  `StorefrontIranGeography`, the legacy route, or the pinned guard (E3); no AddressBook, Order,
  Fulfillment, Payment, Settlement, frontend or checkout change; no migration applied to any live
  database in this task; no coverage/fail-closed behaviour; no Shipping module.
- **Entry criteria:** Architect ruling on D3, D8-OPEN and D9-OPEN; naming approval; acceptance that
  a later `ARCH-COMPLETE-002` certification and manifest registration (D13) will follow.
- **Exit criteria:** additive module compiles; existing solution builds unchanged; **zero**
  behavioural change and **zero** route changes; guard E3 still passes unmodified; no data written.
- **Explicitly NOT authorized by this document.** No next task is started here.

---

## 7. Verification and change proof

- Every claim about the current system cites a path with line numbers verified against
  `a814b8cb924e0c591b36cd99e561ad9bf2b020eb`; anything not provable is marked
  `UNVERIFIED` / `OPEN` / `DEFERRED`.
- §2–§6 are proposals, clearly separated from §1 source facts.
- `AGENTS.md`, `docs/architecture/SHIPPING-ARCHITECTURE.md`, the audit 034 report and the 035 design
  document were read only and are unmodified.
- Exactly one new document was added by this task; see the Worker Result for the changed-path list,
  the single-path git proof and elapsed time.
