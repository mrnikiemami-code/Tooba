# behavior-parity — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2

## Order / Support / Wallet authorizers

| Branch | Semantics |
| --- | --- |
| Panel gate | `IAdminPanelAccess` unchanged |
| Allow | return |
| Unavailable | same stable codes / 503 via SemanticException |
| Deny | Order → `order.operation.denied` 403; Support/Wallet → `admin.authorization.denied` 403 |
| Permission CallContext / tenant fallback | unchanged |

No `ex.Message` classification. Presentation titles come from catalog + resource sets, not Host throw strings.

## Core W1

AdminPanelAccess / HostAdminPanelAccess / DevActorHeader / Marketplace synthetic tenant: PRESERVED.
