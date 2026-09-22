# Wallet Idempotency Audit

Precedence preserved:

1. body IdempotencyKey
2. Idempotency-Key header
3. generated fallback via injected IIdGenerator.NewId().ToString("N")

Guid.NewGuid() removed from Wallet HTTP flow / production Endpoints.
