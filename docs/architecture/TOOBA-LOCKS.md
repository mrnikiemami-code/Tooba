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

### LOCK-OPS-011 — Restore rebinds active Fulfillment to replacement reservation
Cancelled-order restore reacquires Inventory reservation on OrderLine. All active Fulfillment item references for forward-fulfillable quantity must bind to that replacement authoritative reservation before pack/ship/dispatch resume. Released/Consumed reservations remain historical and are never resurrected or consumed.

### LOCK-OPS-012 — No internal reservation enums in Admin UI
Admin surfaces must not expose raw Inventory reservation enum names (Held/Released/Consumed) or unmapped domain exception text. Use stable machine codes with centralized localization.

### LOCK-OPS-013 — Consolidated Package is orchestration over Seller Shipments
ConsolidatedPackage is a central orchestration/container layer over existing Seller Shipments for multi-seller Orders. It must not replace Shipment, own inventory/payment/refund/return/settlement, or invent a parallel fulfillment lifecycle. Order → Seller Shipments → Consolidated Package.

### LOCK-OPS-014 — Central package UI only for multi-seller Orders
The بسته‌بندی مرکزی Admin section is visible only when the Order has at least two distinct Sellers. Single-seller Orders must not show the section, placeholders, or disabled create controls.

### LOCK-OPS-015 — Package creation requires two distinct Sellers, same Order
Creating a Consolidated Package requires selected member Shipments from at least two distinct Sellers on the same Checkout/Order. Cross-order consolidation is out of scope. Two shipments from one Seller alone are insufficient.

### LOCK-OPS-016 — Only ready pre-dispatch Shipments may join
Only Seller Shipments that are genuinely ready for central dispatch may join (backend-authoritative eligibility). At minimum: same Order, distinct Seller Shipment, pre-dispatch Created, not cancelled/voided/delivered, not already active in another package.

### LOCK-OPS-017 — One active membership per Shipment
A Shipment cannot belong to two active Consolidated Packages. Unique active membership is enforced in domain and persistence.

### LOCK-OPS-018 — Pre-dispatch rebuild is cancel-then-create
Before central dispatch, rebuild is Cancel old package (historical Cancelled, members released) then Create a new package combination. Do not silently mutate membership history after creation. After dispatch, membership is immutable; no add/remove/rebuild/simple rollback.

### LOCK-OPS-019 — Active package locks conflicting direct member ops
While a Shipment is an active member of a Created or Dispatched Consolidated Package, conflicting direct Shipment mutations (void/cancel, dispatch, deliver, conflicting tracking assign/correct) are rejected with `fulfillment.shipment.locked_by_consolidated_package`. Readonly view remains allowed. UI must explain membership with human package number (MP-…).

### LOCK-OPS-020 — Central dispatch/deliver reuse Shipment lifecycle
Central Dispatch and Deliver must call existing per-member Dispatch/Deliver commands (no status SQL shortcuts). Package becomes Dispatched/Delivered only when all active members succeed; operations are idempotent for already-finished members.

### LOCK-OPS-021 — Order cancel voids pre-dispatch packages first
Whole-order cancel before central dispatch voids active Created Consolidated Packages and releases membership, then continues existing T016 shipment/order cancel orchestration. Post-central-dispatch whole-order cancel remains blocked by existing first-dispatch rules. Return/Refund stay outside package ownership. Member tracking/history is preserved; central tracking is preferred for customer-facing primary tracking while the package is active.

### LOCK-OPS-022 — Customer tracking prefers active central package
For Orders with an active Consolidated Package (Created / Dispatched / Delivered), customer-facing primary tracking prefers the central final-leg tracking reference and package number. Member Seller Shipment tracking and history remain preserved and may remain visible in detail. Cancelled Consolidated Packages are never projected as active primary tracking; fall back to eligible member shipment tracking. Do not invent a second customer tracking subsystem.

