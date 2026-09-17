# Race Fix — TB-P10-T022-R13-R1-R1

Product fix in `admin-landing-page-composer.tsx`:

- Before `router.replace` on first create, store pending wizard intent in `sessionStorage` key `tooba.landing.openWizard.{pageId}`.
- After authoritative `load(pageId)` succeeds, consume the key and open the section wizard.
- Also set `pageRef.current` during load so wizard save uses persisted page id immediately.
- No sleeps, timeouts, polling, or retries.

