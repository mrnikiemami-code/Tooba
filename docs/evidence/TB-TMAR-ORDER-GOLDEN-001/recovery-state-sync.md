# Recovery state synchronization

The prior Golden Wave is recorded as `COMPLETE + USER_ACCEPTED`. Order is not added to the COMPLETE module manifest.

Current state:

- Order: `INCOMPLETE_REFERENCE_REPAIR`
- nextTask: `TB-TMAR-ORDER-GOLDEN-001-R1`
- gate: `ORDER_GOLDEN_REPAIR_REQUIRED`
- Checkout: `PAUSED_AT_SAFE_W5_CHECKPOINT`
- Frontend: frozen

Synchronized: machine-readable current state, Master Recovery, Architect Bootstrap, and task recovery SoT.
