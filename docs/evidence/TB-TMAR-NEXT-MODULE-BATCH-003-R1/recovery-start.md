# Recovery start — TB-TMAR-NEXT-MODULE-BATCH-003-R1

- Claim: c9bbe36f-a111-440e-88af-177ebaa94b33
- Channel: tooba-main
- Worker: tooba-worker-01
- Bridge: http://127.0.0.1:17321
- Baseline tip: 435f4c8b8253f6bb7172ca02ee7f996d569d7b89 (HEAD==origin/main)
- Parent: TB-TMAR-NEXT-MODULE-BATCH-003 PASS reopened as R1
- Scope: Notification → Order implementation-layer boundary closure ONLY
- Explicit non-scope: BATCH-004; Tax/Pricing; Checkout; frontend; Support redesign; Notification behavior redesign

## Pre-repair defect (Architect-verified)

- `Tooba.Notification.Infrastructure` ProjectReference → `Tooba.Order.Application`
- `NotificationProjector.cs` `using Tooba.Order.Application`
- Architecture guard rejects Payment/Fulfillment/Returns Application/Domain/Infrastructure but does NOT reject Order.Application
- Parent evidence incorrectly treated Order.Application recipient reader as allowed

## Notification-State at start

REOPENED_ORDER_APPLICATION_BOUNDARY

## Support-State at start

PROVISIONALLY_ACCEPTED (preserve; do not reopen)

## Strategy

1. Extract `IOrderNotificationReader` + recipient snapshots into `Tooba.Order.Contracts/Notifications`
2. Retarget Notification.Infrastructure → Order.Contracts only
3. Update Order.Infrastructure bridge + DI registration usings
4. Strengthen NotificationArchitectureGuardTests (+ Order.Contracts layout guard)
5. Focused projector parity tests; evidence; SoT; build
