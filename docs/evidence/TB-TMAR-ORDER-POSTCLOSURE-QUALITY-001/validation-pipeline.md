# Validation pipeline — TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001

## Historical claim vs current

`docs/evidence/TB-TMAR-FND-001/pipeline-foundation.md` claimed ValidationBehavior + LoggingBehavior registered.

**Current code (authoritative):** BuildingBlocks `TmarFoundation.cs` still owns:

- FluentValidation 11.11.0 (+ DI extensions)
- `ValidationBehavior<TRequest,TResponse>` (throws `ValidationException` → SafeErrorMapper `validation.failed` + field ErrorCodes)
- `LoggingBehavior` + `TracingBehavior`
- MediatR **12.5.0**
- `AddToobaCqrsFoundation`

## Foundation change in this task

`AddToobaCqrsFoundation` now also `AddValidatorsFromAssembly` for each additional handler assembly (module Application assemblies), so Order validators are discovered canonically — not an Order-only pipeline hack. Exactly one ValidationBehavior registration remains.

## Registration path

Host `Program.cs` → `AddToobaCqrsFoundation(..., typeof(ListAdminOrderNotesQuery).Assembly, …)` → MediatR handlers + FluentValidation validators from Order.Application.

## Validators added (VALIDATOR_ADDED)

| Request | Validator |
|---|---|
| AddAdminOrderNoteCommand | AddAdminOrderNoteCommandValidator |
| GetAdminOrderOperationalHistoryQuery | GetAdminOrderOperationalHistoryQueryValidator |
| GetAdminOrderDetailQuery | GetAdminOrderDetailQueryValidator |
| CancelOrderCommand | CancelOrderCommandValidator |
| QueryAdminOrdersGridQuery | QueryAdminOrdersGridQueryValidator |
| RecoverOrderInventoryReservationCommand | RecoverOrderInventoryReservationCommandValidator |
| EnsureOrderSupplyCommand | EnsureOrderSupplyCommandValidator |
| SubmitStorefrontCheckoutCommand | SubmitStorefrontCheckoutCommandValidator |
| CancelPendingCheckoutCommand | CancelPendingCheckoutCommandValidator |
| GetCustomerOrderDetailQuery | GetCustomerOrderDetailQueryValidator |
| RetryCustomerUnpaidOrderCommand | RetryCustomerUnpaidOrderCommandValidator |
| GetSellerOrderDetailQuery | GetSellerOrderDetailQueryValidator |

Stable codes: `Tooba.Order.Application.Validation.OrderValidationCodes`.

## NO_VALIDATOR_REQUIRED (examples)

Parameterless / trivial transport requests with no meaningful primitive schema (e.g. `GetAdminOrderDashboardMetricsQuery`, `ListAdminCustomersQuery`, `ListAdminOrdersQuery`) — no empty validators added.

Many Admin Operations commands share the same body shape; CancelOrder is the representative validator for that family. Additional twin validators were not mass-duplicated.

## Business rules intentionally excluded from FluentValidation

- Checkout/order existence and ownership
- Seller scope / view access
- Payment expired / retry limits / reservation cycle state
- Inventory availability / reacquire
- Cancellation allowed by current status (`SellerOrderCancellationPolicy` remains policy)
- Abuse gate open-unpaid / reservation-commit caps
- Grid field whitelist (stays GridPolicy / handler)

## Error semantics

Validation failures → `ValidationException` → SafeErrorMapper → `validation.failed` with per-field ErrorCodes (not localized message identity, not PlatformHttpException, not ex.Message classification).
