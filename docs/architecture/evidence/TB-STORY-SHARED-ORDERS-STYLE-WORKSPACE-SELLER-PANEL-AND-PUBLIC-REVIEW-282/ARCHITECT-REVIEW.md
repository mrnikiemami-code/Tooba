# TB-282 ARCHITECT REVIEW — 2026-11-19

Reviewed code b0da0ab9d4ca593b9af17b0983802c2add936503; actual origin/main ebb921235456c6c75c88a06abfa35fcefa0b00db. History includes a normal merge of the docs issuance; do not rebase/force to erase it.
Verdict: PARTIAL / REPAIR REQUIRED. Worker PASS across A–F is not accepted. Disclosed required omissions are not completed scope.

## Retained progress
Compact row actions, thumbnails/human shop labels, seller server query, fresh detail GET, existing VendorShell page, design-system Dialog, existing react-toastify, selected upload routes, public shop attribution, fake-social removal, video timeupdate/ended and share parameter are landed.
Gate0 full winner identity/single-story loser containment is improved. PG test now preloads both originals; its reachedSave variable remains false/unasserted, so do not claim an observed PG branch probe. InMemory concurrency is injected, not a real provider race.

## F1 — Work protection is not implemented by the helper's existence
story-intent.ts read is never imported by StoryManagementScreen. Store overwrites an unresolved entry under the same action/target, validates only version/key on read and has no actor binding or unreadable state. Create stores {title}, not the exact transport including shop/locale/occurrence. New clicks mint new keys.
Only create/submit store envelopes. upload+add, approve, reject and schedule bypass durable storage. There is no pending-act recovery card or exact retry on remount. A lost create/add/upload reply can therefore be followed by a new logical act. The shared inFlight guard does not protect review/schedule/reject.
epoch is bumped on mode/unmount only; not target/actor/shop changes; mutation answer/finally are not fenced. Old detail GET can paint after selecting another id. Task D is largely OPEN.

## F2 — Merchant editor and review lifecycle incomplete
Admin shop input is literally placeholder «شناسهٔ فروشگاه فعال», not a name picker. No product/category/article destination editor, DB language selector, title edit, cover choice, caption/duration edit, reorder or remove controls. Video rows are img tags. No usable reviewed content preview. Approval directly sends from a button; no deliberate preview confirmation.
UI uses canEditSellerStory/canSubmitStory(reviewStatus), not server actionCodes; stop/return-to-draft absent, platform draft publication incomplete. Schedule is offered on every admin detail regardless of lawful action; approve-and-schedule is not used. Bare Guid public CTA is ignored by returning early, leaving a displayed button with no correct destination. These are missing mandatory product behaviors, not minor polish.
Detail remains an appended section under the grid. No stable routed workspace as issued. Native datetime-local/browser timezone remains instead of accepted date/time UI.

## F3 — Scope/query projection incomplete
Seller saved views call createHostSavedViewStore -> admin ui-preferences with adminHeaders. View key only separates roles, not actor/shop. Seller query conversion drops sort/advanced filters and collapses values to first value. Same shop/actor change has no coherent UI scope transition. Must complete through permitted existing authorities, not lend admin credentials.

## F4 — Platform upload occurrence is only a browser field
uploadAdminStoryMedia appends occurrenceKey to /v1/admin/media/upload. Generic MediaDirectory.UploadAsync has no occurrence parameter or replay, generates a new asset id each call. Retrying an unknown platform upload is not safe keyed replay. A minimal Story-specific platform Media path is authorized in R1 using current persisted asset identity/metadata; no fake seller binding or schema change.

## F5 — Source assertions are not mounted proof
story-workspace-282.guard.test.ts and story-modal.mount.test.ts read files via fs and assert regexes. Neither mounts React/VendorShell nor drives HTTP/lifecycle/video events. The reported 20/20 cannot stand in for issued behavioral matrix. Preserve source guards as such; add real mounted suites with the existing jsdom/tsx harness.
19/20 critical-storefront failure is attributed to untouched landing bg-white. Verify source/guard provenance in the bounded review; do not restyle unrelated landing or claim the full gate green.

## One narrow backend containment correction retained in the UI repair
DetachAttemptOwned treats Guid.Empty for batch reorder as ALL modified Story/StoryItem entries across the context, including unrelated caller/tenant state. Replace that sentinel selection with captured exact command-owned ids/entry set, preserve unrelated tracked state, and prove later-save containment for a losing batch. No reopening the rest of 281.

## Continuation
282-R1 completes these source-confirmed remaining parts of the SAME merchant workflow. Existing shell/nav, UI pieces and accepted backend progress stay. No new product program, migration, live business mutation, seed, key operation or unrelated work.
Architect reviewed remote source, not Windows/browser execution. No merchant/user/Marketplace acceptance. Keep worker's historical report, add correction and truthful recovery state.
