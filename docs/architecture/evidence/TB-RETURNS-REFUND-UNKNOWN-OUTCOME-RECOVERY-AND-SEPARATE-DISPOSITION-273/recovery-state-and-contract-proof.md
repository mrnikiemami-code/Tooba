# TB-273 — recovery state and contract proof

Scope: section A (one authoritative recovery description, reused by read and action) and section B/C contract shape
(the explicit intent, the wire, the errors, the additive read fields). The money/stock behaviour itself is proven in
`same-attempt-and-disposition-proof.md`.

## 1. Source of truth: stored facts only

`ReturnRecoveryPolicy.Describe(ReturnSnapshot)` — `Tooba.Returns.Application/ReturnRequests/Models/ReturnRecoveryDescription.cs`.

Inputs it is allowed to read: the case status and origin, the refund attempts (status + id), the item dispositions.
It performs no I/O, so it cannot contact Payment, Wallet or Inventory, cannot retry money and cannot create a durable
case or attempt. Its signature makes that structural, not a convention.

The same function is invoked from two places:

| Caller | Purpose |
| --- | --- |
| `ReturnDirectory.RecoverAsync` (via `MapSnapshotAsync`) | decides whether the requested intent is executable at all |
| `ReturnDirectory.MapSnapshotAsync` → `snapshot with { Recovery = … }` | every admin/customer/seller/detail read carries the same description |

Because there is one function and one snapshot shape, a read and an action cannot disagree about a case. The read
never persists anything: `Recovery` is a projection on a record that is materialised per call.

## 2. The six required distinctions

| Required distinction | Phase | Where it is visible |
| --- | --- | --- |
| no approved financial attempt / nothing to recover | `None` | `Phase=None`, `RecoveryKind=null`, both completion booleans false |
| unknown/pending money with exactly ONE bound attempt | `PendingMoney` | `RecoveryKind=ResumePendingRefund`, `RefundAttemptId=<that id>`, `FinancialComplete=false` |
| definite financial failure (never mislabeled unknown) | `FailedMoney` | `Phase=FailedMoney`, `RecoveryKind=null` — the terminal `retry_refund` intent owns the new attempt |
| money confirmed, physical disposition unfinished | `PendingDisposition` | `FinancialComplete=true`, `PhysicalComplete=false`, `RecoveryKind=SettlePhysicalDisposition` |
| fully settled / recorded completion | `Settled` | `FinancialComplete=true`, `PhysicalComplete=true`, `RecoveryKind=null` |
| inconsistent / ambiguous binding or missing owning physical evidence | `Blocked` | `BlockerCode` = `return.recovery.attempt_unbound` or `return.recovery.receiving_required`, `RecoveryKind=null` |

`Completed` is deliberately not trusted on its own: a completed case with no durable `Succeeded` attempt, or with a
still-`Pending` attempt, is `Blocked`, not `Settled`. An ordinary (`CustomerDelivered`) case with unfinished
disposition is `Blocked` with `receiving_required` — the same stored shape that is executable work for a courier
receipt — because only the courier origin owns a receipt/inspection fact today.

Financial and physical completion are two separate booleans in the record, in the Contracts snapshot and in the wire
(`ReturnRecoverySnapshot`), so no consumer can infer one from the other.

## 3. Contract shape (additive only)

`Tooba.Returns.Contracts/Operations/ReturnRecoveryContracts.cs`:

```
ReturnRecoveryPhaseOption   None=0, PendingMoney=1, FailedMoney=2, PendingDisposition=3, Settled=4, Blocked=5
ReturnRecoveryKindOption    ResumePendingRefund=0, SettlePhysicalDisposition=1
ReturnRecoverySnapshot      (Phase, RecoveryKind?, RefundAttemptId?, BlockerCode?, FinancialComplete, PhysicalComplete)
```

`ReturnSnapshot.Recovery` is appended as the **last** member with a `null` default, so every existing constructor call
and every existing wire consumer keeps working. No previously shipped numeric enum value changed. No new executable
operator menu verb was published: 275 owns the UI, and this increment must not advertise an action a shipped frontend
cannot handle.

`IReturnAdminOperations.RecoverRefundAsync(returnRequestId, actorUserId, kind, expectedRefundAttemptId, ct)` is the
smallest Contracts adapter addition, mapped in `ReturnAdminOperationsAdapter` to the Application command.

## 4. Wire, validation and typed refusals

`POST /v1/admin/returns/{returnRequestId:guid}/recover` — body `AdminReturnRecoveryRequest { Kind, ExpectedRefundAttemptId }`.
No payment, amount, currency, destination or customer value is accepted from the client.

`ReturnRecoveryKindParser` maps the stable tokens `resume_pending_refund` / `settle_physical_disposition`; an absent or
unknown token is `return.recovery.kind_invalid` (never defaulted — defaulting could repeat a payment or invent a
physical action).

`RecoverReturnCommandValidator` (FluentValidation, registered with the other Returns validators):

| Rule | Code |
| --- | --- |
| `ReturnRequestId != Guid.Empty` | `returns.validation.return_request_id_required` |
| `Kind` is a defined member | `returns.validation.recovery_kind_invalid` |
| a money intent names the attempt | `returns.validation.recovery_attempt_required` |
| a supplied attempt id is not empty | `returns.validation.recovery_attempt_required` |

Business refusals (catalogue + both resx files, EN and FA):

| Code | Classification / status | Meaning |
| --- | --- | --- |
| `return.recovery.kind_invalid` | Business / 400 | token absent or unknown |
| `return.recovery.attempt_mismatch` | Conflict / 409 | the expected attempt is stale, wrong, missing or incoherent with the case payment/amount/currency |
| `return.recovery.state_mismatch` | Conflict / 409 | the intent does not apply to the stored state |
| `return.recovery.receiving_required` | Business / 400 | a delivered-customer case has no receipt/inspection fact |
| `return.recovery.attempt_unbound` | Conflict / 409 | the money phase has no single exact bound attempt |
| `return.recovery.gateway_unavailable` | Business / 503 | the owning adapter is explicitly not configured; nothing was sent |

Every refusal happens before any effect: the actor/guard first, then the stored description, then the attempt
identity, then the external call.

## 5. Observed live shape of the contract

Against the refreshed Host (read-only, no case exists so every executable intent stops at `return.missing`):

```
{}                                                   -> 400 return.recovery.kind_invalid
{"kind":"not_a_kind"}                                -> 400 return.recovery.kind_invalid
{"kind":"resume_pending_refund"}                     -> 400 validation.failed
                                                        ExpectedRefundAttemptId: returns.validation.recovery_attempt_required
{"kind":"resume_pending_refund","expectedRefundAttemptId": <random>} -> 404 return.missing
{"kind":"settle_physical_disposition"}               -> 404 return.missing
no dev actor header                                  -> 403 admin.authorization.denied
```

## 6. AMSC / architecture consequences

The three Returns guard classes pin route, validator and error-catalogue sets. This task updated exactly those pins:
the admin route count (11 → 12 with `/v1/admin/returns/{returnRequestId:guid}/recover` → `RecoverReturnCommand`), the
validator count (4 → 5 with `RecoverReturnCommandValidator`), and the HTTP-reachable error catalogue (28 → 29, 30
including the platform fault `return.outbox.unmapped_event`). No other module reference, no foreign Application/
Infrastructure/Domain dependency and no foreign SQL was introduced: the one raw statement is
`SELECT 1 FROM returns.refund_attempts … FOR UPDATE` inside Returns' own schema.
