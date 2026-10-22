# TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1 — RESULT

Task: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199-R1
Parent: TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199
Mode: BOUNDED_EDITOR_POSTCOMMIT_AUTH_AND_COMPLETE_ATOMICITY_PROOF
Expected base / verified `HEAD == origin/main`: `b0b3bb804228826a1144b9db86ee0c1362b372bf`
Channel: tooba-main · Worker: tooba-worker-01

Scope honoured: TESTS AND EVIDENCE ONLY. No production file changed (see byte-parity section). The
accepted TB199 implementation is retained unchanged; this task completes the proof omissions recorded in
`../TB-SHIPPING-ADMIN-ATOMIC-EDITOR-DOCUMENT-199/ARCHITECT-REVIEW.md`.

## A. New-route denials and post-commit sentinel (executed through the real delegates)

New fact: `New_editor_routes_deny_before_dispatch_body_reads_or_any_business_lookup`.

- **View-only admin.** The same harness creates the draft, then the admin edge is downgraded to
  `fulfillment.view`. `GET …/editor-document` → 200 (revision 1). A malformed PUT
  (`{"expectedRevision":1,"regionGroups":`) → **403 `admin.authorization.denied`** with measured
  `bodyReads == 0`, `ISender dispatch (Sent) == 0`, handler arrivals == 0, type-catalog reads == 0,
  context-source reads == 0, and draft revision still 1. The denial is therefore BEFORE any body read,
  dispatch, catalog/context lookup or write.
- **SingleStore seller.** `GET` and malformed `PUT` on `/v1/seller/…/{guid}/editor-document` both →
  **404 `shipping.authoring.owner_scope_unavailable`** with seller-resolution calls == 0, dispatch == 0,
  body reads == 0, catalog reads == 0, context reads == 0.
- **Marketplace seller with a CATEGORY-scoped manage grant.** The actor/seller pair IS resolved (that is
  how grants are read: 1 seller call) but the malformed PUT → **403
  `shipping.authoring.owner_scope_denied`** with body reads == 0, dispatch == 0, handler arrivals == 0,
  catalog reads == 0, context reads == 0, revision unchanged.
- Permissions, activity counters and request state are per-harness fixtures reset between phases; no
  global culture/permission state is mutated across facts.

Post-commit sentinel fact: `The_new_composite_handler_reads_the_catalog_once_and_a_later_read_failure_keeps_the_commit`.

- The scripted `IShippingTypeCatalogReader` returns ONE valid active fact on the first lookup and throws a
  named `SentinelCatalogException` on ANY second lookup (own read counter, immune to counter resets).
- A real composite PUT with a non-null `typeAssignment` runs the **NEW**
  `ReplaceShippingAuthoringEditorDocumentCommand` handler and its real command workflow, real
  `ShippingAuthoringDraftDirectory`, real EF/InMemory context and real HTTP delegate: **200**,
  `catalogReads == 1`, response `draftId` equals the requested draft, `revision` 1 → 2, and
  `type.shippingTypeId` equals the requested type. Persisted revision is exactly +1.
- A **subsequent deliberate GET** genuinely hits the throwing dependency and fails: labelled separately as
  a READ failure (**500**), with `catalogReads == 1` for that call and persisted revision, UpdatedAt and
  the stored `shipping_type_id` all unchanged by the failed read.
- Clear path in the **same** new handler, with the catalog still poisoned: PUT
  `typeAssignment.shippingTypeId = null` → 200, **catalogReads == 0**, revision 3, `type.shippingTypeId`
  null.

## B. Strengthened EXISTING relational storage facts (real PostgreSQL 17)

`ShippingAuthoringEditorDocumentStorageTests` now seeds ONE complete nontrivial graph per fact through the
canonical directory API (two groups, three rate rows with distinct amounts, two languages with distinct
descriptions, a group title, a seven-day schedule, both method settings, an explicit type) and compares
**one structural snapshot** of every authored root scalar and every own child row: root
id/owner/binding/type/revision/created/updated; ordered group ids/order/currency; all area ids + kind/node
+ parent group; all rate-row ids/parent/three amount bounds/fees/durations; translation language/name/
description; group-title language/text; schedule header/timezone/timestamp and seven days with remembered
hours; settings values/timestamp. Null and empty are distinct tokens; timestamps are normalized to
PostgreSQL microsecond precision so a sub-microsecond round-trip cannot be mistaken for a change. No
record-reference or count-only equality is used.

