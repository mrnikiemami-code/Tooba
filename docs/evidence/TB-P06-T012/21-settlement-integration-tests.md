# 20 — Settlement foundation tests (TB-P06-T012)

## File

`src/backend/Host/Tooba.Host.Tests/SettlementFoundationTests.cs`

## Test cases (3/3 pass when Docker available)

| Test | Type | Coverage |
|---|---|---|
| `Settlement_module_boundary_static_checks` | Unit | Schema name, type isolation, csproj references |
| `Settlement_lifecycle_applies_commission_refund_and_payout_safety` | Integration (Testcontainers PG) | Full accrual → commission 10% → refund debit → payout request → FakePayoutGateway |
| `Settlement_handlers_are_marketplace_gated_in_module_registration` | Unit | Handler DI registration gated on Marketplace edition |

## Integration scenario highlights

1. Seed paid checkout + payment via FakePaymentGateway
2. Fire `PaymentSucceededIntegrationEvent` → accrue credit entry
3. Assert commission snapshot rate 0.10 and net math
4. Duplicate event → inbox skip (no double accrual)
5. Refund event → debit adjustment
6. Payout request + process → balance reservation release

## Infrastructure

- `[Collection("PostgresSerial")]`
- `[SkippableFact]` when Docker/Testcontainers unavailable
- Isolated marketplace commerce context per test

## Suite total

Backend full suite: **239 tests pass** (includes 3 Settlement foundation tests).
