# Template Source Materialization — TB-P10-T022-R10

## Contract

`buildTemplateSectionPayloads(templateKey)` maps Template Catalog presets to normal Store Page section writes:

- `hostType` via `landingHostTypeForSection`
- `variantKey` preserved in config
- `dataSourceIntent` translated to Store source semantics (`Newest` / `Category` / `Brand` / `Manual` / `Latest`)
- empty Manual ID arrays where Store data must be chosen later (no permanent bind to Template Catalog sample IDs)

## Apply path

1. Create Draft page (if needed) with title/slug/locale/pageType
2. `PUT /v1/admin/pages/{pageId}/sections/composition` → `ReplaceCompositionAsync`
3. Atomic replace of Page Section rows only — Catalog Products/Categories/Brands/Banners/Articles/Stories/Reviews untouched

## Evidence

- Guard: `admin-store-pages-r10.guard.test.ts` (Fashion payload length + variant order)
- Host tests: `InsertAt_places_section_between_existing`, `ReplaceComposition_replaces_all_sections_without_silent_append`
