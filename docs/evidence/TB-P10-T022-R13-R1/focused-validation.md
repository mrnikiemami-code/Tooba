# focused-validation — TB-P10-T022-R13-R1

## Commands run

```text
node --experimental-strip-types --test app/admin/landing-pages/admin-hero-slider-r13r1.guard.test.ts
node --experimental-strip-types --test app/admin/landing-pages/admin-variant-picker-r11.guard.test.ts
npm run test:critical-storefront
npm run test:composition-engine
git diff --check
```

## Results

| Gate | Result |
|------|--------|
| R13-R1 hero guards (9) | PASS |
| R11 variant picker guards (10) | PASS |
| critical-storefront (20) | PASS |
| git diff --check | PASS |
| composition-engine | FAIL pre-existing `banner.four-grid` responsive contract (unrelated to Hero) |

## Checklist

| Item | Result |
|------|--------|
| Slider picker exactly 6 variants | PASS |
| Persian/key mapping | PASS |
| One HeroSlider/Swiper engine | PASS |
| No second slider library | PASS |
| Height Medium/Large/ExtraLarge only | PASS |
| No raw pixel Admin input | PASS |
| Image guidance by variant+height | PASS |
| Fake SEO fields removed | PASS |
| Structured destinations | PASS |
| Validation modal + live clear | PASS |
| LOCK-SF-367…374 | PASS |
| Canonical Home reset untouched | PASS |
| No P11/T002/T023 | PASS |
