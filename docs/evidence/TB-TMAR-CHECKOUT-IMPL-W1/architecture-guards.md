# Architecture guards

- Process state Order-owned only
- ARCH-CHECKOUT-001…005 remain design locks (no Saga activation)
- Shared TransactionScope still present
- No authoritative in-memory workflow store
