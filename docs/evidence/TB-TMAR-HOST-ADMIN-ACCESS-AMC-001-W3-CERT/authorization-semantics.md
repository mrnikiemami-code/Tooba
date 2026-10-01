# authorization-semantics — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT

## Core

| Code | HTTP |
| --- | --- |
| admin.actor.missing | 401 |
| admin.tenant.missing | 503 |
| admin.authorization.unavailable | 503 |
| admin.authorization.denied | 403 |

SemanticException + SemanticError only. PlatformHttpException ZERO on expected-failure path.  
DevActorHeader: session wins; Development-only Guid; Marketplace synthetic tenant preserved.

## Authorizers

| Surface | Unavailable | Deny |
| --- | --- | --- |
| Order | order.authorization.unavailable 503 | order.operation.denied 403 |
| Support | support.authorization.unavailable 503 | admin.authorization.denied 403 |
| Wallet | wallet.authorization.unavailable 503 | admin.authorization.denied 403 |
