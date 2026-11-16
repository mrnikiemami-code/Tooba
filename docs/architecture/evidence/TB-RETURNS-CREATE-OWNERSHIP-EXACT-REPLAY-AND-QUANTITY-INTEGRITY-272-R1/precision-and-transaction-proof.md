# TB-272-R1 (A + B) — quantity precision, full binding, and caller transaction scope

Scope: the ordinary (customer-delivered) creation path of the Returns module.
Sources:
`src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.Creation.cs`,
`.../Directories/ReturnCreationSerialization.cs`,
`src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/ReturnRequest.cs`,
`src/backend/Modules/Returns/Tooba.Returns.Domain/ValueObjects/PersistedReturnQuantity.cs`,
`src/backend/Modules/Returns/Tooba.Returns.Application/Validation/ReturnsRequestValidators.cs`.

## 1. A — why rounding for comparison only was not enough

The persisted column is `returns.return_items.quantity numeric(18,6)` (money/price `numeric(18,4)`). TB-272 compared
the stored intent after rounding the *input* to six decimals, but the invoice/eligibility/snapshot path used the raw
value. Three concrete defects followed, each of which the repair now refuses instead of rounding:

| Submitted | `numeric(18,6)` stores | Consequence before |
| --- | --- | --- |
| `1.0000001` and `1.0000002` | both `1.000000` | two *different* requests compare equal ⇒ a changed quantity could be answered as the original |
| `0.0000004` | `0.000000` | a positive request persists zero physical units |
| `0.0000006` | `0.000001` | the tracked response (`0.0000006`) differs from a fresh read (`0.000001`) |

Fixed by one canonical rule and three enforcement points, all refusing rather than rounding:

1. `PersistedReturnQuantity.Scale = 6`, `IsExactlyRepresentable(q) => q == decimal.Round(q, 6, AwayFromZero)`.
2. `CreateReturnCommandValidator` → `returns.validation.quantity_precision` (endpoint-reachable, before the handler).
3. `ReturnDirectory.CreateInternalAsync` → `return.quantity_precision` typed fault. **Ordering matters:** it is placed
   after the mutation gate, order binding, ownership and the duplicate-shape refusal, and **before** the stored-key
   read and before the transaction/lock. An invalid quantity therefore cannot receive an existing snapshot as
   success and writes nothing.
4. `ReturnRequest.Build` → `returns.quantity_precision` for every line, so the aggregate invariant holds for every
   present or future caller (courier-origin included).

Legitimate values are unchanged: `0.000001` stays accepted and persists exactly; `1` and `1.000000` remain numerically
equal, so trailing zeros are not a different intent.

`return.quantity_precision` is HTTP-reachable (HTTP 400, `ErrorClassification.Business`), catalogued in
`ReturnsErrorCatalogContributor`, and localized in `ReturnsErrors.resx` / `ReturnsErrors.fa.resx`. No hardcoded
language count was introduced; the registry/resources remain the single source.

## 2. A — full immutable binding

`OrdinaryCreationIntent` (one deterministic, normalized comparison) now covers:

| Field | Source |
| --- | --- |
| `SellerOrderId` | `OrderReturnContextSnapshot.SellerOrderId` |
| `CheckoutId` | `OrderReturnContextSnapshot.CheckoutId` **(new in R1)** |
| `SellerPartyId` | `OrderReturnContextSnapshot.SellerPartyId` **(new in R1)** |
| `RequestedByUserId` | `OrderReturnContextSnapshot.PlacedByUserId` |
| `Origin` | must equal `ReturnOrigin.CustomerDelivered` on the stored row |
| `RefundDestination` | submitted command |
| Reason | `Trim`med on both sides |
| Lines | ordered `(OrderLineId, Quantity)` set |

A stored row carrying another checkout or seller party is refused with `return.idempotency_conflict` — no payload, no
write, no borrowed snapshot. Deliberately unchanged: the mutable operational refund destination is **not** promoted to
a historical authority, no creation metadata/schema was added, and approval behaviour is untouched.

## 3. B — collision recovery that neither clears the tracker nor ends a caller's transaction

Before: the `DbUpdateException` catch called `_db.ChangeTracker.Clear()`, detaching every tracked entity — including
the caller's pending work — and, when a caller already owned the transaction, the code had no savepoint so recovery
could not preserve it.

