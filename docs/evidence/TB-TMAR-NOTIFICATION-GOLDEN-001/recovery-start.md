# Recovery Start — TB-TMAR-NOTIFICATION-GOLDEN-001

- **Task-ID:** TB-TMAR-NOTIFICATION-GOLDEN-001
- **Claim:** c533a0bd-be12-4284-ad7d-a8c7ce90357c
- **Channel:** tooba-main
- **Worker:** tooba-worker-01
- **Bridge:** http://127.0.0.1:17321
- **Baseline HEAD:** b71cebb2eda0cc28fe9848d568d007d887e0ff30 (== origin/main at claim)
- **Mode:** FAST-SAFE
- **Track:** NOTIFICATION_GOLDEN_CLOSURE
- **Backend-Only:** YES
- **Parent:** TB-TMAR-RETURNS-GOLDEN-001 (accepted)

## Intent

Move Notification HTTP ownership from Host into `Tooba.Notification.Endpoints` with MediatR CQRS Application handlers, Host reduced to composition/security adapters only.

## Protected state (do not disturb)

- Cart / Settlement / Fulfillment / Returns: COMPLETE
- Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
- Tax / Pricing / frontend: untouched
- Do not start Support / Wallet / Payment / Promotion

## Git constraints

- No reset / clean / force push / broad git add
- Preserve stashes and user files
- Do not commit unrelated CART/Fulfillment leftover bridge-result files
