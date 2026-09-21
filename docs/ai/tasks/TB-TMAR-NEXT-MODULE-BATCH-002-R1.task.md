PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-002-R1

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-002

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
FAST-SAFE

Track:
REFERENCE_BATCH_REPAIR

Title:
Wallet Notification Boundary + Financial Behavior Preservation Closure

Backend-Only:
YES

0. Architect verdict

Parent PASS is NOT fully accepted yet.

Direct repository verification found one concrete Golden-boundary defect:

Tooba.Wallet.Infrastructure still directly references:

Tooba.Notification.Application

Notification Domain types transitively used in WalletDirectory

Current project:
src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Tooba.Wallet.Infrastructure.csproj

contains a reference to:
Tooba.Notification.Application

and WalletDirectory.cs imports:

Tooba.Notification.Application

Tooba.Notification.Domain

This violates the strict cross-module Contracts boundary required for a reference-complete module.

Parent also made large structural changes in financial modules. Build/tests are green, but this repair must explicitly prove that core Wallet/Payment behavior was preserved and no logic disappeared during splitting/moves.

Therefore:

Wallet-State:
REOPENED_NOTIFICATION_BOUNDARY

Payment-State:
PROVISIONALLY_COMPLETE_WAITING_BEHAVIOR_PARITY

Do NOT start BATCH-003.

Tax/Pricing remain deferred by user and MUST NOT be modified.

1. Objective

Close exactly two things:

Wallet → Notification foreign Application/Domain dependency

Behavior-preservation evidence for the high-risk Wallet/Payment logic moved/split in BATCH-002

No broad redesign.
No frontend.
No Checkout resume.
No Tax/Pricing.

2. Notification boundary repair

Preferred architecture:

Wallet should call a stable Notification public contract/port.

If Notification has no Contracts project yet and Wallet genuinely needs cross-module notification capability:

create a minimal Tooba.Notification.Contracts project

move/extract ONLY the public notification request/port types needed by Wallet and other obvious existing cross-module consumers

do not perform a broad Notification recovery

do not move Notification business implementation into Wallet

Notification implementation wires to the contract as appropriate

update DI atomically

The Wallet production dependency after repair must be:

Wallet.Infrastructure -> Notification.Contracts

NOT:

Notification.Application

Notification.Domain

Notification.Infrastructure

Do not use:

TypeForwardedTo

duplicate wrapper types

static service locator

Host mediator workaround

If a pre-existing suitable public contract already exists elsewhere, reuse it instead of creating duplicate contracts.

3. Preserve notification behavior

Wallet currently emits notifications for important flows including:

gift-card redemption

admin wallet adjustment

wallet payment success

wallet refund credited

These behaviors MUST remain.

Do not silently remove notifications to clean architecture.

Add focused contract/behavior tests proving:

same semantic notification kind/copy

same recipient identity

same idempotency key pattern

same target route intent

for the Wallet flows touched by this repair.

No need for full Notification integration suite unless contract wiring requires it.

4. Financial behavior-preservation audit

Compare parent baseline before BATCH-002:

2a7e171eff38a54ec81a403689393c088cf7ad73

against current implementation.

Create:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-002-R1/behavior-preservation-audit.md

For each high-risk flow, classify:

structural-only change

intentional infrastructure abstraction change

intentional behavior change

accidental behavior change (must repair)

At minimum audit:

Wallet

Get/Create wallet account

gift-card issue

gift-card redeem

gift-card revoke

admin adjustment

order-payment debit

refund credit

wallet quote

idempotency checks

Serializable transaction boundaries

insufficient balance behavior

currency mismatch behavior

notification side effects

Payment

create/initiate payment

payment success/failure transitions

refund path

manual payment flow

webhook verification/inbox idempotency

payment hold settings

pending-payment/admin query behavior

wallet payment gateway call

provider selection/config semantics

outbox/integration event emission

Do not rewrite these flows.
This is parity verification.

5. Focused characterization tests

Because this is financial code and BATCH-002 moved/split thousands of lines, add only high-value tests for behavior at risk.

Required minimum:

Wallet order-payment idempotent replay

Wallet refund-credit idempotent replay

Wallet insufficient balance rejection

Wallet notification emission for one payment flow and one refund/gift/admin flow

Payment → Wallet gateway preserves amount/currency/customer/payment/idempotency semantics

Payment webhook idempotency or duplicate handling

one payment lifecycle success/failure transition test

Prefer unit/component tests around existing ports/domain.
Do NOT create huge Host integration suites.

If existing tests already cover an item, cite/reuse them rather than duplicate.

6. Physical/namespace state

Preserve BATCH-002 physical structure.

Re-verify:

root dump = 0

namespace alignment = 0 mismatches

TypeForwardedTo = 0

If creating Notification.Contracts, it must itself have correct physical folder + namespace structure from day one.

7. Result / CQRS / endpoints

Do not convert Wallet/Payment to MediatR in this repair.

Current endpoint/cqrs classification may remain:

Wallet Endpoint N/A / Host transport

Payment Endpoint N/A / Host transport

Directory-style retained

This task is not an HTTP redesign.

8. Architecture guards

Strengthen Wallet guard.

Must fail if Wallet production references:

Notification.Application

Notification.Domain

Notification.Infrastructure

Allow only:

Notification.Contracts

Payment guard remains:

Wallet.Contracts only

If Notification.Contracts is introduced, add a focused guard ensuring its public types are in Tooba.Notification.Contracts... namespace and no implementation dependencies leak into it.

9. Behavior safety rules

Absolutely do not:

delete side effects merely to satisfy boundaries

remove idempotency checks

weaken transaction isolation

change payment/refund amount rounding

change currency normalization

change provider selection defaults

change manual-payment timing semantics

change checkout point-of-no-return behavior

swallow exceptions

add retries/sleeps

convert unexpected exceptions into Result

widen baselines

Any behavior change not explicitly required above must be treated as a defect and reverted/preserved.

10. Validation — Fast-Safe

Required:

Wallet focused tests

Payment focused tests

Notification contract tests/build if new project added

focused Host compile/tests only if constructor/DI wiring changes

one final:
dotnet build src/backend/Tooba.slnx

Conditional:

Notification tests only if Notification implementation changed

no broad Host suite

no unrelated module suites

no frontend

no Tax/Pricing

11. Evidence

Write:

recovery-start.md

notification-boundary-audit.md

behavior-preservation-audit.md

focused-financial-tests.md

recovery-sot.md

under:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-002-R1/

behavior-preservation-audit.md must explicitly reference baseline 2a7e171... and list whether each audited flow preserved logic.

12. Success state

Only on true closure:

Wallet-State:
COMPLETE_REFERENCE_PATTERN

Wallet-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Wallet-Notification-Boundary:
CONTRACTS_ONLY

Payment-State:
COMPLETE_REFERENCE_PATTERN

Payment-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Financial-Behavior-Preservation:
VERIFIED

Foundation-State:
RESULT_PATTERN_FOUNDATION_COMPLETE

Offer-State:
COMPLETE_REFERENCE_PATTERN

Inventory-State:
COMPLETE_REFERENCE_PATTERN

Promotion-State:
COMPLETE_REFERENCE_PATTERN

Tax-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

Pricing-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend-Production-Changes:
NONE

Module-Recovery-State:
NEXT_REFERENCE_BATCH_002_COMPLETE

Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-003

13. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Notification-Boundary-Audit
Notification-Contract-Repair
Wallet-Notification-Behavior
Behavior-Preservation-Audit
Wallet-Financial-Parity
Payment-Financial-Parity
Focused-Financial-Tests
Wallet-Guards
Payment-Guards
Notification-Guards
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Wallet-State
Wallet-Physical-State
Wallet-Notification-Boundary
Payment-State
Payment-Physical-State
Financial-Behavior-Preservation
Foundation-State
Offer-State
Inventory-State
Promotion-State
Tax-State
Pricing-State
Checkout-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

After Result:
STOP.
No polling.
No next task fetch.
No Worker IDLE.

END_TOOBA_REPAIR_TASK