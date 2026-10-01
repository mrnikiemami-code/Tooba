# error-catalog — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2

| Code | HTTP | Classification | Owner |
| --- | --- | --- | --- |
| admin.actor.missing | 401 | Forbidden | Foundation (W1) |
| admin.tenant.missing | 503 | Platform | Foundation (W1) |
| admin.authorization.unavailable | 503 | Platform | Foundation (W1) |
| admin.authorization.denied | 403 | Forbidden | Foundation (W1) — not duplicated in Support/Wallet |
| admin.dev.unavailable | 404 | NotFound | Foundation (W1) |
| order.authorization.unavailable | 503 | Platform | OrderErrorCatalogContributor |
| order.operation.denied | 403 | Forbidden | OrderErrorCatalogContributor (registered exactly once in W2) |
| support.authorization.unavailable | 503 | Platform | SupportErrorCatalogContributor (existing) |
| wallet.authorization.unavailable | 503 | Platform | WalletErrorCatalogContributor (existing) |

Descriptor duplication for these codes: ZERO.
