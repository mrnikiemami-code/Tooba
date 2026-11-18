# TB-279 — Delivery-slice allocation, physical custody and shared refund budget proof

Scope proven here: **C** (per-portion custody of rejected received goods), **D** (per-delivery slice allocation) and
**E** (Returns-managed shared payment budget). All money/stock effects are proven with controlled ports; no real PSP,
Wallet or Inventory call was made.

## 1. Allocation is per delivery slice, not "returned minus delivered"

`returns.return_delivery_allocations` binds each new claim to the owning **delivered slices** (one row per claim per
slice, with quantity). `ReturnAllocationPolicy` is the single arithmetic authority; `ReturnEligibilityEvaluator` reads it.

| Rule | Behaviour |
| --- | --- |
| order | the **oldest deadline first**; a claim consumes the nearest-expiring slice before a later one |
| expiry | an expired slice never funds a new claim — a newly delivered item is not consumed by a window that already closed |
| same-instant deliveries | two deliveries at the same instant merge into one honest slice rather than two indistinguishable halves |
| legacy cases | a case with no allocation rows is **conservatively discounted from the oldest slice only**, so it can never be used twice and never blocks a slice it could not have touched |
| insufficient capacity | refused with a typed conflict and zero rows — never a negative or clamped allocation |
| release / re-acquire | a rejection releases its own rows; a reopen re-acquires them (see `decision-and-partial-resolution-proof.md` §3) |

Proof: `Allocation_takes_the_oldest_deadline_first_and_refuses_insufficient_capacity`,
`An_old_legacy_case_is_discounted_from_the_oldest_slice_only`,
`Two_deliveries_at_the_same_instant_merge_into_one_honest_slice`.

This closes the 278 gap where an expired slice could consume a newly delivered quantity, without inventing any
shipping/tax/coupon allocation rule.

## 2. Physical custody per portion

`POST /v1/admin/returns/{id}/custody` records the **actual handback** of rejected received goods as its own durable fact
(`returns.return_item_custody`): item, quantity, condition, server-derived recipient, required proof reference, actor,
occurrence key.

| Rule | Behaviour |
| --- | --- |
| ceiling | handback cannot exceed the rejected received remainder — `return.custody.over_handed` |
| proof | a blank/meaningless proof reference is `return.custody.proof_required`; nothing is answered as received without evidence |
| recipient | derived server-side; a client-supplied foreign recipient is `return.custody.recipient_invalid` |
| item | a foreign/unknown item is `return.custody.item_invalid` (proven live on the settled case) |
| settlement | every **accepted** portion owes its own physical fact AND every owed handback must be recorded before settlement — a mixed line is not "settled" while one portion is unresolved |
| truthfulness | a settled legacy case never claims a rejection was "refunded"; acceptance without a restock fact still owes `settle_physical_disposition` |

Proof: `C_custody_hands_back_only_the_rejected_remainder_and_never_claims_settlement`,
`A_mixed_line_is_not_settled_until_every_accepted_portion_and_every_handback_is_recorded`,
`An_accepted_usable_portion_still_owes_its_own_restock_fact`.

## 3. Shared payment budget

The failure mode this closes: two Returns cases on one payment each believe the whole payment is refundable.

- `ReturnBudgetSerialization.AdvisoryLockKey(paymentId)` gives one stable advisory key per payment
  (`AdvisoryLockNamespace 0x544F4F4241000279`), so all Returns work for that payment serializes; different payments never
  collide.
- Inside that key, the budget counts **Pending + Succeeded** obligations of **all** Returns cases for that payment
  (reserved and confirmed), not just the current case.
- The reservation is committed **with the attempt, before** the external refund call, so a lost answer cannot produce two
  reservations.
- A definite failure releases only its own reservation; a duplicate click/retry of the same attempt/key answers the
  stored attempt instead of minting a second obligation.
- An over-budget obligation is refused with the typed budget error before any provider call.

Proof: `The_budget_lock_is_stable_per_payment_and_never_collides_across_payments` plus the directory money facts in
`A_recorded_money_attempt_blocks_the_correction` and `B_a_partial_plan_freezes_only_the_accepted_money_before_the_first_attempt`
(1 reservation, 1 provider call, and the replay answers storage).

## 4. What is deliberately not claimed

- Money/stock are proven with scripted gateway/Wallet/Inventory ports. There is **no** real PSP/Wallet/Inventory effect
  and no "one provider call happened" claim beyond the controlled counter.
- No committed two-writer race was executed. Serialization is proven structurally: a stable per-payment key, the
  per-case `FOR UPDATE` ordering, and the version CAS on the decision.
- Card refunds outside this Returns authority are not represented in the shared budget, so no universal
  "remaining refundable balance" claim is made.
