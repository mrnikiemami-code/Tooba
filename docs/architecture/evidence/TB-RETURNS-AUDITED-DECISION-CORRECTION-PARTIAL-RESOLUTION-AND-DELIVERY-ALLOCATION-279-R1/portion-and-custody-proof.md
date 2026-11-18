# TB-279-R1 — portion and custody proof (F2, F3, F5)

## F2 — the reviewed portion is the ONLY money/stock authority

`ReturnCorrectionModels.cs` exposes one pure authority over stored facts:

* `ReturnResolutionLineSnapshot.Accepted => AcceptedUsable + AcceptedDamaged` and
  `Rejected => RejectedReceivedUsable + RejectedReceivedDamaged + RejectedUnreceived`. The accepted split is **reviewed
  per condition** — never `Min(accepted, usable)` and never a usable-first inference. A pre-R1 row with null condition
  columns reads as an INCOMPLETE plan, not as a usable split.
* `ReturnResolutionPolicy.TryDescribeCompleteCoverage` is the single coverage gate used by approval, first refund,
  retry and recovery: a reviewed plan is complete when every requested item is accounted for
  (`accepted + rejected == requested`), every accepted portion is covered by its own received condition, and no
  portion exceeds the received ceiling. A case with **no** reviewed split still needs a coherent, complete receipt —
  the implicit baseline branch requires `{ IsCoherent: true, IsComplete: true }`, so an unreviewed partial receipt is
  still `receiving_required`.
* `ReturnResolutionPolicy.MoneyOf(unitPrice, quantity)` derives money from the accepted quantity × the **stored** unit
  price. The requested `refund_amount` stays immutable; the approved amount is frozen once, before the first money
  attempt (`FreezeApprovedAmount`), and all-rejected means zero owed — not "refunded", and never a zero-money gateway
  attempt.
* Physical effect: `ReturnDirectory.Recovery.cs` restocks ONLY the accepted usable portion
  (`line.AcceptedUsable > 0m` + its owning stock binding); accepted damaged never reaches saleable Inventory, a
  rejected received portion is never restocked, and a missing stock binding is the typed PENDING blocker
  `return.recovery.stock_binding_required` instead of a false completion.
* Mixed usable/damaged acceptance requires **each** accepted portion's own durable completion; a single coarse item
  `Disposition` never proves both. `SaleableRestockedQuantity` (additive) exposes the exact restocked quantity, so a
  mixed line never reads as wholly saleable. Status `Completed` without every physical/portion fact is not settled.

Progression proof (`ReturnsDecisionCorrection279Tests`): received 2 usable of 3 requested, reviewed plan
accepted 2 / rejected 1 (never received) is COMPLETE and lawful → approval, first refund and physical settlement all
proceed, Inventory receives exactly 2, and no receipt/custody is invented for the unreceived unit.

## F3 — rejected received goods are held custody

* Rejection releases only the NEVER-received remainder (`ReleaseRejectedDeliveriesAsync`), while received rejected
  goods keep their `return_delivery_allocations` occupancy
  (`TrimActiveAllocationsToHeldAsync`) under the same seller-order serialization. A sibling case cannot claim units the
  seller still physically holds.
* `POST /v1/admin/returns/{id}/custody` (`ReturnDirectory.Corrections.cs`, `return_item_custody`) records ONE actual
  handback: own case, own item, own condition, exact unit quantum (6-decimal integer units), ceiling =
  condition-specific rejected-received minus condition-specific handed-back, server-derived recipient, required proof
  reference and the exact immutable occurrence key. Over-handback, wrong condition, blank proof and a foreign item are
  typed refusals with **zero rows**; an exact replay compares the whole intent instead of trusting the key.
* Proof semantics are explicit: `operator_attestation` (the operator attests an in-person handback to the owning
  customer, with the reviewed reference) or `carrier_delivered_back` when an owning carrier fact exists. Tracking
  creation/dispatch is never accepted as delivery evidence, and no carrier subsystem was built here.
* Custody is zero money and cannot reopen a completed financial attempt; `ReopenForReviewAsync` still refuses any
  custody fact, any disposition, any settlement fact and any refund attempt.
* Whole-case rejection (no prior partial plan) still exposes the same lawful handback action via
  `ReturnResolutionPolicy.DescribeRejectedCustody`, which synthesizes the declined received portions from stored
  receipt facts only.

## F5 — no guessed legacy attribution

`ReturnDirectory.Allocations.cs` hits `ReturnsErrorCodes.AllocationLegacyAmbiguous` in two places: when a legacy case
carries no allocation rows at all, and when its stored facts cannot prove a unique origin. Such a claim is refused
before any allocation row is written — the origin is never "the oldest slice" by default. The honest read blocker is
published instead (distinct from window/quantity blockers), and no backfill or invented date exists. New claims keep the
positively proven behaviour: oldest eligible deadline first, an expired slice never funds a new claim, same-instant
deliveries merge within the owning line, and insufficient capacity refuses with zero rows.

## Facts

* `src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsDecisionCorrection279Tests.cs`
* `src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsDecisionCorrection279DirectoryTests.cs`
* whole Returns project after the final change: **172/172**

Limits: Inventory/Wallet are controlled ports (no real effect); no committed two-writer race; the only live line is
`pcs` with unit decimal places 0, so fractional units are proven by tests only.
