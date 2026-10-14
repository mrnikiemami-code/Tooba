# Shipping — Geography Read Foundation Report (`TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043`)

> **STATUS: PASS FOR THE READ-FOUNDATION SCOPE / CONTRACTS IMPLEMENTED AND RUNTIME-VERIFIED AGAINST A REAL POSTGRESQL / NO HOST WIRING / NO SCHEMA OR MIGRATION CHANGE / NOT CERTIFIED.**
> `IGeographyLookup` and `IGeographyLabelLookup` now have real read-only implementations over the module's
> own `GeographyDbContext`, verified end-to-end on a disposable task-exclusive database with seeded reference
> rows (47/47 checks). No Host/DI registration, no endpoint, no migration edit or application to any Tooba
> database, no shipping/Checkout behaviour change.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected-Base-SHA | `dd3d334b2322b30437b953cb45f5707033e58718` |
| Base-SHA (verified `HEAD == origin/main`) | `dd3d334b2322b30437b953cb45f5707033e58718` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042` |
| Mode | `BOUNDED_GEOGRAPHY_READ_FOUNDATION_NO_HOST_WIRING` |
| Status | `PASS` (read foundation) |
| Stop-State | `STOP_FOR_ARCHITECT` |
| automaticNextImplementationTask | `NONE` |

---

## 1. Preflight

- Claimed exactly one Pending item on `tooba-main` after `BRIDGE-WAKE` (row `91fa4322-b6be-41fd-b288-9706c09d8cac`); the received artifact was persisted verbatim at `docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043.task.md` (receipt only, not staged, never replayed).
- `git fetch origin` → `HEAD == origin/main == Expected-Base-SHA == dd3d334b2322b30437b953cb45f5707033e58718`. No drift, no reset/stash/rebase/force-push.
- Read `AGENTS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`, ADR-036, report 042, the Geography Contracts/Domain/DbContext, the migration source (not edited) and the established module patterns (`Catalog.Infrastructure.Directories.UnitOfMeasureDirectory`, `Localization.Infrastructure.Languages.LanguageDirectory`, `Fulfillment.Infrastructure.Shipping.ShippingServiceLanguageGate`). Findings register not consulted.
- Output-path collision check: nothing existed under `Geography/.../Places/**`, no `SHIPPING-GEOGRAPHY-READ-FOUNDATION-043.md`, no such evidence folder.
- All pre-existing dirty/untracked files recorded and left untouched.

## 2. Changed paths

| Path | Change |
| --- | --- |
| `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Places/GeographyPlaceLookup.cs` | New: `IGeographyLookup` implementation (lists + `GetByIdsAsync`) |
| `.../Places/GeographyPlaceLookup.LegacyResolution.cs` | New: `ResolveLegacyAsync` (partial class, same capability folder) |
| `.../Places/GeographyLabelLookup.cs` | New: `IGeographyLabelLookup` implementation (requested → default → stable-code chain) |
| `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj` | Added one Contracts-only `ProjectReference` to `Tooba.Localization.Contracts` (`ILanguageLookup`), the same seam Catalog/Content/Fulfillment Infrastructure already consume. **No other csproj, `Tooba.slnx`, Host, DI or Endpoint file touched.** |
| `docs/planning/shipping/SHIPPING-GEOGRAPHY-READ-FOUNDATION-043.md` | This report |
| `docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043/**` | Local evidence: seed SQL, verification log, destination proof, harness source copy |

**No migration, schema, domain, contract, or DI file was modified.** `20261014113949_InitialGeography.cs`, its Designer, the model snapshot, `GeographyDbContext.cs`, the Domain/Contracts projects and every previously accepted constraint are byte-identical to the Base-SHA. **No new project was created** (no empty Application scaffolding): the read adapters live in Infrastructure behind the existing Contracts, which is what the Task asked for when a project is not actually required.

## 3. What was implemented

### 3.1 `IGeographyLookup` (`GeographyPlaceLookup`)

| Member | Behaviour |
| --- | --- |
| `ListCountriesAsync` | All countries, `AsNoTracking`, ordered by `CountryCode` then id; `IsActive` surfaced as a flag (inactive rows are **not** hidden) |
| `ListAdministrativeAreasAsync(countryId)` | Areas of one country ordered by `Code` then id, parent link and active flag surfaced; empty for `Guid.Empty` |
| `ListCitiesAsync(countryId, areaId?)` | Cities of one country, optionally restricted to one area (the 040 composite FK already guarantees that area lives in that country); ordered by `Code` then id |
| `ListServiceAreasAsync(cityId)` | Service areas of one city, ordered by `Code` then id |
| `GetByIdsAsync` | Strict validation: country must exist **and be active**; every provided child must exist, be active, and belong to the requested ancestry (area in the same country; city in the same country and exactly in the given area when one was supplied; service area owned by the given city). `Guid.Empty` in any slot is rejected, and a service area without its owning city is rejected because its ancestry cannot then be validated. Returns `null` for an unacceptable destination, otherwise the identifiers unchanged. |

Every query is `AsNoTracking`, read-only, takes no lock and opens no transaction. No cross-module DbContext is read.

### 3.2 `ResolveLegacyAsync` — read-only, deterministic, no identity creation

Declared semantics (explicitly *proposed* by this adapter, since 043 defines the first implementation):

1. **Exact normalised comparison, never fuzzy.** A candidate matches when its stored text equals the input after `Trim()` under `OrdinalIgnoreCase`. No substring, no edit distance, no transliteration, no nearest-match.
2. **Two exact keys per place.** The input may equal the localised label of the place in *any* language, or its stable `Code`. Label rows are collapsed per place, so a label repeated across languages is one candidate.
3. **Country is required.** The legacy surface always carries a country context, and place identity is scoped by `CountryId`; a blank/unknown code returns `Unresolved` instead of a cross-country search that could pick a same-labelled place in another country.
4. **Ambiguity is reported, never collapsed.** If a level matches more than one distinct place (two cities with the same label in two different areas; two areas sharing one label in one country), the result is `Ambiguous` with the distinct candidate count, and `Identifiers` stays `null`. A supplied area therefore acts as a real scope: it is what turns a duplicated city label into one unambiguous candidate.
5. **One factual derivation only.** When a city resolves and no area was supplied, the returned identifiers carry the city's own stored `AdministrativeAreaId` (its actual parent row - not a picked candidate).
6. **Service area requires its city.** A service-area label alone is `Unresolved`, because the same label can exist in many cities and the ancestry *is* the city.
7. No insert, no `SaveChanges`, no migration, no seed: the resolver cannot create identity.

### 3.3 `IGeographyLabelLookup` (`GeographyLabelLookup`)

Fallback chain per ADR-036 D9, with the two ambiguous steps pinned down explicitly:

- **requested language** — honoured only when the Localization port (`ILanguageLookup`, Contracts-only) knows that language; a fabricated/unknown id is *not* used to surface a stale translation row.
- **default language** — `IsDefault` only. Deliberately **no** "first language wins" step: selecting an arbitrary language would be invented policy.
- **stable code** — the place's own `Code`, matching the established repo convention (`picked?.Name ?? u.Code`). An id that does not exist at all is **omitted**; a label is never fabricated.

The language set/default is read through Localization **Contracts**; Geography owns only the label rows, so no language data is duplicated and no global translation service was invented (ADR-036 D9 forbids one).

## 4. Isolation proof for the verification database

`log-destination-proof.txt`:

| Evidence | Value |
| --- | --- |
| Disposable container | `fdc0c224405d09cf8335f27564276f297a3cfdf4e04f735f752b818f557f8d01 /geo043-verify running postgres:17` |
| Pre-existing container (untouched, never connected) | `01d8067a30ce965c2ffc80296c5568c63f8e48eb1b2c5c2d1d3f7963b7b48d9f /postgres-db running` |
| Port binding | `5432/tcp -> 127.0.0.1:55433` (Tooba's own service listens on 5432) |
| Server that answered | `db = geo043`, fresh `pg_postmaster_start_time`, only template + `geo043` databases |
| Schema source | the already-validated 042 migration source (`Up`), applied **only** to this empty disposable DB |
| Seed | `seed-read-verification.sql` - 3 countries, 4 areas, 8 cities, 3 service areas, 15 label rows |

The only connection string used anywhere in this Task was `Host=127.0.0.1;Port=55433;Username=postgres;Password=geo043localonly;Database=geo043`. The container was removed at the end (`geo043-verify` gone; `postgres-db` still running and never touched). No Tooba database was contacted, migrated, seeded or inspected.

## 5. Verified results (47/47 checks, `log-read-verification.txt`)

| Group | Cases | Result |
| --- | --- | --- |
| Listings (L1–L7) | country/area/city/service-area ordering, area scoping, inactive row surfaced, another country excluded, empty-id guards | 7 PASS |
| `GetByIds` (G1–G11) | full valid chain; area-only; country-only; inactive country; inactive city; city from another country; area/city mismatch; service area of another city; service area without city; `Guid.Empty` slots; unknown ids | 11 PASS |
| Labels (B1–B12) | requested language wins; en-only label; default-language step; `null`/`Guid.Empty` language; unknown language falls through; empty id set; country/area/city/service-area labels in fa and en; no `IsDefault` at all; empty language set | 12 PASS |
| Legacy resolution (R1–R16) | exact label + code keys; **duplicated city label in two areas → `Ambiguous(2)`, not collapsed**; supplying the area disambiguates it; duplicated area label → `Ambiguous(2)`; area filled from the city's own row; service-area label without city → `Unresolved`; full 4-id chain; country-only; blank/unknown country; trim + case-insensitive; city outside the supplied area → `Unresolved`; unlabelled city resolved by code; determinism across two runs | 16 PASS |
| Read-only proof (D1) | total row count of the five geography tables unchanged before/after the whole suite (`24 -> 24`) | 1 PASS |

The harness (`harness/Program.cs`, kept as evidence) built the `GeographyDbContext` through `ToobaNpgsql.ConfigureModuleContext`, drove the **real** adapters against the disposable database, and used a test-only `ILanguageLookup` stand-in because the real one is registered by Localization and wiring DI is out of scope for this Task.

## 6. Runtime aspects deliberately left unverified

1. **DI/Host wiring of the adapters** - not registered anywhere (forbidden by this Task), so no request path exercises them yet. The types build and run only under the harness.
2. **`ILanguageLookup` as supplied in production** - the harness used a stand-in. The real `LanguageLookupBridge` reads the Localization database; its content (which language is `IsDefault`, which ids exist) therefore decides the default step. This is the intended ownership, but the *combination* is untested.
3. **Scale/perf** - the resolver loads a country's areas, cities and their labels into memory (deliberate: bounded reference data, exact in-memory comparison). No large-scale measurement was made.
4. **Cross-country duplicate labels** - a duplicated label across *countries* is out of reach by design (country is required), so only the same-country duplicates were tested.
5. **Consumers** (Order/AddressBook/Checkout/legacy shim) - untouched and not adapted; `GetByIds` returning `null` for inactive places is a strict policy that a consumer may later need to reinterpret (e.g. surface instead of reject). That is flagged for the Architect rather than decided here.

## 7. Time, commit, push

- Claim (`12:02:30Z`) → validated commit: **≈9 minutes** of the 30-minute hard timebox.
- Two bounded commits, task-owned paths only: code + report `38019b3e3`, local evidence (seed SQL, verification log, destination proof, harness copy) in the evidence commit that follows it. `git push origin main`; `git fetch origin`; `HEAD == origin/main` verified.
- Disposable container removed; no Tooba database touched; no Results-API probe; no automatic next task; no idle polling.

## 8. Recommendation (exactly one next step)

Have the Architect rule on the two policy points this slice had to pin down - `GetByIds` rejecting **inactive** places (fail-closed) and the label chain stopping at `IsDefault` with **no** arbitrary-language step - and, once ruled, authorize the *next* slice to register the adapters in the Geography module composition (still without touching Checkout). Nothing in this slice should be applied to a Tooba database or consumed by another module before those two decisions are recorded.
