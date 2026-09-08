# Grid refresh

`ServerGridPage` accepts `reloadToken`. `createOrderColumns(onCompleted)` wires `AdminOrderOperationsMenu.onCompleted`. `AdminOrdersScreen` increments the token after a successful kebab op so the grid query adapter re-runs without a browser reload.

Source markers: `function createOrderColumns(onOperationCompleted?)`, `onCompleted={onOperationCompleted}`, `reloadToken={reloadToken}`.

Runtime: after POST cancel on multi-seller checkout `01a07ec5-81ad-7000-b77d-4f60962d6d50`, `POST /v1/admin/orders/query` returned `paymentState=Cancelled` / `status=Cancelled` for that row. After confirm on the single-seller checkout, a later query returned `Paid`.
