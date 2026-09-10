# Applicable locks — TB-P09-T022

Applied from `docs/architecture/TOOBA-LOCKS.md` (final gate; no new locks):

- LOCK-OPS-013 … LOCK-OPS-021 — Consolidated Package orchestration, multi-seller visibility, eligibility, membership, rebuild, member lock, central dispatch/deliver, cancel interaction, customer tracking preference
- LOCK-OPS-001…012 — quantity-aware ops, no cross-seller Shipment ownership, cancel/restore gates
- LOCK-RET-* unchanged — Return/Refund remain outside package ownership
- Guest/customer security proof from T021-R1 preserved (GuestSecret ownership; GuestActor alone does not unlock)
