# Dispatch orchestration

`DispatchConsolidatedPackageAsync`:
1. Package must be Created (idempotent if already Dispatched/Delivered)
2. For each unfinished member: optionally assign central tracking if member lacks tracking
3. Call existing dispatch core per member (not SQL status hack)
4. Mark package Dispatched only when all active members are Dispatched/InTransit/Delivered
5. Retry-safe: already-dispatched members skipped