- **Non-vacuous no-op.** A test-owned SQL update first moves the persisted root `updated_at` ~3 days into
  the past; the baseline snapshot then contains that older stamp. Resubmitting the SAME non-empty document
  at the current revision preserves the EXACT root timestamp and revision and the complete snapshot, and a
  stale identical document still returns the canonical 409.
- **Late invalid child in the same context.** Valid earlier edits plus a late-invalid child in a
  brand-new group are rejected before any tracked mutation: the already-loaded aggregate stays
  value-identical, every tracker entry is `Unchanged`, a later `SaveChanges` writes 0 rows, and a fresh
  complete snapshot equals the baseline. A retained row id taken from the REAL other group is rejected as
  `rate_row_payload_invalid` (not claimed as foreign-identity proof from a random guid); an unknown group
  id is rejected as `region_payload_invalid`. A valid retry at the ORIGINAL revision in the SAME context
  then succeeds exactly once and matches a fresh complete snapshot.
- **Race.** The existing first-`SavingChanges` barrier is preserved: both participants arrive, and the
  recorded `OriginalValues[Revision]` is the real original token for both. Exactly one typed success and
  one canonical `draft_revision_conflict`; root revision exactly +1. The winner is derived from typed
  outcomes, never from stored text; the complete winning proposal (schedule/type/unchanged languages/all
  rows/selections, server ids taken from the actual returned aggregate) equals a fresh snapshot with no
  hybrid. The ACTUAL loser's tracker is asserted clean BEFORE its later `SaveChanges` returns 0, and a
  re-read through the loser context equals the winning snapshot.
- **Failure then cancellation.** After the FIRST staged save completed, the SECOND is failed with ONE named
  sentinel instance: the surfaced exception is that instance (unwrapped only if the provider wrapped it),
  attempts == 2, completed == 1, tracker clean, and a fresh complete snapshot equals the baseline
  IMMEDIATELY after the failure (before any cancellation attempt). Then real caller cancellation at the
  same boundary: `IsCancellationRequested` true, the propagated `OperationCanceledException.CancellationToken`
  equals the caller's own token, attempts == 2 and completed == 1 (the second save genuinely does not
  complete), tracker clean, later `SaveChanges` 0, fresh snapshot baseline, and a retry at the original
  revision in the SAME writing context succeeds once with the complete expected tree and no duplicated
  residue (exactly one new group).

## C. Static transaction boundary (test code only)

`ShippingTransactionBoundary` keeps the single-begin rule and now also verifies, for the reviewed typed
helper in its exact file: at least two staged `SaveChangesAsync` sites, a `CommitAsync`, a `RollbackAsync`
path INSIDE `SaveAuthoringReplacementAsync`, and a caller universe that is EXACTLY
`{ReplaceRegionGroupsAsync, ReplaceEditorDocumentAsync}` with each call an awaited direct call (no
delegate/`Func<`/`Invoke(` indirection). Executed synthetic negatives (in
`OperationalQuoteCompatibilityTests`): third caller in the same file, callback indirection from an allowed
caller, removed commit, removed rollback, removed required staged save, second begin in the reviewed
member, extra transaction in another member of the same file, extra transaction in another Shipping file,
startup-migration inside the allowed file, and operational money/capacity leak. Startup-migrate and
operational money/capacity negatives are unchanged.

**Honest static limit.** Marker/member checks are neither control-flow nor data-flow enforcement: they
cannot prove the commit is reached after both saves on every path, nor that every failure returns through
the rollback. Runtime atomicity is proven only by the executed PostgreSQL facts above.

## Production byte parity

