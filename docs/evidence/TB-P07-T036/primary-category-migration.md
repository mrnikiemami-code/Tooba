# Primary Category Migration (TB-P07-T036)

## Decision (Published safety)
If a **Published** product’s Primary migration causes structural incompatibility
(orphans removed, newly required missing, or variant axis incompatibility),
the product is **Unpublished → Draft** in the same transaction so broken Attribute/Variant state is not public.
Compatible migrations leave Published intact.

## Backend
- Enriched `CategoryChangeImpactReport` (paths, preserved/added/removed lists, variant compatibility, membership promote, readiness blockers).
- `ReplaceProductPrimaryCategoryAsync`: transactional migrate — preserve by DefinitionId, remove orphans, promote Additional→Primary, Draft incompatible variants, human history.
- Doc: `docs/catalog/PRIMARY-CATEGORY-MIGRATION.md`

## Frontend
- `primary-category-migration-wizard.tsx`: 3-step wizard (pick L3 → non-mutating preview → confirm).
- Product Workspace General edit: «تغییر دسته اصلی» opens wizard; no inline primary combobox save.

## Tests
`PrimaryCategoryMigrationTests` + FE wizard/workspace source tests.
