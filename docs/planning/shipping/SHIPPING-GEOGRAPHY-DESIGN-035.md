# Shipping — Geographic Identity Design (`TB-SHIPPING-GEOGRAPHY-DESIGN-035`)

> **STATUS: PROPOSED / NOT CERTIFIED.**
> This is a design proposal for Architect acceptance. It is **not** an implementation order,
> not an ADR ruling, and not a certification of `docs/architecture/SHIPPING-ARCHITECTURE.md`
> (which remains a draft). No product code, schema, migration, config or runtime state is
> authorized or changed by this document.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected / Base / Audited SHA | `bd382eb94106005c8692f7f46b03a0a2a3908616` |
| Parent task | `TB-SHIPPING-AUDIT-034` (Architect ACCEPT granted for the audit only) |
| Evidence source | `docs/planning/shipping/SHIPPING-CURRENT-STATE-AUDIT.md` @ `bd382eb94` |
| Mode | `DESIGN_ONLY_SOURCE_GROUNDED_ADR` |
| Stop-State | `STOP_FOR_ARCHITECT` |
| Timebox | 30 min HARD |

## 0. Decision rationale — why geography first

Audit 034 established blocker **B1**: Tooba has **no destination identity model**. Destination is
free text, the province catalogue is a 12-entry C# constant whose city identity *is* localized
Persian text, and the serviceability gate **fails open** on a blank destination. B2–B5 (delivery
groups, split shipping authority, physical/rate model, commercial ledger) all require a
*resolvable destination* before they can be specified, so geography is the correct first slice.
It is also the cheapest to decide, touches no live checkout behaviour, and can be staged without
rewriting committed orders.

**Non-goals of this document:** rating/tariffs, delivery groups and promises, carrier payable,
COD, packing fee, capacity, any Shipping module extraction, any UX redesign, any code move.

---

## 1. Existing evidence matrix (verified against `bd382eb94`)

