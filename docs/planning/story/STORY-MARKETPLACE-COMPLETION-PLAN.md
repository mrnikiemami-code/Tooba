# CURRENT — R14 PARTIAL; R15 REJECTED-TRANSPORT CLOSURE ISSUED — 2026-11-20

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R14/ARCHITECT-REVIEW.md
Task: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R15/TASK.md
R14 code cbba1b3e247426ead2babd4f4dd0fae1755fd166 / evidence 8c6fe36325f181aba213fb655d735cc48c7384c7.
Accepted: batch scope/epoch stop, per-row result panel, R13 date layout. Remaining: rejected fetch escapes before result Dialog; empty-200 test does not prove connection-loss handling. R15 repairs only this and proves actual grid-selected rejection/exact recovery.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R15
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
No production/user acceptance. Host5088 skip-bootstrap=1; FE3000 next-dev; Html404 production build blocker unchanged. No live mutations/grants/seeds/backend changes authorized. Preserve VendorShell, language registry, date/retry rules, Returns/payout/user work and R6 effects UNKNOWN.
Architect reviewed source/evidence, did not rerun Windows runtime.

# HISTORICAL — prior records preserved below

# CURRENT — R13 B ACCEPTED; ONLY R14 BULK CLOSURE ISSUED — 2026-11-20

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R13/ARCHITECT-REVIEW.md
Code41bbd1e97e526641ded1260a741ec6d1e8129d38 / evidence eae4b3196823a874b04b5987a87e5f4504584f89. No-end END-label layout and single-operation wiring retained. Full bulk PASS not accepted: result.items dropped into count toast; batch continues captured old scope after per-request stale_epoch; no actual multi-selected-grid command test.
Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R14
Expected-Base-SHA: eae4b3196823a874b04b5987a87e5f4504584f89
Exact envelope: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R14/TASK.md
Only two repairs: stop batch on epoch/actor/scope change with untouched rows not-started; visible per-title results and exact unresolved retry. One actual selected-grid mixed/partial test, no whole task-series rerun/redesign/backend change.
One worker tooba-worker-01/tooba-main, issued NOT claimed/running.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R14 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
Preserve VendorShell/R5–R13/owner fences/language registry/Returns/payout/user work. ZERO live business writes/migrations/seeds/grants/screenshots/delegation/polling. Html404 production release blocker/R6 UNKNOWN retained.

# HISTORICAL — superseded by CURRENT above

# CURRENT — OWNER REQUESTED ADMIN SINGLE/BULK ACTIONS; ONLY TB-282-R13 ISSUED — 2026-11-20

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R13
Expected-Base-SHA: d6956b5f4363711aeba12ce7281f00fa42f46b62
Exact envelope: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R13/TASK.md
Owner requires admin single+selected-row deletion/approval/rejection from grid. AppDataGrid already has bulkActions/pageSelectionOnly; Story has not wired review/bulk controls. R13 uses existing grid and per-row reviewed commands/version/exact retry, clear confirmations/reject reasons/partial results; no new bulk backend framework or grant.
Owner rejects no-end at global far corner: move beside END label inside equal-height end header; retain desktop aligned date/clock rows and mobile layout, disabled mounted controls/values/null semantics.
R12 bounded alignment acceptance preserved; this is explicit further owner UX/operation request. One worker tooba-worker-01/tooba-main, issued NOT claimed/running. No screenshots/delegation/polling/live mutations/schema/seeds.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R13 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
Preserve R5–R12/VendorShell/language registry/Returns/payout/user work. Existing production Html404 build blocker and R6 effects UNKNOWN remain.

# HISTORICAL — superseded by CURRENT above

# CURRENT — R12 LAYOUT REPAIR ACCEPTED; AWAIT OWNER REVIEW — 2026-11-20

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R12/ARCHITECT-REVIEW.md
Code d07ce9d90c0fefca6df53613ffc4a39ac167a393 / evidence 99c1e1181750c5c32a7e71412c9534d3e7f464bc.
Decision ACCEPT_LAYOUT_REPAIR_FOR_OWNER_REVIEW. Shared create/edit header checkbox and stable equal date/time columns accepted from source/worker receipts. Desktop day/time diff0; mobile start-then-end, no overflow reported on active admin forms; seller shares component, not separately measured.
State: AWAITING_OWNER_REVIEW
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW
No R13 automatically. Owner rechecks no-end toggle. ZERO backend/live save scope. Existing Story functional/owner acceptance and production Html404 build blocker remain unchanged; frontend next-dev. Architect reviewed source/evidence, did not rerun Windows live geometry.
Preserve VendorShell, local recovery, date/retry rules, Returns/payout/user work and R6 effects UNKNOWN.

# HISTORICAL — superseded by CURRENT above

# CURRENT — OWNER TOGGLE LAYOUT FAILURE; ONLY TB-282-R12 ISSUED — 2026-11-20

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R12
Expected-Base-SHA: 882f7f7bad03d29e1cba00cf2f34bfcbdf8874fe
Exact envelope: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R12/TASK.md
Owner screenshot shows end fields below start after unchecking no-end. Source shared create/edit has checkbox min-height row only above end controls + nested labels. Only a small presentation repair: header checkbox, aligned two-column dates/clocks desktop, single-column mobile, stable disabled end controls, valid accessible markup. No lifecycle/date semantics/backend/auth changes.
R11 local-trial acceptance retained with prior proof/build limits; this actual owner layout failure is not a new feature phase.
One worker tooba-worker-01/tooba-main; issued NOT claimed/running. Verify served1440/390 toggle geometry, no screenshots/live saves. Preserve user data/Returns/payout/R5–R11 safeguards.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R12 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
Existing production Html404 build blocker remains; no production acceptance claimed.

# HISTORICAL — superseded by CURRENT above

