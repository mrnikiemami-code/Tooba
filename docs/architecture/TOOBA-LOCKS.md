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

### LOCK-SF-047 — Cart persistence and Inventory reservation lifetime are separate
Cart:PersistenceHours retains shopping state. Payment:Gateway hold hours govern Order-level reservations only.

### LOCK-SF-048 — AddToCart and Cart read never create hard reservations
Availability is validated; ReserveAsync is not called from Cart mutations or GET.

### LOCK-SF-049 — Hard reservation starts at committed Order / payment boundary
Shipping commit / SubmitCheckout is the first durable hold. Opening Cart or /shipping does not reserve.

### LOCK-SF-050 — Checkout final commit secures all required inventory or fails
No payable Order and no leftover reservations from a failed commit attempt.

### LOCK-SF-051 — Cart uses Availability; Order uses SupplyStatus
Available / LimitedQuantity / Unavailable stay on Cart. Reserved / AvailableForReacquire stay on Order.

### LOCK-SF-052 — Order-level hold durations are Settings-driven
OnlinePaymentHoldHours, ManualPaymentInitialHoldHours, ManualPaymentReviewHoldHours + store overrides. No magic TTL in new code.

### LOCK-SF-053 — Cart browsing does not renew reservations
No polling or timer keeps a Cart hold alive.

### LOCK-SF-054 — Historical Cart reservations are not Order reservations
Old cart:* holds may expire or release only when not bound to an Order. Order-bound holds are never cleaned as cart leftovers.

### LOCK-SF-055 — Unpaid Order timeout is distinct from Cart lifetime and SupplyStatus
PaymentExpired / unpaid timeout is not Cart:PersistenceHours and is not Order SupplyStatus.

### LOCK-SF-056 — Unpaid timeout applies only when money has not been received
No evidence-review and no Succeeded payment. Manual review uses R5 hold. Succeeded never unpaid-expires.

### LOCK-SF-057 — Timeout releases the Order hold and retains history
Expired unpaid releases the active Order reservation. Order and payment attempts are not deleted.

### LOCK-SF-058 — Succeeded and manual-review payments are never unpaid-expired
Unpaid worker is a no-op for Succeeded, refund, and evidence-submitted Pending.

### LOCK-SF-059 — Expired unpaid retry uses the same Order and a NEW reservation
Retry never duplicates checkout. EnsureOrderSupply creates a new hold. Released rows are not resurrected.

### LOCK-SF-060 — Timeout versus payment-success races never lose captured money
If money is captured after timeout, payment becomes Succeeded and Ensure/reacquire applies. Unavailable supply stays an operational paid-but-unsupplied state.

### LOCK-SF-061 — Hold and timeout durations are editable through Settings UX
Cart persistence, online hold, manual initial hold, and manual review hold: Payment Method override > Store override > platform. FA/EN labels, no raw keys.

### LOCK-SF-062 — Frontend never decides expiry with client timers
Expiry is worker/backend authoritative. UI only renders PaymentStatus.Expired / PaymentExpired.

### LOCK-SF-063 — Paid projection equality uses canonical allocation targets
Payment Succeeded applies Paid only when event amount equals Σ seller merchandise + StoreShipping (Order reconstructs this without opening PaymentDbContext). Seller-totals-only equality is forbidden.

### LOCK-SF-064 — StoreShipping never contributes to seller payout
Seller settlement and PaymentSucceeded SellerOrderIds include SellerOrder allocations only. Shipping stays on StoreShippingTargetId.

### LOCK-SF-065 — Payment Succeeded projection is idempotent and allocation-order independent
Inbox keys on EventId. Allocation row order must not change seller amounts or move shipping onto a seller.

### LOCK-SF-066 — Active shopping Cart and committed Order/Payment proof have separate lifecycles
`tooba.storefront.cartId` / `guestSecret` authorize only the current Active Cart. Committed checkout/payment uses `committedCheckoutProofs` and R4 `paymentResultProof`. One browser object must not own both lifecycles.

### LOCK-SF-067 — Successful Order commit detaches the active Cart from the converted source Cart
After Submit/shipping commit succeeds, persist Store+Checkout-scoped proof, then clear the active-cart pointer. Converted source Cart stays immutable history.

### LOCK-SF-068 — Payment/Order guest ownership after commit is independent of the active Cart
GET/initiate/manual/sandbox/retry/result authorize the committed checkout via proof (or authenticated session). Current/new Active Cart id or secret is not post-commit ownership.

### LOCK-SF-069 — ensureStorefrontCart returns or creates only Active carts for mutation
Converted, Cancelled, Expired, or inaccessible current Cart rotates the active pointer and creates a new Active Cart. AddToCart must not POST against a Converted Cart. No cart.rejected retry loop.

### LOCK-SF-070 — Converted carts remain immutable history
Converted Cart lines cannot be mutated. A new Active Cart may coexist with an unpaid committed Order. Old guest secret is never copied into the new Cart.

