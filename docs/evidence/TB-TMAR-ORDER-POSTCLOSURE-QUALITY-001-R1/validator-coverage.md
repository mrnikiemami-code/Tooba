# Validator coverage — TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1

Shared foundation unchanged: MediatR → ValidationBehavior → FluentValidation → ValidationException → SafeErrorMapper (`validation.failed` + field ErrorCodes).

Reusable helpers: `OrderFluentRules` / `OrderValidationCodes` (Admin Operations envelope shared across all 27 concrete operation commands).

## Endpoint-reachable classification

| Request | Endpoint family | Classification | Validator | Reason |
|---|---|---|---|---|
| AddAdminOrderNoteCommand | Admin Completeness | VALIDATOR_REQUIRED | AddAdminOrderNoteCommandValidator | CheckoutId/Actor/Body |
| DeleteAdminOrderNoteCommand | Admin Completeness | VALIDATOR_REQUIRED | DeleteAdminOrderNoteCommandValidator | CheckoutId/NoteId/Actor |
| ListAdminOrderNotesQuery | Admin Completeness | VALIDATOR_REQUIRED | ListAdminOrderNotesQueryValidator | CheckoutId/Actor |
| GetAdminOrderOperationalHistoryQuery | Admin Completeness | VALIDATOR_REQUIRED | GetAdminOrderOperationalHistoryQueryValidator | CheckoutId/page/pageSize |
| GetAdminOrderInvoiceQuery | Admin Completeness | VALIDATOR_REQUIRED | GetAdminOrderInvoiceQueryValidator | CheckoutId/Actor |
| GetAdminOrderReceiptQuery | Admin Completeness | VALIDATOR_REQUIRED | GetAdminOrderReceiptQueryValidator | CheckoutId/Actor |
| GetAdminOrderDetailQuery | Admin Detail | VALIDATOR_REQUIRED | GetAdminOrderDetailQueryValidator | CheckoutId/ViewerUserId |
| QueryAdminOrdersGridQuery | Admin OrdersGrid | VALIDATOR_REQUIRED | QueryAdminOrdersGridQueryValidator | Grid body required |
| QueryAdminCustomersGridQuery | Admin Customers | VALIDATOR_REQUIRED | QueryAdminCustomersGridQueryValidator | Grid body required |
| ListAdminOrdersQuery | Admin Legacy list | NO_VALIDATOR_REQUIRED | — | parameterless |
| ListAdminCustomersQuery | Admin Customers | NO_VALIDATOR_REQUIRED | — | parameterless |
| GetAdminOrderOperationsQuery | Admin Operations | VALIDATOR_REQUIRED | GetAdminOrderOperationsQueryValidator | CheckoutId/ActorUserId |
| ListAdminOrderReturnEligibilityQuery | Admin Operations | VALIDATOR_REQUIRED | ListAdminOrderReturnEligibilityQueryValidator | CheckoutId |
| CancelOrderCommand | Admin Operations | VALIDATOR_REQUIRED | CancelOrderCommandValidator | shared envelope |
| ApproveReturnCommand | Admin Operations | VALIDATOR_REQUIRED | ApproveReturnCommandValidator | shared envelope |
| AssignConsolidatedPackageTrackingCommand | Admin Operations | VALIDATOR_REQUIRED | AssignConsolidatedPackageTrackingCommandValidator | shared envelope |
| AssignTrackingCommand | Admin Operations | VALIDATOR_REQUIRED | AssignTrackingCommandValidator | shared envelope |
| CancelConsolidatedPackageCommand | Admin Operations | VALIDATOR_REQUIRED | CancelConsolidatedPackageCommandValidator | shared envelope |
| CancelShipmentCommand | Admin Operations | VALIDATOR_REQUIRED | CancelShipmentCommandValidator | shared envelope |
| ConfirmDepositCommand | Admin Operations | VALIDATOR_REQUIRED | ConfirmDepositCommandValidator | shared envelope |
| CorrectTrackingCommand | Admin Operations | VALIDATOR_REQUIRED | CorrectTrackingCommandValidator | shared envelope |
| CreateConsolidatedPackageCommand | Admin Operations | VALIDATOR_REQUIRED | CreateConsolidatedPackageCommandValidator | shared envelope |
| CreateShipmentCommand | Admin Operations | VALIDATOR_REQUIRED | CreateShipmentCommandValidator | shared envelope |
| DeliverConsolidatedPackageCommand | Admin Operations | VALIDATOR_REQUIRED | DeliverConsolidatedPackageCommandValidator | shared envelope |
| DeliverShipmentCommand | Admin Operations | VALIDATOR_REQUIRED | DeliverShipmentCommandValidator | shared envelope |
| DispatchConsolidatedPackageCommand | Admin Operations | VALIDATOR_REQUIRED | DispatchConsolidatedPackageCommandValidator | shared envelope |
| DispatchShipmentCommand | Admin Operations | VALIDATOR_REQUIRED | DispatchShipmentCommandValidator | shared envelope |
| MarkFulfillmentPackedCommand | Admin Operations | VALIDATOR_REQUIRED | MarkFulfillmentPackedCommandValidator | shared envelope |
| MarkFulfillmentProcessingCommand | Admin Operations | VALIDATOR_REQUIRED | MarkFulfillmentProcessingCommandValidator | shared envelope |
| PackFulfillmentSelectedCommand | Admin Operations | VALIDATOR_REQUIRED | PackFulfillmentSelectedCommandValidator | shared envelope |
| RecoverInventoryReservationCommand | Admin Operations | VALIDATOR_REQUIRED | RecoverInventoryReservationCommandValidator | shared envelope |
| RejectDepositCommand | Admin Operations | VALIDATOR_REQUIRED | RejectDepositCommandValidator | shared envelope |
| RejectReturnCommand | Admin Operations | VALIDATOR_REQUIRED | RejectReturnCommandValidator | shared envelope |
| RequestReturnCommand | Admin Operations | VALIDATOR_REQUIRED | RequestReturnCommandValidator | shared envelope |
| RestoreCancelledOrderCommand | Admin Operations | VALIDATOR_REQUIRED | RestoreCancelledOrderCommandValidator | shared envelope |
| RestoreDepositCommand | Admin Operations | VALIDATOR_REQUIRED | RestoreDepositCommandValidator | shared envelope |
| RetryRefundCommand | Admin Operations | VALIDATOR_REQUIRED | RetryRefundCommandValidator | shared envelope |
| UnconfirmDepositCommand | Admin Operations | VALIDATOR_REQUIRED | UnconfirmDepositCommandValidator | shared envelope |
| UnpackFulfillmentCommand | Admin Operations | VALIDATOR_REQUIRED | UnpackFulfillmentCommandValidator | shared envelope |
| UnprocessFulfillmentCommand | Admin Operations | VALIDATOR_REQUIRED | UnprocessFulfillmentCommandValidator | shared envelope |
| AssessOrderInventoryRecoveryQuery | Admin Recovery | VALIDATOR_REQUIRED | AssessOrderInventoryRecoveryQueryValidator | CheckoutId |
| AuditOrderInventoryRecoveryQuery | Admin Recovery | VALIDATOR_REQUIRED | AuditOrderInventoryRecoveryQueryValidator | Take range |
| GetOrderSupplyStatusQuery | Admin Supply | VALIDATOR_REQUIRED | GetOrderSupplyStatusQueryValidator | CheckoutId |
| PreviewStorefrontCheckoutQuery | Storefront Checkout | VALIDATOR_REQUIRED | PreviewStorefrontCheckoutQueryValidator | CartId |
| SubmitStorefrontCheckoutCommand | Storefront Checkout | VALIDATOR_REQUIRED | SubmitStorefrontCheckoutCommandValidator | CartId/version/idempotency |
| GetStorefrontCheckoutQuery | Storefront Checkout | VALIDATOR_REQUIRED | GetStorefrontCheckoutQueryValidator | CheckoutId/CartId |
| ListStorefrontPendingPaymentsQuery | Storefront Pending | NO_VALIDATOR_REQUIRED | — | optional body; no required primitives |
| CancelPendingCheckoutCommand | Storefront Pending | VALIDATOR_REQUIRED | CancelPendingCheckoutCommandValidator | CheckoutId/CartId |
| HidePendingPaymentCardCommand | Storefront Pending | VALIDATOR_REQUIRED | HidePendingPaymentCardCommandValidator | CheckoutId/CartId |
| ProjectStorefrontShippingQuery | Storefront Shipping | VALIDATOR_REQUIRED | ProjectStorefrontShippingQueryValidator | CartId |
| SaveStorefrontShippingSelectionCommand | Storefront Shipping | VALIDATOR_REQUIRED | SaveStorefrontShippingSelectionCommandValidator | Body/CartId/version |
| CommitStorefrontShippingCommand | Storefront Shipping | VALIDATOR_REQUIRED | CommitStorefrontShippingCommandValidator | CartId/version/idempotency |
| ListCustomerOrdersQuery | Customer | VALIDATOR_REQUIRED | ListCustomerOrdersQueryValidator | ActorUserId |
| GetCustomerOrderDetailQuery | Customer | VALIDATOR_REQUIRED | GetCustomerOrderDetailQueryValidator | ActorUserId/CheckoutId |
| RetryCustomerUnpaidOrderCommand | Customer | VALIDATOR_REQUIRED | RetryCustomerUnpaidOrderCommandValidator | ActorUserId/CheckoutId |
| ListSellerOrdersQuery | Seller | VALIDATOR_REQUIRED | ListSellerOrdersQueryValidator | SellerPartyId/ActorUserId |
| GetSellerOrderDetailQuery | Seller | VALIDATOR_REQUIRED | GetSellerOrderDetailQueryValidator | SellerPartyId/ActorUserId/SellerOrderId |

## Totals

- Endpoint-reachable requests: **57**
- VALIDATOR_REQUIRED: **54**
- NO_VALIDATOR_REQUIRED: **3**
- Concrete DI-resolvable validators for every VALIDATOR_REQUIRED row (guarded)

## Business rules excluded

Ownership, access/scope, existence, status transitions, inventory/payment/fulfillment/return/settlement/retry/abuse/reservation-cycle policies remain outside FluentValidation.
