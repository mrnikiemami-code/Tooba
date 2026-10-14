# Shipping — Geography Country Integrity Report (`TB-SHIPPING-GEOGRAPHY-COUNTRY-INTEGRITY-040`)

> **STATUS: INERT MODEL HARDENED / NO MIGRATION / NOT WIRED / NOT CERTIFIED.**
> Cross-country parent associations are now impossible at the relational boundary via real
> database-enforced composite foreign keys. No migration was generated or applied, no database was
> contacted, no Host wiring and no behaviour change occurred.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected / Base / Audited SHA | `da6f339440260047f56210bfc36008d09ec71165` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-TRANSLATION-INTEGRITY-039` |
| Mode | `IMPLEMENTATION_BOUNDED_INERT_COUNTRY_RELATION_INTEGRITY_ONLY` |
| Stop-State | `STOP_FOR_ARCHITECT` |

**Cycle prevention remains UNRESOLVED. Migration is NOT authorized and migration readiness is NOT claimed.**

---

## 1. What changed (one file, one concept)

Only `GeographyDbContext` mappings changed. **No Domain edit was needed** (no `Tooba.Geography.Domain`,
`Tooba.Geography.Contracts`, `.slnx`, or any other file was touched — proven by `git status`, which
lists exactly one modified path).

| Relationship | Before (039) | After (040) |
| --- | --- | --- |
| AdministrativeArea self-parent | `FOREIGN KEY (parent_administrative_area_id) REFERENCES administrative_areas (administrative_area_id)` — Restrict, optional | `FOREIGN KEY (parent_administrative_area_id, country_id) REFERENCES administrative_areas (administrative_area_id, country_id)` — Restrict, optional |
| City → AdministrativeArea | `FOREIGN KEY (administrative_area_id) REFERENCES administrative_areas (administrative_area_id)` — Restrict, optional | `FOREIGN KEY (administrative_area_id, country_id) REFERENCES administrative_areas (administrative_area_id, country_id)` — Restrict, optional |
| New principal key (enabler) | — | `CONSTRAINT ak_administrative_areas_id_country UNIQUE (administrative_area_id, country_id)` |

Unchanged: PKs (`country_id`, `administrative_area_id`, `city_id`, `service_area_id`, `translation_id`),
`country_id → countries` on both areas and cities (Restrict, required), the scoped code unique indexes,
the `service_areas → cities` FK (Cascade), all four translation tables with their `UNIQUE(parent, language_id)`
and `FK → place` Cascade, and the `outbox_messages` mapping.

**Delete policies are unchanged by design** — `Restrict` for every place-to-place reference, so the new
composite constraints do not introduce any new cascade path.

## 2. Why a composite foreign key (the mechanism, precisely)

`Tooba.slnx`-level module identity is `country_id`, but the parent column alone could never express
"same country" relationally. The standard relational solution — and the one the Architect asked for — is:

1. declare the trivially-unique alternate key `(administrative_area_id, country_id)`. PostgreSQL requires
   a unique constraint to be an FK target, so this constraint is the enabler;
2. point the self-parent FK at the **pair** `(parent_administrative_area_id, country_id)` instead of at
   `administrative_area_id` alone;
3. point the city FK at the pair `(administrative_area_id, country_id)` instead of at
   `administrative_area_id` alone.

`country_id` is `NOT NULL` on both tables and is part of every composite FK, so the pair must exist in
`administrative_areas` exactly as written. A city that declares `country_id = A` while pointing at an
area that exists only under country `B` can only match a row `(X, A)`, which does not exist → rejected by
the database.

## 3. Null semantics — verified, not assumed (the task's key question)

The rendered DDL from the model emits **no `MATCH` clause**, so PostgreSQL's default `MATCH SIMPLE`
applies. That is the correct semantics here, and it is why `MATCH FULL` would be wrong:

| Scenario | `MATCH SIMPLE` outcome | Is it correct? |
| --- | --- | --- |
| `parent_administrative_area_id IS NULL` (root area), `country_id = A` | Constraint not checked | **Yes** — there is no parent link at all, so no cross-country link can exist. Also unavoidable: `country_id` is `NOT NULL`, so `MATCH FULL` would reject **every** root area. |
| `parent_administrative_area_id = X`, `country_id = A`, `(X, A)` exists | Accepted | Yes — same country. |
| `parent_administrative_area_id = X`, `country_id = A`, `X` belongs only to `B` | **Rejected** | Yes — this is exactly the cross-country parent being eliminated. |
| `cities.administrative_area_id IS NULL`, `country_id = A` | Constraint not checked | **Yes** — the area relation is optional by design and absent. |
| `cities.administrative_area_id = Y`, `country_id = A`, `Y` belongs only to `B` | **Rejected** | Yes — cross-country city→area removed. |

The only "skip" path requires the parent column itself to be `NULL`, which means the association does not
exist — there is no partial-null bypass available that would still create a cross-country link, because
`country_id` can never be `NULL`. Optionality is therefore preserved exactly where it was allowed before
(area-less cities, root areas) while the country invariant is now enforced whenever a parent is present.

`CountryId` is immutable through the domain API (`{ get; private set; }`, assigned only in `Create`;
`Reparent`/`MoveToAdministrativeArea` change only the parent/area, never the country), so there is no
domain-side path that could re-interpret the pair after the fact either.

## 4. Verification actually performed

| Verification | Result |
| --- | --- |
| `dotnet build src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj` | **Build succeeded — 0 Error(s)**, 2 × `NU1900` (NuGet feed network timeout, unrelated to code). Rebuilt Contracts, Domain, Persistence, Infrastructure. |
| Targeted EF **model metadata** inspection (throwaway harness outside the repo, `%TEMP%/geography-model-check-040`, deleted afterwards; `ToobaNpgsql.ConfigureModuleContext` with a placeholder connection string, then `DbContext.Model`) | `MODEL_OK`. Printed `AK ak_administrative_areas_id_country = administrative_area_id,country_id`; `FK parent_administrative_area_id,country_id -> administrative_areas(administrative_area_id,country_id) delete=Restrict required=False`; `FK administrative_area_id,country_id -> administrative_areas(administrative_area_id,country_id) delete=Restrict required=False`; `FK country_id -> countries(country_id) delete=Restrict required=True` on both tables; the four translation tables each still `required=True` with `delete=Cascade`; `TRANSLATION_TABLES 4 NO_FK=0`; `POLYMORPHIC_OWNER_COLUMNS 0`. |
| **Rendered DDL** from the model (`DbContext.Database.GenerateCreateScript()` — pure model→SQL text; no connection, no database, no migration file, nothing written) | Emitted verbatim: `CONSTRAINT ak_administrative_areas_id_country UNIQUE (administrative_area_id, country_id)`; `CONSTRAINT fk_administrative_areas_administrative_areas_parent_administra FOREIGN KEY (parent_administrative_area_id, country_id) REFERENCES geography.administrative_areas (administrative_area_id, country_id) ON DELETE RESTRICT`; `CONSTRAINT fk_cities_administrative_areas_administrative_area_id_country_ FOREIGN KEY (administrative_area_id, country_id) REFERENCES geography.administrative_areas (administrative_area_id, country_id) ON DELETE RESTRICT`; and no `MATCH` clause on either (i.e. PostgreSQL `MATCH SIMPLE`). All other constraints/indexes rendered unchanged. |
| Repo/path integrity | `git status` lists exactly ONE modified file (this `GeographyDbContext.cs`); Domain, Contracts, `.slnx`, Order, AddressBook, Fulfillment, Host and frontend are untouched. |

No real database connection, no `dotnet ef` command, no migration file, no model snapshot, no seed, no
script was created or executed. Per the bounded-verification rule, the full solution was not built and no
broad test suite was run.

## 5. Cost accepted (stated honestly)

The enabling alternate key adds one redundant unique constraint/index —
`UNIQUE (administrative_area_id, country_id)` — which is trivially unique because
`administrative_area_id` is already the primary key. It exists only so PostgreSQL has an FK target for
the two composite constraints. That is the standard price of enforcing "same parent country" in a purely
relational way without a trigger, and it no longer supports any additional index-only query path. The two
former single-column supporting indexes (`parent_administrative_area_id`, `administrative_area_id`) are
replaced by their composite counterparts
(`ix_parent_administrative_area_id_country_id`, `ix_administrative_area_id_country_id`), so no index is
silently lost.

## 6. Residual blockers and status (NOT solved here)

1. **AdministrativeArea cycles remain UNRESOLVED.** A cycle `A → B → A` is still representable: an EF
   `CHECK`/`FOREIGN KEY` constraint cannot follow an arbitrary recursive parent chain, so this cannot be
   closed by a SQL constraint of this kind. It is explicitly deferred to its own reviewed task.
2. **`ServiceArea` has no country column**, so the same country invariant for `ServiceArea → City` is not
   expressible in this slice; it is transitively implied while service areas hang off a single city, but
   any future direct country/area link on `ServiceArea` must re-apply this pattern.
3. **`cities.administrative_area_id` and `administrative_areas.parent_administrative_area_id` remain
   optional** — intentionally: the Architect's acceptance requires "nullable area/parent relations remain
   valid where previously allowed", and the composite FK preserves that.
4. **D10 remains OPEN** — no administrative-level discriminator was added; the composite keys say nothing
   about levels.
5. **No migration and no model snapshot exist**, so none of this has ever touched a database; the first
   authorized migration would create the alternate key and both composite FKs as an initial create.
6. `LanguageId` remains an external scalar identity that PostgreSQL cannot verify (Localization owns the
   language registry) — unchanged from 039.

**Therefore: cycle prevention is UNRESOLVED, migration is NOT authorized, and geography remains INERT,
unregistered and uncertified (no `ARCH-COMPLETE-002` claim, no manifest entry, D3/D8/D9 preserved, D10 OPEN).**

## 7. ONE recommended next bounded step — NOT executed

**`TB-SHIPPING-GEOGRAPHY-CYCLE-PREVENTION-041`** (Architect authorization required): design and deliver
inert cycle prevention for the `administrative_areas` hierarchy — evaluate the genuinely available
vehicles (a domain/Application-level ancestor-walk invariant as the primary guard, optionally backed at
the database level by a shape that PostgreSQL can actually enforce, e.g. a deferred constraint trigger or
a materialised path/level column) and choose one with a written rationale rather than asserting an
unenforceable `CHECK`. Same inert discipline: no migration, no DB, no Host wiring, no behaviour change.
Entry criteria: Architect acceptance of this report and an explicit ruling on whether database-level
enforcement is required at all, or whether a domain invariant is acceptable for Phase 0a. Only after the
cycle question is settled should a migration task be considered. Not authorized, not started.
