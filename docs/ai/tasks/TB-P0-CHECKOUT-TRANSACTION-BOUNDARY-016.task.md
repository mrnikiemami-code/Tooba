TB-P0-CHECKOUT-TRANSACTION-BOUNDARY-016

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-INVENTORY-PAYMENT-HOLD-GATE-015
Mode: READ_ONLY_ARCHITECTURE_PROOF — STOP_FOR_ARCHITECT
Timebox: 15 minutes hard stop

Purpose

Find the SMALLEST provably safe transaction-boundary design joining Order-owned payability, Inventory Held/TTL protection, and Payment initiation without enabling a money-moving endpoint prematurely. In particular, resolve the two-connection ambient transaction promotion failure disclosed in 015, and the time-of-check/time-of-use gap between unlocking inventory rows and external PSP/wallet operation. Do not propose a provider call inside a long PostgreSQL transaction.

Preconditions

Verify exact main and origin/main at claim; identify implementation 015 SHA 60ea9e8481a8e4dc2fb8afdd4486d8dab7647aa6 and evidence follow-up if present. Read AGENTS.md, locks and relevant Inventory/Order/Payment contracts and real implementation. Respect capability-first architecture and Contracts-only.

Mandatory evidence
Trace real EF DbContext/connection lifetimes, Npgsql ambient enlistment, CheckoutProcessManager transaction, Inventory PaymentHoldGate, Inventory reclaimer, Order cycle writer, Payment initiation persistence, provider calls, and wallet debit. Cite precise file:line.
Distinguish: (a) authorizing a payment attempt on valid reserved stock at one linearization point, (b) keeping a hold during asynchronous PSP operation, and (c) reconciliation for late success. Identify exact guarantees achievable without distributing SQL transactions or freezing network calls under locks.
Verify whether the new gate can be called from Order while maintaining a single physical connection. If not, do not offer a naive multi-DbContext TransactionScope. Explore safe minimum options such as Inventory-owned CAS/short durable attempt lease (requires explicitly owned state), Order-owned serialized state transition with Inventory coordination, or existing payability/reacquisition/late-payment compensation. State pros/cons and lock/state changes; don't invent working ports.
Review gate 015 for subtle issues (connection left open, null TTL, duplicate/missing IDs, txid_current_if_assigned confirmation, missing controlled lock tests), especially whether it proves the intended guarantee at runtime. Report defects if found.
Decide ONE bounded next implementation task, scoped to a coherent safe slice, with at most three essential tests and an explicit refusal condition. If proof cannot be reached inside timebox, mark BLOCKED with the concrete missing evidence, not implementation.
Nonnegotiable safety

READ ONLY. No code, test, doc/governance, migration, configuration, DB, or runtime mutations; no build/test runs; no HTTP mutations. Do not stop/rebuild/restart Host 5088, touch Host or Host.Tests, or alter frontend. No actual order/payment/stock mutations, no external PSP call. No automatic next task. automaticNextImplementationTask=NONE.

Output

FACT / RISK / UNKNOWN / DECISION; precise evidence and chosen minimum safe design. Submit worker report and STOP_FOR_ARCHITECT; explicitly say READ_ONLY — NO_CHANGES.