### LOCK-OPS-023 — Customer/guest tracking respects existing ownership proof
Customer fulfillment/tracking projection must reuse the existing ownership/guest-access model: authenticated owned Orders, Development/Testing customer-panel actor seams including `StorefrontGuestActorId`, and/or existing `X-Tooba-Guest-Secret` cart credential proof bound to the Order's CartId. Arbitrary anonymous OrderId/ShipmentId/PackageId guessing must not expose data. Forbidden/unowned access stays 404 without leakage.

### LOCK-OPS-024 — Paid-order inventory hold is not cart TTL
After verified payment success, Inventory reservations bound to paid Order lines must leave the cart hold TTL (`ExpiresAt` cleared / null). Cart-style timed holds must not remain eligible for `ReleaseExpiredHoldsAsync` after payment.

### LOCK-OPS-025 — Inventory owns paid-order reservation commit
Promotion of a paid-order reservation out of cart TTL is owned by Inventory (`CommitReservationForPaidOrder` / `CommitReservationForPaidOrderAsync`). Order/Payment/Fulfillment call the Inventory contract; they do not mutate reservation expiry columns directly.

### LOCK-OPS-026 — Expiry worker excludes committed paid holds
`ReleaseExpiredHoldsAsync` only releases Held reservations with non-null `ExpiresAt` that are past due. Committed paid holds (`ExpiresAt = null`) must never be released by the expiry worker.

### LOCK-OPS-027 — Never resurrect Released/Consumed reservations
`CommitForPaidOrder` must not resurrect Released or Consumed reservations. Those states stay historical; callers receive `inventory.reservation.not_active`.

### LOCK-OPS-028 — Cancelled-order restore reacquires durable (non-TTL) holds
Checkout restore after whole-order cancel reacquires inventory via `ReserveAsync` with `expiresAt: null` so restored paid/pending holds are durable and not cart-TTL-eligible.

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

## Storefront Checkout Journey (P10)

### LOCK-SF-001 — Shopeiva UI contract for storefront checkout
Storefront Cart / Shipping / Payment journeys reuse the purchased Shopeiva structure (layout, drawer, overlays, CTA placement). Tooba accent may stay `#2563EB`. Do not redesign parallel checkout UI.

### LOCK-SF-002 — Checkout price is backend-authoritative
Displayed cart/checkout money comes from Host Cart / Pricing / Checkout projections. Frontend must not treat Product card display price as trusted checkout truth and must not invent authoritative totals by local multiplication.

### LOCK-SF-003 — Storefront does not trust card/display price
Add-to-Cart sends Offer identity + quantity only. Unit/line amounts and subtotals are returned by Cart projection after Pricing quote.

### LOCK-SF-004 — Shipping and payment options are Store-enabled
Shipping (T002) and Payment (T003) option lists come from Store/Admin-enabled configuration and existing Tooba registries (ShippingService catalog ∩ EnabledCodes for shipping; Payment:Gateway Mode + ManualCardToCardEnabled for payment). Template-only carriers or card forms must not be presented as live choices. Frontend must not spoof a disabled method.

### LOCK-SF-007 — Storefront never collects template bank-card credentials
Storefront `/payment` must not present fake card-number / CVV / expiry / second-password fields unless a real PCI-compliant hosted integration explicitly requires them. Online methods redirect/hand off; manual methods use instruction + Admin confirm.

### LOCK-SF-008 — Final payable and payment attempt amount are backend-authoritative
Displayed checkout payable and initiated payment amount come from Host checkout/payment projections (including shipping when present). Client-supplied amounts are ignored.

### LOCK-SF-009 — Checkout commit / payment initiation is idempotent
Shipping commit and payment initiation reuse canonical idempotency keys so duplicate submits cannot create duplicate Orders or duplicate payment attempts beyond replay semantics.

### LOCK-SF-011 — Marketplace shipping charge attribution
A customer shipping charge must never be attributed to an arbitrary Seller by collection ordering / first-Seller shortcut. Non-seller shipping uses explicit Store/platform (`StoreShipping`) ownership so payment allocation balance is not achieved by falsifying seller ownership.

