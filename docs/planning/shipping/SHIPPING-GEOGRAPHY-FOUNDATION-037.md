# Shipping — Geography Foundation Delivery Report (`TB-SHIPPING-GEOGRAPHY-MODULE-FOUNDATION-037`)

> **STATUS: INERT FOUNDATION DELIVERED / NOT WIRED / NOT CERTIFIED.**
> This task created an inert, non-wired code foundation only. It contains **no** persistence,
> **no** migration, **no** DbContext, **no** DI registration, **no** endpoint and **no** behaviour
> change. No live shipping, checkout, order, address or fulfillment behaviour is affected.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected / Base / Audited SHA | `366f18beae463db1c7f2b54f2f1a61c8bdc61db8` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-BOUNDARY-ADR-036` (D3/D8/D9 APPROVED) |
| Mode | `IMPLEMENTATION_BOUNDED_MODULE_FOUNDATION_ONLY` |
| Stop-State | `STOP_FOR_ARCHITECT` |

---

## 1. Created paths (exactly)

New module root: `src/backend/Modules/Geography/`

| # | Path | Purpose |
| --- | --- | --- |
| 1 | `Tooba.Geography.Contracts/Tooba.Geography.Contracts.csproj` | Contracts project (net8.0 → BuildingBlocks only) |
| 2 | `Tooba.Geography.Contracts/Errors/GeographyErrorCodes.cs` | Stable, language-independent error codes |
| 3 | `Tooba.Geography.Contracts/Places/GeographyPlaceSnapshots.cs` | `CountrySnapshot`, `AdministrativeAreaSnapshot`, `CitySnapshot`, `ServiceAreaSnapshot` |
| 4 | `Tooba.Geography.Contracts/Places/DestinationResolutionContracts.cs` | `DestinationIdentifiers`, `DestinationResolutionOutcome`, `DestinationResolution` |
| 5 | `Tooba.Geography.Contracts/Places/IGeographyLookup.cs` | Read-only lookup + legacy resolution seam |
| 6 | `Tooba.Geography.Contracts/Places/IGeographyLabelLookup.cs` | Read-only localized-label seam (`LanguageId`-keyed) |
| 7 | `Tooba.Geography.Domain/Tooba.Geography.Domain.csproj` | Domain project (net8.0 → BuildingBlocks + Contracts) |
| 8 | `Tooba.Geography.Domain/Enums/GeographyPlaceKind.cs` | `Country`/`AdministrativeArea`/`City`/`ServiceArea` discriminator for translation ownership only |
| 9 | `Tooba.Geography.Domain/Aggregates/Country.cs` | Root place identity (ISO country code) |
| 10 | `Tooba.Geography.Domain/Aggregates/AdministrativeArea.cs` | Data-driven self-referencing hierarchy, no level discriminator (D10 left OPEN) |
| 11 | `Tooba.Geography.Domain/Aggregates/City.cs` | Stable city identity, optional area owner |
| 12 | `Tooba.Geography.Domain/Aggregates/ServiceArea.cs` | Sub-city service area under a city |
| 13 | `Tooba.Geography.Domain/Aggregates/PlaceNameTranslation.cs` | Per-`LanguageId` localized name rows (D9 ownership) |

Modified path (additive registration of the two new projects only):

| # | Path | Change |
| --- | --- | --- |
| 14 | `src/backend/Tooba.slnx` | Added one new `<Folder Name="/Modules/Geography/">` group with exactly the two new `.csproj` entries, appended after the `Support` group |

No other file was created, modified or deleted.

---

## 2. Contracts seam delivered (per ADR 036 §3.1)

Read-only, snapshots only, async `CancellationToken` on every member — matching the existing
`ILanguageLookup` / `IAddressBookCheckoutLookup` conventions.

```text
IGeographyLookup
  ListCountriesAsync(ct)
  ListAdministrativeAreasAsync(countryId, ct)
  ListCitiesAsync(countryId, administrativeAreaId?, ct)
  ListServiceAreasAsync(cityId, ct)
  GetByIdsAsync(DestinationIdentifiers, ct)                     -> DestinationIdentifiers?
  ResolveLegacyAsync(countryCode, areaName, cityName, areaName, ct) -> DestinationResolution

IGeographyLabelLookup
  GetCountryLabelsAsync(countryIds, languageId?, ct)
  GetAdministrativeAreaLabelsAsync(areaIds, languageId?, ct)
  GetCityLabelsAsync(cityIds, languageId?, ct)
  GetServiceAreaLabelsAsync(serviceAreaIds, languageId?, ct)