# CURRENT — R11 ACCEPTED FOR LOCAL OWNER TEST; RELEASE BUILD BLOCKED — 2026-11-20

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R11/ARCHITECT-REVIEW.md
Code77fcb2630525fa334508d3e5ffdbc7491f63e3f9 / evidence 1da30eeb6fe211b24b7118022a650ccd321ec7dd.
Decision ACCEPT_FOR_LOCAL_OWNER_TEST_WITH_DECLARED_LIMITS. Delete shop replay/list-detail controls, effective expiry status and strict end validation source accepted for local trial. Source and worker evidence reviewed; Architect did not rerun Windows serving/tests.
State: AWAITING_LOCAL_OWNER_TEST
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW
No R12 automatically. Owner tests actual photo/video preview/duration, dates save/reload, seller submit/admin approval/public playback and intentional test-record delete.
Next3000 next-dev; production build Html404 prerender BLOCKED, no new BUILD_ID; absence of Story next/document import does not fully diagnose root cause. No production readiness/user acceptance/full A-C proof certification claimed.
Host5088 skip-bootstrap1 DLLD5DF9070…E0599BEE health/ready/store-alpha worker-reported. FE5 includes source-regex player assertion, not actual ended runtime proof; receipts route200 not active dialog geometry. Detailed limits in review.
ZERO live business writes by worker; owner actions create/delete real data. Keep R6 effects UNKNOWN/bg-white disclosure, VendorShell, R9 recovery, languages, Returns/payout/user work.
URLs /fa/admin/stories ; /fa/vendor-panel/stories ; /fa

# HISTORICAL — superseded by CURRENT above

# CURRENT — R10 PARTIAL; ONLY TB-282-R11 ISSUED — 2026-11-19

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R10/ARCHITECT-REVIEW.md
Code a58172221170def6a9ef7c316474289fb9a4d6c5 / main 400ccbf1cade071be3ebd72ab95171360475f6fb. Preserve preview/video-duration/draft-window/delete/status/readiness progress; full R10 PASS NOT accepted.
Confirmed admin-for-shop delete replay scope mismatch, expired Active label falsely “در ویترین”, missing list delete, generic pending-delete route completion and date validation mismatch. R10 FE4/4 is date/copy units, not mounted preview/delete/submit proof; backend8/8 Domain facts, not real delete/replay path. Production Next build failed Html404; next-dev serving disclosed, no new BUILD_ID.
Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R11
Expected-Base-SHA: 400ccbf1cade071be3ebd72ab95171360475f6fb
Exact envelope: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R11/TASK.md
Only finish existing concrete defects + actual affected mounted/owning-path tests and final live read-only role screens. No new phase or R1–R10 replay. One worker tooba-worker-01/tooba-main, issued NOT claimed/running.
Preserve VendorShell/registry/owner/version/replay, R9 skip-bootstrap memory recovery, Returns/payout/user work. ZERO migration/live business writes/seeds/grants/screenshots/delegation/polling. R6 effects UNKNOWN.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R11 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW

# HISTORICAL — superseded by CURRENT above

# CURRENT — OWNER TRIAL FOUND FUNCTIONAL GAPS; ONLY TB-282-R10 ISSUED — 2026-11-19

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R10
Expected-Base-SHA: 06e3bd558f0b9a2f323f9dbab596188580edb798
Exact envelope: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R10/TASK.md
Owner reports no Story delete, no selected-file media preview, photo duration5 input also on video, no authorable start/end publication window, repeated/raw statuses and submit error. Screenshot VendorShell says invalid context. These are actual OWNER failures; R9 local recovery acceptance retained, owner UX/function acceptance NOT granted.
Source confirmed unconditional photo-duration input/no local preview, stacked status cell, admin-only scheduling and no whole-Story delete route. One consolidated R10 implements A media/duration, B Jalali start/end including seller proposed window under admin approval, C real scoped Story deletion + separate slide removal, D single truthful status/legal choices, E actual submit/scope failure diagnosis+repair. No new broad auth/UI architecture wave.
One worker tooba-worker-01/tooba-main; issued NOT claimed/running. Preserve R5–R9 exact retry, VendorShell, ownership/review policy, language registry, Returns/payout/user data. R9 skip-bootstrap InMemory recovery retained. ZERO worker live business writes/migrations/seeds/grants/screenshots/delegation/polling.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R10 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW

# HISTORICAL — superseded by CURRENT above

# CURRENT — STORY READY FOR OWNER REVIEW; R9 LOCAL RECOVERY ACCEPTED — 2026-11-19

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R9/ARCHITECT-REVIEW.md
Code 218a5e52d0a2bc6f518eb1e9d52b30f5cc6a4179 / implementation-evidence e5bcb134ad0c415fe547744b2f962dada187cb0d.
Decision ACCEPT_FOR_OWNER_REVIEW_WITH_DECLARED_LOCAL_LIMITS. Development/InMemory projection rebuilt from existing AccessControl assignments under skip-bootstrap; source/evidence reviewed, Architect did not rerun Windows serving.
Worker reports admin Story list 200 and assigned seller 200, unassigned 403; active admin/VendorShell /new at390/1440 no overflow. R8 403 blocker resolved for tested assigned actors. No new Story UI phase/task.
State: AWAITING_OWNER_REVIEW
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW
Next step: OWNER tests actual create/media/product/edit/reload, seller submit -> admin moderation -> public playback. No live Story/Media POST by worker; no full user/commercial/authorization certification.
Host41552 skip-bootstrap=1 DLL54D3DE37…EEDC3; Next34656 BUILD_ID7nEINy82sBP3x0f0H6xdw (R8 FE unchanged). URLs /fa/admin/stories ; /fa/vendor-panel/stories ; /fa.
Party-only/unassigned seller remains denied; known unrelated storefront bg-white failure and R6 startup-effects UNKNOWN preserved. No R10/proof-loop automatically. Preserve VendorShell, existing owner/actor fences, Returns/payout backlog/user work.

# HISTORICAL — superseded by CURRENT above