### LOCK-SF-005 — Delivery slot never earlier than calculated minimum
When delivery date/time selection exists, the customer may choose a later valid slot than the calculated minimum readiness, but must not choose earlier. Minimum is backend-calculated as max(seller preparation days) + method lead days. Frontend must not spoof an earlier date/time.

### LOCK-SF-006 — Shipping price is backend-authoritative
Displayed shipping cost comes from Host shipping quote/configuration (Rates). Frontend must not invent authoritative rates, treat template amounts as truth, or present free shipping unless the real rule/config yields zero.

### LOCK-SF-012 — Sandbox simulator is Development/Sandbox only
The Tooba-hosted payment simulator exists only when Gateway Mode is Sandbox and the Host is not Production. Production never fakes PSP success and never exposes simulator Success/Failure actions.

### LOCK-SF-013 — Online payment success is backend-authoritative including simulator
Simulator Success/Failure buttons call Host Verify. Frontend outcome text is not Payment truth.

### LOCK-SF-014 — Manual/card-to-card transfer reference is mandatory
Customer payment tracking/reference number (`شماره پیگیری پرداخت`) is required, trimmed, max 64. It is not the Order tracking number.

### LOCK-SF-015 — Payment proof requirement is method-configurable
Manual/card-to-card proof upload is Disabled / Optional / Required on Store payment-method configuration. Do not hardcode globally.

### LOCK-SF-016 — Manual customer submit is Pending/AwaitingConfirmation
Customer proof submission never marks Payment Succeeded. Admin ConfirmDeposit remains the success boundary.

### LOCK-SF-017 — Checkout cart finalization prevents duplicate Order on payment retry
After Order commit the Cart is Converted. Storefront GET of a Converted cart returns empty lines. Payment retry operates on the committed Order/Payment, not a second Order from the same cart.

### LOCK-SF-018 — Manual evidence history is immutable across reject/retry
Rejected manual attempts keep their transfer reference and proof MediaAssetId. Retry creates a new Initiated attempt.

### LOCK-SF-019 — Payment/Order result ownership is independent of mutable active Cart
Once Order/Payment exists, storefront Payment GET and result refresh authorize via authenticated session or committed guest Order/Payment proof. A new empty active Cart must not authorize a prior Payment.

### LOCK-SF-020 — Payment result polling is state-aware
Storefront payment-result polls only while status can change asynchronously (Pending/Processing/Verifying without manual AwaitingAdmin). Succeeded, Failed, Rejected, Cancelled, and manual AwaitingAdmin/terminal states must not rapid-poll.

### LOCK-SF-021 — Manual AwaitingAdmin does not rapid-poll for Admin confirmation
After customer manual evidence submit, the result shows در انتظار تایید without 1.5s polling. Customer may refresh/reopen Order later; Admin may act minutes/hours later.

### LOCK-SF-022 — Narrow guest committed-order/payment proof survives Cart finalization
Guest payment-result access may retain only store-scoped Order/Payment-scoped committed cartId + guest secret proof across Cart clear. Wrong proof, paymentId alone, foreign customer, and cross-store access remain denied.

### LOCK-SF-023 — Manual proof submission promotes inventory to Manual Payment Review hold
After successful customer manual/card-to-card transfer reference/proof submit, inventory transitions from Cart TTL hold to Manual Payment Review hold via Inventory-owned PromoteReservationForManualPaymentReview. Released/Consumed reservations are never resurrected.

### LOCK-SF-024 — Manual-review inventory lifetime is independent from Cart TTL
While payment awaits Admin confirmation, reservation ExpiresAt follows ManualPaymentReviewHoldHours (store/gateway config; default 24h), not Cart HoldTtl.

### LOCK-SF-025 — Admin confirm during active review hold commits durable paid reservation
ConfirmDeposit within an active review hold commits ExpiresAt=null paid-order semantics. Confirm must not depend on original Cart TTL.

