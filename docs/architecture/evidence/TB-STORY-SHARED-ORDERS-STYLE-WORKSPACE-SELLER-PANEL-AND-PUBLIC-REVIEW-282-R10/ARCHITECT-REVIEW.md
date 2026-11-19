# ARCHITECT REVIEW — TB-282-R10 — 2026-11-19

Decision: PARTIAL — R11 focused repair required, NOT owner-ready.
Reviewed code a58172221170def6a9ef7c316474289fb9a4d6c5; final implementation/evidence 400ccbf1cade071be3ebd72ab95171360475f6fb.
Method: independent GitHub source/diff/evidence review, no local Windows build/test/live UI rerun.

Retain source progress:
- Local object-URL selected image/video previews + existing-item preview; photo-duration hidden for video, video add/update null duration.
- Additive StartAt/EndAt on create/update with hash binding and SetDraftPublicationWindow without direct activation; Jalali/Tehran form/list fields.
- Actual scoped DELETE commands/endpoints, item+Story removal with occurrence record; role gate and confirmation.
- One localized grid status; URL-only shop items no longer advertised contentReady/submit, consistent with stored-media ownership refusal.

Confirmed defects:
1. AdminDeleteAsync replay expects sellerPartyId=null but occurrence is stored with story.SellerPartyId. Exact replay of admin deletion of a shop Story throws conflict after the row has gone. Critical lost-response recovery broken.
2. displayState returns displayOnStorefront for Active even when effectivelyPublic=false. Public reader derives expiry from timestamps without rewriting Status, so expired Active/Scheduled rows still mislabel.
3. R10 whole-Story delete only added to editable detail, not requested list action; pending delete replay UI uses generic success refresh, without dedicated closure of deleted route.
4. Date validator accepts equal start/end (>=), Domain rejects it (<=), producing inconsistent error. Explicit unchecked no-end does not independently require an end day.

Evidence limitations:
- New Host test file has four Domain-only facts; retained281 tests make total8. No actual directory DELETE, transaction/replay or HTTP mapping tests.
- R10 mount-named FE test is date/copy units only. No mounted preview/object URL cleanup/delete cancel/transport/submit interaction. Ledger claims stronger behavioral evidence than supplied.
- E documented read-only URL-only Story GET and readiness gate. This explains refusal of that row, not proof owner context+upload+submit works; user needs a clear lawful media recovery route.
- Next production build failed Html/404 prerender, next-dev served; no new production BUILD_ID. “pre-existing” unproven from supplied receipts. Page200 alone not active authenticated editor/dialog proof.
- Source change SetDraftPublicationWindow also runs admin update; frozen-state/lifecycle compatibility and real approval-window preservation need affected tests, not separate architecture phase.

Only R11 closes these concrete source defects and remaining R10 interaction evidence. Preserve improvements; no broad redesign/task-series restart. No user/commercial readiness claim. R6 bootstrap effects UNKNOWN and unrelated bg-white guard failure preserved.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R11 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
STOP_FOR_ARCHITECT_AND_USER_REVIEW
