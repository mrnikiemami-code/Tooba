# Foundation adoption scan — BATCH-005

## Cart
- IClock / IIdGenerator: adopted in CartDirectory.
- Domain factories: explicit Guid parameters; no Guid.NewGuid/UuidV7.New/UtcNow in Cart production.
- No hidden `?? new SystemUtcClock()` / `?? new QuantityNormalizer()` in CartDirectory.

## Settlement
- IClock / IIdGenerator: adopted in SettlementDirectory.
- Domain factories: explicit Guid ids; BeginAttempt takes attemptId.
- No Guid.NewGuid/UtcNow/UuidV7.New in Settlement production sources after patch.

## Host residual
- Host SettlementEndpoints still catch InvalidOperationException → `ex.Message` detail (foundation/Result pattern incomplete at HTTP edge).
