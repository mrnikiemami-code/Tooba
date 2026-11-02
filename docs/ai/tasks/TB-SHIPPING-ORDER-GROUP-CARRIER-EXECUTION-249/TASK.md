# TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249

Execute accepted Order shipping groups from admin/seller order UI with server-authoritative carrier capabilities and provenance on accepted carrier quotes.

## Scope

- Persist execution provenance on accepted carrier quote evidence (origin/destination/weight/dimensions).
- Expand committed-order facts + capabilities GET per checkout.
- Harden shipment create against tampering; ignore client package/recipient/account overrides.
- Replace detached carrier shipment panels with per-group execution UI.
- SQL migration `20261102190000_AddAcceptedCarrierExecutionProvenance` (not applied in this worker pass).

## Out of scope

- Live provider acceptance, migrate/deploy, commit/push, Bridge/Chat posts.