### LOCK-SF-071 — Committed proof is Store+Checkout scoped and cannot mutate Cart
Proofs are keyed by checkoutId (and paymentId for result). Proof A does not authorize Order B. Proof cannot mutate any Cart. payment/order id alone is not authorization.

### LOCK-SF-072 — Post-commit ownership failure is never “Order registration failed”
`checkout.access.denied` / `payment.access.denied` map to a payment-access message. After a successful commit, Host must not emit checkout.rejected / «ثبت سفارش انجام نشد».

### LOCK-SF-073 — Payment Succeeded is terminal for initiation and retry
A checkout with any Succeeded Payment cannot start a new online/manual/wallet/sandbox initiation, cannot accept new manual evidence, and cannot retry. Failed/Rejected/Expired retries remain valid only when no Succeeded Payment exists.

### LOCK-SF-074 — Initiation eligibility is backend-authoritative and shared
`HasSucceededPaymentForCheckoutAsync` plus Host checkout `CanInitiatePayment` are the capability. Frontend must not infer pay-again from labels. All initiate/evidence/retry endpoints share this rule.

### LOCK-SF-075 — Duplicate success delivery is not a new initiation
Idempotent replay of the same Succeeded payment/key or Verify callback remains 200 without a new attempt. A new idempotency key or provider after success is `payment.already_succeeded` (409), never 200.

### LOCK-SF-076 — Reservation Cycle is not a Payment Attempt
An Order-level Reservation Cycle is a distinct auditable lifetime. Creating or failing a Payment Attempt never opens, closes, numbers, or extends a cycle.

### LOCK-SF-077 — Failed attempts inside an active cycle never reset the deadline
While a cycle is Active, payment retries keep the same CycleNumber and the same server `ExpiresAt`. Timer reset/extension is forbidden.

### LOCK-SF-078 — A new cycle starts only after the previous cycle ended and EnsureOrderSupply succeeds
Retry after expiry calls canonical `EnsureOrderSupply`. A new numbered cycle is created only when reacquire is atomic and successful. Released/Expired reservation rows stay historical.

### LOCK-SF-079 — Cycle history is immutable and CycleNumber is monotonic
Cycle N never becomes N+1. Closed cycles are not rewritten. Concurrency cannot allocate duplicate cycle numbers for the same Order. Failed reacquire does not consume a cycle number.

### LOCK-SF-080 — Initial and retry hold policies are distinct
`InitialReservationHoldMinutes` applies to Cycle #1. `RetryReservationHoldMinutes` applies to later cycles. Magic TTLs in callers are forbidden.

### LOCK-SF-081 — Reservation-cycle policy precedence is Offer > Category > Store > Platform
Effective minutes and max cycles resolve Offer override, then Category, then Store, then Platform defaults.

### LOCK-SF-082 — Multi-line orders use the shortest TTL and the strictest max cycles
The Order takes the minimum Initial/Retry minutes and the minimum MaxReservationCycles across required lines. Supply remains atomic.

### LOCK-SF-083 — MaxReservationCycles includes the initial cycle
With Max=3 the Order may have #1 initial plus two retries. A further reacquire is `inventory.reservation.retry_limit_reached`.

### LOCK-SF-084 — Frontend countdown is presentation only
Server `ExpiresAt` and server now are authoritative. The client must not expire, extend, or release inventory. A local display timer is allowed; it must not decide reservation lifecycle.

### LOCK-SF-085 — Pending-payment Orders are independent from Active Cart
Committed unpaid Orders appear under در انتظار پرداخت regardless of the current Active Cart. A new Active Cart cannot hide, replace, or authorize a pending Order.

### LOCK-SF-086 — Failed payment inside an active cycle never resets the customer countdown
The pending-payment card keeps the original server `ExpiresAt` after a failed attempt. The UI must not restart or extend the displayed hold.

### LOCK-SF-087 — Expired retry creates a new cycle only after backend reacquire
`بررسی موجودی و پرداخت مجدد` calls backend `EnsureRetryAfterExpiryAsync` / `EnsureOrderSupply`. A new numbered cycle exists only after authoritative reacquire succeeds on the same Order.

### LOCK-SF-088 — Pending-payment UI is capability-driven
Pay / retry / none come from server `canInitiatePayment`, `canRetryPayment`, and `primaryAction`. Manual AwaitingAdmin is informational unless the capability permits a customer action.

### LOCK-SF-089 — Multiple pending Orders are independently scoped
Each card uses its own committed proof or authenticated ownership, countdown, and actions. Proofs are keyed by checkoutId and must not overwrite each other.

### LOCK-SF-090 — No polling maintains or decides reservation state
Per-second or 1.5s API loops for reservation/expiry are forbidden. One refresh at local 00:00, user action, or navigation is allowed.