```

`DestinationResolutionOutcome` is exactly `Resolved | Ambiguous | Unresolved`, with
`Ambiguous`/`Unresolved` carrying **no** identifiers (safe/closed shape).

**Confirmed exclusions inside the seam:** no `DbContext`, no domain entity type, no write/mutate
port, no serviceability/coverage verdict, no commercial or rating concept, no language-registry
ownership.

## 3. Domain identity model delivered

- **Identity is always a stable `Guid` + language-independent `Code`.** Localized names live only
  in `PlaceNameTranslation`, keyed by `(PlaceKind, PlaceId, LanguageId)` — the same
  `LanguageId`-keyed row pattern already used by Fulfillment and Catalog reference data.
- **Hierarchy:** `Country → AdministrativeArea(self-referencing, optional parent) → City → ServiceArea`.
  `City.AdministrativeAreaId` is nullable; `AdministrativeArea.ParentAdministrativeAreaId` is nullable.
- **No equal-name assumption:** nothing in the model derives identity from a name; the gap between
  ID and label is explicit in both projects.
- **Lifecycle:** `IsActive` with `Activate`/`Deactivate`; nothing is deleted or re-keyed, so a
  superseded id stays resolvable for historical orders.
- **Validation:** invalid input raises `SemanticException(new SemanticError(<stable code>))` using
  the codes in `GeographyErrorCodes`; no localized text is embedded in the domain.
- **No static world data, no seed list, no default country set.**

---

## 4. D8 / D9 conformance

| Decision | Requirement | Delivered state |
| --- | --- | --- |
| **D8** | `GET /v1/storefront/geography/provinces`, `StorefrontIranGeography.Provinces`, `StorefrontProvinceOption` and `HostStorefrontAmcR3GuardTests` must remain **unchanged** | Verified byte-for-byte untouched: `git status` clean for `StorefrontOrderEndpoints.cs`, `StorefrontIranGeography.cs`, `HostStorefrontAmcR3GuardTests.cs`, `storefront-shipping.tsx` and all of `src/backend/Host/Tooba.Host`. No route was added or changed; this module has no endpoints. |
| **D9** | Geography owns its own per-`LanguageId` translation rows; Localization owns the language registry; **no generic translation service** | `PlaceNameTranslation` is owned by this module; the Contracts seam takes `Guid? languageId`/`Guid languageId` as plain values and takes **no dependency** on `Tooba.Localization.*`. No translation engine or abstraction was invented. |
| **D10** | Administrative level discriminator remains OPEN | No level/discriminator member was modelled on `AdministrativeArea`; the class comment states D10 is undecided. **Still OPEN.** |

---

## 5. Structure, conventions and certification status

- Followed the existing repo shape: one folder per concern (`Aggregates/`, `Enums/`, `Places/`,
  `Errors/`), **zero `.cs` files at project root** (so the module is already compatible with an
  empty `rootAllowlist` should it ever be manifested), and namespace == project + relative folder
  (`Tooba.Geography.Domain.Aggregates`, `Tooba.Geography.Contracts.Places`, …).
- Project/namespace/assembly naming matches the locked convention (`Tooba.<Module>.<Layer>`).
- `Directory.Build.props` applies unchanged: `GenerateDocumentationFile` + `CS1591` as error. Every
  public type and member in both projects carries an XML doc comment, and both projects build with
  **0 errors**.
- **Certification status: NOT REGISTERED AND NOT CERTIFIED.** No entry was added to
  `docs/architecture/tmar-module-structure-manifests.json`; no existing manifest entry was touched;
  `structureCertified` was not claimed for Geography, and `ARCH-COMPLETE-002` is **not** claimed.
  Inspected impact: no architecture guard enumerates the top-level `src/backend/Modules` directory
  into a set, and the module gates read the manifest (declared modules → disk), so an unmanifested
  inert module does not disturb them. The two pinned geography guards remain in place.
- **Known follow-ups for a later, separately authorized task** (documented, not executed):
  (a) manifest registration + `ARCH-COMPLETE-002` certification once Application/Infrastructure/
  Endpoints exist; (b) Host composition and DI at that time; (c) `Tooba.Geography.Tests`.

---

## 6. Build evidence

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Modules/Geography/Tooba.Geography.Contracts/Tooba.Geography.Contracts.csproj` | **Build succeeded — 0 errors** (2 × `NU1900` NuGet vulnerability-feed timeout warnings, network-only, unrelated to code) |
| `dotnet build src/backend/Modules/Geography/Tooba.Geography.Domain/Tooba.Geography.Domain.csproj` | **Build succeeded — 0 errors** (same `NU1900` network warnings only) |

Per the task's bounded-verification rule, only the two created projects were built. The full
solution was not built, no test suite was run, no database was accessed and no service was restarted.

## 7. Explicit exclusions honoured

No Order, AddressBook, Fulfillment, Catalog, Offer, Payment, Settlement, StoreContext, Localization,
frontend or Host code change. No migration, DbContext, persistence, DB query/write, seed, default
country set, deployment, service/config change, checkout shipping/rate/calendar/coverage logic,
endpoint or API route change. No Geography→Order/AddressBook/Shipping commercial dependency and no
cross-module DB access. Host composition-only and Contracts-only rules respected. The legacy
12-province constant/route/frontend label-valued selection and the pinned Host architecture test are
byte-for-byte untouched. No architecture draft/ADR 036/prior report was edited. No Bridge probe
payload. No automatic next task.

## 8. ONE bounded next-step recommendation — NOT executed

**`TB-SHIPPING-GEOGRAPHY-PERSISTENCE-038`** (Architect authorization required): add the Geography
**Infrastructure + Application** layers — own `DbContext` with the `(PlaceKind, PlaceId, LanguageId)`
unique index, the concrete `IGeographyLookup`/`IGeographyLabelLookup` adapters, an expand-first
migration, and **Phase 0a** catalogue administration with **minimal explicit region data only**. It
must still perform **no** serviceability, coverage, rating or fail-closed switching, and must not
touch Order/AddressBook/Fulfillment. Entry criteria: Architect acceptance of this foundation plus an
explicit ruling on the migration/rollout authorization. This recommendation is **not** authorized and
**not** started.
