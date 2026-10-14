# Shipping — Geography Translation Integrity Report (`TB-SHIPPING-GEOGRAPHY-TRANSLATION-INTEGRITY-039`)

> **STATUS: INERT MODEL HARDENED / NO MIGRATION / NOT WIRED / NOT CERTIFIED / MIGRATION NOT CLAIMED READY.**
> The polymorphic translated-name storage delivered in 038 is replaced by four per-kind translation
> entities, each with a **real database-enforced parent foreign key**. No migration was generated or
> applied, no database was contacted, no Host wiring and no behaviour change occurred.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected / Base / Audited SHA | `a0eb0d1e969875a77271256d25b11285701858c6` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-PERSISTENCE-MODEL-038` |
| Mode | `IMPLEMENTATION_BOUNDED_INERT_TRANSLATION_INTEGRITY_ONLY` |
| Stop-State | `STOP_FOR_ARCHITECT` |

**Migration readiness is NOT claimed.** The Architect expressly deferred two further pre-migration
blockers to their own reviewed slices: cross-country ancestor consistency and administrative-hierarchy
cycles. Both remain open (see §5).

---

## 1. Before → after mapping

**Before (038, polymorphic — replaced):**

| Item | Shape |
| --- | --- |
| Entity | one `PlaceNameTranslation` carrying `PlaceKind` + `PlaceId` + `LanguageId` + `Name` |
| Discriminator enum | `GeographyPlaceKind` (Country / AdministrativeArea / City / ServiceArea) |
| Table | `place_name_translations` |
| Constraint | `UNIQUE(place_kind, place_id, language_id)` only — **no foreign key at all** |
| Integrity gap | `place_id` could reference nothing, or the wrong kind of place; indistinguishable at the database level |

**After (039, relational per-kind — current):**

| Entity | Table | Parent FK (required) | Delete rule | Unique index |
| --- | --- | --- | --- | --- |
| `CountryNameTranslation` | `country_name_translations` | `country_id → countries(country_id)` | `Cascade` | `UNIQUE(country_id, language_id)` |
| `AdministrativeAreaNameTranslation` | `administrative_area_name_translations` | `administrative_area_id → administrative_areas(administrative_area_id)` | `Cascade` | `UNIQUE(administrative_area_id, language_id)` |
| `CityNameTranslation` | `city_name_translations` | `city_id → cities(city_id)` | `Cascade` | `UNIQUE(city_id, language_id)` |
| `ServiceAreaNameTranslation` | `service_area_name_translations` | `service_area_id → service_areas(service_area_id)` | `Cascade` | `UNIQUE(service_area_id, language_id)` |

`GeographyPlaceKind` and `PlaceNameTranslation` were **deleted**; `place_kind`/`place_id` no longer
exist anywhere in the model (verified programmatically, §4). `Tooba.Geography.Contracts` was **not**
modified (`git status` clean for that project): the existing codes
`geography.translation.place_id.required`, `geography.translation.language_id.required` and
`geography.translation.name.required` are reused unchanged by all four entities, so no contract
change was unavoidably necessary.

## 2. Strict equivalence of the replacement (explained and checked)

The replacement preserves every behaviour the polymorphic shape provided, and removes only the gap:

| Property | Polymorphic (038) | Per-kind (039) | Equivalent? |
| --- | --- | --- | --- |
| Stable, language-independent place identity | `PlaceId` Guid | per-kind parent Guid column | Yes — same Guid, same domain factory methods |
| One name per (place, language) | `UNIQUE(place_kind, place_id, language_id)` | `UNIQUE(parent_id, language_id)` per table | Yes — same logical rule, one fewer column and split across four tables that cannot collide because the kind is now the table |
| Kind of the owner | a stored discriminator column (`place_kind`) | the table itself | Yes — strictly stronger: the kind can no longer disagree with the row |
| Name validation/normalization | `Name` ≤ 128, required, trimmed | identical rule and limit on all four entities | Yes — same `GeographyErrorCodes`-backed rules |
| Place id / language id required | Guid.Empty rejected in factory | identical rejection in all four factories | Yes |
| Timestamps | `CreatedAt` / `UpdatedAt` | identical on all four entities | Yes |
| Rename path | `Rename(name, now)` | identical on all four entities | Yes |
| Orphan possibility | possible (no FK) | impossible (real required FK) | **Intentionally stricter** |

Nothing from the Domain snapshot/Contracts surface refers to the removed types (checked repo-wide in
`src`: only the Geography module itself contained them), so no consumer contract changed.

## 3. Delete-rule justification (the task's "explicitly justified repo-consistent rule")

Translation rows use `Cascade` rather than `Restrict`, deliberately:

1. A name row is a **strictly owned child** with no independent identity — unlike a place, it has no
   `IsActive` lifecycle and no historical value once its place is gone, so it must never outlive its
   parent (which `Restrict` would allow to become an orphan-in-place).
2. This matches the repository's established translation convention: `CatalogCategoryTranslation`
   (`CatalogDbContext.cs` L271-L274) and `CatalogCategorySlugHistory` (L285-L288) both map their
   owning parent with `OnDelete(DeleteBehavior.Cascade)`.
3. The cascade is **inert today**: Geography places are never physically deleted (the model uses the
   `IsActive` lifecycle, and every place-to-place reference — `country_id`, `parent_administrative_area_id`,
   `administrative_area_id` — remains `Restrict`), and no delete path exists in code.
4. The four translation tables are therefore independent of the frozen place-delete policy: changing
   that policy later cannot silently orphan a name row.

## 4. Verification actually performed

| Verification | Result |
| --- | --- |
| `dotnet build src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj` | **Build succeeded — 0 Error(s)**; 2 × `NU1900` (NuGet feed network timeout, unrelated to code). Rebuilt Contracts, Domain, Persistence and Infrastructure. |
| Targeted EF **model metadata** validation (throwaway harness outside the repo, `%TEMP%/geography-model-check-039`, deleted afterwards; `ToobaNpgsql.ConfigureModuleContext` with a placeholder connection string, then `DbContext.Model`) | `MODEL_OK`. Printed: `administrative_area_name_translations` / `city_name_translations` / `country_name_translations` / `service_area_name_translations`, each with its FK `delete=Cascade required=True` and `IX UNIQUE(<parent>_id, language_id)`; `administrative_areas` FK `parent_administrative_area_id` `delete=Restrict required=False` and `country_id` `delete=Restrict required=True`; `cities` FKs `Restrict`; `service_areas` FK `city_id` `Cascade`; `outbox_messages` unchanged. **No connection was opened, no database was contacted and no migration was generated.** |
| Programmatic orphan-path assertions in the same harness | `POLYMORPHIC_OWNER_COLUMNS 0` (no `place_kind` / `place_id` column survives anywhere in the model) and `TRANSLATION_TABLES_WITHOUT_FK <none>` (every `*translation*` table has a real FK). |
| Repo-wide residue check (`src`, excluding `bin`/`obj`) | Zero remaining references to `GeographyPlaceKind`, `PlaceNameTranslation` or `place_kind`. |
| Contracts integrity | `git status` clean for `Tooba.Geography.Contracts` → `CONTRACTS_UNCHANGED`. |

Per the bounded-verification rule: no full solution build, no broad test suite, no DB access, no
service restart, no migration generation.

## 5. Consciously deferred risks (NOT this task, per Architect decision)

1. **Cross-country ancestor consistency** — the model cannot yet express that a `cities.administrative_area_id`
   must belong to the same `country_id` as the city. Deferred by the Architect to its own reviewed slice.
2. **Administrative hierarchy cycles** — `administrative_areas` is self-referencing with `Restrict` and
   rejects a direct self-parent in the domain, but multi-node cycles (`A → B → A`) are not prevented by
   the model. Deferred by the Architect to its own reviewed slice.
3. **D10 remains OPEN** — no administrative-level discriminator was added; the per-kind tables say
   nothing about levels.
4. **No model snapshot / no migration exists**, so neither the deferred risks nor this change has ever
   touched a database; the first migration (whenever authorized) will be a full initial create.
5. `LanguageId` is an external scalar identity: the database cannot verify that a language exists
   (that lives in the Localization module, which Geography must not reference). Intentional, per D9.

**Therefore this task does not claim migration readiness**, and the two deferred blockers above must be
resolved in their own authorized slices before any migration is attempted.

## 6. Explicit exclusions honoured

No migration, model snapshot, database update/query, seed, provider/API integration, Host DI,
endpoint, service startup or running-system change. No change to Order, AddressBook, Fulfillment,
Payment, Settlement, Catalog, Offer, Shipping/Checkout, frontend, or the legacy geography
route/constant/guard. No new module or manifest registration/certification; D3/D8/D9 preserved; D10
OPEN; no change to parent/country hierarchy semantics; no unrelated edit; no dummy/probe result POST;
no automatic next task; no idle polling.

## 7. ONE recommended next bounded step — NOT executed

**`TB-SHIPPING-GEOGRAPHY-HIERARCHY-CONSISTENCY-040`** (Architect authorization required, and the
Architect already signalled this slice): enforce the two deferred pre-migration blockers inside the
inert model — cross-country ancestor consistency for `cities` (and any future area→country ancestor
rule) and administrative-hierarchy cycle prevention — using only constraint/check shapes that
PostgreSQL and the existing EF provider actually support, with the same inert-only discipline (no
migration, no DB, no Host wiring). Entry criteria: Architect acceptance of this report plus an explicit
ruling on which enforcement vehicle is acceptable (database check constraint vs. domain-only). Only
after that should a migration task be considered. Not authorized, not started.
