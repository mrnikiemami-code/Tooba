# Performance — TB-P10-T022-R11

## Controls

- `VariantLivePreview` uses `IntersectionObserver` (`rootMargin: 120px`) so offscreen picker cards stay as lightweight placeholders until near viewport (LOCK-SF-341).
- Review step mounts with `eager` (single selected Variant only).
- PreviewFake context is memoized empty + code-owned fakes via `renderSharedLandingSection` + `applyPreviewFill` (no DB / Template Catalog / Store reads for picker).
- No polling / setInterval in the picker shell.
- Constrained preview frames (`max-h-[160px]` card / `max-h-[320px]` review) — no CSS `transform: scale` zoom.
- Nested interactive elements are not wrapped in outer `<button>` (avoids invalid nesting and accidental remounts).

## Expected behavior

Many Variants on Appearance step: only visible cards mount Swiper/production trees; scrolling reveals more without layout jump beyond placeholder→content swap.