### LOCK-SF-026 — Rejected/expired review holds are never resurrected
Admin reject releases the Held review reservation. Review-hold expiry releases via the expiry worker. Late Confirm never resurrects Released/Consumed rows; it must authoritatively reacquire or fail with inventory.manual_review.unavailable.

### LOCK-SF-027 — Historical Cart-TTL defect recovery uses new reservations only
Historical paid/manual-review Orders with Released reservations are recovered by authoritative reacquire + rebind; Released rows are never resurrected.

### LOCK-SF-028 — Inventory recovery is atomic across remaining lines
RecoverOrderInventoryReservation acquires all required remaining lines or rolls back every newly acquired reservation from that attempt.

### LOCK-SF-029 — Paid-but-unrecoverable inventory blocks fulfillment
When Class B recovery cannot reacquire stock, fulfillment remains blocked pending explicit operational resolution (replenish/retry or cancel/refund).

### LOCK-SF-030 — Historical recovery never rewrites financial history
Inventory recovery mutates reservation bindings only; Payment amounts, settlement, refunds, and Order identity stay unchanged.

### LOCK-SF-031 — No automatic mass recovery on startup/migration
Historical audit/recovery runs only via explicit Admin/maintenance commands; never as unbounded EF startup migration.

### LOCK-SF-032 — Order lifecycle is not coupled to one historical Reservation instance
Guaranteed-supply operations reuse a valid hold or create NEW reservations; Released rows stay immutable history.

### LOCK-SF-033 — Supply-guaranteed operations use canonical Inventory EnsureOrderSupply
Callers do not invent replacement reservations around Payment/Admin/Fulfillment; they call EnsureOrderSupply.

### LOCK-SF-034 — CheckOnly never mutates inventory
GetOrderSupplyStatus / Mode=CheckOnly is side-effect-free.

### LOCK-SF-035 — Reacquire always creates new reservations
Released -> Held is forbidden. Reacquire uses ReserveAsync.

### LOCK-SF-036 — Auto-reacquire only under explicit operation policy
Confirm/late-confirm/paid-recovery/restore-allowed/recovery command may reacquire; arbitrary view/cart/cancelled/refunded/fulfilled may not.

### LOCK-SF-037 — Whole-order supply recovery is atomic
All remaining lines succeed or newly acquired reservations from the attempt are released.

### LOCK-SF-038 — SupplyStatus is distinct from PaymentStatus/OrderStatus
Reserved/AvailableForReacquire/Unavailable/PartiallyUnavailable/Fulfilled/NotApplicable.

### LOCK-SF-039 — Hold durations resolve from Settings
Payment:Gateway hold hours + OrderSupplyHoldOverrides; no magic business TTL in new code. Cart start timing unchanged in R7.

### LOCK-SF-040 — UX consumes supply outcomes, not raw reservation state
Normal Admin/customer UI must not receive reservation.not_active / Held / Released / GUID.

### LOCK-SF-041 — Admin Orders and Payments expose business SupplyStatus
List and detail surfaces show SupplyStatus distinct from PaymentStatus/OrderStatus.

### LOCK-SF-042 — List supply projection must not use N+1 requests
Orders/Payments grids batch supply in the page mapper; FE must not call supply-status per row.

### LOCK-SF-043 — Confirm deposit auto-reacquires when supply is available
AvailableForReacquire confirm uses canonical EnsurePaidDurable; no extra manual recovery click.

### LOCK-SF-044 — Unavailable supply blocks payment confirmation success
Payment remains not Succeeded; Admin sees business-safe shortage copy.

### LOCK-SF-045 — Normal UX never exposes raw reservation lifecycle terminology
No inventory.reservation.not_active, Released, or reservation GUIDs in Admin supply UX.

### LOCK-SF-046 — Recovery action visibility is capability-driven
recover_inventory_reservation is hidden when confirm can auto-reacquire.
