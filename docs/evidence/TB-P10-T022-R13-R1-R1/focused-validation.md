# focused-validation — TB-P10-T022-R13-R1-R1

## Commands

```text
node --experimental-strip-types --test app/admin/landing-pages/admin-hero-slider-r13r1.guard.test.ts
npm run test:critical-storefront
git diff --check
node docs/evidence/TB-P10-T022-R13-R1-R1/capture.mjs
```

## Results

| Gate | Result |
|------|--------|
| R13-R1 hero guards (9) | PASS |
| critical-storefront (20) | PASS |
| git diff --check | PASS |
| Admin PNG capture (21) | PASS |
| create→remount→wizard | PASS |

## Checklist 1–21

| # | Item | Result |
|---|------|--------|
| 1 | Race root cause identified | PASS — remount drops wizardOpen |
| 2 | Real bug fixed (sessionStorage pending) | PASS |
| 3 | Six variants visible | PASS |
| 4 | Exact Persian names | PASS |
| 5 | No obsolete choices for new edits | PASS (guards) |
| 6 | Height presets only | PASS |
| 7 | No raw pixel input | PASS |
| 8 | Dynamic image guidance | PASS |
| 9 | Structured CTA UI | PASS |
| 10 | Product picker no GUID | PASS |
| 11 | Category picker no GUID | PASS |
| 12 | Popup validation | PASS |
| 13 | Inline error | PASS |
| 14 | Slide-tab error | PASS |
| 15 | Live clear | PASS |
| 16 | Review surface | PASS |
| 17 | Storefront desktop/mobile | PASS |
| 18 | Home reset unchanged | PASS |
| 19 | critical-storefront | PASS |
| 20 | recovery-staleness | PASS |
| 21 | git diff --check | PASS |

banner.four-grid: OOS — see out-of-scope.md
