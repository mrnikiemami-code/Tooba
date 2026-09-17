# Race Root Cause — TB-P10-T022-R13-R1-R1

## Reproduction
Create landing page (no pageId) → title filled → Add Section → `saveMeta({ openWizardAfter:true })`.

## Root cause (real product bug)
`router.replace(/admin/landing-pages/{newId})` remounts the composer and discards in-memory `setWizardOpen(true)` from the pre-navigation instance.

## Fix
sessionStorage pending-open consumed after `load(pageId)` — see race-fix.md.
