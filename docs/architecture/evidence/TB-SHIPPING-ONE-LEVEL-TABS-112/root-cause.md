TB-SHIPPING-ONE-LEVEL-TABS-112 — root cause, semantic reuse decision, rollback
================================================================================

1. Root cause (source lines)
----------------------------
Two Catalog-owned Development seeds can want the same attribute code, and the
second one used to create it unconditionally:

Ownership order inside the two-phase Development migration (TB-110/TB-111):
  phase 1: all 28 `IModuleSchemaMigrator` migrators (Catalog schema, Party schema, ...)
  phase 2: `IModuleSchemaMigrationStep` post-migration steps, among them
           `CatalogDevelopmentSeed.PostMigration.EnsureAsync`
           -> `CatalogAttributeSchemaDevelopmentSeed.ApplyAsync`
              -> `EnsureDefinitionAsync(catalog, db, "color", Enumeration, isVariantAxis: true, ...)`
                 (CatalogAttributeSchemaDevelopmentSeed.cs:37-44, helper at :183-230)
                 -> creates definition `color` (owner: Catalog attribute-schema seed)
              -> `EnsureOptionAsync(catalog, db, colorId, "black", ...)`
                 (CatalogAttributeSchemaDevelopmentSeed.cs:74, helper at :247-263)
                 -> creates option `black` under that definition

Later legacy Host bootstrap:
  `WorkspaceDemoSeed.SeedNewProductAsync` -> `WorkspaceDemoProductSeed.SeedProductAsync`
      WorkspaceDemoProductSeed.cs:103 (pre-fix)
          var colorId = await catalog.CreateAttributeDefinitionAsync(color, Enumeration, true, ...)
      -> CatalogAttributeDefinitionDirectory.CreateAsync -> duplicate code
      -> CatalogErrorCodes.AttributeCodeDuplicate = "catalog.attribute.code.duplicate"
      -> CatalogDirectory.Unwrap throws `InvalidOperationException("catalog.attribute.code.duplicate")`
      -> unhandled Development startup failure (TB-111 observation, reproduced in the
         negative control of this task).

The seed was not broken by a missing invariant: the code collision is a legitimate
Development seed coordination problem, and the Catalog public rejection policy must
stay exactly as it is. The second seed simply had to read what the first seed owns.

A second, immediately adjacent collision existed on the same statement path: the
reused definition already carries option `black` (CatalogAttributeSchemaDevelopmentSeed.cs:74)
while `attribute_options` has a unique index on `(definition_id, code)`
(CatalogDbContext.cs, `entity.HasIndex(x => new { x.DefinitionId, x.Code }).IsUnique()`).
Re-creating that option would have raised a raw unique-constraint `DbUpdateException`
on the very next statement of the same method, so the compatible reuse covers the
definition AND the option under it. Nothing else in the seed's path can collide
(categories are name/slug-scoped and unique per name; the brand slug and the product
slug are created only when absent, and the seed itself is only reached when the
demo product is absent).

2. Semantic reuse decision
--------------------------
New Development-only helper (owner: Catalog Infrastructure Development):
`Tooba.Catalog.Infrastructure/Development/CatalogDevelopmentAttributeReuse.cs`

  EnsureCompatibleDefinitionAsync(catalog, code, valueKind, isVariantAxis, names, ct)
    - reads the canonical code through the EXISTING Catalog read seam
      `ICatalogDirectory.ListAttributeDefinitionsAsync()` (Application port; no direct
      EF query on the definition path, no raw SQL, no new uniqueness behavior);
    - normalizes the code exactly like the domain does (`Trim().ToLowerInvariant()`);
    - if absent -> creates through the EXISTING `ICatalogDirectory.CreateAttributeDefinitionAsync`
      (so all existing Catalog validation/invariants still apply);
    - if present -> reuses the SAME `DefinitionId` only when EVERY
      downstream-required semantic matches:
        * `value-kind`  : ValueKind must equal the requested kind (Enumeration)
        * `variant-axis`: IsVariantAxisAllowed must equal the requested participation (true)
        * `active`      : IsActive must be true
      otherwise it throws a stable diagnostic naming ONLY the code and the mismatched
      rule(s):
        `catalog-dev.attribute.incompatible-reuse: code=<code>; rules=<rule>[,<rule>]`
      and performs no write. Display names, localized text and any other data are
      deliberately not part of the message.

  EnsureCompatibleOptionAsync(catalog, catalogDb, definitionId, code, names, ct)
    - an option's identity is exactly `(definitionId, code)`, so there is no semantic
      compatibility question beyond existence;
    - reads existence through the owning module's own `CatalogDbContext`
      (`AttributeOptions`), mirroring the already-existing sibling helper
      `CatalogAttributeSchemaDevelopmentSeed.EnsureOptionAsync` — this is the module's
      own persistence access, not a Host-side or cross-module one;
    - if present -> returns the EXISTING `OptionId`; if absent -> creates through the
      EXISTING `ICatalogDirectory.AddAttributeOptionAsync`.
    - NOTE for the Architect: there is currently no Application-level read contract for
      enumeration options (`ICatalogDirectory` exposes `AddAttributeOptionAsync` only).
      This task did not add one, because an option carries no additional required
      semantic that would need validating. If a canonical option read seam is desired,
      that is a widening decision and is being reported as such, not silently taken.

