# race-fix — TB-P10-T022-R13-R1-R1

## Classification
**Real product bug** (not automation-only).

## Cause
On first save without `pageId`, `saveMeta({ openWizardAfter:true })` called `setWizardOpen(true)` then `router.replace(/admin/landing-pages/{id})`. Route param remounts `AdminLandingPageComposer`, discarding in-memory wizard state.

## Fix (parent, this repair)
Before `router.replace`, persist pending open intent:

`sessionStorage['tooba.landing.openWizard.' + pageId] = { insertAt, mode:'create' }`

After authoritative `load(pageId)`, consume key and `setWizardOpen(true)`. No sleep/poll/retry.

## Runtime proof
`capture.mjs` observed:

- create remount URL: `http://127.0.0.1:3000/admin/landing-pages/01a0aeb9-25ef-7000-abcc-64c30d010719`
- `[data-testid=section-wizard]` visible after remount without harness sleeps for remount recovery

See `runtime-report.json` → `race.conclusion = product-bug-fixed-via-sessionStorage-pending-open`.
