# R2 Source of Truth — TB-P09-T020-R2

Authoritative current reservation for forward-fulfillable quantity: **Inventory Held reservation referenced by OrderLine after restore**.

Active Fulfillment items must resolve to that same id before pack/ship/dispatch.

Released/Consumed ids remain historical audit only and must never be consumed/resurrected.

Ownership: Inventory lifecycle; Order stores line reference; Fulfillment rebinds via module contract from Order handoff (no cross-schema SQL).
