# Architect review — TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192
Date: 2026-10-22 (Asia/Tehran)
Reviewed HEAD / worker code: d300dfc57a2c2531a01e9827f94b96a62da657ad
Disposition: PARTIAL_PENDING_REQUIRED_PRESENTATION_PROOF — not Architect ACCEPT.

## What source review confirms
Own translation/title tables and real LanguageId keys; shared context readiness extraction; partial per-language updates; retained raw text and active-registry projection; same root revision; module-owned routes and canonical Result/error composition. No Fulfillment catalog mutation or new language authority found on the reviewed surface.
Two new PostgreSQL facts exercise the real directory/domain for selected-language persistence and a metadata-versus-rate race. Worker reports 579/579 suite; Architect did not rerun .NET/PostgreSQL.

## R192-01 — required presentation endpoint and registry-transition proof missing
The implementation diff adds only:
1. Presentation_patch_touches_only_the_selected_language_and_keeps_regions_rates_and_currency
2. A_same_revision_presentation_patch_racing_a_rate_write_leaves_exactly_the_winner_state
Both call ShippingAuthoringDraftDirectory directly with a supplied HashSet of active ids; they do not execute the presentation query/command handlers or HTTP endpoints. Existing endpoint-test modifications are route-count expectations. Existing TB-188 tests cover the reused context projection, not presentation PATCH/GET with the actual registry and persisted text.

Therefore worker PASS fields for the presentation-specific one/two/three-plus matrix, active/default transitions/reactivation/completeness, interface-culture independence and admin/seller denial/validation-before-dispatch are not fully evidenced by executed presentation tests. Their source implementation may be correct; no functional production defect is asserted solely from missing tests.
Complete those already-required cases through the real presentation routes -> real authorizer -> ISender/validation -> real handlers/projection -> real directory, using labelled external Contracts fixtures and own persisted data. No need to reimplement the feature or audit unrelated modules.

## R192-02 — honest proof boundaries
No new executed raw storage negative is present for the translation composite PK/FK/check claims; migration/model presence is not a direct negative behavior test. Complete a small independent PostgreSQL storage matrix on the two new tables; preserve current migration history.
Record exact tests actually executed versus source/reused evidence, and append corrections to the worker result. Existing 579/579 and two relational facts remain valid reported history, not proof of omitted scenarios.
Domain PreflightPresentation XML says an unchanged inactive-language entry may be accepted, while executable code rejects every included inactive LanguageId. Correct that XML only to match the approved active-only PATCH/retention semantics; no new inactive-write exception.

## Next single bounded task
TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1, parent TB-192: focused endpoint/dynamic-language/proof completion and comment/evidence corrections only.
No executable production feature/schema/contract changes are authorized by that task. If tests demonstrate a real defect, return PARTIAL with exact failure and a minimal proposed repair rather than expand scope.
TB-190 and corrected TB-191 acceptances stand. No TB-193; language count is database-driven, no fixed fa/en inventory. Live merchant Localization DB/browser integration and runtime merchandise-basis/measurement remain open; TB-185 paused.
