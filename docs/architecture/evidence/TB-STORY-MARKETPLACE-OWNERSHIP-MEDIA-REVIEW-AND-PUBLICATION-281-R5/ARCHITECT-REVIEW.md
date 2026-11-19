# TB-281-R5 ARCHITECT REVIEW — 2026-11-19

Reviewed source fa32794e059f8fdf52858cabfcc83494be05ed03; origin/main c9ba2296a3adfbb6f6ed2638df65957f4c3f391c. Later commits are docs-only. No local Windows execution by Architect.

## Verdict
ACCEPT scoped progress A1/A2/A3 and Media transaction-guard B, with C proof limits stated below. NOT full delivery-one closure: A4 collision containment still incomplete. R5 overall remains PARTIAL against its entire issued envelope; do not rewrite its original worker PASS report.
Rather than issue another standalone backend repair, authorize the explicitly named residual gate as section 0 of connected UI task 282. Worker must prove that gate before proceeding to UI in the SAME task. This review is that bounded scope authorization, not a claim all prior code is complete.

## Retained / not reopened
SellerUpdate/AdminUpdateItem/SellerUpdateItem now require occurrence and replay before mutable gates; complete field-wise length-prefixed update hashes replace the incomplete hash. Admin schedule now replays before stale/content gates.
Media guard now refuses EF CurrentTransaction, Transaction.Current and owning RelationalConnection.EnlistedTransaction; typed code honestly remains ValidationFailed.
Fresh-context public/expired reads and actual admin shop-name grid + seller query now exist in PostgreSQL. Caller-entity and fresh after-save Media assertions are improved. Existing schema/migrations remain immutable.

## Residual gate for 282 (source confirmed)
1. SaveChangesWithOccurrenceArbitrationAsync verifies winner.IntentHash only. It does not bind winner ActionCode, SellerPartyId, requested story/item target or command role. Pre-read replay does those checks; collision folding must enforce the SAME identity. E.g. implicit seller scope is absent from CreateStoryCommand.SellerPartyId when null, so the same tenant/key/body race across shops must not return a foreign winner. Admin and seller update hashes can also match while action differs.
2. DetachAttemptOccurrences removes only occurrence entries. A losing create Story/items remain Added; a failed update leaves modified Story/items in the shared context. A later lawful SaveChanges can persist the losing attempt without its occurrence. Restore/detach only this command's staged entries, preserve caller-owned entries, and prove a later SaveChanges cannot publish the loser. Exact unique error injection that returns a winner is not that proof.
3. C2_two_context_stale_version_owning_command_zero_extra_occurrence constructs db2 before writer1 but does not preload an original tracked Story. Its directory later reads the current version and rejects at EnsureExpectedVersion. This proves a stale owning command, not EF optimistic-concurrency collision. Preload both originals before writer1, observe writer2 actually reaching SaveChanges, then assert typed loser/no extra occurrence and safe tracker.

These three checks close the already-issued corner; no new transaction framework, schema, stress/committed two-writer harness or broad recertification.

## Connected UI authorization
Issue ONLY TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282. Section 0 first, then Orders-style shared list/editor/media picker, existing VendorShell integration, moderation and honest public playback. Current UI source still has raw media URLs/default images/fa, window.prompt, seller client filtering, admin saved-view scope shared with seller, inline detail and local fake likes/comments. These are the planned product delivery, not additional backend repair waves.
Owner wants ordinary shopkeepers to use it and then personally test. Mandatory existing purchased-template /vendor-panel/stories under VendorShell/story.view; no new seller panel.
Task includes narrow read/transport integration fixes needed by actual UI/CTA, but no live business data writes or migrations. Existing rows may be read for a populated owner review; mounted/provider fixtures are not live acceptance.
Returns acceptances, payout backlog, key state and user data untouched. No blanket Marketplace/financial/provider/user acceptance.