### LOCK-SF-091 — Admin reservation-cycle audit is immutable backend history
Admin رزرو موجودی reads `IReservationCycleDirectory` projection + append-only events. Frontend must not reconstruct cycles from current Reservation rows or Payment Attempt counts.

### LOCK-SF-092 — Reservation Cycle status stays distinct from Payment/Order/Supply
Admin must keep Order Status, Payment Status, SupplyStatus, and Reservation Cycle Status as separate fields. They must not be merged into one overloaded badge.

### LOCK-SF-093 — Admin cannot reset or extend the reservation timer
No «تمدید رزرو», timer reset, or direct `ExpiresAt` editor. Hold length changes only through a future explicit policy/task.

### LOCK-SF-094 — Admin grid reservation projection is batched
Orders and Payments grids load reservation summaries with `GetProjectionsAsync`. Per-row cycle/history HTTP or `GetProjectionAsync` inside a page map is forbidden.

### LOCK-SF-095 — Retry-limit and reacquire-failure remain operationally visible
When max cycles are used or reacquire fails, Admin Order Detail must show the business copy and shortage lines. These events must not be hidden or collapsed into a generic expired row.

### LOCK-SF-096 — Stored cycle policy snapshot is the historical display source
Effective hold minutes, max cycles, and policy source shown for a past cycle come from that stored cycle. Current Settings must not rewrite historical Admin policy display.

### LOCK-SF-097 — Reservation policy is configurable at Store, Category, and Offer
Admin can set `InitialReservationHoldMinutes`, `RetryReservationHoldMinutes`, and `MaxReservationCycles` at Store, Category, and Offer. Product is not a policy level. Cooldown and unrelated timers are not part of this policy.

### LOCK-SF-098 — Reservation policy precedence stays Offer > Category > Store > Platform
Effective resolution remains Offer override, then Category override, then Store default, then Platform default. Category parent-tree walk is not a policy level unless a later Task explicitly adds it.

### LOCK-SF-099 — Clearing an override restores inheritance
Removing a Store/Category/Offer override stores null and restores the parent policy. The UI must not persist a copied parent value as a new override.

### LOCK-SF-100 — Effective reservation policy is backend-resolved
Admin and Seller surfaces display effective values and per-field sources returned by the backend preview/resolver. Frontend must not re-implement Offer > Category > Store > Platform.

### LOCK-SF-101 — Settings change future cycles only
Changing reservation policy Settings must not rewrite an active cycle `ExpiresAt` or historical cycle policy snapshots. The next new cycle/order uses the updated policy.

### LOCK-SF-102 — Seller reservation-policy mutation requires an explicit permission
Admin may edit Store/Category/Offer reservation policy. Seller mutation is denied unless an explicit backend permission exists and is granted. Absence of that permission is deny.

### LOCK-SF-103 — Checkout commit is atomic
Required inventory reservation, Order aggregate creation, Reservation Cycle #1, and source Cart conversion commit together. Frontend clears or converts Cart only after an authoritative backend commit. External payment initiation stays outside the database transaction.

### LOCK-SF-104 — Pending-card hide is presentation only
Hiding a pending-payment card must not mutate Order, Payment, Supply, Reservation, or future open-unpaid counting. Source of truth is backend and customer-scoped. Hide is denied while the reservation hold is still active.

### LOCK-SF-105 — Customer cancel uses canonical lifecycle
Customer لغو سفارش uses canonical Order cancellation and releases active supply through the backend lifecycle. It is never implemented as presentation-only hide.

### LOCK-SF-106 — Canonical customer login is locale-aware mobile+OTP
Customer login is `/fa/login` and `/en/login` using the existing Identity OTP + session architecture. Do not add a storefront password form, login modal, or a second auth stack.

### LOCK-SF-107 — Default checkout identity is AuthenticatedOnly
Store `CheckoutIdentityPolicy` defaults to AuthenticatedOnly. GuestAllowed is an explicit Admin setting. Cart may stay anonymous; Shipping, Checkout, and Payment require authentication under AuthenticatedOnly.

### LOCK-SF-108 — Checkout auth gate is backend-enforced
Frontend redirect to Login is not sufficient. Shipping projection/commit, checkout submit, and payment initiation/access reject anonymous callers under AuthenticatedOnly with `checkout.authentication_required`.

### LOCK-SF-109 — Anonymous cart merge is server-authoritative
After login, anonymous cart merge uses server-proven guest ownership. It does not create Reservation, Order, or Payment, does not overwrite an authenticated Active cart, and does not silently drop unavailable lines.

### LOCK-SF-110 — Development OTP fixture is environment-gated
Fixed Development mobile/OTP (`09111111111` / `123456`) is enabled only in Development/Testing. Production must not accept that fixed code unless the real configured provider issued it.

### LOCK-SF-111 — Safe returnTo after login
Login `returnTo` accepts only internal locale-prefixed paths. Open redirects, protocol-relative URLs, and guest/payment secrets in query are rejected.

