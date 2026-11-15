# recovery-and-connected-workflow-proof.md — repairs C, D and E

## C. Replacement readiness is read from the owning hold, not from a projected status

### Owner boundary

| Module | Owns | Not owned |
| --- | --- | --- |
| Fulfillment | the unit, the replacement parcel, its source, the damaged resolution decision | the physical reservation quantity |
| Inventory | the reservation row and its `Held`/`Consumed`/`Released` state | the parcel lifecycle, the decision |
| Order | the plan row and the promise printed to the operator | — it never calls Inventory |

`IFulfillmentReplacementReadinessReader` (Fulfillment **Contracts**) is implemented by
`FulfillmentDirectory.Readiness.cs`, which composes Fulfillment's own aggregate with
`IInventoryFulfillmentGateway.FindReplacementReservationAsync`. Order depends on the Fulfillment contract only, and
the Order orchestrator treats an absent reader as "cannot prove readiness" and refuses (`:429`).

### What is actually verified

1. the unit loads and contains a parcel that `IsReplacement`;
2. the parcel records a source shipment, and that source exists in the same unit;
3. exactly ONE decision, resolved by `DamagedResolutionId` **or** by `ReplacementShipmentId`, with outcome
   `Replacement` and status `Reserved|Fulfilled`;
4. the decision's source, replacement parcel and reservation are the same identities on the parcel (no cross-wiring);
5. the parcel carries exactly one line, for the decision's order line, quantity > 0;
6. Inventory's **current** reservation is `Held`, has the same id, and the same quantity as that line.

Any failure returns a typed `FulfillmentReplacementReadinessFact` (`NotReplacement`, `UnitUnavailable`, `SourceMissing`,
`DecisionMissing`, `DecisionNotReady`, `DecisionMismatch`, `LineMismatch`, `HoldReleased`,
`HoldQuantityMismatch`) — never a ready fact. The read writes nothing.

### Proofs — `ReplacementReadiness270R3Tests` 4/4

| Case | Assert |
| --- | --- |
| a released/consumed hold is never ready | after the hold is released, the fact is not ready with `HoldReleased`; the reader itself performed **zero** release/consume calls beyond the seeding baseline |
| a held reservation for the exact decision line and quantity is ready | `Ready` with the exact source/decision/reservation/line/quantity and the decision's own instant |
| a decision for another line is not that parcel's readiness | typed reason, not ready |
| a non-replacement parcel is not a replacement | `NotReplacement`, zero effect |

The seeding baseline is recorded **after** the seed completes, because the seed legitimately dispatches the original
parcel (which consumes and releases during setup). Asserting a naive absolute `0` would have measured the seed, not the
reader — this is called out because the first draft of the test did exactly that.

### Preview vs confirmation

- The **preview** is read-only: it may report a stock-pending replacement as an honest blocker with **no** calendar
  (`A_stock_pending_replacement_hold_is_an_honest_blocker_and_offers_no_date`).
- The **confirmation** re-reads readiness inside its own transaction; a hold released between preview and confirm is a
  typed refusal (`order.shipment_plan.*`) with zero write, and a recorded plan replays from storage
  (`A_replacement_readiness_fact_for_another_parcel_never_authorizes_this_one`,
  `A_ready_replacement_reads_its_hold_from_the_owning_fact`).

## D. One frozen, fingerprinted review with honest recovery

### Server side

| Fact | Behavior |
| --- | --- |
| `ReviewFingerprint` | computed by the server over the reviewed owning facts (parcel, attempt kind, source/decision/hold, snapshot/draft/revision/format/selection, `AreaGroupId`/`RateRowId`, service/option, readiness source + anchor, canonical line fingerprint) and returned with the preview; the client cannot forge or improve it |
| confirmation without it | `order.shipment_plan.review_context_changed` — a fresh review is required (`Confirmation_without_the_reviewed_fingerprint_requires_a_fresh_review`) |
| context changed between preview and confirm | `order.shipment_plan.review_context_changed`, zero extra write, never silently replanned (`A_changed_reviewed_context_is_refused_instead_of_being_silently_replanned`) |
| selection no longer in the fresh calendar | `order.shipment_plan.selection_expired` — refused, never clamped (`A_selection_outside_the_fresh_calendar_is_refused_as_expired`) |
| exact replay of a confirmed key | answered from the stored row before any new feasibility work (`Confirm_writes_one_immutable_plan_and_an_exact_replay_answers_the_same_row`) |
| same key, different selection | conflict with zero extra write (`Same_key_with_a_changed_selection_is_a_conflict_with_zero_extra_write`) |
| different key on a planned parcel | never overwritten (`A_different_key_never_overwrites_the_one_confirmed_plan`) |
| stored plan after time passes | read from storage, never recalculated (`The_confirmed_plan_is_read_from_storage_after_time_elapsed_and_is_never_recalculated`) |

### Client side — `admin-shipment-plan-modal.tsx` + `admin-shipment-plan-attempt.ts`

- One submission at a time (`submitting`), one envelope per attempt.
- An **unknown** outcome (transport failure / 5xx) **freezes the exact serialized body**: other slot picks are disabled,
  the same bytes are re-sent on retry, and the freeze is only released by a terminal answer (success, typed refusal) or
  an explicit operator reset. This is what makes "same occurrence, same selection" true after a reload.
- A **typed refusal** keeps the reviewed intent selectable, so the operator resumes the SAME reviewed intent instead of
  re-deriving a new one.
- A slot that the server no longer offers is reconciled against the retained intent (the retained selection is dropped
  with the refusal retained), never re-mapped onto another slot.
- Late responses from a superseded attempt are fenced by an epoch counter, so a stale preview/confirm response cannot
  overwrite the current surface.

Proofs: `admin-shipment-plan-270r2.mount.test.ts` **10/10** (8 retained R2 + frozen-envelope/epoch-fencing and
expired-slot reconciliation), `tsc --noEmit` 0.

## E. Create/recover hands the exact created parcel to planning

`features/order-operational-groups/created-parcel.ts` resolves the created parcel from the owning command response
(parcel id + fulfillment + group + lineage) and **only** offers the plan action for that exact parcel.

- a genuinely created parcel → the plan surface opens for **that** parcel (`created-parcel.test.ts`);
- `Pending`, referral, or a completed-replay answer → no calendar, no plan action (`post-dispatch-outcome.ts`);
- the existing card entry point keeps working, so an operator who reloads the page is not stranded;
- no fabricated parcel id, no "first parcel of the group" guess.

`admin-order-items-shipping-panel.tsx` carries the handoff, and `admin-order-operational-delivery.mount.test.ts`
(**74/74**) keeps the retained R2/R3 operator surface green.

## What is deliberately NOT claimed

No carrier booking, no provider/waybill number, no customer notification/consent, no money movement, no customer-facing
calendar, and no claim that a stock-pending replacement has a promise. A parcel whose supply cannot be proven has no
plan and is not dispatchable — that is the intended outcome, not a gap.
