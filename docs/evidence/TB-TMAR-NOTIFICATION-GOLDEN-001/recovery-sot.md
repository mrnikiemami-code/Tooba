# Recovery Source of Truth — TB-TMAR-NOTIFICATION-GOLDEN-001

## Baseline
- HEAD at claim: `b71cebb2eda0cc28fe9848d568d007d887e0ff30` == origin/main

## End state
- `Tooba.Notification.Endpoints` present and mapped
- Host Notifications HTTP ownership removed
- Application CQRS MediatR 12.5.0 handlers for all 10 HTTP use cases
- Host = authorizer adapters + composition only
- Notification.Tests: 18 passed
- `dotnet build src/backend/Tooba.slnx`: 0 errors

## Protected siblings
- Cart / Settlement / Fulfillment / Returns: COMPLETE (untouched functionally)
- Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
- Tax / Pricing / frontend: UNTOUCHED

## Untracked leftovers preserved (not committed)
- CART/Fulfillment leftover bridge-result files
- stashes / user files
