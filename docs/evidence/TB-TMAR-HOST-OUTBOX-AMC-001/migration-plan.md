# migration-plan — TB-TMAR-HOST-OUTBOX-AMC-001

## Recommended: 2 waves (≤20 min each)

### W1 — `TB-TMAR-HOST-OUTBOX-AMC-001-W1` (~12–15 min)

MIGRATE + bounded hygiene:

1. Namespace → `Tooba.Host.Outbox`; Program/Transport/tests usings.
2. Split to one top-level type per file (6+ files).
3. Cancellation: rethrow `OperationCanceledException` when token requested in target/module/message catches.
4. Add `OutboxHostOptionsValidator` + ValidateOnStart (positive ints).
5. Replace Persian FromPollTarget exception with machine English operator text (no localization ceremony).
6. Add `HostOutboxAmcW1GuardTests` + focused OCE/options tests.

No Messaging/Persistence redesign. No StoreContext move.

### W2-CERT — `TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT` (~8–10 min)

CERTIFY_ONLY: durable cert guard + evidence; production change ZERO if W1 clean.

## Rejected

- Direct CERT only — blocked by namespace/cohesion/cancellation/options debt.
- Separate W2 behavioral-only wave — cancellation+validator fit W1 with structure (still ≤15).

## automaticNext

NONE after Analyze — user/Architect review gate.