### LOCK-SF-112 — Open unpaid limit counts Orders
`MaxOpenUnpaidOrdersPerCustomer` is scoped by Store + CustomerId and counts Seller Orders, never Payment Attempts.

### LOCK-SF-113 — Hidden pending cards remain counted
Hiding a pending-payment card does not change open-unpaid counting while the underlying Order is still open/unpaid.

### LOCK-SF-114 — Cancel frees open capacity, not churn quota
Successful customer cancel removes the Order from the open-unpaid count and releases the active reservation. It does not delete or refund the reservation-churn quota event.

### LOCK-SF-115 — Churn event is Cycle #1 only
Every new successful checkout that creates Reservation Cycle #1 writes one immutable `CheckoutReservationCommit`. Payment retries on the same Order and Cycle #2+ do not add another checkout-commit event.

### LOCK-SF-116 — Limits are backend and concurrency-safe
Open-unpaid and reservation-churn limits are enforced on the backend before inventory reservation and Order creation. Concurrent last-slot submissions cannot both succeed.

### LOCK-SF-117 — Active Cart is server commerce state
The Active Cart is persistent server-side commerce state. Auth Session carries customer identity only and must not store Cart line data.

### LOCK-SF-118 — Login merge preserves Active Cart until atomic COMMIT
Login and anonymous-to-authenticated cart merge must keep the authenticated Active Cart populated until the final atomic checkout COMMIT converts it. Empty leftover authenticated carts must not shadow a guest cart that has lines.

### LOCK-SF-119 — Canonical storefront header/account state
Authenticated storefront header/account state is canonical and shared across all storefront routes through one header/account menu. Logged-out shows ورود; logged-in must not.

### LOCK-SF-120 — Logout is always visible when authenticated
Customer logout is always available when authenticated, uses the canonical session logout endpoint, and must not leak the authenticated Cart pointer to the next anonymous browser.

### LOCK-SF-121 — New addresses store FirstName and LastName separately
New shipping addresses persist FirstName and LastName independently. Committed order recipient snapshots keep both. Historical RecipientName-only rows remain readable without guessing a split.

### LOCK-SF-122 — Explicit FirstName/LastName are canonical
For new or updated checkout recipient data and committed Order snapshots, FirstName and LastName are the canonical source. Display is `FirstName + " " + LastName` when both are non-empty. Legacy RecipientName is a fallback only when those split fields are absent. Legacy names are never guessed-split, and RecipientName must not override non-empty FirstName/LastName.

### LOCK-SF-123 — Logout hides Cart, never deletes it
Logout terminates authentication and must not delete, convert, cancel, or abandon the customer-owned Active Cart. The Cart is hidden from the anonymous browser context and restored or merged on the same customer's re-login.

### LOCK-SF-124 — Authenticated header shows canonical customer identity
Authenticated Storefront header displays canonical customer identity — profile name when available, otherwise mobile — through one shared account menu on all Storefront routes. Shipping recipient is not account identity.

### LOCK-SF-125 — Cart conversion is visible only after atomic COMMIT
Header badge and cart lines must not be optimistically cleared on the final checkout click. Conversion becomes visible only after the authoritative atomic checkout COMMIT succeeds.

### LOCK-SF-126 — Canonical storefront session resolution
Storefront account state is resolved through one in-memory session cache with in-flight dedupe. Desktop and mobile header copies reuse that cache. Expected anonymous 401 after logout is cached and must not be retried. Cart merge is one logical POST per login transition. No auth/cart interval polling.

### LOCK-SF-127 — Store appearance is declarative and Store-scoped
Store appearance is a declarative, Store-scoped contract. Executable HTML, CSS, or JavaScript from the database is forbidden.

### LOCK-SF-128 — Curated appearance now; custom theme only via validated tokens
The current release uses curated/preset appearance choices only. A future custom theme may exist only as validated declarative tokens — never as free-form CSS/HTML/JS.

### LOCK-SF-129 — Brand tokens stay separate from status semantics
Brand/primary tokens must not redefine danger, success, or warning semantics.

### LOCK-SF-130 — Appearance changes need no per-store build
Changing Store appearance must not require a per-store frontend build, publish, or app-pool restart. Runtime applies CSS variables from a cached Store projection.

### LOCK-SF-131 — One Product Card behavior; skins are future presentation only
StorefrontProductCardView remains one canonical commerce/behavior component. Future controlled visual skins may change presentation only; pricing, actions, and accessibility stay shared. Skins are not implemented in T005.

### LOCK-SF-132 — Future Landing Pages are DB-backed approved sections
Future Landing Pages are Store+Locale database pages composed only from approved registered Sections. They are not physical Next.js files.

### LOCK-SF-133 — Publishing a Landing Page needs no Storefront redeploy
Adding or publishing a Landing Page must not require a new physical frontend page file or a Storefront redeploy.

### LOCK-SF-134 — One selectable Home Page per Store
A Store has one canonical selectable Home Page reference (Store.HomePageId or equivalent). Menu selection is independent of that reference.

