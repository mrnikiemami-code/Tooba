PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R4-R1
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_RECOVERY_SUPPLY_HARDENING
Title: Fix Clock + Exception Swallowing in Order Inventory Recovery
Backend-Only: YES

Architect verdict

R4 Host-removal work is ACCEPTED.

R4 overall PASS is REJECTED due to two defects directly verified in:

Tooba.Order.Application/Admin/InventoryRecovery/Services/OrderInventoryRecoveryService.cs

Direct DateTimeOffset.UtcNow usage remains.

RecoverAsync catches broad Exception, performs best-effort rollback with silent catch { }, then converts unknown failures to inventory.recovery.insufficient.

This violates canonical clock and failure semantics.

Reference

Use:

canonical IClock

Offer-style typed Result / stable failure semantics

unknown exceptions must propagate

no silent catch/workaround

Exact repair goal

Repair ONLY Inventory Recovery error/clock handling introduced by R4.

Do NOT:

move any new Host surface

touch Storefront

touch Order detail

change OrdersGrid

redesign Inventory/Fulfillment

start R5

start Checkout W6

Clock

Replace all direct current-time calls in R4 InventoryRecovery/Supply touched paths with canonical IClock.

At minimum inspect:

OrderInventoryRecoveryService

OrderSupplyService

R4-created/modified related application code

No DateTimeOffset.UtcNow
No DateTime.UtcNow

Recovery failure semantics

In RecoverAsync:

Expected inventory/recovery failures must use typed/stable failures or Result codes.

Unknown exceptions must propagate.

Do not collapse arbitrary exceptions into inventory.recovery.insufficient.

Rollback/release behavior must remain safe.

If rollback itself fails:

do not silently swallow it

preserve the original failure while making rollback failure observable through the existing canonical mechanism available in the repo (typed aggregate failure/logging/telemetry as appropriate)

do not invent polling/retry loops

do not hide failure with empty catch

No catch { }.
No broad catch that converts unknown failures to a business code.

Preserve

R4 Host removals

R4 endpoints/CQRS

Recovery classes/outcomes

Supply behavior

R3B typed Admin Operations

Contracts-only foreign boundaries

Checkout W5 pause

Guards

Add/strengthen durable Order guards for R4 paths:

no DateTimeOffset.UtcNow

no DateTime.UtcNow

no empty catch

no broad catch (Exception) used to map unknown failure to SemanticError

unknown exceptions propagate

Host R4 files remain absent

Validation

Run:

Order.Tests

Recovery/Supply focused tests

Order architecture guards

affected Inventory tests if touched

focused Host regression tests

dotnet build src/backend/Tooba.slnx

Recovery SoT

On PASS:

record R4-R1

Order remains INCOMPLETE_REFERENCE_REPAIR

nextTask = TB-TMAR-ORDER-GOLDEN-001-R5

do NOT start R5

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

PASS criteria

PASS only if:

R4 InventoryRecovery/Supply touched Application paths use IClock.

No unknown exception is converted to inventory.recovery.insufficient.

No silent catch remains.

Expected failures remain stable/typed.

R4 Host removal remains intact.

Tests/build pass.

Recovery SoT synced.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R4-R1
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R4
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Clock-State:
Unknown-Exception-State:
Rollback-Failure-State:
Typed-Failure-State:
R4-Host-Removal-Preserved:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Recovery-State:
Recovery-Next-Task:
Residual-Defects:
Order-Overall-State:
Checkout-State:
Git:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start R5.
Do not migrate Storefront.
Do not start Checkout W6.
Do not poll.

END_TOOBA_TASK