| # | Concern | Exact path + lines | Verified current behaviour | Risk |
| --- | --- | --- | --- | --- |
| E1 | Address aggregate | `src/backend/Modules/AddressBook/Tooba.AddressBook.Domain/Aggregates/CustomerAddress.cs` L42–L79 | `Country` (ISO-like string, default `IR`, L21/L59–L60), `ProvinceName` **nullable string** (L63), `CityName` **required string** (L67), `PostalCode`, `PostalAddress`, optional `BuildingUnit`, `Label`. Max lengths L17–L33. | Identity is textual and mutable |
| E2 | Address validation | same file, L199–L209 | `Country` upper-invariant trimmed; `ProvinceName` optional; `CityName` only length-bounded. **No membership check against any geography catalogue.** | Any string is a valid city |
| E3 | Address contracts | `.../Tooba.AddressBook.Contracts/Dtos/CustomerAddressRecord.cs` L4–L18 | Flat DTO mirrors E1 (no IDs for country/province/city). | Propagation of free text to Order |
| E4 | Address → checkout port | `.../AddressBook.Contracts/Ports/IAddressBookCheckoutLookup.cs` L9–L13 | `GetAsync(actorUserId, addressId)` returns the record; ownership enforced. | No coverage/serviceability question asked here |
| E5 | Geography catalogue | `.../Tooba.Order.Application/Storefront/Geography/StorefrontIranGeography.cs` L6–L24 | `static IReadOnlyList<StorefrontProvinceOption>`; 12 provinces; cities as raw Persian strings; **no country level, no Sub-city/ServiceArea, no stable IDs for cities, no translation rows** | Single hard-coded source of destination truth |
| E6 | Pinned architecture guard | `src/backend/Host/Tooba.Host.Tests/Architecture/HostStorefrontAmcR3GuardTests.cs` L80–L84 | Asserts `MapGet("/geography/provinces"` and `StorefrontIranGeography.Provinces` both appear in `StorefrontOrderEndpoints.cs` | **The constant must keep existing**; any replacement must preserve this surface (or the guard must be changed by explicit Architect authority) |
| E7 | Geography HTTP surface | `.../Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs` L41–L42 | `GET /v1/storefront/geography/provinces` returns `StorefrontIranGeography.Provinces` directly, **unversioned, unlocalized, no country parameter**. | Order module owns a geography read |
| E8 | Geography DTO | `.../Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs` L110 | `StorefrontProvinceOption(string Code, string Label, IReadOnlyList<string> Cities)`. A `Code` exists but **cities have no code at all** | Half-identity: province has a code, city does not |
| E9 | Projection DTO | same file, L114–L150 (`StorefrontShippingDraftView` L114–L131, `StorefrontShippingProjection` L134–L150) | Carries `ProvinceName` / `CityName` / `PostalAddress` / `PostalCode` + `SavedAddressId` as **strings**; `Provinces` returned as an option list | No IDs on the wire |
| E10 | Frontend consumes **label**, not code | `src/frontend/app/storefront/storefront-shipping.tsx` L170–L172, L442–L446, L758–L759 | `provinceOptions = provinces.map(p => ({ value: p.label, label: p.label, cities: p.cities }))`. The province `<select>` value is the **Persian label**; cities are `<select>` options from raw `cities` strings (L460–L467, L770–L775) | `StorefrontProvinceOption.Code` is effectively **unused**; the wire value is localized text |
| E11 | Serviceability gate | `.../Tooba.Order.Application/Storefront/Services/StorefrontShippingCalculator.cs` L72–L83 | `AllowedProvinces` empty ⇒ allowed. Otherwise blank province ⇒ `false`; else **case-insensitive exact string match** on the province name | Measurement is by name, not identity |
| E12 | **Fail-open caller bug** | `.../Storefront/Services/StorefrontShippingService.cs` L429–L433 | `if (!IsDestinationAllowed(rate, provinceName) && !string.IsNullOrWhiteSpace(provinceName)) return;` — the allow-list is only enforced when the province is **non-blank** | **Blank destination bypasses coverage entirely** |
| E13 | Province allow-list config | `.../Fulfillment.Contracts/Shipping/ShippingMethodsOptions.cs` L10–L17 (L16 `AllowedProvinces`); defaults L36–L47 leave it empty everywhere | The shipped defaults have **no coverage restriction at all**, so E11/E12 are unexercised in practice today | Latent behaviour, not current behaviour |
| E14 | Order snapshot | `.../Tooba.Order.Domain/Aggregates/CheckoutGroup.cs` L96 `ProvinceName`, L101 `CityName`, L106 `PostalAddress`, L222–L224 assignment | Committed order keeps **free-text** destination strings; `ShippingMethodCode` L116, `MinimumDeliveryDate` L131, `RequestedDeliveryDate` L136, `RequestedDeliveryTimeWindow` L141 | Historical promises must stay interpretable |
| E15 | Cart draft | `.../Tooba.Order.Domain/Checkout/CartShippingDraft.cs` L40, L43, L52, L145–L149 | Pre-commit draft mirrors the free-text destination plus `SavedAddressId` | Draft is the cutover point |
| E16 | Fulfillment destination use | `.../Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs` L25 `SellerOrderId`, L28 `CheckoutId`; `FulfillmentDirectory.cs` L60–L85 | Fulfillment groups by **seller order** and does not read a destination identity | No delivery-group concept to reconcile yet |
| E17 | Localization primitives | `.../Tooba.Localization.Domain/Aggregates/Language.cs` L18–L26 | `LanguageId`, `Code`, `NativeName`, `Direction`, `IsActive`; a `Language` aggregate + `ILanguageDirectory` port exist | A translation mechanism exists to reuse; `UNVERIFIED` whether it is generic enough for arbitrary reference data |
| E18 | Translation pattern precedent in shipping | `.../Fulfillment.Infrastructure/Shipping/ShippingCatalogReader.cs` L78–L92; `StorefrontShippingService.cs` L392–L410 | Service/option names are stored as **per-`LanguageId` translation rows** and resolved by `langId` | Proven in-repo precedent for translated reference data |

**Gaps in this evidence:** runtime behaviour was not probed (no service restart, no DB query, per
task constraint); E17's reusability for non-`Language` reference data is `UNVERIFIED`; whether any
other frontend surface duplicates the 12-province list is `UNVERIFIED`.

---

## 2. Candidate domain and contract design (PROPOSED)

### 2.1 Text diagram

```
                          ┌──────────────────────────────────────────┐
                          │  GEOGRAPHY REFERENCE CATALOG (proposed)  │
                          │  globally stable IDs + localized names   │
                          └──────────────────────────────────────────┘
   Country ──1:N──► AdministrativeArea ──1:N──► City ──1:N──► ServiceArea
   (ISO code)       (optional levels:            (stable id)    (optional,
                     Province/State/…,                           carrier-facing
                     self-referencing parent)                    sub-city zone)

   identity          = stable ID / code ONLY
   display name      = translation rows keyed by LanguageId (E17/E18 precedent)
   Hierarchy depth   = data, not hard-coded levels

                 ┌────────────────────┐            ┌───────────────────────────┐
   BUYER  ──────►│  AddressBook       │            │  Coverage / Serviceability │
                 │  address endpoint  │──refs─────►│  (Shipping policies)       │
                 │  (owns the row)    │            │  origin × destination ×    │
                 └────────────────────┘            │  service ⇒ ALLOWED/DENY    │
                            │                      └───────────────────────────┘
                            │ resolved IDs + display snapshot
                            ▼
                 ┌──────────────────────────────────────────────────┐
                 │  ORDER  (immutable destination snapshot)          │
                 │  ids for resolution  +  frozen display strings    │
                 └──────────────────────────────────────────────────┘
```