After:

```
ownedTransaction  = ownsTransaction ? BeginTransactionAsync() : null
callerTransaction = ownedTransaction is null ? _db.Database.CurrentTransaction : null
trackedBefore     = SnapshotTrackedEntities()          // immediately before Add
if (callerTransaction is not null) CreateSavepoint(tb272_creation_intent)
Add + AddRange + SaveChanges
if (callerTransaction is not null) ReleaseSavepoint(tb272_creation_intent)
if (ownedTransaction  is not null) Commit
```

and on a genuine idempotency collision:

```
if (ownedTransaction  is not null) Rollback(owned)                       // this attempt owns it
else if (callerTransaction is not null) RollbackToSavepoint(tb272_creation_intent)  // caller keeps ownership
DetachFailedCreation(trackedBefore)                                     // only this attempt's entries
re-read winner → same authorization + exact-intent comparison
```

- Entries that were already tracked keep their instance (`Assert.Same`), state and values; the caller's dirty entity is
  still `Modified` with the same `Status`/`PaymentId`/`Reason` after recovery.
- The caller's transaction is never committed or rolled back; it remains usable and its own rollback removes the test
  facts (proven on real PostgreSQL).
- If savepoint recovery is impossible, the provider failure propagates honestly — it is not reclassified.
- The arbitration is still exact: only SQLSTATE `23505` on `returns.return_requests` /
  `ix_return_requests_idempotency_key` is recognised; a real `pk_return_items` violation is asserted *not* to be
  classified as one.

## 4. B — the ordinary factory has no bypass

`ReturnRequest.Create` lost its public `enforceOneLinePerOrderLine` parameter. It always enforces one line per order
line and the precision rule. The courier-origin factory (`CreateCourierOrigin`) uses the same private `Build` path with
its legitimate `(order line, condition)` identity, so the recorded per-line condition now comes from `Build` instead of
a clear-and-rebuild post-step. No courier claim, stock, disposition or workflow behaviour changed.

## 5. Test mapping (owning boundary, not helper-only)

| Requirement | Test |
| --- | --- |
| `1.0000001` / `1.0000002` / `0.0000004` / `0.0000006` refused; `0.000001` and `0.0000010` accepted and replay alike; invalid never answered by a stored snapshot; no case/item written | `Unrepresentable_quantities_are_refused_before_any_write_or_replay` |
| aggregate refuses and exposes no bypass flag | `The_aggregate_refuses_unrepresentable_quantities_and_keeps_no_bypass` |
| validator refuses over-precision, accepts six digits | `The_transport_validator_refuses_unrepresentable_quantities` |
| wrong checkout / wrong seller party cannot replay | `A_stored_case_with_another_checkout_or_seller_binding_is_not_replayed` |
| caller's dirty tracked work survives a refused creation; nothing leaks into the tracker | `A_refused_creation_preserves_unrelated_caller_tracked_work` |
| (real PostgreSQL) persisted six-decimal quantity equals the tracked answer; precision refusal precedes comparison; checkout/seller binding refusals write nothing | `Persisted_quantity_precision_and_the_full_binding_hold_inside_one_transaction` |
| (real PostgreSQL) collision restores the savepoint, detaches only this attempt, keeps the caller transaction usable and the caller's `Modified` entity intact, then creates normally | `A_key_collision_removes_only_this_attempt_and_keeps_the_caller_transaction_usable` |
| (real PostgreSQL) real `23505` on the real index, exact constraint/table/schema, savepoint recovery | `A_real_unique_violation_inside_a_caller_transaction_is_exactly_classified_and_savepoint_recoverable` |
| (real PostgreSQL) `pk_return_items` is not reclassified | `Only_the_idempotency_index_is_classified_as_a_key_collision` |

## 6. Limits

- The **owned-transaction** branch (no caller transaction) is not executed by the repaired harness: a successful owned
  creation commits by definition, and committing fixture rows on the canonical database is forbidden by this task. It
  differs from TB-272 only by the commit call receiving the own-transaction handle and by scoped detachment in the
  catch; that residual gap is declared rather than papered over.
- No real PSP/Wallet/Inventory/provider operation is reachable from these tests: the controlled ports record and
  assert zero external effects, and no live case was approved, rejected or refunded to obtain proof.
