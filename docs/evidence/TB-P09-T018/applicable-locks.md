# Applicable Locks — TB-P09-T018

Read `docs/architecture/TOOBA-LOCKS.md`.

Applied:
- LOCK-QTY-001..005 — decimal quantity, product policy, one subsystem, snapshots
- LOCK-I18N-001 — dynamic language registry
- LOCK-OPS-001 — exact line+qty, sequence, cancelled precedence, capability-driven
- LOCK-OPS-002..005 — whole-order cancel until first dispatch; no hard-delete; inventory/payment/settlement; restore gates
- LOCK-OPS-006 — work queue reuses Order Detail capabilities (added this task)
- LOCK-OPS-007 — no cross-seller Shipment (added this task)
- LOCK-OPS-008 — queue bulk requires shared valid capability (added this task)
