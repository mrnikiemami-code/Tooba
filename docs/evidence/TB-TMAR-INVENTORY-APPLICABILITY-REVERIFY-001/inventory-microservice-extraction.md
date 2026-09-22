# Inventory Microservice Extraction

Today:

`foreign modules -> Inventory.Contracts -> Inventory Application/Infrastructure -> inventory schema`

Future:

`foreign services -> API/message adapters preserving Contracts semantics -> Inventory service Application/Infrastructure -> inventory database`

Synchronous surfaces are Seller, Checkout, Orders, Availability, Fulfillment, and Returns contracts. Async surfaces are Inventory integration events and inbox/outbox messaging. Inventory exclusively owns its schema and migrations. Host supplies composition only and contains no Inventory business endpoint or database authority.

Extraction therefore replaces in-process adapters with transport adapters; use cases, invariants, Result semantics, and persistence ownership require no rewrite.

Verdict: `READY_WITHOUT_REWRITE`.
