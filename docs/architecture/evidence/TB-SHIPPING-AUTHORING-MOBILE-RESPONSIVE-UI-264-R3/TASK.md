PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3
Parent-Task: TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Mode: REPAIR_USER_REPORTED_MOBILE_AUTHORING_HEADER_AND_KEEP_TIMING_FORM_USABLE
Expected-Base-SHA: 18c291d7eb4ebec2cb5c7b6a5c97cf2a721f11fb
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Deliver ONLY the reported responsive UI correction
264/R1/R2 scheduling/connected delivery are ACCEPTED within scope; do NOT reopen them. This264-R3 is a responsive presentation task ONLY, not another scheduler/Returns repair.265 remains PLANNED/NOT ISSUED.
Read CURRENT recovery/AGENTS, design-system/UI rules and264-R2/ARCHITECT-REVIEW.md. One worker/task, no delegation, no broad/full-suite/base-parity/green repetition, no screenshot deliverable. Expected base permits legitimate issue/recovery docs descendants; no force/reset/amend/stash/rebase/unrelated overwrite.

## 2. Source-confirmed screenshot cause
src/frontend/features/admin-shipping-authoring/ui/authoring-screen.tsx:
shipping-authoring-page-header contains one flex-wrap row with back button, min-w-0 flex-1 title/badge/method, and flex toolbar. On narrow content width the back+actions consume the row, leaving title a sliver; Persian "ویرایش روش ارسال" stacks word by word and method name becomes T..., as user's screenshot shows.
Fix responsive hierarchy with existing primitives/classes:
- on mobile title/badge/name have a real full-width readable area; back/edit/save actions flow in explicit rows/groups without crushing the title;
- retain one Save action/owner, same callbacks/dirty predicate/disabled state, sticky behavior only if it doesn't swallow the mobile viewport;
- title isn't truncated into vertical word columns; long METHOD name may truncate/wrap intentionally inside its own bounded row without displacing status/actions;
- actions touch targets>=44px, readable labels, no overlap/offscreen/fixed desktop widths; don't hide required Save/Edit/Back simply to fit;
- desktop keeps compact professional arrangement, RTL and real underlined tabs. No new app framework/sidebar/navigation redesign.

## 3. Whole affected form remains usable on mobile
Check existing General/Regions and customer/carrier/processing tabs, 7-day timing editors, minute inputs, language tabs, publication confirmation, existing MessageBox and relevant save errors.
Horizontal overflow must not affect the DOCUMENT; tab-strip scrolling is acceptable when explicit and keyboard/touch usable, not clipped unseen content. Day/time rows must stack cleanly, open/close labels stay bound to their inputs, no squeezed text/numeric input.
Only adjust actual affected responsive layout, not unrelated pages or typographic redesign. No backend/schema/API/locale-registry/scheduling logic change.
Existing Toast once after authoritative action, MessageBox only blocking decision, local field validation; no new notification implementation/persistent duplicate success.
No arbitrary hardcoded FA/EN language list, preserve DB languages/direction.

## 4. Verification — brief actual browser proof, not a test loop
Use canonical served /fa/admin/shipping-services/authoring/01a1cd43-b98d-7000-b2a9-a35ab39d8058 (labelled task-owned draft) for read-only layout inspection; user service01a17d0d-... may be read only, NEVER saved/published/mutated. R1 user-service incident is already audited; do not restore/rewrite it.
Fresh browser inspect mobile360/390 (user screenshot may have a wider browser but narrow content area) and desktop1440 once after final build. Inspect actual available content width with shell/sidebar, not just an isolated component.
Verify readable horizontal title/actions, no document overflow/overlap, tabs and processing time fields accessible, key buttons>=44px; save disabled/enabled semantics unchanged. A local unsaved edit on task draft may verify layout/dirty indication then discard; no save/publication/data mutation required.
DOM bounding boxes/read-only browser inspection sufficient; no screenshot matrix/artifact. Do not call a static source regex a visual pass.
Run existing relevant authoring UI/mounted checks if applicable, typecheck and production FE build once. Don't add tests mirroring CSS. Repeat only if concrete failure/change warrants it.
Deploy ONLY changed FE to same3000 -> Host5088, verify served BUILD_ID/chunk and review route. Backend remains untouched; no backend build/restart/test necessary.

## 5. Environment and invariants
Same ONE tooba_alpha5432, FE3000/Host5088 SingleStore/store-alpha. No new database/disposable/Docker/15433/edition/port/connection change. No user or fixture business write/new product/order/payment/publication. No reset/reseed/backfill/manualDDL/businessSQL/global reclaimer.
Preserve DataProtection/CapturedPreview localON7dates checked-inOFF.255 parked/Tipax deferred. No provider call/fake waybill.
264 timing source+evidence stays accepted, original quantity/tracking/reservation/retained occurrence rules untouched.

## 6. Handoff
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3/TASK.md; evidence TASK/RESULT/RECEIPTS/result-contract under docs/architecture/evidence/TB-SHIPPING-AUTHORING-MOBILE-RESPONSIVE-UI-264-R3/.
Update AGENTS/START-HERE/recovery/263–265 plan:264 scheduling accepted, mobile correction result pending Architect/user visual review;265 still NOT ISSUED.
Report Base/Code/Evidence/Merge SHA, HEAD==origin/main, actual responsive root cause/change, widths/content geometry/overflow/touch facts, checks/build/served identity/review URL. Commit AND push; if network fails report transport separately, no repeated development/test loops.
One worker result then STOP_FOR_ARCHITECT. No automated265 continuation.
automaticNextImplementationTask: NONE
END_TOOBA_TASK
