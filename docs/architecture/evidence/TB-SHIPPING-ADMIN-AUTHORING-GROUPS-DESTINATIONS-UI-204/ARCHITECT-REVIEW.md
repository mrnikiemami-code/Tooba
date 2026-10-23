# TB204 Architect review — 2026-10-23

Disposition: REPAIR_REQUIRED. Independently confirmed main790c2583c12953986666b493d30122aeca603ce4 and compared to06466c7546aa2801690f888015583db958866e4e. Read group/choice/Geography models, actual picker and screen/groups wiring, focused tests and RESULT. Preserve group collection/title/rate intent and the single PUT architecture; no backend/schema changes.

## Blocking picker defects

1. All four useEffects share ONE epoch ref. On a country change the areas effect increments it, then cities effect immediately increments it again. Areas completion is therefore always stale even on a correct response. Cities captures old/empty lists.areas, excludes areas from dependencies, and passes undefined knownAdministrativeAreaIds when empty. parentAccepted then accepts ANY non-null city parent. The picker thus both loses the areas list and bypasses the promised country-parent verification. Country/language concurrent reads can invalidate each other too. Source execution order establishes the conflict; no mounted execution claimed.

2. Inactive options are displayed as ordinary selectable options; OptionSelect has no inactive disabling and Add/choiceAncestry never checks isActive. Independent Architect Node24 execution of the unchanged choice module produces a valid Country ancestry for isActive:false. Current code does not enforce active-only new picks or ancestor activity.

3. choiceAncestry takes administrativeAreaId from choice.city.parentId EVEN WHEN terminal is AdministrativeArea. Independent execution: select country+admin, choose a lawful null-admin city, then switch level back toAdministrativeArea -> terminal is the selected admin but ancestry=null. A city under a different admin can instead create a mismatched tuple. Terminal-level ancestry must depend only on that terminal's verified path.

4. Area/city/service-area placeholder onChange returns previous on null: operators cannot clear that staged parent to return to country-wide/null-admin selection, contradicting cascading semantics. Old lists also remain visible while loading a new parent, allowing stale branch rows to be chosen.

5. Blank-label OptionSelect falls back to option.code despite the adapter/report promising unavailable. Use the existing localized unavailable label; never invent a name.

The focused204 test's late-epoch claim merely matches source regex /epoch.current/ and /abort/; it never executes competing deferred reads. This is insufficient for the concrete race and must be replaced/supplemented by an executed coordinator used by the picker, without requiring a new React test framework.

## Reporting corrections

Actual shipped entry remains /admin/shipping-services/authoring/[draftId]; worker204's /drafts/{id} claim is wrong and no such new route is in its diff. Correct evidence only, not routing.
The55 count is from feature-root *.test.ts; nested adapter/session files are not matched by that glob. Do not claim every adapter/session foundation ran. Report explicit matched file set/count; no confidence sweep is requested to repair wording.
Worker reports8 focused,55 root,tsc and7 guards green. Architect did not rerun these suites. Visual/mounted/live remains NOTRUN. No process compliance is independently inferred from missing claim/finish receipts.

Only next task204-R1: bounded picker request coordination, chain/active validation, clear behavior and label correction, plus honest report amendments. Do not reopen backend199,adapter200,readbridge201,General202 or rates/week203. No205/visual task before result review; production acceptance stays PENDING.