### LOCK-SF-135 — Page data sources are approved pickers, never SQL
Page authors choose approved product/data sources (Manual / Category / Brand / Newest / BestSelling / Featured / Discounted). SQL or arbitrary query authoring is forbidden.

### LOCK-SF-136 — Menu remains independent
Menu/MenuItem ownership stays independent. Pages or Sections may reference an approved Menu or MenuGroup; they do not own the menu tree.

### LOCK-SF-137 — Dynamic Page routes cannot shadow reserved routes
Dynamic Store+Locale+Slug page resolution cannot shadow reserved system, product, category, cart, checkout, account, admin, or API routes.

### LOCK-SF-138 — Free-form page-builder execution is forbidden
Free-form page-builder HTML/CSS/JS execution is forbidden by this architecture.

### LOCK-SF-139 — Admin appearance persists PaletteKey only
Store Admin appearance palette selection persists only approved PaletteKey values. Raw colors are not stored by this preset feature.

### LOCK-SF-140 — Palette registry is code-owned
Palette preset registry is code-owned and typed. The database references stable keys only.

### LOCK-SF-141 — Appearance save invalidates one Store cache
Appearance save invalidates only the affected Store cache and must not require a TTL wait, process restart, or frontend redeploy.

### LOCK-SF-142 — Admin preview and Storefront share one registry
Admin preview and live Storefront consume the same canonical palette token definitions.

### LOCK-SF-143 — Brand palettes do not redefine status colors
Brand palette changes must not redefine danger, success, or warning semantic colors.

### LOCK-SF-144 — Appearance write is backend-authoritative
Appearance write authorization and Store isolation are backend-authoritative. The frontend cannot grant cross-store writes.

### LOCK-SF-145 — Storefront brand surfaces use semantic tokens
Every Storefront brand-bound visual surface must resolve brand color from canonical semantic appearance tokens, not page-local hard-coded brand hexes.

### LOCK-SF-146 — Legacy Shopeiva brand red is not a Storefront CTA
Legacy Shopeiva brand red must not survive as a Storefront brand CTA/accent where the canonical Store palette should apply.

### LOCK-SF-147 — Status semantics stay independent of PaletteKey
Danger/success/warning/validation/payment-status semantics remain independent of Store brand PaletteKey.

### LOCK-SF-148 — Home/PDP palette migration changes color only
Home and PDP palette migration may change brand color only; locked geometry, layout, and interaction remain unchanged.

### LOCK-SF-149 — Curated palettes must meet contrast
All curated palettes must satisfy canonical contrast requirements before being selectable.

### LOCK-SF-150 — No page-local palette registry or fetch
Storefront pages must not define local palette registries or independent appearance fetch/state.

### LOCK-SF-151 — ThemeMode is a controlled Store setting
ThemeMode is a controlled Store appearance setting; arbitrary mode values are forbidden.

### LOCK-SF-152 — Canonical ThemeMode semantics
LightOnly/DarkOnly/System/UserChoice semantics are canonical and backend-validated.

### LOCK-SF-153 — Dark mode uses semantic surface tokens
Dark mode must be driven by semantic surface/text/border tokens, not page-local dark color branches.

### LOCK-SF-154 — UserChoice is device preference
UserChoice preference is user-device preference and must not mutate Store ThemeMode or commerce state.

### LOCK-SF-155 — First paint resolves theme without flash
First paint must resolve the effective theme without avoidable light/dark flash.

### LOCK-SF-156 — Palette and ThemeMode compose; status stays independent
PaletteKey and ThemeMode compose through one canonical appearance system; status semantic colors remain independent.

### LOCK-SF-157 — Home/PDP dark changes color only
Home/PDP dark implementation may change color/surface only; locked geometry/interactions remain unchanged.

### LOCK-SF-158 — Dark brand-emphasis is a canonical token
Meaningful brand text/links/focus on dark surfaces resolve from a canonical per-palette on-dark brand-emphasis token. CTA fill stays primary/on-primary. No page-local or per-palette component color branches.

### LOCK-SF-159 — ProductCardSkin is a controlled Store appearance key
ProductCardSkin is a Store-scoped curated appearance key. Arbitrary style payloads, raw HTML/CSS/JS from the database, and custom color builders are forbidden.

### LOCK-SF-160 — Skins are presentation-only on one canonical card
Product card skins change presentation only. There is one canonical StorefrontProductCardView behavior/component; no duplicate business card per skin.

### LOCK-SF-161 — Skin may not alter commerce semantics
A skin may not alter product data, pricing, stock, cart, navigation, analytics, or accessibility semantics.

### LOCK-SF-162 — Card geometry and grid density stay canonical
Card width, image aspect, title/price structure, action positions, hover-action semantics, and listing grid density stay canonical. Radical layouts require a future ProductCardLayout, not a skin.

