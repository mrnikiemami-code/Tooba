# Shipping-group fee allocation policy — decision record (INPUT CONTRACT, NOT A DECISION)

Status: **OPEN — AWAITING BUSINESS/ARCHITECT APPROVAL.** This document records what must be decided and by
whom; it deliberately decides nothing commercial and contains no proposed amounts, percentages, rates or
splits.
Owner of the decision: Architect / business owner (not the Coding Agent).
Related: `docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md` (binding invariant),
`docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-130.md`, `SHIPPING-ONE-LEVEL-UI-143.md`,
`docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-144.md`.
Date: 2026-10-16.

## 0. Why this document exists

The per-group shipping-fee ledger is implemented and runtime-proven (TB-130, TB-143): every paid shipping
group writes exactly one `PendingAllocation` charge row carrying the **known** customer charge and the
stable group identity, and every commercial column stays `NULL` (`UNSPECIFIED`). The ledger is
deliberately **non-payable**: `ShippingGroupFeeAllocation.EnsurePayoutAllocationAvailable()` throws
`settlement.shipping_group.allocation_policy_missing` while the row is `PendingAllocation`, and
`EnsureNotSellerRevenue()` permanently forbids presenting the row as seller revenue.

That is a correct fail-closed stop, not a completed capability. Nothing further can be built without the
business deciding **who owns the money**. This record is the versioned input contract for that decision.
It is written so that once the owner answers the numbered questions in §3–§4, the allocation policy can be
implemented deterministically without re-deriving anything from live tariffs, UI labels, or a residual.

## 1. What is already implemented (do not re-decide)

| Fact | Where |
| --- | --- |
| One charge row per paid, stable shipping group, idempotent | `SettlementDirectory.RecordShippingGroupFeesAsync` (`.../Directories/SettlementDirectory.ShippingGroupFees.cs:20`) |
| Known customer charge comes from the Order-accepted quote, not from a UI total | `SettlementShippingGroupBridge.GetForPaymentAsync` (`.../Bridges/SettlementShippingGroupBridge.cs:29`) |
| Charge must equal the customer's paid group allocation, else fail-closed | same bridge, `ShippingGroupChargeMismatch` (`:65`, `:71`) |
| Zero-charge group writes no ledger row | same bridge (`:58`) |
| All commercial fields exist, typed and nullable, as the extension point | `ShippingGroupFeeAllocation` (`.../Domain/Aggregates/ShippingGroupFeeAllocation.cs:62-89`) |
| Only `PendingAllocation` exists as a state today | `.../Domain/ValueObjects/ShippingGroupFeeAllocationState.cs:10` |
| Reversal is additive, never rewrites the original row | `ReverseAcceptedCharge` (`:182`) |
| Ledger never becomes seller revenue/payout without an approved policy | `:230`, `:237` |
| Recording path has exactly one production caller (the paid event handler) | `SettlementEventHandlers.cs:39` |

Schema note (TB-143 readback): `settlement.shipping_group_fees` has 26 columns and 6 indexes; the six
commercial columns plus `allocation_policy_version` / `allocation_policy_reference` are all nullable.

## 2. Invariants any approved policy must satisfy

These are structural, not commercial, and are already enforced or required by the binding contract
(`SHIPPING-METHOD-GROUPING-UI-CONTRACT.md`, "Shipping-fee ledger, allocation and settlement"):

1. **Per-group, never per-order.** Each stable `ShippingGroupId` has its own allocation row; multiple
   methods of one seller and multiple sellers never merge.
2. **One currency per row**, matching the accepted quote currency; canonical Money/precision/rounding rules.
3. **Balance sheet identity** (§4.1) must hold exactly, with no double counting.
4. **Signs are explicit.** A stored amount must carry a typed direction (receivable vs payable, revenue vs
   subsidy/expense). A marketplace subsidy may legitimately be negative; positivity must not be forced.
5. **Immutable snapshot.** Rows are written from the order-placement snapshot and are never recomputed from
   mutable current tariffs after the fact.
6. **Auditability by append.** Any change is a new linked `Reversal`/adjustment row referencing the original;
   no silent overwrite.
