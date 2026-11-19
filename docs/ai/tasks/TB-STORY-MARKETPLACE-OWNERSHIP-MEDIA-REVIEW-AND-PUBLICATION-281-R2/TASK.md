# TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2
Parent-Task: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R1
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Mode: FINISH_THE_ALREADY_ISSUED_FIRST_DELIVERY_NO_NEW_PRODUCT_SCOPE
Expected-Base-SHA: ae947c863867437c13008029971a77eeb90edd41
Issued-Date: 2026-11-19 (Asia/Tehran)
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT

## Purpose — complete what was already requested

Owner requires prompt progress, ONE task at a time and review before the next. TB-281 and R1 returned PARTIAL with original work still OPEN, not an external blocker. This is the completion envelope for that SAME first delivery. Do not design it again, repeat only seven existing facts and stop, or leave the same B6/B7 work for another microtask.

Read parent R1 TASK.md + ARCHITECT-REVIEW.md and original281 A–G. Their requirements remain binding except explicit bounded clarifications here. Keep the fixed B1 attach-before-filter, role-specific mapper, fail-closed errors, version fields and hash encoding. Do not reopen closed Returns280-R1/279-R3, payout backlog or unrelated modules.

Before implementation write a short COMPLETION-LEDGER.md under this task evidence: each remaining parent requirement -> current source -> specific implementation -> proving test -> actual final status. It is a work checklist, not a new planning-only deliverable. Continue all in-scope work in THIS task. Unfinished issued work is not by itself a reason to declare a completed slice; if a genuine external blocker prevents continuation, show its exact failing command/source/required missing resource and identify what could still be completed independently.

Expected base above; docs-only review/issuance descendants permitted after actual claim ancestry/diff check. Normal commit/push only; no force/amend/rebase/reset/stash. Preserve unrelated local work and original evidence. Exact issued UTF-8 task bytes in both TASK.md paths/receipt; do not reconstruct a different envelope. No delegation, old agents, screenshots or polling loop. UI delivery remains PLANNED NOT ISSUED.

## 1. Finish safe Media ownership/upload and missing owning transport

Source: MediaDirectory.Story.cs still UploadAsync -> Ready save -> binding/occurrence save, then compensation reuses a failed tracker with added metadata and swallows failures.

- Refactor ONLY the new seller/admin-for-shop path over existing Media storage/policy: coherent durable identity/status/result, authoritative tenant+shop, full bytes/type/name binding. Same occurrence/intent resolves the SAME asset; changed intent conflicts. A local failure after storage may leave an explicitly unresolved attempt, never an asset silently treated as unscoped legacy Ready.
- No DB transaction/lock spans external store upload. Duplicate/concurrent occurrence folded under actual constraints with exact constraint identity; unrelated faults/cancellation propagate correctly.
- Never retry SaveChanges blindly on the same failed binding tracker as “cleanup”; restore/detach ONLY attempt-owned entries and use a deliberate owning result/failure protocol. Preserve caller tracking/transaction. Compensation only for positively attempt-owned objects; no broad storage/database cleanup or suppressed success.
- Use real MediaDirectory + recording object store to prove save failure/replay/conflict/duplicate and row/object counts; no fake IStoryMediaAssetPort replacing the upload algorithm.
- Finish seller-authorized Media query/get/upload AND admin-upload-for-validated-shop. Thin authenticated endpoints -> ISender -> Media-owned request/validator/handler -> owner service. Do not bless direct business execution by loosening architecture pins.
- Remove duplicated Story tenant constants/hash logic from Media Endpoints through the smallest canonical compatible context/Contracts seam. Preserve existing stored Story tenant identifiers; no tenant rewrite or generic framework.
- Owner scope means tenant+shop, not uploader user. No global admin library grants to seller; metadata/use permission is distinct from previously public binary URLs.
- Type/size/Ready/canonical URL and shop authority must be enforced; no raw URL bypass for new seller-owned assets.

## 2. Finish media/target authority on ALL owned Story content paths

- Cover create/update for shop-owned story, including admin-for-shop, must use same authority as slide add/update. Missing/foreign/Failed/wrong MIME asset or client id+URL mismatch refuses BEFORE mutation. Revalidate required media at submit/approve/publish/schedule.
- A nonempty Guid alone is not a Media Ready fact. Preserve the one pure Story policy but supply only positively established owning facts; fail closed when required authority unavailable.
- Existing legacy admin media compatibility remains narrowly readable/useable; do not assign unowned assets to a shop or repair existing records by guess.
- Finish seller product/CTA ownership via actual owning product/offer read. Foreign/nonexistent targets refuse through real Directory create/update/submit/publish paths. No fake role/grant/product seed.
- Shop creation validates real eligible shop, actor provenance distinct from business owner; include registered DI and authorizer seam proofs. Seller A/B and tenant isolation hold on replay as well as new commands.

