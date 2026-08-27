# 09 — Settlement bridge contracts (TB-P06-T012)

## Cross-module read bridges (Settlement.Infrastructure)

| Bridge | Reads from | Contract interface |
|---|---|---|
| `SettlementOrderBridge` | Order schema | `ISettlementOrderReader` |
| `SettlementPaymentBridge` | Payment schema | `ISettlementPaymentReader` |
| `SettlementReturnsBridge` | Returns schema | `ISettlementReturnsReader` |

## Reverse bridges (other modules → Settlement snapshots)

| Bridge | Module | Contract |
|---|---|---|
| `PaymentSettlementBridge` | Payment | `IPaymentSettlementReader` |
| `ReturnSettlementBridge` | Returns | `IReturnSettlementReader` |

Contract files:

- `Tooba.Payment.Application/PaymentSettlementContracts.cs`
- `Tooba.Returns.Application/ReturnSettlementContracts.cs`
- `Tooba.Settlement.Application/SettlementContracts.cs`

## Design constraint

Each bridge uses its **own** DbContext with `AsNoTracking()` queries. SettlementDirectory orchestrates across reader interfaces — never joins foreign schemas in SQL.

## Snapshot DTOs (examples)

- Payment: `PaymentSettlementSnapshot`, `PaymentSettlementAllocationSnapshot`
- Returns: `ReturnSettlementSnapshot` (seller order, party, refund amount)
- Order: seller order paid status, seller party id, currency context

## Wiring

Bridges registered in respective modules (`PaymentModule`, `ReturnsModule`, `SettlementModule`) and composed at Host via `ToobaModuleComposition`.
