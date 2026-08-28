# TB-P07-T001-R3 — Product specs typed

## Decision
Preserve T001 typed attribute foundation. Admin Product **attributes** tab hosts `ProductAttributesPanel` only.

## Editors (no raw JSON)
Typed controls by `valueKind`:

| Kind | Control |
| --- | --- |
| Text | text input |
| Number | text/numeric input |
| Boolean | select true/false |
| Enumeration | enumOptionId (+ optional raw) |
| Instant | ISO text |

Plus required/axis flags from definitions; localized labels via `valueKindLabel`.

## What is absent
- No JSON blob editor on Product workspace.
- Specs are definition-driven Host writes: `PUT /v1/admin/catalog/products/{id}/attributes/{definitionId}`.

## Files
- `src/frontend/app/admin/catalog-attribute-ui.tsx` — `ProductAttributesPanel`
- `src/frontend/app/admin/product-workspace-screen.tsx` — attributes section keeps panel
