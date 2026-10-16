# TB-SHIPPING-ONE-LEVEL-TABS-139 — Paid-order fulfillment event delivery repair (bounded)

Parent: `TB-SHIPPING-ONE-LEVEL-TABS-138`
Mode: `BOUNDED_PAID_ORDER_FULFILLMENT_EVENT_DELIVERY_REPAIR`
Base: `ca4ee6e97298420f60614634fde2dbfc70ec944f`
Status: **PARTIAL — STOP_FOR_ARCHITECT**

## 1. What the parent run proved and what it left open

TB-138 paid a real grouped checkout in an isolated environment: 1 Checkout, 1 SellerOrder, 2 accepted
`order.order_shipping_groups` (disjoint line membership), 3 payment allocations, SellerOrder `Paid`.
`payment.succeeded.v1` was published and **Order** consumed it (`order.payment_inbox = 1`), while
**Fulfillment** showed `payment_inbox = 0` and `fulfillments = 0`.

## 2. Root-cause work in this task (evidence chain, no speculation)

1. `Tooba:Outbox` `MarkProcessedAsync` runs at **enqueue** time, not at consume time
   (`Outbox/OutboxDispatcher.cs:146-153`, `Messaging/MassTransitIntegrationEventPublisher.cs:69-99`).
   The TB-138 `processed=…|attempts=1|err=-` row therefore says nothing about handler execution.
2. Registration is **not** the missing boundary: `ToobaModuleComposition.Modules` registers
   `OrderModule` and `FulfillmentModule` unconditionally, `FulfillmentModule.cs:128` registers
   `FulfillmentPaymentSucceededHandler`, and the isolated database had 10 applied fulfillment
   migrations — so that flat method ran past line 128.
3. The dispatcher **did** reach Order's handler: one deployment-wide receive endpoint
   (`MessagingRegistration.cs:59-69`) resolves every `IIntegrationEventHandler<PaymentSucceededIntegrationEvent>`
   in one consume scope (`Transport/ToobaIntegrationTransportConsumer.cs`) and `OrderModule` precedes
   `FulfillmentModule` in the composition list, so Order's handler is enumerated first and commits its
   inbox (`OrderPaymentSucceededHandler.cs:88-101`).
4. `FulfillmentDirectory.CreateFromPaidSellerOrdersAsync` commits units, items, shipping groups **and**
   its `PaymentInbox` row in a **single** `SaveChangesAsync`. One domain-invariant throw or one write
   failure therefore leaves `payment_inbox = 0` and `fulfillments = 0` with no self-healing path,
   because the transport message is retried and dead-lettered while Order's committed inbox keeps Order
   idempotent. That is exactly the TB-138 fingerprint.

**First failing boundary:** the pre-139 delivery loop aborted on the **first** failing module handler and
so removed the delivery of the same event to every later module — the failure has no per-module durable
evidence. The residual fault **inside** the Fulfillment owner path (domain invariant vs its single
`SaveChanges`) is **not** identified by source reading and is **not** fixed, bypassed or guessed here.

## 3. Owner-correct change (smallest, bounded)

`src/backend/Host/Tooba.Host/Transport/ToobaIntegrationTransportConsumer.cs`

- New internal seam `ToobaIntegrationTransportConsumer.InvokeRegisteredHandlersAsync(...)`:
  every registered `IIntegrationEventHandler<TEvent>` of the delivered event is invoked **independently**;
  a failing module handler no longer starves the other module handlers of the same event.
- Failure is never swallowed: failures are collected, counted (`tooba.messaging.handler_failures`) and
  **re-thrown** as `IntegrationHandlerDeliveryException`, so MassTransit retry/dead-letter semantics and
  the outbox contract are preserved.
- The reflection wrapper is unwrapped (`TargetInvocationException.InnerException`), so the log line
  reports the **real** error type per handler — that is the observability the next isolated run needs.
- Cancellation (`OperationCanceledException` with a cancelled token) is re-thrown as cancellation, not
  converted into a delivery failure.
- No behaviour change for a healthy deployment: with all handlers succeeding the invocation count, the
  `Consumed` counter and the existing `Integration events processed` log are unchanged; each module still
  deduplicates through its own inbox (exactly-once per module).
- Transport folder still holds **exactly** the three platform files
  (`HostTransportAmcGuardTests`); no module type, DbContext, Contracts or module-boundary rule touched.

## 4. Focused proof (tests that fail on the pre-139 loop)

`src/backend/Host/Tooba.Host.Tests/IntegrationHandlerDeliveryFanOutTests.cs` — 5 tests, all passing
(`Passed! Failed: 0, Passed: 5`):

1. one `payment.succeeded.v1` reaches **every** registered module handler exactly once (same `EventId`);
2. a throwing module handler does **not** starve the later module handlers (order=1, settlement=1) and the
   failure names the failing handler;
3. handler failure is still surfaced so retry/dead-letter is preserved;
4. consume cancellation is not swallowed as a handler failure;
5. `Legacy_sequential_loop_starved_the_module_handlers_that_came_after_a_failing_handler` — characterises
   the pre-139 loop (order=1, settlement=**0**, starved=true) and then the new seam (order=2, settlement=1)
   for the same input, i.e. the red/green pair for the defect.

Guard/regression scope in the same project: `51 passed / 1 failed`. The single failure is
**pre-existing and unrelated**: `PaymentFoundationTests.Payment_verifies_provider_evidence_and_projects_order_without_trusting_callback_text`
expects `InvalidOperationException` but the payment-initiate path throws
`ContractOperationException: payment.initiate.reservation_forbidden` (`PaymentFoundationTests.cs:192`);
this task touched neither Payment initiate logic nor the in-process publisher.

## 5. Not proven here (honest ledger)

- **Runtime acceptance not executed** inside the 45-minute box: no isolated Host/Postgres/browser re-run,
  so `FulfillmentUnit` creation with two stable `ShippingGroupId`s and exact line membership is
  **UNPROVEN** at runtime by this task.
- Settlement / payout closure remains out of scope and untouched (no `settlement` schema assumption, no
  allocation workaround).
- The residual Fulfillment-side fault is deliberately **not** guessed and not bypassed (no direct DB
  inserts, no manual handler calls from Order, no unit creation in Host).

## 6. Next bounded experiment (for the Architect)

Re-run the TB-138 paid path in the same disposable isolation and, **before cleanup**, read
`tooba_*_messaging` transport error/dead-letter payload plus the new
`Integration handler failed. EventType=… Handler=… ErrorType=…` log line. That yields the exact
`ContractOperationException`/EF failure of the Fulfillment owner path, which can then be repaired in one
owner-correct change.

## 7. Rollback

Revert the single commit of this task: the transport seam returns to the sequential first-throw-aborts
loop and the new test file is removed. No data, schema, migration, configuration or shared environment is
affected by the change.