# CURRENT — R8 BOUNDED ACCEPTED; LIVE AUTH BLOCKED; ONLY TB-282-R9 ISSUED — 2026-11-19

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R8/ARCHITECT-REVIEW.md
Code bd23deeec736746170ed6fe101cd880ba1b2e33d / main dcd5e4e383f819924b182d421ffcb7dce32c1857. R8 recovered-create adoption and read-only dev-context bind accepted within recorded limits; no new Story UI phase.
Actual live Story list still 403 admin.authorization.denied; dev-context/login 200 is not readiness. InMemory auth loses process tuples on restart; snapshot Find alone does not establish tenant#view.
Only task TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R9; docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R9/TASK.md. Expected-Base dcd5e4e383f819924b182d421ffcb7dce32c1857. Rebuild ONLY Development InMemory projection from verified existing membership/assignments through owning Contracts; no persistent grants/identity/business writes or bootstrap/seed. If durable authority absent, explicit blocker instead of invented grant.
One worker tooba-worker-01/tooba-main; issued NOT claimed/running. Host refresh skip-bootstrap=1; preserve user work, Returns, payout, VendorShell, language registry. No screenshots/delegation/polling. R6 effects UNKNOWN.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R9 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW

# HISTORICAL — superseded by CURRENT above

# CURRENT — R7 BOUNDED PROGRESS ACCEPTED; ONLY TB-282-R8 ISSUED — 2026-11-19

Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R7/ARCHITECT-REVIEW.md.
Reviewed code 419d62d46bd3a547f275bd60ec44e7040afd6189 / main 8dc12e7cffed98ee40457639290ed3ae98989a70. Retain create/add/preview/dirty-back/labels and cookie hardening progress; delivery remains PARTIAL.
Confirmed remaining defect: successful recovered create clears pending but does not adopt owning draft in /new; later Save/Add can mint another create. Initial retention test did not click replay.
Safe live admin review blocked by dev-context in-memory snapshot missing under skip-bootstrap despite persisted BFF login. Only a bounded read-only existing-identity/tenant initialization/read repair is permitted, no registration/grants/seeds.
Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R8
Expected-Base-SHA: 8dc12e7cffed98ee40457639290ed3ae98989a70
Exact envelope: docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R8/TASK.md
One worker tooba-worker-01/tooba-main; issued NOT claimed/running. Only recovered-create adoption and safe owner-review runtime; no redesign or new wave.
R6 startup effects remain UNKNOWN. Preserve user work/Returns/payout/VendorShell/languages. ZERO new migration/seed/live business writes. Host refresh requires TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R8 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW

# HISTORICAL — superseded by CURRENT above

# CURRENT — TB-282-R7 WORKER PASS; AWAIT ARCHITECT AND USER REVIEW — 2026-11-19

Worker PASS within issued A–C. BUILD_ID wxqCG9BP0aNO3_zyiNnnQ. ZERO migration; ZERO live Story/Media write.
NOT Architect ACCEPT, NOT owner acceptance. URLs: /fa/admin/stories, /fa/vendor-panel/stories, /fa.
activeTask: NONE; automaticNextImplementationTask: NONE.

# HISTORICAL — TB-282-R7 ISSUED; WORKER PASS ABOVE

# HISTORICAL — TB-282-R6 PARTIAL; R7 ISSUED

# HISTORICAL — TB-282-R6 WORKER PASS; LATER PARTIAL

Worker PASS within issued A–D. BUILD_ID yq3D1_b-sYRUJXbreLqoj. ZERO migration; ZERO live Story/Media write.
NOT Architect ACCEPT, NOT owner acceptance. URLs: /fa/admin/stories, /fa/vendor-panel/stories, /fa.
activeTask: NONE; automaticNextImplementationTask: NONE.

# HISTORICAL — TB-282-R6 ISSUED; WORKER PASS ABOVE

# HISTORICAL — superseded by CURRENT above

# CURRENT — STORY READY FOR OWNER TEST; NO IMPLEMENTATION TASK ACTIVE — 2026-11-19

Review: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R5/ARCHITECT-REVIEW.md
Reviewed code 352a252ad9cf21645cf54c2d71a51e91c4da9d98 / implementation-evidence main 71e489bcd7a0a1b871dad6bcec86bca1050c6e96.
Decision ACCEPT_FOR_OWNER_TEST_WITH_DECLARED_LIMITS. Product names are bound per slide; shared edit uses existing update-item with preserved media; actual Catalog query >40 proof present at EF InMemory scope. No further automatic repair task. Full A-D proof certification/user/commercial acceptance NOT claimed.
State: AWAITING_OWNER_TEST
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_USER_REVIEW
User requested fast handoff: now user tests admin creation/main-or-shop, seller own-shop/product slides+edit/reload, submit -> admin moderation -> public playback/product/shop link, mobile grid/form. Title first/actions last existing logical-end pin must be checked in real active UI.
Important evidence limits: shell-only served geometry is NOT active editor/grid proof; retention test is direct executor, not mounted pending-card/no-toast; detailed limitations in review. Do not reopen task series solely to recertify these before owner feedback. Actual owner failure -> one narrow justified repair.
URLs http://127.0.0.1:3000/fa/admin/stories ; http://127.0.0.1:3000/fa/vendor-panel/stories ; http://127.0.0.1:3000/fa
Worker reported Next BUILD_ID SpxfHsxtpvAChwrYh4xI7 / Host29956; Architect source review only, no live writes/tests. No business fixtures/seeds/migrations. Existing VendorShell/story.view, Returns/payout backlog/separate key state preserved.

# HISTORICAL — superseded by CURRENT above

Worker PASS within issued A–D (F1 per-slide CtaProductTitle, F2 mounted update_item + retention + negative control, C real Catalog >40). Evidence: docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R5/.
NOT Architect ACCEPT, NOT user acceptance. User-Review-URLs: /fa/admin/stories, /fa/vendor-panel/stories, /fa.
activeTask: NONE; automaticNextImplementationTask: NONE; STOP_FOR_ARCHITECT_AND_USER_REVIEW.
BUILD_ID SpxfHsxtpvAChwrYh4xI7; Host5088/Next3000; ZERO migration; ZERO live Story/Media write.

# HISTORICAL — TB-282-R5 ISSUED; WORKER PASS ABOVE

# HISTORICAL — TB-282-R4 REVIEWED; 282-R5 WAS ISSUED