## 3. Complete durable commands/versions; keep stopped stories editable through the review cycle

- Finish original create/add/submit/lifecycle replay contract for BOTH admin and seller, all applicable old/new routes. Full immutable scoped identity and unambiguous canonical intent; require reviewed content/version on new effectful edit/review/publication paths, including slide update/remove/order.
- Exact AUTHORIZED recorded occurrence replay BEFORE new-write stale/version gate. Repeat of successful stop/return/approve must answer its own recorded result; changed body/target/action/expected-version same occurrence conflicts.
- Scope durable key/constraints consistently across tenant/shop; no cross-tenant raw PK leak. Do not add another generic idempotency service; use the existing owning records.
- Admin approve-only and composite approve/publish/schedule remain explicit distinct commands with atomic durable outcome; no version fallback from a fresh read. A failed composite action must not leave tracked approval available for the next Save.
- Role-specific action/read projection across list/query/detail/mutation must match commands. After seller stop -> Disabled/Approved, the lawful return-to-draft/edit/resubmit path must still be offered. Test the ACTUAL stop -> fresh read -> return -> edit -> submit sequence.
- Current MapAdmin role union fix stays. Fix remaining unavailable/expired/invalid-schedule action projections without weakening eligibility; no force-activation or time-data rewrite.

## 4. Finish original queries and public attribution; this is not UI work

These were OPEN twice and remain mandatory:
- Real seller server-side query with owner tenant+shop mandatory, stable paging/count/filter/search.
- Admin shop HUMAN-NAME search/filter with owning Party Contracts; matching set/count must be complete for the actual contract, no silently truncated lookup producing wrong totals.
- Batch actual shop labels across list/detail/query/mutation; public card uses canonical public shop identity/name/link. Do not fabricate public id from Party Guid.
- Public shop scope where supported by canonical public identity, with foreign/draft/disabled exclusion. Fresh persisted public reader keeps B1 fix and exact start/end boundary semantics.
- Configured locale codes supported consistently; no hardcoded fa/en registry.
- Finish ui-contract-handoff.md with exact endpoint/body/query/response samples for both roles, versions/keys, asset picker/admin-for-shop upload, query columns/operators/counts, publication/readiness/blockers and errors. This must let the next worker implement the shared UI without discovering another unfinished backend contract.
- Frontend changes limited to typed adapter and necessary existing transport argument plumbing; no Orders-grid restyle, new editor or public-player redesign yet.

## 5. Required proof and truthful served process

Complete parent G1–G7 rather than substitute domain/pin tests:
A. Real Directory + explicit authoritative Party/Media/product ports: negative+positive shop/cover/slide/CTA, all-route stale/refusal and replay identity.
B. Real MediaDirectory recording storage: changed bytes conflict before another save, lost binding/local result failure + recovery, duplicate scoped key, store failure, caller state preserved; assert actual rows/object calls.
C. Real canonical PostgreSQL rollback-only: fresh-context public reader at exact start/end; scoped uniqueness/version/atomic publication; query count/shop-filter; Media binding/occurrence constraints. Use shared caller-owned transaction for fresh context materialization if needed, then rollback; separate post-rollback count committedRows0. Controlled store for file operations. No committed fixture/concurrent business writer.
D. Registered DI and actual owning MediatR/auth/transport seam proofs: absent/foreign actor/seller fail with zero writes. Statically counted routes are supplementary.
E. Retain directly affected previous tests, FE transport tests/tsc if touched, exact changed contract guards, Release build/publish after final code. Do not sweep unrelated projects or re-certify all modules.

Validate client target loopback/5432/tooba_alpha and server database/port BEFORE relational writes; missing/noncanonical is visible SKIP. No second DB/Docker/seed StoryFoundationTests. Test fixtures only inside rollback or isolated controlled ports; canonical media files untouched. Report controlled vs real proof precisely. Fix product defects these facts expose within THIS original scope.