### LOCK-SF-163 — All storefront cards consume the Store skin
All storefront card usages consume the same effective Store ProductCardSkin unless a future page override is architected.

### LOCK-SF-164 — Skins compose with PaletteKey and ThemeMode
Skins compose with every PaletteKey × ThemeMode through semantic tokens. No per-palette or per-dark component branches.

### LOCK-SF-165 — Admin appearance choices stay human-readable
Store Admin appearance choices use human-readable labels and meaningful visual previews. Stable technical keys remain internal and must not appear as normal Admin UI copy.

### LOCK-SF-166 — Landing Page is Store-scoped DB content
Landing/Page is Store-scoped, locale-aware, database-backed content. Adding a Page must not require a frontend rebuild or redeploy.

### LOCK-SF-167 — Public Page resolution is Published only
Public dynamic Page resolution uses Store + Locale + Slug and only Published pages.

### LOCK-SF-168 — System routes precede Landing slugs
System, product, category, cart, checkout, account, admin, and API routes have precedence and cannot be shadowed by Page slugs.

### LOCK-SF-169 — Home Page selection is same-Store and optional
Store Home Page selection references one same-Store eligible Page. An unset reference preserves the canonical Home until a later composer migration.

### LOCK-SF-170 — Pages do not execute markup
Page records do not store or execute arbitrary HTML, CSS, or JavaScript.

### LOCK-SF-171 — Page authoring is not a query console
Page authoring never exposes SQL or arbitrary query authoring.

### LOCK-SF-172 — Landing Pages use approved SectionTypes only
Landing Pages are composed only from approved code-owned SectionTypes.

### LOCK-SF-173 — PageSection config is schema-validated
PageSection configuration is typed/schema-validated and cannot execute arbitrary HTML, CSS, or JavaScript.

### LOCK-SF-174 — Controlled data sources only
Product/content sections use approved controlled data sources; user-authored SQL or arbitrary query expressions are forbidden.

### LOCK-SF-175 — PageSection and references stay Store-scoped
PageSection ownership and all referenced commerce/content entities remain Store-scoped.

### LOCK-SF-176 — Public projection is enabled Published sections
Public Page projection includes only enabled Sections of a Published Page in deterministic order.

### LOCK-SF-177 — Reorder keeps PageSection identity
Reordering preserves stable PageSection identity; reorder is not delete/recreate.

### LOCK-SF-178 — Section limits are server-enforced
Section configuration limits are server-enforced to prevent unbounded page/query cost.

### LOCK-SF-179 — Landing Composer stays human-readable
Store Admin Landing/Page Composer must be human-readable and must not expose technical IDs, enum keys, JSON, SQL, or route internals.

### LOCK-SF-180 — Typed section forms and Store-scoped pickers
Section editing uses typed section-specific forms and Store-scoped searchable pickers, never raw config editing.

### LOCK-SF-181 — Draft preview is authorized and non-indexable
Draft preview is authorized/non-indexable and must not make Draft publicly resolvable.

### LOCK-SF-182 — One canonical Landing renderer inherits Store appearance
Public Landing rendering uses one canonical approved Section renderer and inherits Store PaletteKey, ThemeMode, and ProductCardSkin.

### LOCK-SF-183 — Home selection uses HomePageId with safe fallback
Selecting a Published Landing Page as Home replaces Home composition only through canonical HomePageId; unset/invalid selection falls back safely to canonical Home.

### LOCK-SF-184 — Publish and writes invalidate relevant caches only
Publishing/unpublishing and Page/Section writes invalidate only relevant Page/Home caches and never require rebuild/redeploy.

### LOCK-SF-185 — Reorder keeps identity and stays accessible
Composer reorder preserves stable PageSection identity and must remain accessible beyond pointer-only drag/drop.

### LOCK-SF-186 — Menu is Store-scoped structured navigation
Menu/MenuItem is Store-scoped structured navigation. Arbitrary HTML, CSS, or JavaScript in menus is forbidden.

### LOCK-SF-187 — Menu destinations use typed pickers
Menu destinations use approved typed link kinds and Store-scoped searchable pickers. Technical IDs are not normal-facing inputs.

### LOCK-SF-188 — Menu hierarchy is bounded and cycle-safe
Menu hierarchy has bounded canonical depth (L1–L3), cycle prevention, deterministic sibling order, and stable MenuItem identity.

### LOCK-SF-189 — Disabled MenuItems stay in the editor
Disabled MenuItems are excluded from public projection without deleting editor state. A disabled parent hides its descendants publicly.

### LOCK-SF-190 — Menu references fall back safely
Page/Home/Header menu references must resolve only enabled same-Store compatible menus. Unset or invalid references preserve accepted fallback navigation.

### LOCK-SF-191 — One Menu projection, controlled presentations
Header and Landing may use different controlled presentations but share one canonical Menu tree/projection and business rules.

