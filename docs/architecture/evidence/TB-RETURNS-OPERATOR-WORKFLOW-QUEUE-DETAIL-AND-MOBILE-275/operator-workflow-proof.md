# TB-275 — operator workflow proof (A, B, C1, C2)

All backend facts below are proven by `ReturnsOperatorWorkflow275Tests` (12/12) over in-memory EF with controlled
ports. The directory and the classification are the production types; only the provider/stock ports are fakes.

## A — one work description, two consumers

`ReturnWorkPolicy.Describe(ReturnSnapshot)` is the only classification, and the queue's `ClassifyAsync` plus the
single-case `MapSnapshotAsync` both call it. Consequences proven:

| Fact | Test |
| --- | --- |
| A `Requested` customer-delivered case with incomplete receiving is `receiving` (not `decision`), and offers `record_receipt` only — never approve/reject it cannot run yet | `Work_stage_puts_receiving_before_the_decision_and_never_offers_an_action_it_cannot_run` |
| The stage is derived from the ONE `ReturnRecoveryPolicy` answer: pending money → `refund` with `resume_pending_refund` + the exact stored attempt; definite failure → `refund_failed` with `retry_refund` + the exact failed attempt; confirmed money + undisposed goods → `disposition`; money+physical complete → `settled` (not open) | `Work_stage_is_derived_from_the_one_recovery_answer` |
| Every legacy queue token lands on the honest stage (`pending_review`→decision, `approved`/`refund_needed`/`refund_pending`→refund, `refund_failed`→refund_failed, `completed`/`rejected`→history) and an unknown token is the ACTIVE set, never everything | `Every_legacy_queue_token_lands_on_the_honest_stage_and_never_on_everything` |
| The selection is canonicalized server-side, and naming it inside an advanced expression is refused before any read; a malformed selection operator is refused too | `Grid_policy_canonicalizes_the_selector_and_refuses_it_inside_an_advanced_expression` |

Blocked ACTIVE work stays visible: the typed blockers (`return.recovery.receiving_required`,
`return.recovery.stock_binding_required`, `return.recovery.physical_resolution_required`,
`return.recovery.evidence_conflict`, `return.recovery.attempt_unbound`) are attached to the stage the operator must look
at, and `IsOpen` is true for them, so nothing is hidden because no action is currently executable. A decided-away case
with received goods reports `physical_resolution_required` on the closed stage (no invented button).

## B — one action owner

- `admin-returns-work-queue-screen.tsx`: the row action is `review-return` → `/admin/returns/{returnRequestId}`. The
  financial kebab mutations were retired from the queue; the queue is read-only.
- `admin-screens.tsx`: `AdminReturnDetailScreen` is now a compatibility re-export of the workspace. The previous
  `status === RefundFailed` bare retry button (no reviewed identity, no confirmation, no durable intent) is gone.
- The workspace renders actions only from the server's `work.actionCodes`, so it can never offer a verb the directory
  refuses, and it never invents force-approve / restock / return-to-customer.

## C1 — lawful legacy partial-receipt continuation

`RecordReceiptAsync` still runs every existing guard (actor required, customer-delivered origin, exact-key replay,
coherent stored evidence, foreign-line refusal, cumulative over-receiving). What changed is the state gate for a NEW
occurrence:

```text
status != Requested  ⇒  allowed only while  status ∉ {Rejected, Cancelled}
                                         ∧  no requested item has a disposition
                                         ∧  cumulative coverage is still incomplete
```

| Fact | Test |
| --- | --- |
| A `RefundProcessing` case with one partial occurrence (incomplete coverage, nothing disposed) records partial #1 then partial #2 to exact coverage, with 0 money/stock/provider effects | `A_financially_progressed_case_can_still_finish_its_partial_receiving` |
| Once coverage is complete, or any item carries a disposition, a further receipt is refused (`return.receipt.state_invalid`) — the stored usable amounts behind the stock key cannot be rewritten | `A_disposed_or_complete_case_never_accepts_a_further_receipt` |
| A rejected case never receives NEW goods through this command | `A_rejected_case_never_receives_new_goods` |
| Over-receiving remains `return.receipt.over_received`; an incoherent stored summary refuses a NEW occurrence with `return.recovery.evidence_conflict` and 0 rows (274-R1 rule, re-exercised) | same suites (asserted inside the four tests above) |

## C2 — reviewed definite-failure retry

Identity (`ReturnRetryIdentity`):

```text
childAttemptId   = GUID( SHA256( "tb275-refund-retry:{case:N}:{failedAttempt:N}" )[0..16] )
idempotencyKey   = "refund-retry-{failedAttempt:N}"
currentReviewable = the single bound FAILED attempt whose derived child does not exist yet
```

| Fact | Test |
| --- | --- |
| The child is derived from `(case, reviewed failure)`; the second retry of the same failure answers the recorded child with ONE gateway call, not two | `Retry_binds_the_child_attempt_to_the_reviewed_failure_deterministically` |
| A duplicate POST of the same reviewed request answers the recorded success with no new money moved | `A_duplicate_post_answers_the_recorded_success_without_sending_money_again` |
| A reviewed attempt that is not the stored failure is refused (no arbitrary first/latest fallback) | `Retry_refuses_a_reviewed_attempt_that_is_not_the_stored_failure` |
| A stored failure that is not positively bound (foreign/mismatched case/payment/amount/currency/destination) is refused with `return.recovery.attempt_unbound` | `Retry_is_refused_when_the_stored_failure_is_not_positively_bound` |
| After the reviewed retry fails AGAIN, the new child is the only reviewable failure; retrying the stale parent is refused with 0 gateway calls | `After_the_reviewed_retry_fails_again_the_new_child_is_the_only_reviewable_failure` |

`ReturnAdminOperationsAdapter.RetryRefundAsync` (the older caller path) now reads the case's stored description and
retries its CURRENT reviewable failure, or throws `return.recovery.attempt_unbound` — there is no count-based key and no
silent bypass. `ReturnRecoveryPolicy` uses the same `CurrentReviewableFailureId` rule for reads, so the failure the
workspace offers and the one the endpoint answers for cannot disagree.

## Gates that were not touched

Actor/scope revalidation, the receiving gate before any money, the destination/amount/currency binding, the
`transaction_owned` refusal and the scoped duplicate-key arbitration all remain the 273-R2 / 274-R1 implementations.
No external call was added inside a transaction; no global `ChangeTracker.Clear` exists in the touched paths.
