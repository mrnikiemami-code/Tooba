# Variant Contracts — TB-P10-T022-R13-R3

Shared engine: `storefront-product-showcase-embla-rails.tsx` + `VARIANT_CONTRACTS` + `embla-carousel-autoplay@8.5.2`.  
ProductCard: `StorefrontProductCardView` unchanged.

| Variant | Persian | Composition language | Motion |
|---|---|---|---|
| sunny / سانی | warm amber elevate + edge glow + soft scale | delay 5000ms, duration 28 |
| money / مانی | commerce orange ring, tight gap, stronger active scale | delay 3600ms, duration 16 |
| cinematic / سینمایی | perspective stage, rotateY neighbors, vignette, edge fade | delay 6000ms, duration 32 |
| cinematic-plus / سینمایی پلاس | deeper Z, stronger rotate/scale, larger slides | delay 6800ms, duration 40 |
| explorer / کاشف | asymmetrical peek cue + offset rotates | delay 4500ms, duration 24 |

Autoplay: `enableAutoplay` on storefront; Admin storePreview keeps autoplay off (LOCK-SF-383).  
Hover/focus pause via section handlers; reduced-motion disables autoplay.  
Fix R13-R3: decorative glow/peek overlays moved **outside** Embla viewport so container is the sole first child (sunny/explorer autoplay).
