# Recovery SoT — TB-TMAR-NEXT-MODULE-BATCH-003-R1

Claim: `c9bbe36f-a111-440e-88af-177ebaa94b33`
Channel: `tooba-main`
Worker: `tooba-worker-01`
Baseline: `435f4c8b8253f6bb7172ca02ee7f996d569d7b89`

## Success states

- Notification-State: COMPLETE_REFERENCE_PATTERN
- Notification-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
- Notification-Public-Boundary: CONTRACTS_ONLY
- Notification-Order-Boundary: CONTRACTS_ONLY
- Support-State: COMPLETE_REFERENCE_PATTERN
- Support-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
- Support-Notification-Boundary: CONTRACTS_ONLY
- Behavior-Preservation: VERIFIED
- Batch-State: COMPLETE
- Module-Recovery-State: NEXT_REFERENCE_BATCH_003_COMPLETE
- Next-Recommended-Task: TB-TMAR-NEXT-MODULE-BATCH-004

## Repair

Extracted Order notification recipient reader contract into `Tooba.Order.Contracts/Notifications`. Notification.Infrastructure → Order.Contracts only. Guards reject Order.Application/Domain/Infrastructure.

## Validation

- Notification.Tests: 13 passed (architecture + behavior + projector parity)
- Order.Infrastructure build: succeeded
- `dotnet build src/backend/Tooba.slnx`: succeeded

## Untouched

Tax/Pricing, Checkout, frontend, BATCH-004, Support redesign, Payment/Fulfillment/Returns suites.
