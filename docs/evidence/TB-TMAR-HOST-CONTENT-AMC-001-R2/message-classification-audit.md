# Message classification audit

Searched Content.Application + Content.Infrastructure (excl. Migrations):

- IsKnownCode(ex.Message) = ZERO (helper removed)
- SemanticError(ex.Message) = ZERO
- .Message.Contains / StartsWith = ZERO
- PlatformHttpException in App/Infra = ZERO
- Media readiness InvalidOperationException("media.asset.missing") = ZERO