Safe runtime handling of reported Development pricing.overlap:
- Do not run repeated startup seed attempts, mutate Pricing data, suppress a business exception or switch edition/database to get a green health.
- First identify actual listener PID/executable/start time/content root/loaded artifact. A disk DLL hash or an old serving process is NOT proof of newly served code.
- Prefer an existing documented no-business-bootstrap startup option.
- If none exists, this envelope narrowly authorizes ONE Host composition-only Development startup opt-out (default behaviour unchanged) guarding the startup bootstrap block, not module business logic. Select it only for the refresh PROCESS invocation; keep Development auth/tenant/edition/ports and appsettings files unchanged. Prove bootstrap delegates are NOT invoked under the option and host services/routes still resolve. Schema is applied by MigrationRunner, never a hidden startup seed.
- This does not authorize fixing Pricing rules, seeds, Catalog/Wallet/Shipping data or turning off normal authorization. If safe startup is genuinely impossible, report the exact external blocker while finishing all independent code/proof; do not claim served parity.
- After safe refresh record the actual new listener/process identity, health/ready/platform, loaded published artifact; no business POST. FE rebuild/restart only when permitted transport changed.

## 6. Applied schema and data protection

Canonical ONE tooba_alpha 127.0.0.1:5432 SingleStore/store-alpha, Host5088/Next3000. ZERO committed business/test rows, no live story/media mutation/upload, no orders/stock/money/provider/notification, no fixture/seed/backfill/reset/delete/restore, no keys/history rewrite. Exception: deliberate new-upload object compensation is proven only on controlled test storage here, never canonical file cleanup.

Parent281 and R1 migrations ALREADY APPLIED are immutable; no editing/reapply/Down. At most one minimal necessary additive repair migration per affected Story/Media module is permitted for the existing durability/scope requirements. Use existing fields whenever sufficient; no business backfill or generic workflow/lease platform.
Before any apply: model/SQL review, only intended pending plan, FRESH private pg_dump bytes/hash + pg_restore --list exit0 BEFORE apply, normal Tooba.MigrationRunner store-alpha, verify history and all pending0. No manual DDL. If none needed report ZERO.
No schema/edition/connection/port/settings changes beyond the precise process-only startup opt-out above.

## 7. Evidence/provenance and finish

R1 issuance receipt/local original file hash is 05ca4b11bccc68281174ce44fbc12a6b1af9809e73a0ec2e1afbc89190673e48 (16772 bytes); R1 worker receipt states 2BCA9E9A... . Append a factual clarification of actual executed bytes/claim ancestry; do not replace old TASK.md/reports or claim a hash comparison that was not performed. If Bridge delivered different bytes, preserve both identities and identify substantive delta instead of silently reconstructing.

Evidence docs/architecture/evidence/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2/:
TASK.md exact issued bytes, COMPLETION-LEDGER.md, RESULT.md, RECEIPTS.md, result-contract.txt, remaining-scope-closure.md, media-failure-proof.md, rollback-only-proof.md, schema-and-runtime-proof.md, ui-contract-handoff.md.
Exact envelope also docs/ai/tasks/TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2/TASK.md. Update compact recovery and story plan checkpoint; preserve previous PARTIAL reviews, Returns acceptances and future payouts backlog.

Completion means the remaining implementation AND its original proof are finished. PASS is not allowed while requirements above are OPEN. If unable to finish, return truthful PARTIAL with exact unfinished paths, the actual reason for stopping and attempts made; distinguish an external blocker from ordinary unfinished in-scope work. Merely listing “matrix not completed” does not explain why authorized independent work stopped. Never fabricate proof or claim user/Marketplace acceptance.

Task-ID: TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2
Status: PASS/PARTIAL
Expected-Base-SHA: ae947c863867437c13008029971a77eeb90edd41
Base-SHA:
Code-SHA:
Evidence-SHA:
Final-HEAD:
HEAD-Equals-Origin-Main:
Remaining-Media-Ownership-And-Durability:
AllPath-Version-And-Replay:
Stopped-Story-Review-Cycle:
Seller-Query-ShopFilter-CTA-PublicIdentity:
G1-G7-Actual-Proof:
Schema-State:
Startup-NoSeed-And-Served-State:
Live-Business-Write-State: ZERO
Evidence-Limits:
User-Acceptance: NOT_CLAIMED
Marketplace-Readiness: NOT_CLAIMED
UI-Delivery-State: PLANNED_NOT_ISSUED
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT

Single worker, no delegation. Finish the unfinished first delivery and STOP; no automatic UI task.

