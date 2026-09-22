# Architecture Guard Audit

`NotificationArchitectureGuardTests` upgraded to enforce:
- Endpoints project exists; path↔namespace; Application-only refs (no Host/Infra/DbContext)
- Endpoints use `ISender` + `ApiResponseFactory`; no direct `INotificationDirectory`
- Real MediatR Commands/Queries/Handlers in use-case folders
- Host `NotificationEndpoints` absent; Host Notifications folder absent
- No Host notification projection/business authority
- No TypeForwardedTo / clock-id bypass / silent catch / raw StartActivity / localized exception prose
- Cross-module Contracts-only (Payment/Fulfillment/Returns/Order)

## Verdict
Notification-Architecture-Guards: ENFORCED
