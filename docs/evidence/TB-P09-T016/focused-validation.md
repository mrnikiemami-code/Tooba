# Focused validation — TB-P09-T016

## Backend

`dotnet test Tooba.Host.Tests` filter WholeOrderCancelUntilDispatch + SellerOrderCancellationGuard + AdminOrderCancelPrecedence + AdminOrderOperationsTests + AdminOrderCorrectiveActions + QuantityDecimalRegression + FulfillmentLineQuantityOps: **70 passed**.

## Recovery

`node --test docs/ai/recovery-staleness.guard.test.mjs`: **3 passed**.

## Frontend

`admin-order-operations.test.ts`: all pass including one-cancel, confirm Dialog, reloadToken, `order.cancel.forbidden` FA.

## Git

`git diff --check` clean on task-owned files.
