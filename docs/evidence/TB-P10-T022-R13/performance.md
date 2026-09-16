# Performance — TB-P10-T022-R13

## Controls retained from R9-R1 / R11

- Canonical Landing/Home resolver dedupe (`storefront-store-page-resolver.ts` + React `cache`)
- Store+locale+page scoped cache tags; admin preview remains no-store
- Variant Picker IntersectionObserver lazy mount (`rootMargin: 120px`)
- Template selector mounts **one** live iframe for the selected card (not 10 eager expensive previews)
- No polling / magic retries in Builder surfaces

## Local timings (this task)

| Scenario | Result |
| --- | --- |
| Home `/` first fetch | ~516 ms (200, ~347 KB HTML) |
| Home warm repeat | ~570 ms |
| Capture landingWarm probe | 3641 ms first hit to missing `/landing/demo` (404) — not a stall |
| Published Landing `/landing/{slug}` after publish | ~2211 ms first navigation; Home warm ~516–570 ms |
| Capture total | ~57 s for full 20-shot journey (expected UI automation) |
| Host template preview APIs (10 packs) | low hundreds of ms each; no 90–120 s stalls |

## Acceptance

- No 90–120 s SSR stalls observed
- Warm storefront Home remains low-single-digit / sub-second locally
- Variant picker and 10-card selector remain responsive under capture
- R9-R1 SSR guards still PASS (`storefront-ssr-perf-r9r1.guard.test.ts`)
