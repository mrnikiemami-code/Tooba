# TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1

Persisted claim artifact — full Architect body received via Bridge `GET /api/tasks/next?channelId=tooba-main` at claim row `1cc2026a-8440-452f-ae57-c41f974d33e4`.

- Parent-Task: `TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001`
- Channel: `tooba-main`
- WorkerId: `tooba-worker-01`
- AgentType: `cursor`
- Program: TMAR — Tooba Microservice-Ready Architecture Recovery
- Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`
- Track: `PAYMENT_PRECERT_DIRECTORY_SPLIT_PROOF_REPAIR`
- Title: Repair Payment directory DI resolution proof only
- Backend-Only: YES

## Architect verdict

Parent production decomposition is ACCEPTABLE IN SUBSTANCE at commit
`9612eba0f36c0b67027e380e362d0acd943c926b` (PaymentDirectory only `IPaymentDirectory` at 476 LOC;
the three focused directories exist; four focused scoped registrations; cast down-casts removed;
Payment -> Host ZERO; `PaymentContractBridge` intact; Payment still NOT ARCH-COMPLETE-002
structure-certified; Checkout paused; frontend frozen).

## Exact defect

`Directory_ports_resolve_to_distinct_focused_implementations` inspected `IServiceCollection`
descriptors only. It did not build a `ServiceProvider`, create a scope, resolve the four
`IPayment*Directory` ports, or prove the runtime registrations produce the expected four concrete
implementations without a construction cycle — while the parent PASS criteria required exactly that
runtime-resolution proof.

## One objective

Repair the focused DI proof. Do not change Payment production behavior unless an actual DI defect is
revealed by the runtime-resolution test.

Forbidden: Payment validators; structure certification; folder reorganization; further directory
refactoring; contract/event/schema/migration change; Host ownership change; Cart/Order/StoreContext/
Offer changes; resuming Checkout; frontend changes.

## Required test repair

Build the `ServiceProvider` from the Payment module registration; create one `IServiceScope`; resolve
`IPaymentDirectory`, `IPaymentReconciliationDirectory`, `IPaymentAdminDirectory`,
`IPaymentExpiryDirectory`; assert exact concrete runtime types `PaymentDirectory`,
`PaymentReconciliationDirectory`, `PaymentAdminDirectory`, `PaymentExpiryDirectory`; assert the four
resolved instances are distinct as appropriate; prove resolution completes without circular-dependency
failure; preserve the existing reflection assertion that each concrete class implements only its own
one directory port. If production DI already works, production files MUST remain unchanged.

## Architecture locks

Unchanged: ARCH-COMPLETE-002; HOST-MODULE-ENDPOINT-001; ARCH-CQRS-001/002; Payment -> Host = ZERO;
Host Payment residue = exactly two approved thin security adapters; Payment structure certification =
PENDING; Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`.

## FAST-VALIDATION-BUDGET

Run ONLY the single focused runtime DI-resolution test; if production DI changed,
`PaymentArchitectureGuardTests` only; and one project build:
`dotnet build src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-restore`.
Do not run full Payment/Host/TMAR/solution suites or Testcontainers integration suites.

## Recovery

On PASS: do not rewrite the parent decomposition state; mark the parent
`ACCEPTED_AFTER_R1_RUNTIME_DI_RESOLUTION_PROOF`; keep
`nextTask = TB-TMAR-PAYMENT-PRECERT-VALIDATION-001`; Payment remains NOT structure-certified. Update
only the minimum Recovery SoT / durable guard fields needed to record parent acceptance, runtime DI
resolution = PROVEN, and the next task. Do not claim validation coverage.

## Evidence

`docs/evidence/TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1/runtime-di-resolution-proof.md` —
exact test, exact four runtime types, whether production DI changed, focused validation result,
project build result.

## PASS criteria

All four directory ports resolved from a real scoped `ServiceProvider`; exact concrete runtime types
proven; no construction cycle; each concrete directory still implements only one directory port;
parent production behavior unchanged; Payment -> Host ZERO; Payment NOT structure-certified;
Checkout/frontend unchanged; focused test passes; Payment test project builds.

## After Result

STOP completely. Do not start Payment validation automatically. Do not start Payment structure
certification. Do not run broader tests. Do not poll. Wait for Architect verification.
