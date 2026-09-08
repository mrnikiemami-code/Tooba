# Grid-Refresh-Regression — TB-P09-T012

T011 `reloadToken` + `createOrderColumns(onCompleted)` unchanged.

Runtime G: after cancel, `POST /v1/admin/orders/query` row `status=Cancelled` without browser reload.
