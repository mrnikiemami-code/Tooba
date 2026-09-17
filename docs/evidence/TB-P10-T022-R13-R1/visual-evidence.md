# visual-evidence — TB-P10-T022-R13-R1

## Runtime

Host `:5088` and FE `:3000` were reachable (HTTP 200).

## Captured

| File | Status |
|------|--------|
| slider-storefront-desktop.png | CAPTURED |
| slider-storefront-mobile.png | CAPTURED |
| slider-capture-error.png | diagnostic during Admin open failures |

## Admin Builder shots

Admin wizard screenshots (six variants / height / destination / validation) were **not fully captured** in this run due to:

1. Create-page flow requires a successful draft save before `section-wizard` opens; Playwright title-fill → save races intermittently triggered «امکان ذخیره نیست».
2. Existing editor URL open did not reliably expose an add-section control for this cookie/session snapshot.
3. Code contracts for the missing shots are covered by `admin-hero-slider-r13r1.guard.test.ts` (PASS) and source review.

Honest blocker: Admin visual PNGs for picker/settings/validation require a stable authenticated Admin session + saved page with insert CTA; storefront desktop/mobile evidence is present.

See `runtime-report.json`.
