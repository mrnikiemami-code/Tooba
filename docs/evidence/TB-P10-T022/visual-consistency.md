# Visual consistency — TB-P10-T022

## Shared system (not flattened looks)

- Surface roles: page / section / alternate / accent via `wrapWithSurfaceRole` (LOCK-SF-254).
- Banner empty chrome uses `bg-section-surface` + muted text (not hard gray giant wrappers).
- Four-grid banner section padding aligned to `py-8 md:py-10` rhythm with other storefront sections.
- Product showcase continues to reuse ProductRail / ProductCard (LOCK-SF-244).
- Card radius remains rounded-2xl / rounded-3xl by role; no redesign of Shopeiva skins.

## Spot fixes

- Missing banner media placeholder inherits section surface tokens.
- Home brand empty state matches dashed empty chrome used on Landing.

## Out of scope

No Shopeiva Home/PDP visual redesign. No new color roles. No free-form CSS controls.
