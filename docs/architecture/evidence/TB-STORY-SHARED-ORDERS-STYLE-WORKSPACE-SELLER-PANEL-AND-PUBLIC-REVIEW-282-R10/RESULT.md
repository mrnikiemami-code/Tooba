# TB-282-R10 RESULT

Status: **PASS** within issued A–E (worker). NOT Architect ACCEPT. NOT owner acceptance.

Decision: `A_LOCAL_PREVIEW_PHOTO_DURATION_VIDEO_ENDED_B_DRAFT_PUBLICATION_WINDOW_START_END_NO_ACTIVATION_C_WHOLE_STORY_DELETE_CQRS_DS_CONFIRM_D_ONE_PERSIAN_STATUS_E_SELLER_PLAYABLE_REQUIRES_MEDIA_ASSET_SUBMIT_GATE_ALIGNED_ZERO_MIGRATION_ZERO_LIVE_WRITE`

## A — preview / duration
- Slide dialog: object-URL local image/video preview before upload; revoke on change/close.
- Photo: «مدت نمایش این عکس (ثانیه)» default 5 → ms.
- Video: duration control hidden; add/update payload `durationMs=null`; public `story-modal` already advances video on `ended` (unchanged).

## B — publication window
- Additive `StartAt`/`EndAt` on Create/Update commands + HTTP bodies.
- Domain `SetDraftPublicationWindow` sets dates without Status activation (≠ `SetSchedule`).
- Shared create/edit «بازهٔ انتشار» with JalaliDatePicker + Tehran clock; optional بدون پایان.
- List columns show Jalali day+clock via `formatStoryScheduleClock`.

## C — whole-Story delete
- Admin/Seller `DELETE /v1/{admin|seller}/stories/{id}` + CQRS delete commands.
- Seller: only `IsSellerContentEditable`; Admin: allowed.
- Occurrence recorded before remove; replay after missing target returns tombstone snapshot.
- UI: «حذف استوری» + DS Dialog with title; cancel = zero request; durable DELETE envelope.

## D — one status
- Grid/detail: single Persian `displayState` (no stacked Status/Review badges).
- Unified «در انتظار تأیید» for filter/status/submit labels; Expired → «منقضی».

## E — submit 403 root cause + fix
- Owner seed/draft with `mediaUrl` only / no `MediaAssetId` had `contentReady=true` + `submit` action, then submit → `story.media.forbidden` (403) in `EnsureStoredContentAuthorizedAsync`.
- Fix: seller-scoped `HasPlayableContent` requires `MediaAssetId`; live GET now `contentReady=false`, `actionCodes=update,add_item,delete` (no submit) for story `01a04246-…`.
- Persian media.forbidden copy clarified; FE maps that code to `mediaRequiredHint`.
- Lawful path unchanged: upload → asset id → contentReady/submit.

## Proof
- Host.Tests Story282R10 + StoryMarketplace281Domain: **8/8**
- FE unit schedule+R10 copy: **4/4**; `tsc --noEmit` 0 on story surface
- Host publish Release; served `5088` skip-bootstrap=1; dll SHA256 `1FDEC58FCFBC5E439C1B0F51342C4E996538C93DE4C38A3FF6663657186B28A3`
- Next: `next build` blocked by pre-existing `/404` Html prerender (disclosed); served `next dev` :3000 — `/fa/vendor-panel/stories` 200, `/fa/admin/stories` 200, `/fa` 200
- ZERO migration / ZERO live Story/Media POST/PUT/DELETE by worker / ZERO grant/seed

## Limits
- Next production BUILD_ID not minted this pass (Html/404 prerender). Owner first real upload/delete/submit trial.
- R6 bootstrap effects UNKNOWN retained; VendorShell unchanged.

automaticNextImplementationTask: NONE  
STOP_FOR_ARCHITECT_AND_USER_REVIEW
