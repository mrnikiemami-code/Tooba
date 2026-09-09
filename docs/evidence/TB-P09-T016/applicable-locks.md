# Applicable locks — TB-P09-T016

Read: `docs/architecture/TOOBA-LOCKS.md`

- LOCK-QTY-001..005 — decimal `numeric(18,6)`, product unit/places/step, offer min/max, no integer truncation on 1.25 / 0.50 / 0.75
- LOCK-I18N-001 — dynamic Language Registry (untouched)
- LOCK-ROUND-001..003 — one GlobalRoundingMode; historical snapshots stay
- LOCK-INVOICE-001..005 — header aggregates; LineCount vs TotalQuantity unchanged
- LOCK-OPS-001 — exact line+quantity, cancelled precedence, capability-driven actions (not duplicated)
- LOCK-OPS-002 — whole-order cancel until first dispatched quantity
- LOCK-OPS-003 — cancel cleanup without hard-delete
- LOCK-OPS-004 — inventory / payment / settlement on cancel
- LOCK-OPS-005 — restore after whole-order cancel
