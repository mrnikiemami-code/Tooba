# Shipping — Geography Persistence Model Report (`TB-SHIPPING-GEOGRAPHY-PERSISTENCE-MODEL-038`)

> **STATUS: INERT PERSISTENCE MODEL ONLY / NOT WIRED / NOT MIGRATED / NOT CERTIFIED.**
> This task added an owned `DbContext` + Fluent mappings for the accepted 037 Geography domain.
> It contains **no** EF migration, **no** `dotnet ef` execution, **no** schema alteration, **no**
> seed data, **no** connection string, **no** database access, **no** Host DI registration and
> **no** endpoint. No runtime, checkout, shipping, order, address or fulfillment behaviour changed.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected / Base / Audited SHA | `2d3070c05ef5e2a236da26d3ee6f5cb33e7126e0` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-MODULE-FOUNDATION-037` |
| Mode | `IMPLEMENTATION_BOUNDED_INERT_PERSISTENCE_MODEL_ONLY` |
| Stop-State | `STOP_FOR_ARCHITECT` |

---

## 1. Created paths (exactly two new files, one additive solution line)

| # | Path | Purpose |
| --- | --- | --- |
| 1 | `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj` | Infrastructure project (net8.0; EF Core Design 8.0.11 + Domain/Contracts/BuildingBlocks/Persistence) |
| 2 | `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Persistence/GeographyDbContext.cs` | `GeographyDbContext` (`Schema = "geography"`) + `GeographyDbContextFactory` (design-time) + all five entity mappings |

Modified path (strictly additive):

| # | Path | Change |
| --- | --- | --- |
| 3 | `src/backend/Tooba.slnx` | One added line inside the existing `<Folder Name="/Modules/Geography/">` group: `Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj`. No existing line rewritten. |

Deliberately **not** created: any `Persistence/Migrations/**`, any `Migrations/**`, any
`DependencyInjection/**` or module composition class, any Application project, any `.cs` at the
project root.

## 2. Conventions studied (real repo evidence, not assumption)

| Convention | Source used |
| --- | --- |
| Infrastructure csproj shape, EF Design 8.0.11 `PrivateAssets=all`, snake_case naming via shared helper | `Modules/Media/Tooba.Media.Infrastructure/Tooba.Media.Infrastructure.csproj`, `Modules/Localization/Tooba.Localization.Infrastructure/...` |
| `DbContext` + `const string Schema` + single `OnModelCreating` with inline Fluent mappings + design-time factory | `MediaDbContext.cs` L10 (class) / L49 (factory), `LocalizationDbContext.cs` L9 (class) / L42 (factory) |
| Provider/EF versions and connection resolution (`ToobaNpgsql.ConfigureModuleContext`, `MigrationsHistoryTable`, `MigrationsAssembly`, `UseSnakeCaseNamingConvention`) | `BuildingBlocks/Tooba.Persistence/ToobaNpgsql.cs` L33-L47, `Tooba.Persistence.csproj` (Npgsql 8.0.7 / Npgsql.EFCore.PostgreSQL 8.0.11) |
| Translation uniqueness index: `HasIndex(new { ParentId, LanguageId }).IsUnique()` | `FulfillmentDbContext.cs` L183 and L200, `CatalogDbContext.cs` L599 |
| Parent/self FK behavior: parent = `DeleteBehavior.Restrict`, child/dependent = `DeleteBehavior.Cascade` | `CatalogDbContext.cs` L250-L253 (self parent, Restrict), L271-L274 / L285-L288 (dependent, Cascade) |
| Outbox table mapped into the owning module schema in **every** module DbContext | `OutboxMessageMapping.Map` (`OutboxMessageMapping.cs` L20), confirmed present in all 31 existing module DbContexts |

## 3. Schema decisions

Default schema: **`geography`**. Tables and constraints actually produced by the model:

| Table | PK | Unique | Other indexes | FKs (delete behavior) |
| --- | --- | --- | --- | --- |
| `countries` | `country_id` | `country_code` | `is_active` | — |
| `administrative_areas` | `administrative_area_id` | `(country_id, code)` | `country_id, is_active`; EF FK index `parent_administrative_area_id` | `country_id → countries` (Restrict); `parent_administrative_area_id → administrative_areas` (Restrict) |
| `cities` | `city_id` | `(country_id, code)` | `country_id, administrative_area_id, is_active`; EF FK index `administrative_area_id` | `country_id → countries` (Restrict); `administrative_area_id → administrative_areas` (Restrict, optional/nullable) |
| `service_areas` | `service_area_id` | `(city_id, code)` | `city_id, is_active` | `city_id → cities` (Cascade) |
| `place_name_translations` | `translation_id` | `(place_kind, place_id, language_id)` | — | **none** (see §4) |
| `outbox_messages` | `id` | — | `occurred_at` | — |

Decisions and rationale:

1. **Identity stays `Guid` only.** All five PKs are `ValueGeneratedNever()` — ids come from
   `UuidV7` in the domain factory methods, exactly as `MediaAsset`/`Language` do. No name, label or
   localized string is ever part of a key or an identity rule.
2. **Codes carry scoped uniqueness, names carry none.** `country_code` is unique; area/city/area
   codes are unique inside their owning scope (`country` / `country` / `city`). This is the honest
   reading of "appropriate scoped place codes": codes are administrator-assigned, stable,
   language-independent identifiers, so equal *display names* can never collide while codes cannot
   be duplicated inside a scope. Any future need to allow the same code under different parents
   would be a deliberate, separately authorized index change — not a silent relaxation.
3. **Hierarchy is data-driven and reference-locked.** `administrative_areas` self-references with
   `Restrict` (never cascade-delete a subtree of reference data) and `cities.administrative_area_id`
   is nullable, so `Country → Area → City → ServiceArea` supports shallow and deeper trees without
   any level discriminator. **D10 remains OPEN** — nothing in the schema encodes an administrative
   level.
4. **Lifecycle is `is_active`, not deletion.** No row is ever physically removed by the model; the
   `Restrict` choices on country/area references mean a superseded row cannot be silently
   cascaded away, so historical orders can still resolve an identifier.
5. **`outbox_messages` is convention-only.** Every existing module DbContext maps the shared outbox
   contract into its own schema; this project follows that skeleton so the module is not
   non-canonical. There is no published event, no dispatcher registration and no consumer in this
   task. No index, default or payload behaviour differs from the shared mapping.
6. **No database defaults and no seed data.** Values such as `is_active` are always written by the
   domain (`Create`/`Activate`/`Deactivate`); the model adds no `HasDefaultValue` and no seed rows,
   so nothing in the database can contradict domain state.

## 4. Translation integrity limitation (explicit) and the narrow follow-up

`place_name_translations` uses a **polymorphic** owner: `(place_kind, place_id)` where `place_id`
may point at a country, an area, a city or a service area. Because EF Core 8 / relational providers
cannot express a foreign key over a discriminator-column pair, **this table has no FK and no cascade
behaviour**: the database enforces uniqueness of `(place_kind, place_id, language_id)` but cannot
enforce that `place_id` exists, nor that it points at the right kind. Practical consequences:

- an orphan or mis-kinded translation row is possible at the database level;
- referential integrity for translations must be maintained by the (not-yet-existing) Application
  layer, and re-checked by any future import.

**Narrow follow-up (proposed, NOT executed):** in a later authorized task, split the polymorphic
table into four sibling tables (`country_name_translations`, `administrative_area_name_translations`,
`city_name_translations`, `service_area_name_translations`), each with a real FK to its parent and a
unique index on `(parent_id, language_id)`. That preserves the exact identity semantics, removes the
polymorphic gap, and still needs no generic translation framework. It is deliberately **not** done
here because it would change the accepted 037 `PlaceNameTranslation` shape, which this task is not
authorized to alter.

## 5. Verification actually performed

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj` | **Build succeeded — 0 Error(s)**; 2 × `NU1900` (NuGet vulnerability-feed network timeout, unrelated to code). Produced `Tooba.Geography.Infrastructure.dll`. |
| Targeted EF **model** validation in a throwaway harness outside the repository (`%TEMP%\geography-model-check`, deleted afterwards; referenced the project, called `ToobaNpgsql.ConfigureModuleContext` with a placeholder connection string and read `DbContext.Model`) | `MODEL_OK`. Printed exactly the schema, tables, PKs, FKs with delete behavior and unique indexes listed in §3 — i.e. the full relational model builds and validates. **No connection was opened; no database was contacted; no migration was generated; nothing was written inside the repository.** |

Structural self-checks performed against the working tree:

- `Modules/Geography` contains **0** files matching `*Migration*`;
- **no** `*.csproj` outside `Modules/Geography` contains the string `Tooba.Geography` — the Host and
  every other module are untouched and nothing composes this context;
- the 037 `Tooba.Geography.Contracts` and `Tooba.Geography.Domain` trees show **no** modification
  (`git status` clean for both paths);
- no `.cs` file sits at the new project root (only `Persistence/GeographyDbContext.cs`), preserving
  the folder/namespace rule (`Tooba.Geography.Infrastructure.Persistence` ↔ `Persistence/`).

Per the bounded-verification rule, the full solution was **not** built, no test suite was run, no
database was accessed, no service was restarted and no migration was generated or applied.

## 6. Guard impact (checked, not bypassed)

- `ArchitectureBoundaryTests`: `Module_infrastructure_does_not_reference_foreign_module_infrastructure_or_persistence`
  and `Non_host_projects_do_not_reference_foreign_module_infrastructure` use a path-based module
  resolver, so the `Tooba.Persistence` building-block reference (module-less) is out of scope, and
  the Domain/Contracts references are same-module. `Infrastructure_to_foreign_Application` and
  `Infrastructure_to_foreign_Domain` edge baselines are name-scoped per module → no new edge.
  `Domain_does_not_reference_infrastructure_or_host` → the Domain project is unchanged.
  `No_global_business_dbcontext_exists` → the new type is `GeographyDbContext`.
- Structure gates (`TmarCompleteReferenceStructureGateTests`) read the manifest and validate
  declared modules against disk; nothing is manifested for Geography, so no certified entry was
  touched and none was added.
- `ModuleSchemaMigrationOrder`: **no** constant was added (no migration is authorized), matching the
  seam's design; the guards that pin existing numbers (Promotion 12 / Reviews 14 / Support 29 / no
  13) are unaffected.
- **Certification status: NOT REGISTERED, NOT CERTIFIED.** Geography remains absent from
  `docs/architecture/tmar-module-structure-manifests.json`; `ARCH-COMPLETE-002` is not claimed and
  the module is not structure-certified.

## 7. Explicit exclusions honoured

No EF migration file, no `dotnet ef database update`, no schema alteration, no seed/import, no live
database access, no connection string committed, no endpoints, no Application handlers, no lookup
adapters, no admin UI, no Geography or Host DI wiring, no service startup, no deployed-behaviour
change. No change to Order, AddressBook, Fulfillment, Payment, Settlement, Shipping/Checkout,
frontend, seller origin or any legacy geography surface. No cross-module DbContext dependency. No
`ARCH-COMPLETE-002` self-certification, no manifest update, no unrelated edit, no automatic next
task, no dummy/probe payload to Bridge.

## 8. ONE recommended next bounded task — NOT executed

**`TB-SHIPPING-GEOGRAPHY-MIGRATION-039`** (Architect authorization required): generate the
**expand-first** migration for the `geography` schema from this frozen model, with the
`ModuleSchemaMigrationOrder` entry and migration assembly wiring decided explicitly, and **no** data
import. Entry criteria: Architect acceptance of this model, an explicit ruling on the geographic
data-entry source for Phase 0a, a decision on the §4 four-table split (do it now or accept the
polymorphic gap for one more step), and an explicit ruling on whether a fail-closed switch may ever
be enabled (per ADR 036 gates G1-G7). Must still perform no serviceability, coverage, rating or
fail-closed switching and must not touch Order/AddressBook/Fulfillment. Not authorized, not started.
