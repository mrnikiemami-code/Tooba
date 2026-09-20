# Checkout migration plan

| Stage | Name | Prerequisites | Changes | Tests | Rollback | Risk | Exit |
|---|---|---|---|---|---|---|---|
| 0 | Document/guard | this design | locks ARCH-CHECKOUT-*; no behavior change | architecture guards | revert docs | low | locks merged |
| 1 | Workflow state + idempotency primitives | Stage 0 | durable process state in Order; explicit step log; no TX removal yet | characterization | feature flag off | med | state machine persisted parallel to current TX |
| 2 | Contract one participant | Stage 1 | e.g. Promotion or Cart port behind Contracts in-process | contract tests | revert adapter | med | edge shrink |
| 3 | Outbox/Inbox for workflow msgs | Stage 1–2 | command messages via outbox between steps | inbox tests | dual-write | med | at-least-once proven |
| 4 | Activate PM in monolith | Stage 3 | orchestrate reserve/order/convert via PM; keep single DB | checkout foundation tests | flag | high | behavior parity |
| 5 | Remove shared TransactionScope | Stage 4 parity | delete ambient TX; local ACID only | chaos/partial failure | emergency re-enable TX (short) | high | baseline TX file shrink |
| 6 | Extract first participant | Stage 5 | e.g. Inventory service | contract+e2e | redeploy monolith adapter | high | separate DB OK |
