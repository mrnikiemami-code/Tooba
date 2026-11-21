# CORRECTION ADDENDUM — TB-282-R19

Corrections made during this pass, and corrections the reviewer should apply to earlier claims.

## C1. Earlier UI bound a route the platform never maps

R18-era receipts described "seller media upload" as working. In source the UI posted
`/v1/seller/media/upload-story`; `MediaSellerEndpoints` maps only the group root `POST /v1/seller/media`, so every
seller upload from the workspace returned 404 and the durable intent was retried against a dead route. R19 binds the
real endpoint and the negative control now asserts the old path is refused instead of silently used.

## C2. Earlier UI bound admin stop to the seller route

The binder accepted only `/v1/seller/stories/{id}/stop`. The admin button therefore failed client-side as
`route_binding`, and `AdminDisableAsync` was never contacted. This was reported in prior evidence as an "admin stop"
capability; it was not reachable. R19 binds `/v1/admin/stories/{id}/disable` for the admin role and keeps `/stop` for
the seller.

## C3. "2xx means done" was an over-claim

Prior receipts treated a 2xx with a same-id snapshot as completion. Nothing in the client checked that the returned
row actually showed the effect of the frozen command, so an unchanged snapshot (same id, same `Draft`, same version)
counted as success and the durable intent was retired as if it had been applied. R19 asserts the specific owned effect
and treats a mismatch as uncertain (`story_state_unchanged`) with the intent retained for exact replay. Any earlier
"command completed" claim based on a status code alone should be read as unverified.

## C4. `Approved + Draft` was a dead end, and the display hid it

Two separate defects produced the owner-visible "approved but nothing happens" impression:
1. the seller had no action at all in `Approved + Draft` (`IsSellerContentEditable()` is false because
   `ReviewStatus != None/Rejected`, so `update`/`submit` were absent, and `return_to_draft` was only offered for
   `Disabled`), and
2. the row rendered «پیش‌نویس» for `Approved + Draft`, which reads as "not yet reviewed".

R19 fixes both: the lawful `return_to_draft` action is offered and «تأییدشده، منتشر نشده» states the truth.

## C5. Grid filters were silently dropped, not "working"

`AdminStoryGridPolicies` rejected `shopDisplayName`, `shopName`, `startAt`, `endAt` and `updatedAt`, and
`updatedAt` was not in `SortableFields`. Because the workspace swallows a rejected filter, earlier screenshots of a
"filtered" grid could not distinguish a working filter from a dropped one. R19 admits the fields, translates the
nullable date windows to SQL, and proves the behaviour on the real engine.

## C6. Public player decided video-ness per story

`const isVideo = currentItem?.mediaType ?? (currentStory?.isVideo ? "video" : "image")` meant a story with any video
slide forced its JPEG cover into video mode, so the cover waited for a media `ended` event that never happens and the
modal stalled. The photo timer also started before the image painted. R19 decides per slide, gates the dwell on
`onLoad`, renders the caption, and gives the overlay proper dialog semantics.

## C7. Test-fixture drift is pre-existing

Six failures in the Story-filtered backend run were reproduced identically at HEAD with the R19 work stashed. They are
recorded in RESULT.md "Architecture findings" and must not be read as R19 regressions; equally, R19 does not claim to
have fixed them.

## C8. Architectural debt was reported, not papered over

The `Story.Infrastructure → Catalog.Contracts` project reference predates R19 and fails the module's own W6 guard.
R19's one additional port usage sits inside that already-existing adapter. The clean fix (Application-owned port +
Catalog/Host adapter) is out of the issued scope and is reported for a separate bounded decision rather than performed
silently.
