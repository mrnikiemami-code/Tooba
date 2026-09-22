# Architecture Guard Audit

`InventoryArchitectureGuardTests` now durably asserts:

- Inventory Endpoints and Host/Inventory endpoint trees are absent.
- `MapInventoryEndpoints` is absent.
- Offer owns `/offers/{offerId}/inventory` and sends its command.
- Offer Application crosses through `Inventory.Contracts.Seller`.
- Inventory is absent from the COMPLETE HTTP manifest.
- machine state declares INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES.

Existing guards continue to enforce contracts, data ownership, deterministic time/id/tracing, Result semantics, idempotency, and physical layout.