### LOCK-SF-192 — External menu URLs are safe web schemes
External menu URLs allow only validated http/https web schemes. javascript/data and other schemes are rejected.

### LOCK-SF-193 — Storefront background tone is a controlled Appearance setting
Storefront background tone is a controlled optional Store appearance setting. Arbitrary background colors, free-form color pickers, and page-authored CSS washes are forbidden.

### LOCK-SF-194 — BackgroundStyle is Neutral or PaletteTint
BackgroundStyle supports only Neutral and PaletteTint. Neutral is the backward-compatible default for missing/legacy values.

### LOCK-SF-195 — PaletteTint uses curated semantic tint tokens
PaletteTint uses curated semantic tint tokens per palette and theme. It must not derive a page wash from primary alpha, repurpose status semantics, or heavily tint elevated cards, inputs, modals, or mini-cart surfaces.

### LOCK-SF-196 — One Store BackgroundStyle for Home and Landing
Home, Landing, and custom Home inherit one canonical Store BackgroundStyle. Page-local or section-local background overrides are forbidden.

### LOCK-SF-197 — One storefront semantic surface hierarchy
All storefront page families share one semantic surface hierarchy: PageBackground, SectionSurface, SectionAlternate, SectionAccent.

### LOCK-SF-198 — Page-specific theme token families are forbidden
Home, PDP, Blog, Cart, Checkout, Account, and other storefront families map to the shared global surface roles. Page-specific theme token families are forbidden.

### LOCK-SF-199 — PaletteTint supplies curated four-role values
PaletteTint supplies curated light and dark values for the four surface roles. Neutral remains the backward-compatible white/gray hierarchy.

### LOCK-SF-200 — Landing SectionTypes have code-owned surface roles
Dynamic Landing SectionTypes have code-owned default surface-role mappings. No user-facing per-section arbitrary color override exists in this release.

### LOCK-SF-201 — Future custom theme edits global roles only
A future custom theme may override the global semantic surface roles without page-by-page color configuration. Custom color DB fields are not part of this release.

### LOCK-SF-202 — Status and card surfaces stay independent of page/section roles
Status semantics and Card, Elevated, and Input surfaces remain independent from the four page/section roles.

### LOCK-SF-203 — Customer-facing routes inherit one Store Appearance
Every public Storefront and customer-facing route must inherit the canonical Store Appearance projection. The customer panel is not a separate theme system.

### LOCK-SF-204 — Major backgrounds require a semantic surface role
Every major page/section background must map to a canonical semantic surface role or an explicitly allowed Card/Input/Header/Footer/Media/Status role.

### LOCK-SF-205 — New routes join inventory and coverage guards
New Storefront/customer routes must be added to the route coverage inventory and pass semantic surface coverage before acceptance.

### LOCK-SF-206 — No large hardcoded page/section backgrounds
Large hardcoded white/gray/arbitrary backgrounds are forbidden on Storefront/customer page/section wrappers when they bypass semantic theme roles.

### LOCK-SF-207 — Dynamic and customer-nav routes require crawl coverage
Dynamic routes require representative runtime coverage. Customer navigation destinations require authenticated crawl coverage.

### LOCK-SF-208 — Coverage is enforced by shared wrappers and guards
Theme coverage is enforced by shared wrappers/primitives and generalized guards, not manual screenshot-by-screenshot patching.

### LOCK-SF-209 — Customer-facing components consume semantic surfaces
Customer-facing components consume semantic or centrally derived surfaces. Structural backgrounds cannot bypass Store Appearance.

### LOCK-SF-210 — Only global surface roles are user-configurable
Only the four global surface roles are user-configurable Store settings. Card, Elevated, Input, Interactive, Media, Overlay, and Border are centrally derived and must not gain DB, API, or Admin color fields.

### LOCK-SF-211 — Component compliance uses shared primitives
Component-level theme compliance uses shared primitives and guards, not page- or component-specific color settings.

### LOCK-SF-212 — Shared semantic system across customer surfaces
PDP, Home, Commerce, Auth, Content, and Customer Panel components share the same semantic surface system.

### LOCK-SF-213 — New customer-facing components pass surface compliance
New customer-facing components must pass component-level surface compliance before acceptance.

### LOCK-SF-214 — Raw structural white/gray/hex is classified or forbidden
Raw structural white, gray, or hex backgrounds are forbidden on customer-facing components unless centrally classified as Media, Status, Decorative, or another semantic exception.

### LOCK-SF-215 — Structural children inherit section context
Structural and content children inherit the parent section context by default. Independent Card, Elevated, Input, Interactive, Media, and Overlay surfaces are explicit semantic opt-ins.

### LOCK-SF-216 — Local surfaces derive from section context
SectionSurface, SectionAlternate, and SectionAccent establish local derived card, elevated, input, interactive, media, and border relationships. Those local surfaces are not Store settings.

### LOCK-SF-217 — Card/Elevated are not generic wrappers
Card and Elevated semantics may not be used as generic wrappers for whole sections or page regions.

