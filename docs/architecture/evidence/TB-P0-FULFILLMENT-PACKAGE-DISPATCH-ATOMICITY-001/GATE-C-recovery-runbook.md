# GATE C — Recovery of the existing partially-dispatched package (design only, NO live mutation)

Verdict: **PARTIAL — recoverable through an existing public command *after* deploying this fix;
any state reset is BLOCKED_RECOVERY_REQUIRES_ARCHITECT_DECISION.**

**No mutation was performed.** No dispatch, no cancel, no tracking write, no SQL repair, no
migration. Only read-only `SELECT` statements and public-command *invariants* were inspected.

## 1. Confirmed live state (read-only)

Tenant database `tooba_alpha`, schema `fulfillment`:

```
package 01a12f79-6ef1-7000-8578-dd7cc841195d / MP-01A12F796EF1
  status              = Created
  tracking_reference  = 125444
  dispatched_at       = NULL
  checkout_id         = 01a12e97-ee9e-7000-826c-a9eca536b223

member shipment 01a12f79-1ad4-7000-9174-3237c0d645bf  status=Dispatched  tracking=125444-01A12F79
member shipment 01a12f79-33ed-7000-b7ad-e562dcf67be8  status=Created     tracking=NULL
```

(Raw output: `live-package-state-readonly.txt`.)

## 2. Is this state even representable through public commands? NO

`CreateConsolidatedPackageAsync` rejects any member that is not `Created`:

```csharp
if (shipment.Status != ShipmentStatus.Created || shipment.DispatchedAt is not null)
{
    throw new ContractOperationException("fulfillment.package.shipment_not_eligible");
}
```

`DispatchShipmentAsync` / `DeliverShipmentAsync` are blocked while a shipment is an active member
of a package (`EnsureShipmentNotLockedByPackageAsync` → `fulfillment.shipment.locked_by_consolidated_package`).

⇒ `package = Created` together with `member = Dispatched` is **not reachable** through the current
public API. It could only be produced by the pre-repair non-atomic loop. This is why a
"reproduce the live state with a test" is impossible and why the `NeedsDispatch == false` skip
branch exists as **defensive** logic, used for exactly this legacy state.

## 3. Which invariants block a reset

| Intent | Public command | Blocking invariant |
| --- | --- | --- |
| cancel the stuck package | `CancelConsolidatedPackageAsync` | **Allowed** (only blocks when the *package* is `Dispatched`/`Delivered`); it releases the member lock. It does **not** fix the package. |
| cancel the already-dispatched member | `CancelShipmentAsync` | `Shipment.CancelPreDispatch` throws `fulfillment.shipment.cancel_invalid` for `Dispatched` → **impossible** |
| un-dispatch / reset the member | — | no such command exists |
| clear or rewrite `125444-01A12F79` | `CorrectTrackingAsync` | refuses to modify a dispatched shipment; and rewriting would falsify a shipment that a real carrier may already hold |
| deliver the package | `DeliverConsolidatedPackageAsync` | requires package `Dispatched` → `fulfillment.package.deliver_before_dispatch` |
| rebuild a replacement package with the same shipments | `CreateConsolidatedPackageAsync` | needs `Created` members; member 1 is `Dispatched` → `fulfillment.package.shipment_not_eligible` |

⇒ Cancelling is technically permitted by the domain but leaves a **permanently dispatched member
with a live tracking reference** and cannot be used to rebuild. It is a business decision, not a
repair.

## 4. Runbook — OPTION 1 (recommended): complete the package with the public command

**Precondition: this commit is deployed to the Host. Without Gate A the retry reproduces the
collision and fails again.**

1. Obtain explicit Architect/user approval for the write (it mutates the live order).
2. Confirm the deployed binary contains the repaired generator
   (`BuildMemberTrackingReference`, full-`N` suffix).
3. Re-run, through the normal admin surface, the *same* action that previously failed for package
   `MP-01A12F796EF1` (admin order detail → consolidated package → «ارسال بسته» /
   dispatch package). No script, no SQL, no direct directory call is required — this is the
   ordinary public command `DispatchConsolidatedPackageAsync`.
4. What the repaired command does, in order:
   * member 1 is already `Dispatched` → **skipped**, its reference `125444-01A12F79` is preserved
     and never rewritten;
   * member 2 has no reference → generates
     `125444-01A12F7933ED7000B7ADE562DCF67BE8` (41 chars ≤ 128), validated free by the single
     pre-check query;
   * member 2 is dispatched, then `MarkDispatched` sees
     `{member1: Dispatched, member2: Dispatched}` and the package becomes `Dispatched`
     in the **same single commit**;
   * inventory reservations are consumed for member 2 only (member 1's `ReservationConsumed` flag
     is already persisted, so it is not re-consumed).
5. Verify (read-only):
   * `consolidated_packages.status = Dispatched`, `dispatched_at` set;
   * member 1 unchanged: `Dispatched` / `125444-01A12F79`;
   * member 2: `Dispatched` / `125444-01A12F7933ED7000B7ADE562DCF67BE8`;
   * no third row in `fulfillment.shipments` carries either code (global unique index intact).
6. If step 3 fails, **stop and report** — do not fall back to SQL.

Rollback consideration: the commit is additive (statuses only advance); there is no automated
undo, which is exactly why step 1 requires approval.

## 5. Runbook — OPTION 2 (business decision, NOT a repair)

If the business decides the order must not be shipped:

1. Architect/user approval required.
2. Cancel the order through the normal order-cancellation flow (which voids `Created` packages).
3. Accept explicitly and in writing that member shipment
   `01a12f79-1ad4-7000-9174-3237c0d645bf` remains `Dispatched` with reference `125444-01A12F79`
   forever and that the package cannot be rebuilt from these shipments.

## 6. Explicitly BLOCKED

* `BLOCKED_RECOVERY_REQUIRES_ARCHITECT_DECISION`: resetting member 1 to `Created`, clearing or
  rewriting its tracking reference, cancelling a dispatched shipment, or any direct
  `UPDATE`/`DELETE` repair.
* Not attempted, not recommended, not authorized here.