Reviewed implementation 35b9ffc6406a770147ddfd8fc1ef514f0db20a1e; fresh main/evidence 08f73aaa7dce43c73ff25d4a67c8ffec6a394314.
Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R4/ARCHITECT-REVIEW.md.
Retain compact save/localized file/shared slide Dialog/update-item wire and upload dispatch gate progress. Full A-E PASS not accepted: F1 row product label borrows current editor selection and lacks stored target name hydration; F2 actual save/no-extra-write, malformed-response mounted proof, true negative control, real grid/saved-view/geometry and owning Catalogue query proof remain incomplete. Finish existing narrow obligations only; no new UI/backend phase.
Correction: actions logical-end pin already comes from existing legacy-grid-bridge, so missing sticky on Story column alone was not proof of missing pin.
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
Owner may inspect /fa/admin/stories, /fa/vendor-panel/stories and /fa; no user/commercial/complete connected acceptance claimed. Worker-reported Next build VKOLCT7WFUKHA67tlaFV-; Host18724; Architect source/evidence review only.
Existing VendorShell /vendor-panel/stories/story.view retained; seller fixed shop/products, admin shop/main placement and moderation. ZERO migration/live-write scope; Returns/payout backlog/separate key state unchanged.

# HISTORICAL — superseded by CURRENT above

Read docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R4/TASK.md and evidence TB-...-282-R4/{RESULT,RECEIPTS,COMPLETION-LEDGER,user-review}.
Status PASS within issued A–E (Orders-style list, shared merchant editor fa/en catalogs, per-slide update_item, S1 bind every send, S2 >40 catalogue proof). Host5088/Next3000 BUILD_ID VKOLCT7WFUKHA67tlaFV-; ZERO migration; ZERO live Story/Media write.
NOT Architect ACCEPT, NOT user acceptance. User-Review-URLs: /fa/admin/stories, /fa/vendor-panel/stories, /fa.
Expected-Base f35f476c51346858bb1d61e96bb5e7d2068c5a79. automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT_AND_USER_REVIEW.
Seller remains VendorShell /vendor-panel/stories /story.view — no parallel panel.

# HISTORICAL — TB-282-R4 ISSUED; WORKER PASS ABOVE


# HISTORICAL — superseded by CURRENT above

Reviewed implementation 70c5dd36a2a9508f7a95edce1ee575c0fda9dfae / evidence 9047660042696e205c04cf161c5c035abd912840.
Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R3/ARCHITECT-REVIEW.md.
Retain malformed-2xx retention, explicit storage barriers and real published-catalogue search/page/count source progress. Worker PASS does NOT close issued S1/S2: upload_add executor bypasses the allowed-route gate; new R3 tests do not mount the workspace or run the claimed negative control; required >40-product owning-query/paging/shop-isolation proof is absent. Only finish these existing obligations; no UI redesign/backend wave/Gate0/Media reopening.
activeTask: NONE
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT
Current screens may be visually inspected at /fa/admin/stories, /fa/vendor-panel/stories and /fa; full connected/user/commercial acceptance NOT claimed. Worker-reported Next build fFmkskCMJiO4PrZyHPO6l, Host36320; Architect source review only, no live writes/tests.
Seller remains inside existing VendorShell /vendor-panel/stories/story.view, no parallel shell/menu/login. Returns acceptance, payout backlog and separate key state unchanged.

# HISTORICAL — superseded by CURRENT above

Worker PASS within issued S1–S2 for TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R3: verified bound response retention + real published catalogue search/page/count; Host5088/Next3000 BUILD_ID `fFmkskCMJiO4PrZyHPO6l`; ZERO migration; ZERO live Story/Media write.
NOT Architect ACCEPT, NOT user acceptance. User-Review-URLs: `/fa/admin/stories`, `/fa/vendor-panel/stories`, `/fa`. Expected-Base `9776ab9623aadb2d58f001113ce8269039b11a94`. automaticNextImplementationTask NONE; STOP_FOR_ARCHITECT_AND_USER_REVIEW.
activeTask: NONE

# HISTORICAL — TB-282-R2 REVIEWED PARTIAL; 282-R3 WAS ISSUED

Reviewed code 2085d39d5641c387008edccaff5294b18ca8f4ae; final docs head 9776ab9623aadb2d58f001113ce8269039b11a94.
Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R2/ARCHITECT-REVIEW.md. Parent PARTIAL closed by R3 worker PASS above.

# HISTORICAL — superseded by CURRENT above