### 2.2 Ownership proposal (module boundaries)

| Concern | Proposed owner | Cross-boundary rule |
| --- | --- | --- |
| The customer's address row (recipient, phone, line, postal code) | **AddressBook** (unchanged owner) | External modules read via `IAddressBookCheckoutLookup`-style Contracts only (E4) |
| The geography reference catalogue (Country / AdministrativeArea / City / ServiceArea + translations) | **a reference-catalogue capability**, *not* Order and *not* AddressBook | Read through a Contracts-only port; **which module hosts it is left OPEN** (§6 D3) |
| Coverage / serviceability policy (origin × destination × service) | **Shipping capability** (to be bounded by a later ADR) | Consumes catalogue IDs via Contracts; never reads AddressBook tables |
| The order's destination promise | **Order** (unchanged owner) | Snapshot only; never re-resolves |
| Host | composition only | No business logic in Host (locked rule) |

### 2.3 Sample identity and coverage rules (specification)

```
R1  Identity      A destination is (countryCode, cityId[, serviceAreaId]) —
                  never a display name. Two distinct cities may share one
                  localized label (e.g. two "Bandar Abbas" entries in different
                  administrative areas) and remain distinct by id.
R2  Resolution    provinceName/cityName from legacy input NEVER create identity;
                  they are only inputs to a resolution step that yields ids or
                  a RESOLVED / AMBIGUOUS / UNKNOWN outcome.
R3  Coverage      coverage(origin, destinationIds, serviceCode) ⇒ ALLOWED | DENIED.
                  Absence of a coverage row is NOT "allowed"; it is DENIED unless
                  the service declares an explicit country-wide grant.
R4  Fail-closed   UNKNOWN, blank, unresolvable or AMBIGUOUS destination ⇒ DENIED
                  for any service that declares any coverage restriction, and
                  ⇒ DENIED for every service when the destination cannot be
                  resolved to a country at all. (Corrects E12.)
R5  Legacy allow- Province-name allow-lists (E13) remain readable as a
    lists          COMPATIBILITY input: they resolve to a set of ids once, then
                  participate as ordinary coverage rows.
R6  Snapshot      The order freezes BOTH the resolved ids and the display strings
                  as rendered at commit time (E14), so a later catalogue rename
                  cannot rewrite history.
R7  Versioning    Catalogue entries are soft-deactivated, never deleted or
                  re-keyed; a superseded id keeps resolving for historical orders.
```

---

## 3. Old → new field mapping (PROPOSED)

### 3.1 AddressBook

| Current (E1/E2/E3) | Proposed | Compatibility rule |
| --- | --- | --- |
| `Country` string, default `IR` (L59–L60) | keep `Country` string (ISO-2/3) + `CountryId` **nullable** | Backfill attempts resolution; unresolvable rows keep `CountryId = null` and remain valid for editing |
| `ProvinceName` string? (L63) | keep `ProvinceName` + `AdministrativeAreaId` **nullable** | Legacy rows stay null; new/edited rows resolve |
| `CityName` string (L67, required) | keep `CityName` + `CityId` **nullable** | `CityName` stays required during transition (no schema-required break) |
| — | `ServiceAreaId` **nullable** (new) | Optional; absent means "city-level destination" |
| `PostalCode`, `PostalAddress`, `BuildingUnit`, `Label` | unchanged | No change |

**Invariant:** adding columns is expand-only; no existing column is dropped, renamed or made
required in this proposal.

### 3.2 Checkout / Order

| Current (E14/E15) | Proposed | Compatibility rule |
| --- | --- | --- |
| `CartShippingDraft.ProvinceName` / `.CityName` (L40/L43) | + `DestinationCountryId` / `AdministrativeAreaId` / `CityId` / `ServiceAreaId` (nullable) | Draft tolerates unresolved destination; commit-time gate must resolve |
| `CheckoutGroup.ProvinceName` L96 / `.CityName` L101 / `.PostalAddress` L106 | + frozen `DestinationCountryId` / `AreaId` / `CityId` / `ServiceAreaId` (nullable) | **Existing committed rows remain valid**; ids are null there and that is acceptable forever |
| `CheckoutGroup.ShippingMethodCode` L116 | unchanged | Method code still a code, not a name |
| `CheckoutGroup.MinimumDeliveryDate` L131 / `RequestedDeliveryDate` L136 / `RequestedDeliveryTimeWindow` L141 | unchanged | Historical promises untouched |