### LOCK-SF-218 — PaletteTint acceptance includes composition
PaletteTint acceptance includes section-context visibility and surface-area composition, not token compliance alone.

### LOCK-SF-219 — New components declare inherit vs local surface
New customer-facing components must declare inherit versus an explicit local surface.

### LOCK-SF-220 — User-configurable colors remain four global roles
User-configurable colors remain the four global surface roles. All local surfaces are automatic.

### LOCK-SF-221 — Shared Home+Landing composition engine
Home and Landing share one composition engine and one Section/Variant registry.

### LOCK-SF-222 — Controlled composition only
Composition is controlled and preset-driven; arbitrary CSS, HTML, or JS settings are forbidden.

### LOCK-SF-223 — Mobile behavior is code-owned
Mobile responsive behavior is code-owned per Variant via Responsive Contracts; users do not configure breakpoints.

### LOCK-SF-224 — Templates are composition presets
Industry templates are editable composition presets from the shared registry, not special renderers or pages.

### LOCK-SF-225 — Reuse Shopeiva blocks before redesign
Existing Shopeiva Home/Landing blocks are reused or adapted before redundant redesign.

### LOCK-SF-226 — Four global surface roles only
Only four global surface roles are user-editable; local colors remain derived.

### LOCK-SF-227 — Truthful data sources
Data-source options must reflect real backend truth; unsupported ranking sources are not faked.

### LOCK-SF-228 — Prefer Variants over SectionTypes
Visual diversity should prefer curated Variants over proliferating Section Types.

### LOCK-SF-229 — Shared Section/Variant renderer path
Home and Landing runtime sections resolve through the shared Section/Variant renderer contract.

### LOCK-SF-230 — Admin exposes only implemented Variants
Admin may expose only implemented/valid Variants with human-readable previews; planned variants remain unavailable until implemented.

### LOCK-SF-231 — Capability-driven Variant settings
Variant settings are capability-driven and controlled; unsupported controls are hidden/rejected.

### LOCK-SF-232 — Responsive contracts own mobile layout
Responsive behavior comes from registered Variant contracts; mobile layout remains code-owned.

### LOCK-SF-233 — Reuse accepted Shopeiva Home blocks
Existing accepted Shopeiva Home blocks are adapted/reused rather than redundantly redesigned.

### LOCK-SF-234 — Landing compatibility adapters
Existing Landing configs remain readable through deterministic compatibility adapters until explicit migration.

### LOCK-SF-235 — Swiper primary carousel engine
One primary carousel engine (Swiper) serves composition variants unless a future proven gap justifies otherwise.

### LOCK-SF-236 — Native StoryRail and BannerShowcase contracts
StoryRail and BannerShowcase are native composition section contracts, not semantic proxies.

### LOCK-SF-237 — Distinct selectable Variant renderers
Every selectable Variant must have a real distinct renderer, responsive contract, and capability metadata.

### LOCK-SF-238 — Bounded banner slot layouts
Banner layouts use bounded Variant slot patterns and size presets; arbitrary pixel layout is forbidden.

### LOCK-SF-239 — Industry templates are editable compositions
Industry templates are distinct editable compositions built only from implemented Variants and truthful sources.

### LOCK-SF-240 — Templates create normal Drafts
Template selection creates a normal Draft; templates do not own special renderers or CSS.

### LOCK-SF-241 — Mobile remains system-owned per Variant
Mobile rendering remains system-owned for every Variant.

### LOCK-SF-242 — Plain-language Admin capabilities
Admin exposes only plain-language controlled capabilities supported by the selected Variant.

### LOCK-SF-243 — Selectable Variants require meaningful visual difference
Every selectable Variant must present a meaningful visual difference plus preview metadata. Alias-only Variants are forbidden.

### LOCK-SF-244 — ProductShowcase reuses shared product presentation
ProductShowcase variants reuse the shared product presentation system (ProductCard / ProductRail) rather than creating a disconnected second card system.

### LOCK-SF-245 — Template fidelity never owns renderers or CSS
Template fidelity may vary composition, order, and Variant choice, but must never introduce template-owned renderers or CSS.

### LOCK-SF-246 — Builder Admin summaries stay ordinary-user-facing
Builder Admin summaries remain ordinary-user-facing and hide technical configuration internals (raw keys, JSON, breakpoints).

### LOCK-SF-247 — Empty data states degrade gracefully
Empty data states must degrade gracefully in Admin and Storefront — friendly guidance in Admin, no broken chrome on Storefront.

### LOCK-SF-248 — Mobile visual behavior remains code-owned
Mobile visual behavior remains code-owned and bounded for all Variants; Admin must not expose mobile layout controls.

### LOCK-SF-249 — Ranked Variants stay truthful-source only
ProductRankedList / ticker Variants may use only truthful Admin-selectable sources (Manual/Category/Brand/Newest). Fake ranking APIs are forbidden.
