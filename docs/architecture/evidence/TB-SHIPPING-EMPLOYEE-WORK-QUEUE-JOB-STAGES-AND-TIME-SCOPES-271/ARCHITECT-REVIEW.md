# TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271 — ARCHITECT REVIEW (PARTIAL, NOT ACCEPTED)

Reviewed `main` `09cbaa436a03310f0cb46e6e622824f938d6c728`: classifier, counts builder, query engine, actual query
handler/policy, queue screen/columns and the relevant test source. The existing worker proofs do not cover the actual
handler/normalize/query interactions below.

Status **PARTIAL — NOT ACCEPTED**. The original 271 RESULT.md is preserved unchanged; this review is the checkpoint
that 271-R1 repairs. Five job stages and the Today/Future/All UI remain a good base.

## 1. ALL PAGE VS COUNTS

`AdminFulfillmentWorkQueueCountsBuilder.Select` → `InSelection` → `MatchesStage` accepts EVERY stage when
`selectedStage=all`, including `history`; `!IsActive(row)` then returns true. But `Build` computes `All`/`SelectedTotal`
from active only. A history row inside the classified set can be returned on the active «همه» page while missing from
its badges/total.

## 2. ARCHIVE BADGE DEPENDS ON VIEW

`ClassifyAsync` excluded `FulfillmentStatus.Cancelled/Delivered` unless the selected stage was `history`, BEFORE
projection/counts. The archive count was therefore 0/missing on an active view and changed when history was selected,
even for the same search. A source-status prefilter contradicts one shared complete physical row set.

## 3. BLOCKED ACTIVE WORK DISAPPEARS

`ResolveStage` ended in `History` for a `pendingDispatch` group with no capability, and marked no-forward-code rows
history without positive evidence that the physical obligation was closed. A pending group whose
permissions/payment/capture/source are blocked is not completed history. `AvailableActionCodes == []` is not proof of
no work.

## 4. CLAIMED ONE PASS IS TWO READS

`QueryAdminFulfillmentWorkQueueHandler` called `_query.QueryAsync` and then `_query.CountAsync`; BOTH independently
called `ClassifyAsync`/loaded facts/read `_clock.UtcNow`. There was no shared classification snapshot, so rows and
counts could differ under a change/time boundary, and every read happened twice. Sharing the builder's definition is
not sharing the row set.

## 5. DEFAULT ORDER UNREACHABLE

`AdminFulfillmentGridQueryPolicy.Normalize` injected `updatedAt desc` when `Sort` was empty. `QueryEngine.ApplySort`'s
overdue/job-time branch ran ONLY if sort was missing. The actual default handler therefore bypassed the new priority
ordering, despite a helper test being green.

## 6. ADVANCED FILTER SILENTLY IGNORED

`NormalizeAdvanced` accepted `jobStage`/`timeScope` through `AllowedFields`/`OperatorsByField`; `EvaluateAdvancedAsync`
then called `ApplyFilterAsync`, whose switch lacked these fields and defaulted to the unfiltered source. They were NOT
refused as reported. A filter that cannot be evaluated must not be silently accepted.

## 7. ZONE CLASSIFICATION

The new job `StateOf` called the old `AdminFulfillmentDispatchTimingAuthority.LocalDate`, whose `ResolveZone` silently
used UTC on missing/invalid ids. The new stage/time projection must not classify unknown-zone facts as an accepted
local future/today date. Old helper semantics outside this task are preserved; the new classifier boundary is repaired.

## Good base to preserve

Exact physical `WorkUnitKey`, current row/plan binding, pure read-only job semantics, own-plan/first-dispatch
distinction, one operator link, independent controls/legacy URL normalization and the 64px existing grid.

## Scope of the repair

One cohesive query selection/count/default-order repair (issued as 271-R1), not authorization to reopen
shipping/payment/Returns architecture. No independent Architect runtime/test execution was performed for this review;
no user acceptance or complete-shipping claim. 270-R4 remains accepted within its bounded repairs. The R2 backup audit
and the true-concurrency limitation are unchanged.

`STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