Reviewed feature 98d3e1fcdaf74227e22034325cbdef639b0d5d78 + stub compile fix 77cb3d9bf12c7ec9cbd70c28e0cc9515c1937789; final d0c09e2ed340bdf82c9a6e4ae8eaeb8f7ed96d58.
Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R1/ARCHITECT-REVIEW.md. Worker PASS is NOT accepted as full F1-F5. Retain routes/name-picker/editor/action controls/scoped views/product href and explicit Gate0-owned ids progress.
Only issued task TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R2; exact docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R2/TASK.md. One worker tooba-worker-01/tooba-main, await result/review; issued is not running.
Four closure groups: B1 recovery BUTTON must dispatch frozen command and resume correct upload child; B2 lawful role-scoped lookup/date picker+zone; B3 actual scope/grid/shell behavioral proof (no production test bypass); B4 real platform identity (no fake seller scope) and FINAL builds/current serving proof.
Existing template VendorShell /vendor-panel/stories/story.view mandatory, shared feature only; no new panel/menu/login/admin credential borrowing. DB registry languages, real picker/navigation, correct dates and plain merchant copy required.
ZERO migration/live business writes/seed/key operation. User receives usable current build URLs only after evidence; user performs first live new-story act. Returns acceptances/payout backlog/history/user work unchanged.
Previous PASS/current-status paragraphs below are historical. Worker PASS != Architect/user acceptance. automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Reviewed code b0da0ab9d4ca593b9af17b0983802c2add936503; actual final head ebb921235456c6c75c88a06abfa35fcefa0b00db (normal merge/docs history preserved).
Read docs/architecture/evidence/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282/ARCHITECT-REVIEW.md. Worker PASS A-F is NOT accepted as complete. Retain compact grid/query/shell/Dialog/toast/public-player progress; real editor, name/destination pickers, scoped recovery and real mounted proof remain due.
Only issued task TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R1; exact docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R1/TASK.md. One worker tooba-worker-01/tooba-main; issued is not claimed/running. Await result/review; no parallel/automatic next task.
R1 closes existing F1 work protection, F2 complete merchant editor, F3 actual actor/shop query-view scope, F4 schema-free keyed platform Media upload, F5 real React/VendorShell/player mounted proof. One residual batch loser cleanup must preserve unrelated context entries. ZERO migration/live business write/seed/key operation.
Seller integration MUST stay in purchased-template EXISTING VendorShell /vendor-panel/stories/story.view; shared feature, no new panel/menu/login or admin credential borrowing. See SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
Merchant must select shop/product/file by human labels, preview and correct/re-submit content; source-regex tests are not mounted behavioral proof. User receives actual review URLs after real closure; worker does no live mutations.
Returns acceptance/payout backlog/user work/key state unchanged. Worker PASS != Architect/user acceptance. automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Source reviewed fa32794e059f8fdf52858cabfcc83494be05ed03; final docs head c9ba2296a3adfbb6f6ed2638df65957f4c3f391c.
Read docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R5/ARCHITECT-REVIEW.md.
R5 A1/A2/A3 and Media guard scoped progress accepted; complete 281 closure NOT claimed. Residual winner full identity + losing-entry containment + real EF concurrency branch are Gate 0 of the SAME connected task 282 (no standalone R6 issued).
Only issued task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282; exact docs/ai/tasks/TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282/TASK.md. One worker tooba-worker-01/tooba-main; issued does not mean claimed/running. Gate0 FIRST; then shared Orders-style list/editor/media/review/public playback. Await result and review; no parallel task or automatic continuation.
UI delivery is NOW EXPLICITLY ISSUED within 282, contingent on its first gate. Existing prior NOT ISSUED blocks below are historical, not current instructions.
Mandatory purchased-template seller integration: existing VendorShell /vendor-panel/stories with story.view navigation/theme/auth/shop context; no parallel panel/menu/login. See SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
Product owner needs merchant-friendly language, shared design-system Dialog, existing react-toastify, real owner-scoped media/pickers, exact retry, honest public playback. Deliver user-review URLs + short Persian checklist. Worker does no live business writes; user starts actual new-story trial.
ZERO migration/seed/live Story-Media mutation/key operation; Returns acceptances/payout backlog/user data preserved. Worker PASS != Architect/user acceptance. automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Code reviewed 1c7a7789344f1367875f882b687aa608dd5a0fdd; final source/docs head aad32713b7486c429dbbfb5e948e95e30eb6bb67.
Read docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4/ARCHITECT-REVIEW.md. R4 worker PASS is NOT accepted as full A/B/C: owner edit replay missing, update intent incomplete, schedule replay after stale gate, duplicate arbitration absent, ambient/enlistment and relational claims exceed assertions.
Only issued task TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R5; exact docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R5/TASK.md. Preserve R4 progress; close specific source defects and truthful focused tests, no new product design/schema/live data.
One worker tooba-worker-01/tooba-main, await this result; issued is not running. No automatic next task or parallel work.
Shared Orders-style UI remains PLANNED NOT ISSUED until owning-path review closes. Mandatory seller integration: purchased-template EXISTING VendorShell /vendor-panel/stories with existing story.view menu/auth/context; shared feature, no new seller panel/menu/login. See SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
Returns acceptance/payout backlog/user data/key state untouched. Worker PASS != Architect ACCEPT != user acceptance. automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Reviewed code 46c639c4c6cdd5f1ae0e3af2afbeb03f0dcf3dca; actual final head 14b2395eea3585b7e12af71d135c84c77ce7f9c2 is its docs-only addendum.
Read docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3/ARCHITECT-REVIEW.md.
Only issued task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4. Exact TASK.md: docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R4/TASK.md.
Close A full version/occurrence transport (including broken delete/reorder), B actual Media save/ack failure proof, C canonical rollback-only PostgreSQL facts. No new product scope, migration, seed or live business mutation. Retain landed R3 checks/public attribution/scoped schema; old 57/57 is not missing behavior proof.
One worker tooba-worker-01/tooba-main; issued does not mean running. Await result and review; no task chain.
Seller Story UI MUST use purchased-template EXISTING VendorShell and /vendor-panel/stories, existing story.view/menu/auth/context. No parallel panel/menu/login. See SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
Shared Orders-style UI delivery is PLANNED NOT ISSUED. Worker PASS != Architect ACCEPT != user acceptance. Returns acceptances/payout backlog preserved; no key operations.
automaticNextImplementationTask NONE.

# HISTORICAL — superseded by the CURRENT checkpoint above

Reviewed source 4c086d32346ce2b6f695c254d7fa58db98c4f322. Read docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2/ARCHITECT-REVIEW.md.
Only issued task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3; exact envelope docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R3/TASK.md. One worker tooba-worker-01, tooba-main. Await its result; issued does not mean claimed/running.
Finish existing owning-path gaps and actual recording-store/PostgreSQL rollback proof. Retain landed work. No new product design, live business writes, seed, parallel task or automatic continuation. UI remains PLANNED NOT ISSUED pending review.
Mandatory owner requirement: Story belongs in the PURCHASED TEMPLATE'S EXISTING seller panel: vendor-panel/layout.tsx -> VendorShell, existing story.view navigation /vendor-panel/stories. Shared Story feature, existing shell/auth/context; no new seller panel/menu/login. See docs/planning/story/SELLER-PANEL-INTEGRATION-REQUIREMENT.md.
Returns accepted history and payout-method/batch backlog preserved; no key retirement authorization.
automaticNextImplementationTask NONE.

# HISTORICAL — superseded by the CURRENT checkpoint above

## یادآوری اجباری معمار — استوری داخل پنل Seller قالب (۱۹ نوامبر ۲۰۲۶)

