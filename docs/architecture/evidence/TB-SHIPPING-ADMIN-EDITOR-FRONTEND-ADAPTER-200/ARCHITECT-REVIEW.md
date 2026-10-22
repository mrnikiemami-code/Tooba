# Architect review — TB200 / 2026-10-23
Disposition: REPAIR_REQUIRED; no UI wiring yet.
Reviewed main44d74284874964d24929a426094d2264c8faefd8 against ccca74258fb70c3eb64be5c743b9ee2d93235e5a. Source review only, no Architect execution.
Feature ownership/API/session separation, shared transport, exact-number text and unresolved-ancestry boundary are directionally accepted; backend199 stays closed. Reported13 tests/typecheck green do not cover the following concrete defects.

R200-01 DESTRUCTIVE OMITTED REGIONS: EditorEdits.regionGroups is optional; buildEditorDocumentBody uses (edits.regionGroups ?? []). selectApplyRequest passes only edits, not baseline. A loaded draft with groups + methodTranslations-only (or settings/type-only) edit emits regionGroups:[]; backend's required whole collection DELETES all groups/rates/titles. Merge baseline groups/rows/typed selections when no explicit group edit; explicit[] remains intentional removal. Retained ancestry must still resolve; never bypass by emptying groups.

R200-02 STALE SESSION REUSE: loadStarted sets generation1 when switching; discardConfirmed also resets1. A(first generation1)->B->A(new generation1) accepts oldA loadSucceeded. Save actions do not carry generation at all, request ids reset. Scope every completion to never-reused session epoch plus request identity; include same-draft close/discard/reopen.

R200-03 UNKNOWN OUTCOME REPLAY: selectApplyRequest blocks only saving/nochanges/notloaded. After saveFailed(outcomeUnknown) it returns ready, and edited resets statusclean. A retry may duplicate newly created groups after an already committed but unreadable response. Keep uncertainty latched; require explicit safe reconciliation/read or discard+freshload before another PUT. Do not silently discard edits or auto-replay.

R200-04 READ CONTRACT / TARGET INTEGRITY: decoder accepts zero GUID, nested type.draftId inconsistent with root, duplicate language ids/multiple-default metadata as long as referenced id exists, fewer than7 schedule days, and regex-shaped99:99:99. Request areaWrite checks kind only and can resolve the same kind to a DIFFERENT terminal id. Reject incoherent facts/target mismatches before exposure/write. Validate only canonical boundary shape/cardinality, not duplicate backend business pricing rules.

Active-language intent also needs an enforced baseline-aware boundary: currently builder serializes any edits.languageId. Reject explicit inactive/unknown edits; preserve untouched inactive text instead of filtering an attempted edit into success. This naturally shares R200-01 baseline-aware builder.

Process: three reported corrections exceed task allowance2 (not >1); record exact ledger rather than relabel compliant. Four source-size guard offenders are outside task and may be source-attributed as unchanged here, but 'pre-existing' failure reproduction on base was not independently executed. No baseline widening, unrelated fixes or proof loop for process label.

Only next task TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1, bounded current feature repair and regressions. No task201 while200 unfinished. Preserve existing wire precision, explicit clears and public feature ownership; no new JSON framework or broad API redesign. Then review and proceed to UI.
