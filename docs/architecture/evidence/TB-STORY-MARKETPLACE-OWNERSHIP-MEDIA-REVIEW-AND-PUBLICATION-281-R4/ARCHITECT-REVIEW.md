# TB-281-R4 ARCHITECT REVIEW — 2026-11-19

Source reviewed independently at code 1c7a7789344f1367875f882b687aa608dd5a0fdd; origin/main aad32713b7486c429dbbfb5e948e95e30eb6bb67 (docs-only final-head addendum).
Verdict: REPAIR REQUIRED / delivery ONE remains PARTIAL. Worker PASS is not accepted as complete A/B/C.

## Retained progress
Delete/reorder/composite transport now has reviewed-version and occurrence fields. Platform product CTA has Catalog validity lookup, schedule has a stored-content gate. Media pre-save and after-save interceptor tests are real progress; exact PostgreSQL unique identity replaces substring matching. Real PostgreSQL tests now exist. No new migration. Do not reopen these implementations merely to repeat work.

## Source-confirmed failures in the claimed closure
A1. StoryDirectory.SellerUpdateAsync, AdminUpdateItemAsync and SellerUpdateItemAsync neither RequireOccurrenceKey nor replay nor RecordOccurrenceAsync. Their DTO may carry the key but the owner ignores it. R4 all-route-matrix says required; source disagrees.
A2. AdminUpdateAsync hash contains title/CTA only, omitting Locale, Market, CoverMediaAssetId and CoverMediaUrl that the same method writes. It also pipe-concatenates title/type/target before wrapping this one string in length-prefix encoding; inner delimiter ambiguity remains. A same-key changed-cover/locale request can be answered as the prior update, not conflict.
A3. AdminSetScheduleAsync checks version and current content before TryReplayMappedAsync. After successful schedule increments version, replaying the original reviewed command fails VersionStale instead of reading its recorded occurrence. Seller edit replay must also precede editable-state refusal.
A4. Story create/add SaveChanges and SaveStoryAsync have no scoped occurrence unique arbitration. A sequential replay fact cannot prove duplicate-winner folding under concurrent insertion. Two contexts can pass prior-read then collide at the PK. Match only exact owning occurrence constraint/table/schema and preserve unrelated tracked state; do not claim a stress harness is required.
B. Media UploadForSellerAsync guard checks Database.CurrentTransaction only. This is not System.Transactions.Transaction.Current or provider explicit enlistment; the report calls it ambient/caller protection although only an EF-owned transaction is proven. Use existing supported transaction authority (the Returns guard is a local reference) before object effect; no raw-connection reflection or transaction framework.
C. Relational proof descriptions exceed actual tests:
- C1 uses the same db/context for writes and reads, has no expired case, and never calls the admin grid/shop-name filter/page/count. Source is a read test on PostgreSQL, not the claimed full package.
- C2 inserts a raw duplicate in a savepoint then calls tracked.EnsureExpectedVersion directly. It does not run the directory stale command, winner recovery, or EF concurrency through two contexts.
- C3 correctly proves EF CurrentTransaction refusal and intercepted pre-save failure. It does not prove a TransactionScope ambient or an explicit provider enlistment. It prints committedRows=0 for Media keys without an independent occurrence/binding verification.
- Media unit fact named leaves_caller_entities stages and asserts no caller entity; after-save test uses same context, not fresh-context verification.
These are missing counterexamples in the already-issued tests, not newly invented features. Keep original reports intact; append truthful correction.

## Decision and bounded continuation
Issue only 281-R5 for A/B/C concrete methods and missing behavior assertions. No UI redesign, new schema or broad test replay. Require a test-to-assertion ledger; no OPEN-free matrix by declaration.
Owner still wants one task at a time and a prompt usable UI delivery. Do not hide product defects to move stages. Shared Orders-style UI remains NOT ISSUED until these owning-path defects are reviewed closed.
Mandatory seller surface: existing purchased-template VendorShell, /vendor-panel/stories and story.view. Returns acceptances, payout backlog, key state and user data unchanged.
Architect did remote source review; did not execute Windows tests or claim independent runtime validation.
