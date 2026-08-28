# TB-P07-T001-R3 — Product video deferred

## Decision
**DEFER** product video. No safe binary/video media pipeline on Host for Admin authoring.

## UI
- No video upload / preview / poster controls in Admin Product workspace.
- Media section is image-gallery only (Guid attach + storefront SVG preview).
- Placeholder `data-testid="product-video-control"` remains `hidden` so absence is explicit and not faked.

## Rationale
- Catalog stores opaque `MediaAssetId` references.
- Storefront serves presentation SVG for any asset id.
- Video upload/validation/reorder would imply a Media module that does not exist yet.

## Status
DEFERRED honestly — control hidden, not stubbed as working.
