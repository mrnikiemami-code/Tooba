# CORRECTION-ADDENDUM — TB-282-R4

## Architecture / i18n guidance applied mid-task

Owner required that bug fixes must not destroy frontend/backend architecture, extensibility, or multilingual readiness.

Applied inside this task (not a separate redesign):

- Story UI copy centralized as `faStoryCopy` / `enStoryCopy` with `storyCopy(locale)` and `resolveStoryUiLocale(pathname)` (WorkspaceMessages pattern), instead of scattering Persian literals or inventing a parallel i18n framework.
- Existing AppDataGrid / legacy-grid-bridge end-pin for `actions` retained; Story only constrains column order via `normalizeStoryColumnOrder` for title-first / actions-last.
- S2 proof uses Contracts fakes against the real `StoryProductOptionsAdapter` — no Host join inventing Catalogue ownership.
- VendorShell / `story.view` / `/vendor-panel/stories` unchanged; no parallel seller panel.
