# Dispatch final — TB-P09-T022

`DispatchConsolidatedPackageAsync`:

1. Package must be Created (idempotent if already Dispatched/Delivered)
2. Per unfinished member: seed central tracking if missing, then existing dispatch core
3. Package Dispatched only when all active members Dispatched/InTransit/Delivered
4. Direct `dispatch_shipment` on members rejected while locked
5. After central/member dispatch: whole-order cancel blocked (LOCK-OPS-002 / T016)
