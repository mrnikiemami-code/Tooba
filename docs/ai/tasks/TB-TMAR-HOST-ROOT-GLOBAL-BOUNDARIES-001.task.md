PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001
Parent-Task: TB-TMAR-HOST-MEDIA-EVACUATE-001
Channel: tooba-main
WorkerId: chatgpt-architect-direct
AgentType: chatgpt
Program: TMAR — Tooba Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ROOT_GLOBAL_BOUNDARIES
Title: Analyze, migrate, and certify six root Host business-boundary files

Scope:
- CheckoutReservationHoldPolicy.cs
- CommerceHoldPolicy.cs
- GlobalUsings.SettlementApp.cs
- GlobalUsings.SettlementDomain.cs
- UnpaidOrderExpiryHostedService.cs
- UnpaidOrderExpiryHostOptions.cs

Sequence:
1. ANALYZE ownership and direct dependencies.
2. MIGRATE business responsibility to owning modules with lawful Contracts seams.
3. CERTIFY the bounded migration surface without weakening guards.

Locks:
- Host is composition/platform shell only.
- No foreign Application/Infrastructure/Domain dependency may be introduced by the migration.
- Cross-module reads use Contracts.
- Preserve behavior/config keys/telemetry/logging.
- Do not touch Authorization work running in parallel.
- No frontend/schema/migration change.
- Do not merge into main until certification evidence is complete.

Expected disposition:
- Checkout hold policy: Order consumes Payment Contracts via a thin Order-side adapter.
- Commerce hold policy: Payment-owned source; store override reads through Catalog.Contracts.
- Settlement global usings: remove as stale global coupling.
- Unpaid expiry worker/options: Order-owned background worker; Host retains only neutral composition seams.

Certification:
- Root six-file residue = ZERO.
- Destination files have ZERO Tooba.Host reference.
- New cross-module edges are Contracts-only.
- Focused builds/tests required before final CERTIFIED verdict.
- If executable validation is unavailable or a touched destination surface violates certification prerequisites, verdict = NOT_CERTIFIED with exact blocker.

STOP after certification result.
END_TOOBA_TASK