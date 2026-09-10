# Performance — TB-P09-T022

- Package projection loads by checkout id (no unrelated order hydration)
- Member seller/provider labels from existing detail projection
- Customer tracking preference selected once per fulfillments response (no per-member package re-query loop)
- Orchestration reuses sequential existing dispatch/deliver cores
- No N+1 repair required unless runtime/focused tests show regression
