PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-AREA-CONTRAST-AND-EDITOR-CHECKS-211
Parent-Task: TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: a2e274d1275847305b6997d090380e0bd3295eae
Mode: BOUNDED_AREA_CARD_CONTRAST_EDITOR_REGRESSION_AND_STOREFRONT_STATUS
Hard-Timebox: 20min, stop implementation12; final8 validation/evidence/commit/push.
One bounded correction maximum; no confidence/retry loop. Honest PARTIAL if incomplete.

USER
UI is better. Alternate DELIVERY AREA cards white/very-light-gray to distinguish dense areas.
Run editor checks quickly; answer whether Storefront is done.
Read TOP AGENTS/recovery, applicable frontend skill/architecture standard,210/210-R1 reviews.
Standing local nonproduction authorization persists; no new approval questions.
No accepted190–210 replay/redesign. Positive feedback is not blanket production acceptance.

A. MINIMAL CONTRAST
Existing ui/authoring-groups-editor.tsx maps groups with position, currently identical
rounded-xl border border-border bg-surface-elevated p-3.
Alternate WHOLE area card background by current display position, VIEW AND EDIT, white/elevated then
very light neutral gray using existing utility/token. Not alternating cost rows. No child opacity,
saturated color, new globalCSS/designsystem/token/model/persistence. Keep input backgrounds/readability,
errors/focus, shared widths/tabs/week, ids and save logic. Reorder/add/remove parity follows display order.
No tests mirroring CSS source.

B. CHECKS ONCE
Enumerate ALL test files RECURSIVELY under features/admin-shipping-authoring, including nested api/model/
session. Pass actual enumerated paths to Node runner; root glob is NOT full feature coverage.
Record exact file list/count and passed/failed/skipped. Don't suppress/weaken failures.
One npm run typecheck; existing import-boundary/feature-boundary/flat-folder/test-discovery guards once.
No backend/Host/solution/full discovery/source-size loop. Four historical unrelated size failures untouched.
If genuine feature failure, one small correction within remaining time; otherwise exact blocker/PARTIAL.

C. SHORT REAL VISUAL, ZERO BUSINESS WRITES
Use running FE3000/Host5088 and isolated browser. Do not reload user unsaved tab.
http://127.0.0.1:3000/admin/shipping-services/authoring/01a16eeb-70b3-7000-87c2-564c82835496?mode=view
Read actual current data; priorrev2 is not authority to reset it. If only one stored area, add SECOND
LOCAL UNSAVED area in isolated Edit to inspect contrast. No Apply; exit through normal discard.
One desktop screenshot with >=2cards plus computedbackgroundcolors, one390px nooverflow check.
No HTTP POST/PUT/PATCH/DELETE/seed/newdraft/migration/reset/backendbuild/restart/package install.
No new browser harness loop. If runtime unavailable record blockage, never fabricate visual proof.
209 live composite save accepted; no repeated save needed for background change.

D. STOREFRONT QUESTION: SOURCE ANALYSIS ONLY <=3min
Distinguish EXISTING storefront/checkout from integration of this NEW draft editor.
Trace exact files/members: editor persistence; any actual publish/activation/registry bridge to runtime
quote; existing storefront/checkout shipping choices consumer; whether draft type/areas/fees/week reach it.
Short implemented/absent/unknown matrix with source evidence and ONE smallest next delivery slice.
No absence claim from incomplete/zero-result search. Don't invent activation authority/enable TB185 or
defaultOFF, implement quote engine, alter Shopeiva DOM/CSS. Not full storefront implementation in211.
Feature tests passing != all parts production-ready.

DELIVERY
Scoped commit/push/fetch parity. Persist verbatim task and
docs/architecture/evidence/TB-SHIPPING-ADMIN-AREA-CONTRAST-AND-EDITOR-CHECKS-211/RESULT.md
with actual checks/counts, screenshot/colors, no-write observation, concise storefront status.
Update TOP recovery +AGENTS with feedback and exact next state; preserve histories/user data/sharedruntime.
Return worker result; STOP_FOR_ARCHITECT; Automatic-Next-Task NONE; no further task envelope.