صاحب محصول تأکید کرد استوری باید در پنل فروشندهٔ موجود قالب تکمیل شود، نه پنل تازه. سورس بررسی شد: `vendor-panel/layout.tsx` -> `VendorShell`؛ منوی «استوری‌ها» با مجوز `story.view` و مسیر `/vendor-panel/stories` از قبل وجود دارد؛ صفحه همان feature مشترک را با قابلیت فروشنده می‌گیرد. مرجع اجباری تسک UI آینده: `docs/planning/story/SELLER-PANEL-INTEGRATION-REQUIREMENT.md`. پوسته، ناوبری و auth/context موجود حفظ شوند؛ منو/پنل/ورود موازی ساخته نشود. این فقط یادداشت است؛ تسک فعال R2 و انتظار نتیجه‌اش تغییر نکرده، UI هنوز NOT ISSUED و automaticNextImplementationTask NONE.

R1 head ae947c863867437c13008029971a77eeb90edd41 independently source-reviewed. See R1 ARCHITECT-REVIEW and exact `docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2/TASK.md`. Keep completed fixes; finish original media/command/query/proof scope. No new features or automatic task chain. Shared UI delivery waits for reviewed backend completion. Returns acceptances and future payment-method backlog preserved.

# HISTORICAL — 281-R1 repair checkpoint

Parent worker head 860c6a2c72842c8971d8b9f9885e26cdb0f3f657 independently source-reviewed; see parent ARCHITECT-REVIEW B1–B7. Complete original delivery-one scope and regression proofs in `docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1/TASK.md` before issuing the connected shared UI delivery. No automatic next implementation. Existing owner requirements, parent evidence and accepted unrelated work remain preserved.

# HISTORICAL — first delivery issuance checkpoint

Owner requested starting the first task and waiting for its result before issuing the next. Exact envelope: `docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281/TASK.md`. Active worker tooba-worker-01. This task's bounded permission now supersedes the original analysis-only execution state for delivery ONE only; the original plan below is preserved as historical product context. No automatic continuation. UI/shared-list delivery remains planned, not issued. automaticNextImplementationTask NONE.

# برنامهٔ تکمیل استوری مارکت‌پلیس و پنل مشترک فروشگاه/ادمین

تاریخ: ۱۹ نوامبر ۲۰۲۶
وضعیت: گزارش و طرح پیشنهادی بر اساس درخواست مالک؛ اجرای کد و تسک صادر نشده است.
مبنای سورس: `e242d4dd9cc69c9094187caf838bb60d0cd87c70`
گزارش مکمل: SOURCE-AUDIT-2026-11-19.md
معیار محصول: docs/product/OPERATOR-UX-ACCEPTANCE.md

## ۱. هدف تحویل

کاسب بدون دانستن URL، MediaAssetId، CTA، locale یا نام وضعیت فنی بتواند:
فروشگاه خودش -> استوری جدید -> انتخاب عکس/ویدیو -> پیش‌نمایش -> ذخیرهٔ پیش‌نویس یا ارسال برای تأیید.
ادمین همان فرم و ابزار را داشته باشد، به‌علاوهٔ صف بررسی، تأیید/رد و انتخاب فروشگاه برای ثبت از طرف آن.
نتیجه در ویترین عمومی واقعاً قابل‌پخش باشد و به فروشگاه درست منتسب شود.

«یک پنل مشترک» یعنی یک فهرست/ویرایشگر و قرارداد رفتاری مشترک با دامنهٔ داده و مجوز سمت سرور؛ نه اعتماد به capabilities مرورگر یا تکثیر دو کدبیس. تفاوت اصلیِ نقش ادمین، بررسی و دسترسی به فروشگاه‌های مجاز است. فروشنده به اطلاعات و فایل‌های فروشگاه دیگر دسترسی نمی‌گیرد.

## ۲. فهرست، شبیه Orders و مناسب کار روزمره

### نمای ادمین
- عنوان «استوری‌ها»، دکمهٔ «استوری جدید»، فیلترهای سریع «همه / در انتظار تأیید / منتشرشده / زمان‌بندی‌شده / پیش‌نویس / ردشده / متوقف / پایان‌یافته».
- جدول با همان AppDataGrid، نظم، تراکم، رفتار RTL و ابزار جستجو/فیلتر/ستون/خروجی/ذخیرهٔ نمای سفارش‌ها؛ طراحی جدول جدید ساخته نشود.
- ستون‌های پیش‌فرض: تصویر کوچک + عنوان، نام فروشگاه یا «ویترین اصلی»، وضعیت قابل‌فهم، تعداد اسلاید، شروع/پایان نمایش و آخرین تغییر، عملیات.
- «مشاهده/پیش‌نمایش» و منوی سه‌نقطهٔ جمع‌وجور جای دیوار دکمه‌ها را بگیرند. تأیید از روی پیش‌نمایش محتوای دقیق انجام شود؛ تأیید کور از ردیف، پیش‌فرض نباشد.
- شناسهٔ فروشگاه، locale و market ستون پیش‌فرض نباشند. زبان با نام انسانی در صورت نیاز نمایش یابد. اطلاعات تخصصی در جزئیات/ستون اختیاری.
- تعداد، فیلتر و صفحه‌بندی از یک query معتبر بیایند. جستجوی نام فروشگاه با مرجع Party و بدون join خارجی انجام شود.
- جزئیات از GET همان رکورد خوانده شود، نه فهرست قدیمی rows.
- scope نمای ذخیره‌شدهٔ admin و seller جدا و برای فروشنده به هویت فروشگاه وابسته باشد.

### نمای فروشنده
همان جدول و ویرایشگر با دادهٔ فروشگاه جاری؛ انتخاب مالک و تأیید/رد ندارد. دلیل رد و قدم بعدی روشن دیده شود. جستجو/صفحه‌بندی واقعی برای فهرست بزرگ از سرور؛ فیلترِ browser روی همهٔ رکوردها الگوی نهایی نیست.

