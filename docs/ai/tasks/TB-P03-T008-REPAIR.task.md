Tooba — TB-P03-T008 — REPAIR — Durable Payment→Order Projection

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P03-T008
Repair: YES
Phase: P03 — Commerce Core
Type: REPAIR / Durable Payment→Order Projection
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Depends-On: TB-P03-T008
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

Objective

Close the crash window where Payment can be Succeeded while Order stays unpaid forever.

Required path:

Payment local persist + Outbox
→ payment.succeeded.v1
→ Order-owned consumer
→ Paid projection

Hard rules:

Do not redesign Payment, add a real PSP, or start T009 / P03 Gate.
Do not mark T008 accepted.
Payment remains SoT for provider verification; Order is a recoverable projection.
Durable event delivery/replay must be enough by itself.
Duplicate payment.succeeded.v1 must be durable-idempotent (Order-owned inbox).
Consumer must re-check amount and currency against Order snapshot before Paid.

END_TOOBA_CURSOR_TASK_V1
