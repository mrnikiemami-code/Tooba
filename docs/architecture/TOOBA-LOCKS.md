# Tooba — Architecture / Product Lock Registry

Canonical lock file. Future tasks must read applicable locks, list IDs, discover, state reuse vs change, and regress touched locked behavior.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
```

## Quantity

### LOCK-QTY-001 — Product quantity is decimal
True product amounts are `decimal` / `numeric(18,6)`. Record counts stay `int`.

### LOCK-QTY-002 — Product owns quantity policy
Product owns `UnitOfMeasureId`, `QuantityDecimalPlaces`, optional `QuantityStep`. Variant does not duplicate in this phase. `EffectiveQuantityPolicy` is the resolver.

### LOCK-QTY-003 — Offer owns purchase limits only
Offer owns optional `MinimumOrderQuantity` / `MaximumOrderQuantity`. No Unit / DecimalPlaces / Step on Offer.

### LOCK-QTY-004 — One quantity subsystem
No duplicate UoM/quantity platform. Inventory / Fulfillment / Returns reuse existing modules. Multiple selling units are out of scope; architecture must not block a later extension.

### LOCK-QTY-005 — Historical quantity snapshots
OrderLine quantity + unit/places/step snapshots are immutable. Later Product/rounding changes do not rewrite history.

## Multilingual

### LOCK-I18N-001 — Language Registry is dynamic
Multilingual data uses the DB-backed Language Registry (`LanguageId`). Do not model translations as fixed fa/en columns. UoM Name/ShortName exist for any active language.

## Rounding

### LOCK-ROUND-001 — One GlobalRoundingMode
Store-level Floor / Ceiling / Nearest only. No Product- or Offer-specific rounding mode.

### LOCK-ROUND-002 — Quantity vs money
Quantity normalization uses `IQuantityNormalizer` (places + optional Step). Financial rounding uses the same GlobalRoundingMode at money precision. Do not mix Step into money.

### LOCK-ROUND-003 — Historical documents
Changing GlobalRoundingMode affects new calculations only. Historical orders/invoices are not recalculated.

## Invoice / Finance

### LOCK-INVOICE-001 — Order-backed invoice
Canonical invoice is the Order projection (`SellerOrder` header + `OrderLine` lines). Do not create a second Invoice module.

### LOCK-INVOICE-002 — Header aggregates
Header stores LineCount (int), TotalQuantity (decimal), Gross, Discount, NetBeforeTax, Tax, Duty, TaxAndDuty, Payable. List/report primary totals read Header, not Line JOIN+SUM.

### LOCK-INVOICE-003 — Finalize then sum
Each Line financial result is rounded once (GlobalRoundingMode + money places). Header sums finalized Line values with no extra sum-only rounding. A new Invoice-level calculation is rounded once, then stored.

### LOCK-INVOICE-004 — Tax and Duty
`TaxAmount` = مالیات. `DutyAmount` = عوارض. `TotalTaxAndDutyAmount` = Tax + Duty. Do not use VatAmount to mean Duty.

### LOCK-INVOICE-005 — Invoice rounding snapshot
Header snapshots `RoundingModeUsed` and `MoneyDecimalPlacesUsed`. Historical invoices stay stable if settings change.

## P09 operations (reference)

### LOCK-OPS-001
Exact line+quantity targeting, lifecycle sequence, cancelled-order precedence, capability-driven actions. No Orders UI regression on touched surfaces.

### LOCK-OPS-002 — Whole-order cancel until first dispatched quantity
Whole-order cancel is allowed until the first real dispatched quantity anywhere in the Checkout. Waiting payment, waiting manual confirm, Paid/ReadyToFulfill, Processing, Packed, Shipment Created, and Created+tracking are allowed. Any Dispatched / InTransit / Delivered quantity blocks. Packed and Created shipment do not block. Tracking does not block. Multi-seller: any one dispatched quantity blocks the whole cancel. Human block: `پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.`

### LOCK-OPS-003 — Cancel cleanup without hard-delete
Pre-dispatch shipments are cancelled/voided through existing fulfillment abort. Shipment + tracking/history stay. No hard-delete. Processing/packing history is preserved. After cancel, pack/ship/dispatch/deliver are blocked.

### LOCK-OPS-004 — Inventory / payment / settlement on whole-order cancel
Inventory release is exact decimal via existing `IInventoryDirectory.ReleaseAsync` (no cross-module SQL). Pending/unconfirmed payments close without a fake refund. Successful payment starts the existing refund workflow. Order becomes Cancelled immediately and does not wait for refund. Refund failure keeps Order Cancelled. Unpaid accrual is neutralized; completed payout is not rewritten; compensating debit is used when needed.

### LOCK-OPS-005 — Restore after whole-order cancel
T009 / T009-R1 restore gates remain. Cancelled pre-dispatch shipments are not resurrected. Completed refund blocks restore.

### LOCK-OPS-006 — Fulfillment work queue reuses Order Detail capabilities
Admin `ارسال و تحویل` is a cross-order work queue over the same fulfillment domain and `AdminOrderOperationsComposer` commands. Do not create a second fulfillment lifecycle or move shipment eligibility into React.

### LOCK-OPS-007 — No cross-seller Shipment
A Shipment belongs to one seller and one shipping method. Bulk queue actions that create or advance shipments must reject mixed sellers.

### LOCK-OPS-008 — Queue bulk requires shared valid capability
Work-queue bulk toolbar shows only the intersection of backend-projected capabilities for every selected row. No fake atomicity; partial failures are explicit.

### LOCK-OPS-009 — Partial dispatch does not terminalize remainder
Dispatch of one allocated quantity blocks whole-order cancellation (LOCK-OPS-002) but does not terminalize undispatched remainder. Remaining quantity may continue processing, packing, and new Shipment creation. Aggregate fulfillment capabilities/status are quantity-aware. One Seller Order may create multiple Shipments over time. Dispatched quantity and its shipment history stay immutable.

### LOCK-OPS-010 — Operational truth is quantity-aware and seller-scoped
Persisted aggregate `FulfillmentStatus` is not the sole operational truth when exact quantities remain. Admin Orders / Order Detail / Fulfillment Queue project a composed operational status (including ارسال جزئی) from remaining vs dispatched quantity. Capabilities stay quantity-based. Seller-scoped status and actions must not leak across sellers. Do not invent a second fulfillment status engine.

## Returns / Refunds

### LOCK-RET-001 — Return and Refund are independent lifecycles
Return = مرجوعی. Refund = بازگشت وجه. Do not merge them into one status machine. Admin surfaces must show Return status and Refund status separately. Completing one does not silently complete the other unless the existing Return aggregate already defines that transition.

### LOCK-RET-002 — Return rights survive seller settlement
Seller settlement / payout never ends customer return rights. Eligibility does not consult settlement state.

### LOCK-RET-003 — Immutable OrderLine effective return-policy snapshot
Eligibility uses OrderLine `IsReturnableSnapshot` / `ReturnWindowDaysSnapshot` / `ReturnPolicyLabelSnapshot` captured at checkout. Do not recompute historical policy from current Product/Offer settings.

### LOCK-RET-004 — Delivery-based, split-delivery, quantity-aware deadline
Return window starts per delivered quantity. Split deliveries keep separate clocks. Undelivered quantity has no deadline. Remaining returnable quantity subtracts already requested/approved/in-progress/returned amounts. Product quantities are decimal; no integer truncation.

### LOCK-RET-005 — Post-payout refund uses compensating seller debit
Completed payout history is immutable. If a refund occurs after payout, create/reuse the canonical seller debit/compensating adjustment and reduce future payable. Do not rewrite historical settlement/payout rows. Do not create a second ledger.

### LOCK-RET-006 — Cancellation refund can exist without Return
Paid-order cancellation starts the existing Payment refund workflow without a Return request. Return-sourced refunds reuse the same Payment refund implementation. No duplicate refund rows for the same idempotency key/business event.

### LOCK-RET-007 — One Return domain and one Refund implementation
Admin `مرجوعی‌ها و بازگشت وجه` is a work queue over existing Return/Refund aggregates and Admin order operations. Do not create a second Return or Refund subsystem.
