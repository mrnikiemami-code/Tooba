TB-P0-PAYMENT-IDEMPOTENCY-METHOD-BINDING-020

Task-ID: TB-P0-PAYMENT-IDEMPOTENCY-METHOD-BINDING-020
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-START-PAYMENT-COMPOSER-019
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 4b605be1cd713a9a33b8618c3c8d08479bdd54e4
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD; automaticNextImplementationTask=NONE

Mandatory preflight
Verify HEAD == origin/main == Expected-Base-SHA; if drift or conflict, STOP_FOR_ARCHITECT without rebase/stash/force-push. Read AGENTS.md, BRIDGE-WAKE-V1 protocol, recovery files and the 012R/018/019 evidence. Host CLOSED, no build/restart/stop/deploy of Host or Host.Tests.

Mission — ONE bounded defect, not endpoint publication
Audit and, only if safely possible, fix Payment-owned persisted idempotency replay so reusing the SAME payment idempotency key with DIFFERENT selected payment methods (wallet versus manual versus explicit gateway, including default-gateway selector resolving to a concrete provider) can never silently return a previous method's payment as a new-method success or initiate a second charge. The payment module owns method resolution, actor policy, payment/attempt persistence and replay; keep authority there rather than infer resolved gateway in Order.

Correctness gates

Compare the persisted canonical selected provider/method, checkout/cart owner identity and idempotency key BEFORE any replay success. Preserve legitimate exact-method retry and lost-response replay, with no new PSP call, wallet debit, payment record or attempt.
If a mismatch is detected, fail closed with an ALREADY DECLARED semantically exact Payment-owned typed error if available. Do not repurpose a code or return a generic success/500. If the only correct solution requires a new Payment error code pinned by Host.Tests or schema/migration/new Host registration, STOP BLOCKED with the exact file-level change proposal. No edits to certified Host.Tests guards.
Do not change the canonical Payment provider eligibility, wallet/manual/online routing, timeout rules, configured hold durations, Order commit or Inventory extension. Avoid changing direct checkout Payment endpoint in this slice unless necessary to enforce existing idempotency rule without broad effects.
Critically verify how a generic catalog selector 'gateway' is resolved relative to persisted concrete provider; if canonical comparison needs unavailable information or cannot distinguish selector from provider safely, STOP rather than deny valid same-method replays.
Cover concurrent duplicate initiation only to the extent existing persistence/idempotency guarantees can be proven; record unresolved races honestly. No external provider network access in tests.

Absolute restrictions
No modifications under src/backend/Host/**, including Program.cs and Host.Tests; no Host binary build, process stop/restart/deploy or 5088 contact. No frontend or new endpoint. No DB/migration/schema/config changes and no mutation of tooba_alpha. No PSP/wallet real calls. No existing payment actor-policy weakening, no error-catalog repurposing, no broad AMSC or broad test suite. Limit implementation to Payment module and Payment module focused tests, <=3 production files preferred. Do not stage unrelated dirty paths.

Verification
Run only minimum focused Payment module tests for: exact-method repeat returns existing payment/attempt without gateway invocation, changed-method repeat rejected BEFORE money movement, unauthorized replay rejected, default gateway resolution handled correctly, existing wallet/manual paths unchanged. Avoid repeated builds/tests; STOP if no safe typed error or trustworthy repository-level test seam.

Result protocol
Commit only task-scoped changes and compact evidence under docs/architecture/evidence/TB-P0-PAYMENT-IDEMPOTENCY-METHOD-BINDING-020/, task artifact docs/ai/tasks/TB-P0-PAYMENT-IDEMPOTENCY-METHOD-BINDING-020.task.md. Return BEGIN_TOOBA_WORKER_RESULT including exact base/after SHA, files, tests, invariant evidence, blockers, Host untouched attestation and automaticNextImplementationTask=NONE. STOP_FOR_ARCHITECT. Do not start another task.
