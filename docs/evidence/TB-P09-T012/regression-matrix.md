# Regression matrix — TB-P09-T012

| Area | Result |
| --- | --- |
| Exact selection (`pack_selected` requires selections) | PASS — pack before StartProcessing 400 `order.operation.invalid`; after start, pack qty 1 only that line |
| Lifecycle Start then pack | PASS — T010 sequence tests + runtime B/C |
| Cancel precedence | PASS — cancel → no confirm/reject/fulfillment; POST 400 `order.cancelled.blocks_action`; restore independently evaluated |
| Grid refresh | PASS — `POST /v1/admin/orders/query` after cancel shows `status=Cancelled`; FE `reloadToken` + `onCompleted` unchanged |
| Return display once | PASS — `returnRemainingDisplay=""` + deadline `7 روز پس از تحویل`; `canonicalReturnDisplay` once |
| Mixed bulk | PASS — packed+processing POST pack_selected 400 `fulfillment.bulk.incompatible` |
| T011 kebab fallback | PASS — `lineLifecycleActions` still recovers seller-level pack/unpack |