### 3.3 Shipping projection / UI

| Current (E9/E10) | Proposed | Compatibility rule |
| --- | --- | --- |
| `StorefrontProvinceOption(Code, Label, Cities)` L110 | superseded **additively** by a country-aware option set carrying `{id, code, label}` for country/area/city/service-area | `Code` is now actually used; the legacy shape may remain to satisfy guard E6 |
| `GET /v1/storefront/geography/provinces` E7 | keep the route present, optionally parameterized by `countryId` | Guard E6 asserts this route and the constant; **do not remove** |
| Frontend province `<select>` value = label (E10 L171/L445) | value = **id** (or code), label rendered from translation | Behavioural change; requires frontend consent, out of scope here |
| Frontend city `<select>` from raw strings (E10 L460–L467) | options carry ids | Same |
| `provinces` payload embedded in the projection (E9) | can later become a separate, cacheable catalogue read | Deferred |

---

## 4. Rollout, backwards-compatibility and fail-closed matrix

**Rollout principles (all additive, no downtime assumed, no committed order rewritten):**

1. **Phase 0 — Catalogue bootstrap (data only).** Introduce the reference catalogue with a
   *minimal* initial set: **no nation-wide seeded assumption**. Only destinations explicitly
   onboarded become resolvable. (Consistent with E13's current empty allow-lists.)
2. **Phase 1 — Additive identifiers.** Add nullable ids to AddressBook, cart draft and order
   snapshot. Nothing reads them for decisions yet. Existing rows and orders are untouched.
3. **Phase 2 — Resolution + reconciliation states.** Address write/read resolves free text to ids
   and records `RESOLVED | AMBIGUOUS | UNRESOLVED`. UI may prompt only on `AMBIGUOUS`.
4. **Phase 3 — Fail-closed gate.** Switch serviceability to id-based coverage with R4 semantics.
   The E12 fail-open caller guard is corrected here, under explicit approval.
5. **Phase 4 — Legacy allow-list bridge (R5).** Translate `AllowedProvinces` strings into
   coverage rows so existing configuration keeps its intent.
6. **Phase 5 — Guard-safe catalogue source swap.** Only with Architect authority; until then
   `StorefrontIranGeography.Provinces` remains the served source (E6/E7).

**Fail-closed matrix (target semantics):**

| Destination state | Country known? | Service has coverage rows? | Result |
| --- | --- | --- | --- |
| Fully resolved (country+city) | yes | yes, matching | `ALLOWED` |
| Fully resolved | yes | yes, not matching | `DENIED` |
| Fully resolved | yes | none, service declares country-wide grant | `ALLOWED` |
| Fully resolved | yes | none, no grant declared | `DENIED` |
| `AMBIGUOUS` | yes | any | `DENIED` (+ user-facing disambiguation) |
| `UNRESOLVED` / blank province or city | yes | any | `DENIED` |
| `UNRESOLVED` / blank country | no | any | `DENIED` |
| Foreign service area not onboarded | yes | any | `DENIED` |
| Historical order with null ids | n/a | n/a | **Never re-evaluated**; frozen promise stands (R6) |

**Hard constraint honoured:** the pinned guard (E6) is neither removed nor bypassed in this
proposal. Any change to it requires explicit Architect authority and is listed as a decision, not
an action.

---

## 5. Accepted vs pending decisions

| ID | Decision | State |
| --- | --- | --- |
| D1 | Identity must be stable ids/codes; localized labels are never identity | **PROPOSED — for Architect acceptance** |
| D2 | Destination = `Country → AdministrativeArea(optional hierarchy) → City → ServiceArea(optional)`, depth as data | **PROPOSED** |
| D3 | Which module hosts the geography reference catalogue (new capability vs Catalog vs Fulfillment) | **OPEN — deliberately unresolved**; requires the broader module-boundary ADR |
| D4 | Coverage/serviceability semantics incl. fail-closed R4 and country-wide grants | **PROPOSED** |
| D5 | Legacy `AllowedProvinces` translated into coverage rows as a compatibility bridge | **PROPOSED** |
| D6 | Order freezes ids **and** display strings; historical orders never re-evaluated | **PROPOSED** |
| D7 | Catalogue lifecycle = soft-deactivate + versioning, never re-key | **PROPOSED** |
| D8 | Whether `StorefrontProvinceOption`/`StorefrontIranGeography` is eventually replaced or kept as a compatibility shim, and when the guard E6 is amended | **PENDING — Architect only** |
| D9 | Translation storage: reuse `Language`-keyed translation rows (E18 precedent) vs a dedicated mechanism | **PENDING** — E17 reusability `UNVERIFIED` |
| D10 | Whether `AdministrativeArea` needs an explicit level discriminator (province/state/region) | **PENDING** — depends on future cross-border scope |
| D11 | Postal-code / polygon-based serviceability | **DEFERRED** — no current requirement evidence |
| D12 | Postal code authoritative format per country (E1 keeps it generic) | **DEFERRED — unchanged** |

---

## 6. Acceptance scenario specifications (specifications, not executable tests)

| # | Scenario | Expected behaviour |
| --- | --- | --- |
| S1 | **Exact match.** Buyer picks Iran → Bandar Abbas (city) → Bandar Abbas (area); a courier covers exactly that area | Resolves to one city id + one area id; coverage `ALLOWED`; order stores ids + the Persian label as rendered |
| S2 | **Duplicated localized names.** Two distinct cities both display «بندرعباس» in different administrative areas; buyer picks the second | Ids differ; only the second is evaluated for coverage; the stored snapshot is unambiguous to machines while identical to humans |
| S3 | **Blank province/city.** Legacy or crafted request with empty province (and/or empty city) | Coverage `DENIED` under R4; **regression guard against E12's fail-open** |
| S4 | **Incomplete legacy address.** Address created before ids existed (country `IR`, free-text city) | Still readable, editable and usable; resolution yields `RESOLVED` or `UNRESOLVED`; `UNRESOLVED` is `DENIED` for restricted services but never blocks address editing |
| S5 | **Unsupported foreign destination.** Country resolved but no onboarding | Coverage `DENIED`; no fabricated windows, dates or prices are offered |
| S6 | **Coverage restricted to one sub-city.** Service covers only one `ServiceArea` of a city | `ALLOWED` only for that area id; sibling areas in the same city are `DENIED` |
| S7 | **Historical order preservation.** An order committed before this design (null ids, free-text strings) | Rendered and honoured exactly as committed; never re-resolved; no migration rewrites it |
| S8 | **Catalogue rename.** A city's Persian label changes after an order | New orders show the new label; the historical order keeps the frozen label (R6) |
| S9 | **Ambiguous resolution.** Free text matches two catalogue cities equally | `AMBIGUOUS` ⇒ `DENIED` plus a disambiguation prompt; never silently pick the first match |
| S10 | **Guard continuity.** While the new catalogue is being introduced | `GET /v1/storefront/geography/provinces` still responds and `StorefrontIranGeography.Provinces` still exists (E6/E7), so no existing consumer breaks |

---

## 7. Bounded recommendation for a future implementation task (NOT executed)

**Recommended next slice — `TB-SHIPPING-GEOGRAPHY-CATALOG-036` (Architect authorization required):**

- **Scope:** Phase 0 + Phase 1 only, i.e. the reference catalogue *hosting decision* (resolve D3)
  plus the additive nullable identifier columns on AddressBook / cart draft / order snapshot, with
  a resolution service returning `RESOLVED | AMBIGUOUS | UNRESOLVED` and **no** serviceability
  behaviour change.
- **Deliberately excluded:** Phase 3 fail-closed gate switch, `AllowedProvinces` bridging, any
  change to `StorefrontIranGeography` or guard E6, rating, delivery groups, carrier payable,
  frontend wiring, module extraction.
- **Entry criteria:** Architect rules D1–D9; a decision on whether the catalogue is a new
  capability or belongs to an existing module; confirmation of the translation mechanism (D9).
- **Exit criteria:** additive schema only; historical orders untouched; existing projection
  response byte-compatible for current consumers; guard E6 still passing unmodified.

This recommendation is **not** authorized and **not** started by this task.

---

## 8. Verification of this document

- Every claim about the current system cites a path and a line range verified against
  `bd382eb94106005c8692f7f46b03a0a2a3908616`; anything not provable is marked `UNVERIFIED`/`PENDING`.
- Proposal (§2–§5) is clearly separated from source facts (§1, §3 "current" column).
- The pinned guard is preserved; nothing is removed or bypassed.
- Only one file was added by this task (see the Worker Result for the changed-path list).