7. **Idempotency.** Repeated delivery of the same paid event, or of the same settlement instruction, must
   never create a second charge or a second payout for the same group.
8. **Fail-closed default.** Absent an approved policy version, the row stays `PendingAllocation` and is
   neither settled nor paid out.
9. **No inference.** No aspect may be derived from a displayed total, UI label, seller name, SKU, tariff
   guess, or "whatever is left over".

## 3. Values to decide — per group

Every row below is `BUSINESS APPROVAL REQUIRED`. The "current state" column is the factual repository state,
not a suggestion.

| # | Decision field | Ledger column | Current state | Approver |
| --- | --- | --- | --- | --- |
| D1 | Customer shipping charge (the amount actually charged) | `customer_charge_amount` | **KNOWN today** (accepted quote) | — |
| D2 | Shipping discount amount and its funder/timing (who gives it up, at which moment) | `shipping_discount_amount` + `funder_reference` | `NULL` | BUSINESS APPROVAL REQUIRED |
| D3 | Funder adjustment (subsidy/adjustment amount, direction, reason) | `funder_adjustment_amount` + `funder_reference` | `NULL` | BUSINESS APPROVAL REQUIRED |
| D4 | Carrier quoted vs actual payable, and who absorbs the difference | `carrier_payable_amount` | `NULL` | BUSINESS APPROVAL REQUIRED |
| D5 | Signed seller share (seller receivable or seller-borne cost; direction typed) | `seller_share_amount` | `NULL` | BUSINESS APPROVAL REQUIRED |
| D6 | Signed marketplace share (revenue, subsidy or expense; direction typed) | `marketplace_share_amount` | `NULL` | BUSINESS APPROVAL REQUIRED |
| D7 | Shipping tax: whether charged, jurisdiction, authority, and who remits | `shipping_tax_amount` (jurisdiction/authority not yet modelled) | `NULL` | BUSINESS APPROVAL REQUIRED |
| D8 | Currency and rounding rule (single currency per row, rounding to which minor unit, per-row vs per-order) | `currency` + rounding convention | currency known; rounding rule for shares not specified | BUSINESS APPROVAL REQUIRED |
| D9 | Allocation policy identity: version identifier and human-readable reference | `allocation_policy_version`, `allocation_policy_reference` | `NULL` | BUSINESS APPROVAL REQUIRED |
| D10 | Effective-date / applicability window for a policy version (and whether historical rows are re-allocated) | not modelled | absent | BUSINESS APPROVAL REQUIRED |
| D11 | Whether a percentage of the charge may ever be used, and if so as what (a policy formula, never a residual) | n/a | explicitly forbidden today | BUSINESS APPROVAL REQUIRED |
| D12 | Payout timing: when an allocated group fee becomes payable, and by which payout capability | n/a | payouts blocked by design | BUSINESS APPROVAL REQUIRED |

Questions D2–D12 are independent enough to answer separately, but D4–D6 and D7 must be answered together
because they share the same balance equation.

## 4. Required semantics to decide (behaviour, not amounts)

### 4.1 Accounting equation — must be stated and signed

State the exact identity the system must enforce for one group, in one currency. The record must specify
which term is the dependent one and that no term may be derived as a residual, for example (form is
illustrative; the owner must confirm the terms, not the shape):

```
carrier_payable + seller_share + marketplace_share + shipping_tax  ==  customer_charge − shipping_discount + funder_adjustment
```

Required sub-decisions: which side each signed share sits on; whether `seller_share` may be negative
(seller pays part of shipping); whether `marketplace_share` may be negative (marketplace subsidises);
whether tax is included in `customer_charge` or added; what happens to rounding remainders.

### 4.2 Zero-charge / free shipping

Decide: a group whose customer charge is zero writes **no** ledger row today (implemented). Confirm whether
that stays true — and if a carrier still costs money on a free-shipping group, decide whether the cost is
recorded elsewhere (seller expense, platform expense) or is intentionally unrecorded.

### 4.3 Seller-funded vs platform-funded promotion

