# TB-281-R3 ARCHITECT REVIEW — 2026-11-19

Verdict: PARTIAL, not delivery-one ACCEPT. Reviewed code 46c639c4c6cdd5f1ae0e3af2afbeb03f0dcf3dca and actual origin/main 14b2395eea3585b7e12af71d135c84c77ce7f9c2. The latter is a docs-only Final-HEAD addendum. This explains the two SHAs; it is not evidence of source divergence. Historical RESULT remains intact.

## Retain progress
Admin update/add/update-item now apply shop Media/CTA checks; stored-content gate added to submit/approve/composites/enable. SellerSubmit gains version and optional occurrence. Add-item hash now includes storyId. Public seller attribution resolves through the existing Catalog storefront authority. Tenant-scoped Story occurrence PK migration is applied and immutable. Five Media recording-store InMemory facts execute, with limits below.

## Remaining concrete findings
R4-A — End-to-end command plumbing is unfinished, with a real regression:
- StoryDirectory.AdminRemoveItemAsync, AdminReorderItemsAsync, SellerRemoveItemAsync now require expectedVersion via EnsureExpectedVersion, but corresponding endpoint -> command -> composer still passes no version, so shipped paths always fail. Do not fix by making the guard optional.
- Admin approve-and-* still has no occurrence on wire. Update/remove/reorder and other existing mutations lack the issued full occurrence protocol; optional missing occurrences still mutate. AdminReorderStoriesAsync still loads and mutates the whole set without reviewed per-story versions. Need one enumerated full transport/directory matrix, including FE transport, before the UI delivery can rely on it.
- AdminSetScheduleAsync does not call the stored-content authorization gate which the approve-and-schedule path calls.
- R3 AdminCreateAsync/AdminUpdateAsync now call EnsureProductCtaAsync(null, ...), which unconditionally refuses product CTA for a platform story. Preserve the original platform product feature with a positive Catalog validity check, while shop stories require shop ownership; do not turn seller ownership into an accidental global product prohibition.

R4-B — Failure proof does not exercise its claimed path:
- MediaStorySellerUpload281R3Tests.Db_save_failure_after_store_cleans_attempt_object_and_detaches creates a successful upload then calls the ordinary exact replay. It inserts no simulated row, injects no SaveChanges failure, triggers no duplicate catch and expects Deleted.Count==0. Its name/comment are not failure evidence.
- Media IsUniqueViolation checks 23505 plus substring constraint OR substring table, without exact schema+table+constraint. This does not implement the requested exact arbitration identity.
- Generic SaveChanges catch deletes the attempt object immediately. A save/commit whose acknowledgement is lost can have durable rows; prove durable success is preserved before any cleanup. Existing caller transaction can also outlive method success; establish refusal before object effect or a supported owning guarantee.
These are the already-issued upload durability requirements, not a new object-store subsystem.

R4-C — PostgreSQL behavior proof entirely absent
rollback-only-proof.md explicitly says OPEN and no execution this wake; no failed command/external blocker is provided. Migration apply and 57 focused tests do not prove fresh relational Items loading, scoped key collisions, concurrency or query counts. Required small real-PostgreSQL rollback facts remain due.

## Evidence boundaries / next action
Architect reviewed source remotely and did not execute local Windows tests. Worker receipts are attributed claims. No live business write authorized by this review.
Only next continuation 281-R4 closes A/B/C; no new migration or redesign. Do not replay already-proven happy paths as a substitute. Keep original reports, correct misleading test/evidence descriptions with an explicit addendum, and stop only after focused completion or a concrete reproducible external blocker.
UI delivery remains NOT ISSUED. Existing purchased-template VendorShell /vendor-panel/stories integration requirement remains mandatory; no parallel seller panel. Returns accepted scope and payout backlog untouched.
