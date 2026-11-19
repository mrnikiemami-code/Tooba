# ARCHITECT REVIEW — TB-282-R7 — 2026-11-19

Decision: ACCEPT_BOUNDED_PROGRESS; DELIVERY PARTIAL — recovered-create and safe runtime review remain.
Reviewed code 419d62d46bd3a547f275bd60ec44e7040afd6189 / evidence main 8dc12e7cffed98ee40457639290ed3ae98989a70. Independent GitHub source/diff/evidence review only; Architect did not rerun Windows tests/builds or inspect served UI.

Retain:
- ownedDraftIdRef, single normal create transition and bound detail dialog continuation; controlled admin/seller tests now select file/product/caption/duration and add, remount and inspect persisted preview.
- Dirty back-button DS dialog; truthful unknown destination/origin; list-only filter/context retained.
- Cookie helper no longer honors unrestricted false override or arbitrary public HTTP; HTTPS stays Secure even with forwarded-http. Local loopback exception remains; proxy/deployment configuration was not independently certified.
- Host restored to skip-bootstrap; R6 startup effects correctly UNKNOWN.

Confirmed remaining source defect:
StoryManagementScreen.tsx recovery button calls replayPendingStoryIntent. Success only toast + refreshGrid + reloadToken, unlike normal create which adopts owning id and navigates. After an unknown create is successfully replayed and pending storage cleared, /new still has no adopted draft id; subsequent Save/Add can issue a fresh create. storeStoryIntent correctly blocks another occurrence WHILE pending, but not after successful replay. R7 unknown-create test ends before retry; it proves initial retention only.

Runtime blocker:
RECEIPTS says dev-context 404 under skip, BFF login 200, live admin editor not claimed. Source AdminDevContextEndpoints requires in-memory AdminDevActorBootstrap.Snapshot; EnsureAsync populates it alongside registration/membership writes. prepareAdminDevActor fetches this seam. Existing persisted identity must be read safely; bootstrap writes are not authorized to fix the snapshot. R8 permits a bounded read-only initialization/read repair using existing Contracts and authorization.

Evidence limits:
R7 helper opens edit then cancels, not actual edited PUT (retained R5 tests cover controlled edits). “Preview” is persisted detail slide list, not independent public-player proof. Controlled remount evidence is not live authenticated geometry. No full A–C certification, owner acceptance or commercial readiness.
Only R8 issued for the two confirmed blockers; preserve accepted progress. Do not restart UI/task series.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R8 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
STOP_FOR_ARCHITECT_AND_USER_REVIEW
