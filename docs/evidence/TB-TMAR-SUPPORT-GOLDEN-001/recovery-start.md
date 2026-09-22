# Recovery Start — TB-TMAR-SUPPORT-GOLDEN-001

- **Task-ID:** TB-TMAR-SUPPORT-GOLDEN-001
- **Channel:** tooba-main
- **Worker:** tooba-worker-01
- **Baseline HEAD:** a20ef51 (== origin/main at claim)
- **Mode:** FAST-SAFE
- **Track:** SUPPORT_GOLDEN_CLOSURE
- **Backend-Only:** YES
- **Parent:** TB-TMAR-NOTIFICATION-GOLDEN-001 (accepted)

## Intent

Move Support HTTP ownership from Host into `Tooba.Support.Endpoints` with MediatR CQRS Application handlers, Result/SemanticError, authorizer interfaces, Host reduced to composition/security/bootstrap only.

## Protected state (do not disturb)

- Cart / Settlement / Fulfillment / Returns / Notification: COMPLETE
- Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
- Tax / Pricing / frontend: untouched
- Do not start Wallet / Payment / Promotion

## Git constraints

- No reset / clean / force push / broad git add
- Preserve stashes and user files
- Do not commit / push / POST Bridge from this worker
- Do not commit unrelated CART/Fulfillment leftover bridge-result files
