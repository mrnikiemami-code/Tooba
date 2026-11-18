# TB-279-R3 — owner proof (B1/B2) and controlled-port effect proof (C1/C2)

## 1. One decision authority, one version gate (B1)

`ReturnDirectory.Corrections.cs` → `ResolveExistingDecision` (the function shared by approve and reject):

1. `TryResolveReplayAsync` runs FIRST, so an exact replay of the effective decision (same immutable reviewed identity)
   answers its stored fact with zero new effect and needs no version.
2. Then, still inside the owning `FOR UPDATE` scope, a NEW decision over undecided/reopened state requires the reviewed
   revision:

```csharp
if (expectedVersion is not { } reviewedVersion)
{
    throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
}

if (reviewedVersion != state.EffectiveVersion)
{
    throw new ContractOperationException(ReturnsErrorCodes.CorrectionVersionStale);
}
```

`ReturnAdminOperationsAdapter.ApproveAsync/RejectAsync` forward `expectedDecisionVersion`/`expectedResolutionVersion`
verbatim. The R2 back-fill (`??= current.DecisionVersion`) was removed, so the reachable Order/admin shape cannot take an
unreviewed decision against whatever the store held at send time.

Owner facts: A1–A4 rows of `closure-matrix.md`, all state assertions on counts/status/amount/allocation (never a source
regex).

## 2. Held goods authority (B2)

`ReturnHeldGoodsPolicy.Describe` (Application, pure, no I/O) answers the only question that matters for capacity:

| Evidence | Result |
| --- | --- |
| reviewed plan | `accepted (both conditions) + outstanding rejected-received per condition after its own handbacks` |
| no plan | every coherently received unit, per condition, minus that condition's handbacks |
| incoherent receipt | `OutstandingHeld = requested`, `IsUnprovable = true` — never zero |

Read path: `ReturnEligibilityEvaluator.ReadOccupancyAsync` collects REJECTED/CANCELLED cases of the seller order, skips the
lines that already own a LIVE allocation row (the row is the claim — no double count), and for every other line derives
held quantity from `ReturnReceivingSummary` + the latest complete `ReturnItemResolution` + `ReturnItemCustody`, then adds
it to `legacyUnallocated`. `ReturnAllocationPolicy.Describe` either subtracts it from a single provable slice or marks
EVERY plausible slice (expired included) `LegacyAmbiguous`, and `Allocate` refuses.

Write path: `ReturnDirectory.Allocations.cs` `DescribeRejectedHeldAsync` runs BEFORE the decision/status row is touched and
refuses an incoherent receipt with `return.recovery.evidence_conflict`. `ReleaseRejectedDeliveriesAsync` receives that
same map, so the release and the decision cannot disagree; a line whose held quantity cannot be attributed falls back to
the typed ambiguity path instead of silently returning.

Owner facts: B1–B4 rows of `closure-matrix.md`.

## 3. Effectful owner proofs with scripted ports (C1/C2)

`ReturnsFinalClosure279R3Tests` uses the REAL `ReturnDirectory` with ports that SUCCEED and RECORD — the opposite of the
retained throw-if-called negative doubles, which remain in place.

- C1: `requested/received 3 usable`, reviewed `accepted 2 / rejected-received 1`, decision-only approval freezing
  `2 × unit price`, then `StartFirstRefundAsync` on the real shared reservation/outcome/settlement path. The assertions
  read the actual port arguments: gateway amount and key = stored attempt, Inventory `(reservationId, 2, key)`. The
  rejected unit is still owed (`PendingDisposition`), so money success never claims physical settlement.
- C2: a positively bound FAILED attempt of 60 on a 100 payment plus a sibling Pending obligation of 60 on the SAME
  payment. The reviewed retry (`ExpectedFailedAttemptId` + reviewed decision version) is refused with
  `return.refund.budget_exceeded` and mints nothing. The lawful control (sibling 40) mints exactly the deterministic child
  (`ReturnRetryIdentity`), sends one gateway call and one accepted-usable restock of 1, and a duplicate reviewed intent
  mints nothing. A missing payment authority refuses with `refund.payment_missing`.

Limits: these are controlled in-process ports; no PSP/Wallet/Inventory service was called and no row of the canonical
database was written. The real-PostgreSQL rollback-only facts retained from earlier tasks keep their stated narrow scope
(lock/budget-query/atomic approval) and are not re-described as executing this retry reservation.
