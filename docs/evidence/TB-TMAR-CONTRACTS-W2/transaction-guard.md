# Transaction guard — TB-TMAR-CONTRACTS-W2

## ARCH-TX-001
Canonicalized in `docs/architecture/TMAR-architecture-locks.md` and migration notes.

## Guard
Baseline: `tmar-cross-context-transaction-files.json`
Test: `Cross_context_TransactionScope_orchestrators_do_not_expand_beyond_baseline`

Current allowed TransactionScope orchestrator:
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutDirectory.cs`

NEW TransactionScope sites fail CI. No global MediatR TransactionBehavior.
