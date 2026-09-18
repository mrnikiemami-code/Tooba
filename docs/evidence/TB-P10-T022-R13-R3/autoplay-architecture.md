# Autoplay Architecture — TB-P10-T022-R13-R3

- Plugin: `embla-carousel-autoplay@8.5.2`
- Options: playOnInit, stopOnInteraction false, stopOnMouseEnter false, stopOnFocusIn false
- Section `onMouseEnter/Leave` + focus capture pause/resume (LOCK-SF-383)
- `enableAutoplay={!storePreview}` in Admin shared renderer; storefront `enableAutoplay` true
- `prefers-reduced-motion: reduce` → autoplay off (`data-product-showcase-autoplay=off`)
- Embla viewport must have **only** the container as direct child (glow/peek overlays outside)

Proof: `autoplay-proof.md` — all five advanced on exact Landing route.
