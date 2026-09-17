# validation-runtime-proof — TB-P10-T022-R13-R1-R1

Flow A–K exercised in `capture.mjs`:

| Step | Action | Evidence |
|------|--------|----------|
| A | Open Slider settings | hero-slider-settings visible |
| B | ≥2 slides via `hero-slide-count=2` | slider-tabs.png |
| C | Leave slide 2 title/alt empty | — |
| D | Next/Review | section-wizard-next |
| E | Popup | slider-validation-popup.png |
| F | Slide 2 tab `data-has-error=true` | slider-tab-error-state.png |
| G | Invalid fields visible | slider-invalid-fields.png |
| H | Enter valid title/alt on slide 2 | — |
| I | Field aria-invalid clears | slider-error-cleared-live.png |
| J | Tab error clears when fields valid | same shot / live clear |
| K | Review surface | slider-review-final.png |

No polling/timeouts in product validation path.
