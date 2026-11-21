# COMPLETION LEDGER — TB-282-R19

| # | Issued obligation | Source change | Evidence | State |
| --- | --- | --- | --- | --- |
| A1 | Admin stop binds `/v1/admin/stories/{id}/disable` | `story-response.ts` `bindStoryEnvelopeRoute` case `stop` | R19 mount A (admin `/disable` accepted, seller route rejected for admin) | DONE |
| A2 | Seller stop binds `/v1/seller/stories/{id}/stop` | same case, role branch | R19 mount A | DONE |
| A3 | Seller media upload binds `POST /v1/seller/media` | `story-response.ts` `mediaUpload` regex; `StoryManagementScreen.tsx` add + replace paths | R19 mount A; `rg` shows no stale route; `MediaSellerEndpoints` maps the group root | DONE |
| A4 | Foreign suffix / other role's route stays `route_binding` | anchored regexes + role switch | R19 mount A negative control (`/v1/seller/media/upload-story` rejected) | DONE |
| B1 | 2xx must show the owned effect | `story-response.ts` `effectSatisfied()` for approve/approve_publish/approve_schedule/reject/submit/publish_now/schedule/stop/return_to_draft | R19 mount B (unchanged Draft to `approve` ⇒ `story_state_unchanged`; real `Approved` accepted) | DONE |
| B2 | Unchanged snapshot is not completion | `verifyStoryTarget` returns `story_state_unchanged` for both root and `data` shapes | R19 mount B | DONE |
| B3 | Partial-but-truthful replies still valid | `carries()` only asserts fields present in the reply | R19 mount B + existing story unit suite 22/22 | DONE |
| B4 | Non-platform 4xx is not a no-effect refusal | `story-executor.ts` `finalizeResponse` → `story.response.unverified`, intent retained | code + story unit suite | DONE |
| C1 | Approved-but-unpublished has its own state | `displayState()` + `displayApprovedUnpublished` in fa/en catalogs | R19 mount C (row never reads «در ویترین»); served chunk carries the Persian string | DONE |
| D1 | Seller can return an approved never-published story to draft | `StoryPublicationPolicy.ProjectActionCodes` offers `return_to_draft` for `Active/Scheduled/Disabled/Draft` under `Approved` | R19 backend test 1 on the real `StoryDirectory` (offer + execution + occurrence) | DONE |
| D2 | Only states the domain accepts are offered | the offer list mirrors `ReturnToDraftForEdit` preconditions (`Origin=Seller`, `ReviewStatus=Approved`) | R19 backend test 1 (`approve`/`publish_now` absent) | DONE |
| E1 | Grid admits shop/date/updated fields | `StoryAdminGridPolicies` whitelist + operators | R19 backend test 2 | DONE |
| E2 | Nullable date filters translate to SQL | `AdminStoryGridQueryEngine.ApplyNullableDateFilter` (`EF.Property`, day windows) | R19 backend test 3 (real engine, filtered + sorted page) | DONE |
| E3 | `updatedAt` sortable | policy `SortableFields` + engine `Order` | R19 backend tests 2–3 | DONE |
| F1 | Video detection is per slide | `story-modal.tsx` `itemIsVideo()` (mediaType → URL extension) | R19 mount D (JPEG cover of a video story dwells as an image) | DONE |
| F2 | Photo dwell waits for the image | `imageLoaded` state set by `onLoad`, reset per slide; timer effect gated | R19 mount D | DONE |
| F3 | Caption renders | `story-caption` block | R19 mount D; served storefront chunk | DONE |
| F4 | Dialog semantics + focus restore | `role="dialog"`, `aria-modal`, `aria-label`, focus to close button, body overflow restored, visibility hold | mount D + code | DONE |
| G1 | Explicit slide file replacement | `story-replace-file` input + button in edit mode, `replaceMediaPick/replaceMediaHint` copy | code + tsc + served chunk | DONE |
| G2 | Old media preserved until upload AND update succeed | upload runs first; on failure the flow returns before `update_item`; `mediaReplaceFailed` copy states the old media is unchanged | code path review + unit/typecheck | DONE |
| G3 | Media type follows the new file | `readUploadedStoryMedia()` → `contentType.startsWith("video/")` | code + tsc | DONE |
| G4 | Caption/CTA-only edit uploads nothing | `if (slideFile)` guard around the whole upload branch | code | DONE |
| X1 | Zero migration/schema/seed/grant/config | no such file touched | `git status` in RECEIPTS §2 | DONE |
| X2 | Zero live Story/Media business write | no POST issued; served checks were GETs | RECEIPTS §7 | DONE |
| X3 | VendorShell/menu/login unchanged | no shell or route file touched | `git status` | DONE |
| X4 | R16/R17/R18 behaviour preserved | typing/selection, toast presentation, approval readiness untouched | story unit 22/22 + mounts 6/6 | DONE |

Open (reported, deliberately not executed):

| # | Finding | Why it is not in R19 |
| --- | --- | --- |
| ARCH-1 | `Story.Infrastructure` → `Catalog.Contracts` direct project reference fails the module's own W6 coupling guard (pre-existing at `d3de64c2`) | fixing it means relocating an adapter to a Catalog/Host-owned composition with a new Application port — a module migration, not a bug fix inside the issued scope |
| TEST-1 | 6 pre-existing test failures (media placeholder fixtures, missing occurrence key, missing Catalog file, exception-type drift) | identical at HEAD; repairing them would rewrite tests the owner has not asked to change |
| LIM-1 | No browser geometry/RTL measurement and no real `<video>` `ended` runtime measurement this pass | declared as a limit in RESULT.md rather than claimed as proof |