Decide how the two cases are distinguished and recorded (which `funder_reference` semantics, which share
goes negative, whether the seller's merchandise accrual is ever touched). Requirement: the shipping row must
never appear inside the seller's merchandise accrual.

### 4.4 Carrier overage (quoted vs actual)

Decide who absorbs a difference between the quoted carrier amount and the actual invoice, at which moment,
whether it creates a new adjustment row, and whether the customer is ever re-charged.

### 4.5 Cancellation and partial shipment

Decide the treatment when: the whole order is cancelled before shipment; one of several groups is cancelled;
a group ships partially. Specify whether an allocation is reversed, reduced, or re-split, and how the
untouched sibling groups remain unaffected.

### 4.6 Return / refund

Decide whether a returned shipment reverses the group's shipping allocation and whether shipping charges are
refundable to the customer at all (a commercial decision, not a technical default). Currently no refund path
records a group-fee reversal (`ReverseShippingGroupFeeAsync` has no production caller) — this must be
explicitly decided as "not yet wired" or "must be wired".

### 4.7 Compensating entries, idempotency, audit

Decide: that corrections are append-only compensating rows linked to the original; that replay of the same
event/instruction is a no-op; and the audit fields required on every correction (actor, reason, reference).

## 5. Responsible owner per value (technical, already answerable)

| Value / action | Owner | Notes |
| --- | --- | --- |
| Customer charge + accepted quote version + group identity | Order | accepted order snapshot; already the source |
| Paid amount per group | Payment | `payment.allocations` (`StoreShippingGroup`) |
| Carrier actual/quoted cost, fulfillment adjustments | Fulfillment | execution owner, **not** settlement authority |
| Allocation policy definition, version, reversal | Settlement | this document's consumer |
| Tax rule/jurisdiction/authority | Tax | must be the authority if shipping tax is charged |
| Payout execution | Wallet/Payout capability | only after D12 + policy version exist |

Cross-cutting rule already binding: Host composes only; no financial authority in Host, browser, or a global
table without a module owner.

## 6. Decision-ready choices for the owner (no numbers proposed)

Answering these closes the gate. Each is a genuine either/or; none of them implies a number.

* **Q1 (D4–D6, D7, §4.1):** which term is derived, and which terms are independently stated? (A) all four
  independently stated with a balance check; (B) customer charge + carrier payable independently stated and
  seller/marketplace shares derived by a versioned formula; (C) another stated rule.
* **Q2 (D5/D6 sign, §4.3):** may seller share and/or marketplace share be negative, and in which documented
  cases?
* **Q3 (D2, §4.3):** who funds a shipping discount — seller, marketplace, or an external funder — and is the
  funder recorded per row?
* **Q4 (D7, §4.2):** is shipping taxed at all in the target jurisdiction; if yes, is tax inside
  `customer_charge` or additive, and who remits?
* **Q5 (D8):** single currency per row confirmed; rounding rule and remainder owner?
* **Q6 (§4.2):** free shipping with a real carrier cost — recorded where, or intentionally unrecorded?
* **Q7 (§4.4):** carrier overage absorbed by whom, recorded as which row kind?
* **Q8 (§4.5–§4.6):** cancellation/partial/return behaviour, including whether shipping is refundable and
  whether a reversal row is required.
* **Q9 (D9/D10):** policy version identifier format, effective date semantics, and whether already-recorded
  rows are ever re-allocated.
* **Q10 (D12):** when a group fee becomes payable, and which capability executes it.

## 7. Explicit non-goals of this task

No payout or commercial settlement was activated. No payout code, handler, ledger, migration, module
registration, frontend, CSS, theme, token, RTL, responsive, admin or shared setting was modified. No source
file was changed by TB-144. No numerical default, percentage, tariff, tax rate or split is proposed anywhere
in this document, and none may be inferred from it.

## 8. What "approved" unlocks (for planning only)

Once the owner answers §6 and signs §4.1, a single bounded implementation task can add the typed policy
(version + reference), the allocation computation for a charge row, the state transition out of
`PendingAllocation`, the reversal/refund wiring, and the focused reconciliation tests — without changing any
existing group identity, charge value, or the fail-closed default.
