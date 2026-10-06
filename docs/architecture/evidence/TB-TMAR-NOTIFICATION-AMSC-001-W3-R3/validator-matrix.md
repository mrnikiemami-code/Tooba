# TB-TMAR-NOTIFICATION-AMSC-001-W3-R3 — validator-matrix

Exhaustive request → handler → validator matrix re-verified against the current tree at HEAD
`028ef769`. Exactly 10 endpoint-reachable MediatR requests (all module `IRequest` types), 10 real
`IRequestHandler` implementations, `ISender` dispatch only, MediatR 12.5.0 via
`AddToobaCqrsFoundation`.

| # | Request (flattened home) | Validator | Classification |
| --- | --- | --- | --- |
| 1 | `Customer/Queries/ListCustomerNotificationsQuery.cs` | `Customer/Queries/ListCustomerNotificationsQueryValidator.cs` | VALIDATOR_REQUIRED ✓ (untrusted `Skip`/`Take` paging: `Take` 1..100 → `notification.validation.customer_take_out_of_range`, `Skip` >= 0 → `notification.validation.customer_skip_negative`) |
| 2 | `Customer/Queries/GetCustomerUnreadNotificationCountQuery.cs` | — | NO_VALIDATOR_REQUIRED — auth-scoped: `ActorUserId` derives only from `INotificationCustomerAuthorizer`; no untrusted transport input (Fulfillment/Settlement/Media precedent) |
| 3 | `Customer/Commands/MarkCustomerNotificationReadCommand.cs` | `Customer/Commands/MarkCustomerNotificationReadCommandValidator.cs` | VALIDATOR_REQUIRED ✓ (untrusted route `NotificationId` != Guid.Empty → `notification.validation.notification_id_required`) |
| 4 | `Customer/Commands/MarkAllCustomerNotificationsReadCommand.cs` | — | NO_VALIDATOR_REQUIRED — auth-scoped: `ActorUserId` authorizer-derived only |
| 5 | `Customer/Commands/DismissCustomerNotificationCommand.cs` | `Customer/Commands/DismissCustomerNotificationCommandValidator.cs` | VALIDATOR_REQUIRED ✓ (untrusted route `NotificationId` != Guid.Empty) |
| 6 | `Seller/Queries/ListSellerNotificationsQuery.cs` | `Seller/Queries/ListSellerNotificationsQueryValidator.cs` | VALIDATOR_REQUIRED ✓ (untrusted `Skip`/`Take` → `notification.validation.seller_take_out_of_range` / `seller_skip_negative`; `SellerPartyId` authorizer-derived and never policed) |
| 7 | `Seller/Queries/GetSellerUnreadNotificationCountQuery.cs` | — | NO_VALIDATOR_REQUIRED — auth-scoped: `SellerPartyId` from `INotificationSellerAuthorizer.RequireAuthorizedAsync` |
| 8 | `Seller/Commands/MarkSellerNotificationReadCommand.cs` | `Seller/Commands/MarkSellerNotificationReadCommandValidator.cs` | VALIDATOR_REQUIRED ✓ (untrusted route `NotificationId` != Guid.Empty) |
| 9 | `Seller/Commands/MarkAllSellerNotificationsReadCommand.cs` | — | NO_VALIDATOR_REQUIRED — auth-scoped: `SellerPartyId` authorizer-derived only |
| 10 | `Seller/Commands/DismissSellerNotificationCommand.cs` | `Seller/Commands/DismissSellerNotificationCommandValidator.cs` | VALIDATOR_REQUIRED ✓ (untrusted route `NotificationId` != Guid.Empty) |

## Coverage totals

- `validatorRequiredCount = 6` — all 6 present, colocated on the request axis, discovered through the
  existing `AddToobaCqrsFoundation`/`AddValidatorsFromAssembly` pipeline (no manual endpoint/handler
  invocation).
- `noValidatorRequiredCount = 4` — durable auth-scoped reasons recorded above; no ceremonial
  validators created.
- Missing validators: **ZERO**. Gap: **ZERO**. `validatorCoverageState = EXHAUSTIVE`.
- Codes: `Application/Validators/NotificationValidationCodes.cs` owns 5 `notification.validation.*`
  transport codes mapped through the foundation `validation.failed` descriptor; deliberately NOT
  catalog descriptors (`NotificationErrorCatalogContributor` contains no `NotificationValidationCodes`
  — W1 guard `Validation_codes_are_not_registered_as_error_catalog_descriptors`).
- Behavior tests re-prove reject/accept semantics post-R2 (`Tooba.Notification.Tests` 18/18).
- Durable guards: `NotificationModuleAmsc001W1MigrateGuardTests.Validators_emit_transport_codes_and_are_exhaustively_present`
  (repointed to the flat axes), `NotificationModuleAmsc001W3R2StructureRepairGuardTests.All_ten_requests_and_six_validators_are_colocated_on_their_axis`.