### جزئیات و ویرایشگر
جزئیات با آدرس پایدار یا workspace مشخص باز شود، نه بخش ناگهانی زیر جدول. مدال برای اقدام‌های کوتاه مانند رد، توقف، حذف اسلاید و زمان‌بندی؛ فرم چنداسلایدی فضای ویرایش مناسب و پیش‌نمایش موبایل داشته باشد.

## ۳. فرم مشترک و گردش کار پیشنهادی

۱. مالک: در پنل فروشنده از session/مجوز سرور؛ در ادمین «ویترین اصلی» یا فروشگاه با نام قابل جستجو.
۲. عنوان، جلد، یک یا چند عکس/ویدیو از فایل/کتابخانهٔ مجاز؛ مشاهدهٔ وضعیت آپلود و خطای هر فایل.
۳. اسلایدها: پیش‌نمایش، ترتیب، ویرایش، حذف، توضیح و مدت مناسب. ذخیرهٔ draft ناقص مجاز؛ ارسال/انتشار ناقص مجاز نباشد.
۴. مقصد اختیاری با عبارت «با زدن دکمه کجا باز شود؟»: محصول، دسته، صفحه/مقاله، فروشگاه یا لینک مجاز؛ انتخاب واقعی به‌جای تایپ کد. مالکیت محصول فروشنده سمت سرور بررسی شود.
۵. زبان از رجیستری فعال؛ پیش‌فرض از تنظیمات، نه fa ثابت. برای فروشنده خواندن تنظیمات مجاز فراهم شود و مجوز ادمین قرض گرفته نشود.
۶. زمان نمایش با مؤلفهٔ تاریخ/ساعت استاندارد و منطقهٔ زمانی پذیرفته‌شده؛ اگر زمان پایان تعیین نشده، این معنی صریح باشد. قانون اجباری ۲۴ساعته از خودمان اختراع نشود.
۷. «ذخیرهٔ پیش‌نویس» و «ارسال برای تأیید» روشن و مجزا.
۸. ادمین محتوا را دقیق می‌بیند و «تأیید و انتشار» یا «تأیید و زمان‌بندی» را صریح انجام می‌دهد؛ رد با دلیل روشن و امکان اصلاح فروشنده.
۹. ساخت استوری توسط ادمین برای فروشگاه: مالک فروشگاه می‌ماند، ایجادکننده ادمین ثبت می‌شود، در پنل همان فروشگاه دیده می‌شود. دورزدن review با جعل seller header ممنوع؛ این یک use-case ادمینِ معتبر است.
۱۰. بعد از تأیید، تغییر محتوا نیازمند بازبینی مجدد است؛ نسخهٔ تغییرکرده بدون تأیید عمومی نشود. توقف استوری خود توسط فروشنده مسیر روشن و مجاز داشته باشد؛ انتشار دوباره طبق چرخهٔ تأیید انجام شود.
۱۱. انتشار قبلی و ویرایش جدید نباید با هم اشتباه شوند. در این تحویل، الگوی کم‌پیچیدگیِ «توقف/بازگشت به پیش‌نویس و تأیید مجدد» پیشنهاد می‌شود؛ نگهداری هم‌زمان دو نسخهٔ زنده/درانتظار تنها اگر مالک بخواهد وارد دامنه شود.

جزئیات بندهای انتشار مرکب، توقف فروشنده و محل نمایش صفحهٔ فروشگاه پیشنهاد محصول این برنامه‌اند؛ تا صدور تسک نهایی، تغییر قرارداد پذیرفته‌شده یا مجوز اجرا محسوب نمی‌شوند.

## ۴. اصلاحات مالکیت و انتشار که باید همراه UI بسته شوند

- یک مرجع سیاست قابلیت و وضعیت مؤثر انتشار در Story؛ read/list/detail/public و فرمان‌ها نتایج سازگار بدهند.
- بررسی tenant، فروشندهٔ فعال/مجاز، محتوای معتبر و رسانهٔ Ready/مجاز قبل از submit/approve/publish؛ seller مالک و actor را جعل نکند.
- استوریِ برنامه‌ریزی‌شده واقعاً در زمان شروع نمایش یابد و در پایان حذف شود؛ شرط Scheduled/Active موجود اصلاح شود، همراه آزمون ساعت. صرف تغییر badge کافی نیست.
- عملیات تأیید به نسخهٔ محتوای دیده‌شده متصل باشد؛ فرم قدیمی نتواند محتوای عوض‌شده را تأیید کند.
- create/add/submit/review و خطای پاسخِ گم‌شده هویت و رفتار تکرار مشخص داشته باشند؛ کلیک دوباره استوری/اسلاید تکراری نسازد.
- گذارهای مرکب در یک owning operation ثبت شوند؛ create موفق + enable ناموفق نباید «انتشار موفق» نشان دهد.
- Media مالک ذخیره و اعتبار فایل، Story مالک چرخهٔ استوری، Party/مرجع فروشگاه مالک نام و وضعیت فروشگاه؛ ارتباط فقط از Contracts.
- مهاجرت از پیش قطعی نیست: اول بررسی شود فیلدهای فعلی کافی‌اند یا برای actor/هویت درخواست/بازبینی پایدار افزودهٔ محدود لازم است. هر تغییر schema باید در تسک صریح، با طرح و بکاپ پیش از apply مجاز شود؛ این گزارش اجازهٔ migration نیست.

## ۵. ویترین عمومی قابل‌فروش