Unchanged on purpose: `CreateAttributeDefinitionAsync` and `AddAttributeOptionAsync`
public behavior, `catalog.attribute.code.duplicate` rejection, all Catalog module
ownership, `I/ModuleSchemaMigrator` ordering (TB-110), the schema seed's own
definition/option ownership and metadata, variant axis/option identifiers, and every
checkout/order/fulfillment/shipping/payment surface.

3. Changed paths
----------------
src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoProductSeed.cs
    (10 lines: the two unconditional creates become the compatible read-or-create calls)
src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogDevelopmentAttributeReuse.cs (new)
src/backend/Host/Tooba.Host.Tests/CatalogDevelopmentAttributeReuseTests.cs (new, 7 tests)
src/backend/Host/Tooba.Host.Tests/WorkspaceDemoProductSeedCollisionRegressionTests.cs (new, 1 test)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-112.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-112/**

4. Focused test proof
---------------------
`CatalogDevelopmentAttributeReuseTests` (Testcontainers PostgreSQL 16, real
`CatalogDirectory` against a real `catalog` schema):
  1. Incompatibility_diagnostic_names_only_the_code_and_the_mismatched_rules         (pure)
  2. Compatible_definition_reports_no_incompatibility                                 (pure)
  3. Empty_catalog_creates_the_required_attribute_through_the_catalog_api
  4. Existing_compatible_attribute_is_reused_with_the_same_persistent_identifier
  5. Existing_incompatible_attribute_is_refused_before_any_downstream_write
  6. Repeated_preparation_is_idempotent_and_keeps_definition_and_option_identity
  7. Reused_axis_and_option_identifiers_stay_valid_for_variant_creation

`WorkspaceDemoProductSeedCollisionRegressionTests`:
  8. Legacy_workspace_seed_completes_by_reusing_the_schema_seed_attribute_and_option
     runs the real schema seed first (owner of `color` + `black`), then the real
     affected operation `WorkspaceDemoProductSeed.SeedProductAsync`, and asserts the
     workspace product publishes, exactly one `color` definition exists with the SAME
     `DefinitionId`, the `black` option keeps the SAME `OptionId`, and the created
     variant's attribute value still points at the reused definition.

Whole-run output: Passed! Failed: 0, Passed: 8, Skipped: 0 (focused-tests.txt).
Negative control (pre-fix code): 1 failed with `catalog.attribute.code.duplicate`
at `WorkspaceDemoProductSeed.cs:103` (negative-control.txt).

Build: `dotnet build src/backend/Tooba.slnx -c Debug` -> 0 Error(s).

NOT run on purpose: the broad known-failing Host suite (it contains a pre-existing
architecture-guard drift unrelated to this task: `Host_Admin_count_15_*` expects 15
`*.cs` files under `src/backend/Host/Tooba.Host/Admin` while both the expected base
SHA `c862d6c` and HEAD contain 17 — proven with `git ls-tree -r`), the 545-product
demo seed, and any live browser flow.

5. Exact rollback
-----------------
Revert this task's commit (`git revert <Code-SHA>`); the change is exactly:
  - delete `CatalogDevelopmentAttributeReuse.cs`;
  - delete the two new test files;
  - in `WorkspaceDemoProductSeed.SeedProductAsync`, restore the two statements
    `catalog.CreateAttributeDefinitionAsync(ColorAttributeCode, Enumeration, true, ...)`
    and `catalog.AddAttributeOptionAsync(colorId, BlackOptionCode, ...)`.
No schema, migration, data, config, contract or Host change is involved, so a rollback
cannot require data repair.

6. Next scratch prerequisite (TB-113 runtime proof)
---------------------------------------------------
TB-112 deliberately did not launch any Host. TB-113 must re-run the single isolated
scratch Host startup (watchdog <= 180s, empty scratch PostgreSQL, distinct scratch
connection strings/tenant, offline effective-config guard, absolute-PID cleanup) and
verify on `schema-mobile-demo-phone`:
  - `party.parties` 42P01 count stays 0 (TB-110/TB-111 fix holds);
  - the `catalog.attribute.code.duplicate` failure is gone and the legacy workspace
    seed no longer aborts Development startup;
  - whether real sellable offers/prices/inventory rows are created for the schema demo
    phone and whether the workspace demo product reaches Published;
  - any FIRST new blocker after the former collision point, reported without repair.
