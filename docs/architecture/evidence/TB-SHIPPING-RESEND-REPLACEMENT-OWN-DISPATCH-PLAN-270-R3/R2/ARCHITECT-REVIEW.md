# ARCHITECT REVIEW — TB-270-R2: PARTIAL, NOT ACCEPTED

Persisted for TB-270-R3 (this file records the independent review that produced the R3 repair task).
Reviewed at `febeb27e36829cdceb1a8c6a3dcf4a9b0f22f659`; implementation `ce5d7f998ce8a3f391c416881afcfa5f0a5c951f`
(R2 code), documentation `febeb27e3`.

Verdict: **PARTIAL — NOT ACCEPTED.** The two-stage design and the useful implemented base are retained
(owning plan table/reader, scheduling preview/confirm, shared guard call sites, projection and modal). Worker
test/build/deploy claims are not Architect runtime verification.

## Source-confirmed defects that R3 must repair

| # | Defect |
| --- | --- |
| 1 | `AdminOrderOperationsOrchestrator.ShipmentPlan.ResolvePlanInputsAsync` builds the scheduling lookup request with `binding.ShippingGroupId` as `GroupId` — that is the **Order** group id, while R1 requires the captured `payload.Groups[].GroupId`, i.e. `binding.AreaGroupId`. They are distinct identities; valid previews were blocked with `GroupUnavailable`, and unit mocks never proved the real R1→Order seam. |
| 2 | `FulfillmentDirectory.EnsureLinkedAttemptsPlannedAsync` checked only the plan dictionary key/`ShipmentId` + `AttemptKind`. Checkout/seller/fulfillment/group/source/decision/hold/method/exact item fingerprint were stored but never verified, so a wrongly bound row could authorize dispatch or look scheduled. |
| 3 | `ResolveReadinessAsync`'s replacement branch trusted a projected decision status plus a matching reservation GUID and never read Inventory's current hold; it summed parcel quantity instead of checking each line against the decision/source line. A released/consumed/wrong-line hold could be called "ready". |
| 4 | `admin-shipment-plan-modal.confirm` read `retained.occurrenceKey` but overwrote the retained selection with the currently selected slot, still allowed another slot after an unknown outcome, and the load path cleared the selection when the old slot was no longer offered. That is not an immutable reviewed envelope, and not a valid retry of a possibly committed intent. |
| 5 | `onParcelException` in `admin-order-items-shipping-panel` returned `completedPostDispatchOutcome` after create/resolve + refresh and never handed the exact created/recovered parcel to planning; the modal only opened through the separate card `onParcelPlan` click. The claimed create/recover → scheduling continuation was not implemented. |
| 6 | The two relational tests used one DB/context/transaction at a time, sequential store calls and a raw SQL duplicate insert. They established stored-row/index facts, **not** a race between two independent application writers, and both silently returned when the connection was absent — appearing passed without exercising storage. |

Additional evidence limits: plan confirmation performed a mutable `LocateParcelAsync` **before** the existing-plan lookup
(so lost-response reconciliation could fail after lifecycle advancement); `BlockedPreview` discarded `confirmed` although
it had been received; the reviewed request carried only key/date/open/close rather than the reviewed
binding/source/timezone/calculation context; close/reopen reset `inFlightRef` while an earlier request could still be
pending.

Do not certify these behaviours from the then-green mocks.

## Migration audit — non-repeatable failure

The R2-required **pre-apply** backup was **not** taken. `pre-apply-plan.json` is not a database backup, and the post-apply
dump cannot be renamed or described as pre-apply protection (only
`D:\temp\tb270r2-backup\tooba_alpha-post-tb270r2.dump`, 2,949,234 B, SHA-256 prefix `331B373AD45882D9`, exists).
This disclosure is preserved; the table was not rolled back, dropped or recreated, and no retroactive proof was
manufactured. R3 adds **no** schema change, so the missing artifact has no new dependency.

## Exact relational proof limits (retained wording)

- The two R2 relational cases prove stored-row immutability and the unique-index arbitration on one context, sequential
  store calls, inside a rolled-back transaction. They do **not** prove a two-writer race.
- Their early `return` on an absent connection was removed/closed by R3's explicit "no silent success" requirement;
  required proof now fails rather than skipping.
- The R2 captured-binding fixture was a hand-built factory fixture, so it could not prove the real 270-R1 seam; R3
  replaces that claim with a connected case against the actual adapter.

No `zero data change` blanket claim: applied schema/migration metadata, rolled-back test writes and untouched live
business rows are reported separately.

## Scope handed to R3

Repairs A–E as issued: exact captured binding + real seam proof; one shared binding in guard/projection/package/bulk;
inventory-owned current hold readiness; frozen fingerprinted review with expiry/epoch reconciliation; exact
create/recover → plan handoff. `271` remains **NOT ISSUED**; no new architecture, demo dataset or broad suite campaign.