- عکس/ویدیو همان محتوا و ترتیب تأییدشده، با نام و نشان واقعی فروشگاه و مقصد درست.
- ریل عمومی مارکت‌پلیس فقط استوری‌های واجد شرایط را نشان دهد؛ scope فروشگاه اختصاصی هم، اگر وارد تحویل شد، فقط استوری همان فروشگاه باشد.
- ویترین اصلی را با فروشگاه اشتباه نکنیم. پروفایل seller-profile فعلاً استوری ندارد؛ افزودن ریل به آن یک تغییر صریح محصول و مشمول حفظ ظاهر Shopeiva است، نه نتیجهٔ خودکار افزودن sellerPartyId.
- پیش‌نمایش draft در پنل به API عمومیِ دورزنندهٔ تأیید تبدیل نشود.
- ویدیو بر اساس پخش واقعی، پایان، خطا، توقف و mute کنترل شود. تکرار ویدیو و تایمرِ مستقل نباید محتوای در حال پخش را بی‌دلیل عوض کند.
- اشتراک‌گذاری همان استوری منتشرشده را با لینک پایدار باز کند؛ استوری غیرمجاز با لینک مستقیم هم دیده نشود.
- لایک/نظر/ذخیره/گزارش بدون ذخیره/handler واقعی در نسخهٔ فروش خاموش شوند. برای جلوگیری از بزرگ‌شدن کار، ساخت شبکهٔ اجتماعی/تحلیل بازدید پیشرفته جزو حداقل تحویل نیست. قابلیت نمایشیِ گمراه‌کننده تحویل نشود.
- حالت خالی، رسانهٔ خراب و نبود مجوز، متن ساده داشته باشند؛ URL داخلی و عبارت Host/header در پیام کاسب دیده نشود.

## ۶. مسیر اجرای پیشنهادی: دو تحویل منسجم، نه ده تسک خرد

### تحویل اول — چرخهٔ واقعی فروشگاه و ادمین
مالکیت ادمین-برای-فروشگاه، Media فروشندهٔ محدود به مالک، سیاست مشترک وضعیت/مجوز، آماده‌بودن محتوا، شروع/پایان واقعی زمان‌بندی، ویرایش و تأیید مجدد، نسخه و تکرار فرمان، query و نام فروشگاه.

خروجی: backend قابل‌اتکا و قراردادهای مشخص برای یک فرم مشترک؛ تست‌های owning module و قراردادها. این تحویل فقط افزودن route و pin نیست و نباید با نبود فایل آپلود یا زمان‌بندیِ نمایشی PASS شود.

### تحویل دوم — پنل آمادهٔ تست کاسب و نمایش متصل
جدول شبیه Orders، editor مشترک، انتخاب مالک/فایل/مقصد، مدال و Toast، preview و بررسی، کارکرد عمومی، حذف affordanceهای غیرواقعی، RTL و موبایل. تست واقعی دو نقش تا نمایش عمومی و تحویل تست صاحب محصول.

اگر اندازهٔ واقعی تغییرات اجازه دهد، این دو می‌توانند دو بخش یک تسک اجرایی منسجم باشند؛ اکنون هیچ‌کدام صادر نشده‌اند. هدف، پایان یک مسیر قابل‌فروش است، نه افزایش شمار تسک‌ها. برآورد زمانی قطعی پیش از تعیین قرارداد Media/مالکیت داده نمی‌شود.

## ۷. شرط پایان و سناریوهای پذیرش

| سناریو | نتیجهٔ لازم |
| --- | --- |
| فروشگاه A عکس/ویدیو می‌گذارد، draft ذخیره و submit می‌کند | فقط A/ادمین می‌بینند؛ قبل از تأیید در ریل عمومی نیست |
| فروشگاه B شناسهٔ A یا فایلش را می‌فرستد | رد بدون خواندن/تغییر دادهٔ بیگانه |
| ادمین رد با دلیل می‌کند | A دلیل را می‌بیند، اصلاح و دوباره ارسال می‌کند |
| ادمین محتوای دیده‌شده را تأیید/منتشر می‌کند | همان محتوا/فروشگاه در ویترین عمومی و CTA درست |
| ادمین برای A ثبت می‌کند | مالک A، ایجادکننده ادمین؛ در پنل A قابل مشاهده و در B غایب |
| اسلاید/فایل ناقص یا حذف‌شده/غیرمجاز | draft قابل ذخیره، submit/publish ممنوع با خطای قابل‌فهم |
| زمان قبل شروع / برابر شروع / قبل پایان / برابر پایان | نمایش عمومی دقیق و badge/عملیات سازگار |
| تغییر محتوای تأییدشده یا بازبینی قدیمی | محتوای تأییدنشده منتشر نمی‌شود؛ تأیید stale رد می‌شود |
| کلیک دوگانه یا قطع پاسخ | یک ثبت و همان نتیجه در تکرار؛ نه دو استوری/اسلاید |
| جستجو/فیلتر/صفحه/نمای ذخیره‌شده | شمارش و داده همخوان، نام فروشگاه انسانی، جدایی نقش/مالک |
| موبایل ۳۹۰ و دسکتاپ ۱۴۴۰ | بدون overflow صفحه، دکمهٔ اصلی روشن، لمس مناسب و مدال قالب |
| ویدیو/تصویر خراب یا پایان پخش، لینک اشتراک | رفتار واقعی؛ بدون دکمهٔ دروغین یا نمایش استوری نامجاز |
| چند زبان فعال از دیتابیس | انتخاب و نمایش از رجیستری، نه فرض دو زبان یا fa ثابت |

تست ثابت capabilities و source regex به‌تنهایی پذیرش این مسیر نیست. با اجرای واقعی کامپوننت و owning service، negative case و اتصال HTTP باید سنجیده شود؛ نسخهٔ دادهٔ کنترل‌شده را از walkthrough واقعی جدا گزارش کنید. StoryFoundationTests فعلی Docker ایجاد و seed می‌کند؛ روی محیط canonical بدون مجوز اجرا نشود. برای کار آینده طرح rollback-only ایمن یا تست کنترل‌شده انتخاب شود؛ چیزی روی دادهٔ کاربر ساخته/پاک نشود مگر صریحاً در تسک مجاز شده باشد.

## ۸. خارج از مجوز این مرحله

این مرحله فقط audit و برنامه است. هیچ تغییر src، schema، داده، seed، runtime، تنظیمات یا مجوزی اجرا نشود؛ هیچ تسک Bridge یا اعلان به worker صادر نشود. بررسی/قبولی مرجوعی‌های قبلی و backlog روش پرداخت محفوظ‌اند. نقشهٔ بستن کار باید همین‌جا بازیابی شود، نه از حافظهٔ چت.

activeTask: NONE
automaticNextImplementationTask: NONE
Implementation-State: NOT_ISSUED
Next-Step: OWNER_REVIEW_OF_REPORT_AND_PLAN
