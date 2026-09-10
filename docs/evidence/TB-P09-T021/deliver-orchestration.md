# Deliver orchestration

`DeliverConsolidatedPackageAsync`:
1. Package must be Dispatched (idempotent if already Delivered)
2. Call existing deliver core per member
3. Mark package Delivered only when all members Delivered
4. Member already Delivered treated as success (core is idempotent)
5. Feeds existing delivery facts / return clocks via current shipment lifecycle