`git status --porcelain` for production paths is empty: only
`Tooba.Shipping.Tests/Authoring/ShippingAuthoringEditorDocumentEndpointTests.cs`,
`Tooba.Shipping.Tests/Authoring/ShippingAuthoringEditorDocumentStorageTests.cs`,
`Tooba.Shipping.Tests/Compatibility/ShippingTransactionBoundary.cs`,
`Tooba.Shipping.Tests/Compatibility/OperationalQuoteCompatibilityTests.cs` plus evidence/task/recovery docs
changed. No Domain/Application/Infrastructure/Contracts/Endpoints/DI/Host/frontend/schema/migration/
designer/snapshot/manifest/skill/AGENTS change.

## Commands actually run (filters, counts)

- `dotnet build …Tooba.Shipping.Tests.csproj` — compiled Shipping.Tests + its project references only. No
  whole-solution/Host/Fulfillment build was used as proof.
- `dotnet test … --filter "FullyQualifiedName~OperationalQuoteCompatibilityTests"` — 21 passed.
- `dotnet test … --filter "FullyQualifiedName~ShippingAuthoringEditorDocumentEndpointTests"` — 6 passed.
- `dotnet test … --filter "FullyQualifiedName~ShippingAuthoringEditorDocumentStorageTests"` — 4 passed
  against disposable PostgreSQL 17 (`127.0.0.1:55432`, `shipping_tb190r1`, `tb190r1`), verified
  `current_database()`/`current_user()` before DDL.
- Final single combined focused run (`EndpointTests | StorageTests | OperationalQuoteCompatibilityTests`)
  — **31 passed, 0 failed**. No whole-645 rerun, no confidence loops, no Host/Fulfillment/solution run.

Executed vs reused: 2 NEW endpoint facts + 4 strengthened storage facts + 3 NEW compatibility facts
executed; the remaining endpoint/storage/compatibility facts in those classes were reused unchanged (and
ran in the same focused run).

## Harness corrections (disclosed, not hidden)

Four bounded correction rounds were needed (the task budgeted two — reported honestly):
1. Late-invalid row error code: the domain rejects the reversed delivery interval as
   `rate_condition_invalid` before duration validation (`rate_duration_invalid` wrong assumption).
2. Timestamp comparison: PostgreSQL microsecond precision vs in-memory 100 ns ticks — added exact
   microsecond normalization in the snapshot helper.
3. Caller cancellation: cancelling the CTS inside the interceptor let the second save complete and the
   commit observe cancellation; the interceptor now cancels the caller token and then surfaces it, so the
   second save genuinely does not complete (attempts 2 / completed 1).
4. Two over-eager assertions removed (a meaningless baseline `Contains` and a residual-node assertion that
   contradicted the successful retry) plus a stray brace from the edit.

## TB199 bookkeeping correction (append-only, evidence-based)

- Bridge rows: TB199 created `2026-10-22T21:01:01.3319435Z`, claimed `2026-10-22T22:17:19.8903939Z`,
  completed `2026-10-22T22:46:07.5908908Z` → ~28.8 minutes claim→complete, i.e. inside the 30-minute
  box but already beyond the "stop new work at minute 23" line.
- The previously reported "UTC 22:5x" is the R1 ISSUE time (`2026-10-22T22:50:24.2602287Z`), not the
  TB199 finish time. The earlier result's "only one proof missing" claim was inaccurate; the Architect
  review lists four omissions (new-route denials, complete snapshot/token/time coverage, scanner scope,
  and the catalog sentinel), all addressed above.
- Whether a whole-solution build was attempted in the TB199 session cannot be reconstructed from the
  available artifacts: **UNKNOWN_NOT_RECONSTRUCTABLE**. What is certain for THIS task: only the
  Shipping.Tests project (and its references) was built and only the focused filters above were executed.
- This task's claim: bridge row `claimedAtUtc = 2026-10-22T22:52:16.403383Z`; finish/result posting time
  is recorded by the Bridge completion timestamp, not invented here.

## Cleanup

Disposable container `tb199r1pg` removed; `127.0.0.1:55432` verified free. No shared database, port 5432,
image download, Redis/RabbitMQ, shared app port or unrelated service touched. No credentials logged.
Unrelated pre-existing untracked evidence files were preserved, not cleaned.

Production-Functional-Acceptance: PENDING
Shared-Database-Data-Flag-Changes: NONE
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
