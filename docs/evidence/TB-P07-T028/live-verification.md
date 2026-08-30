# Live verification — TB-P07-T028

Date: 2026-08-30

## Runtimes
- Host `:5088` health 200
- Frontend `:3000` `/fa/admin/products` 200, `/en/admin/products` 200
- Shopeiva `:3001` 200

## Product with variant-enabled attributes
- ProductId: `01a0455c-53c8-7000-a110-061ffa1f936e`
- Editor: axes=2, variants=2, maxCombinations=200
- Workspace page EDIT: `/fa/admin/products/01a0455c-53c8-7000-a110-061ffa1f936e?scope=edit` → 200

## UI checks (source + live page load)
- Attributes helper copy present (`product-attributes-helper`)
- Variant-enabled label: «قابل استفاده برای تنوع»
- Variants intro + 4-step builder markers
- No محور / Schema jargon in Attributes/Variants panels
- No Price/Stock on variant panel
- Cap 200 preserved

## Validation
- Host.Tests: 342 passed / 0 failed / 0 skipped
- FE typecheck, lint (pre-existing facet/mega-menu unused only), test:admin, test:product-workspace, production build OK

## USER_VISUAL_ACCEPTED
NO
