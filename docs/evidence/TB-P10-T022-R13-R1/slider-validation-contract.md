# slider-validation-contract — TB-P10-T022-R13-R1

## Required per slide

- image (`mediaAssetId` or `imageUrl`)
- Alt
- visible title

## Optional

- description (visible subtitle)
- CTA label + destination pair

## Conditional

- destination ≠ none ⇒ CTA label required
- CTA label entered ⇒ destination required
- custom-url ⇒ valid internal path or http(s) URL
- product ⇒ target product selected
- category ⇒ target category selected

## UX surfaces

1. Modal summary (`hero-validation-modal`) on blocked Next/Save
2. Inline field errors (`aria-invalid`, red border/helper)
3. Slide tab error badge (`data-has-error` + icon)
4. Settings/Preview step error chips (`data-step-error`)
5. First invalid slide auto-activate + field focus/scroll
6. Live clear when fields become valid (no re-press Next)

API: `validateHeroSliderDetailed` / `validateHeroSliderSettings`.
