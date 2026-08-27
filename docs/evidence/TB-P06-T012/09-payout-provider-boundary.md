# 06 — Payout gateway policy (TB-P06-T012)

## Interface

`IPayoutGateway` in `Tooba.Settlement.Application` — `PayoutAsync(payoutRequestId, sellerPartyId, amount, currency, idempotencyKey)`

## Implementations

| Gateway | Environment | Behavior |
|---|---|---|
| `FakePayoutGateway` | Development / non-Production | Always succeeds; provider code `fake-payout`; returns synthetic reference |
| `FailClosedPayoutGateway` | Production | Throws `InvalidOperationException("payout.gateway.unconfigured")`; provider code `fail-closed-payout` |

Registration in `SettlementModule.AddServices`:

```csharp
if (environment.IsProduction())
    services.AddScoped<IPayoutGateway, FailClosedPayoutGateway>();
else
    services.AddScoped<IPayoutGateway, FakePayoutGateway>();
```

## Payout request lifecycle

1. Seller POST `/v1/seller/settlement/payout-requests` — reserves available balance
2. Admin POST `/v1/admin/settlement/payout-requests/{id}/process` — invokes gateway
3. Status transitions: Pending → Processing → Succeeded / Failed
4. Admin retry endpoint for failed requests

## Safety

- No silent payout in Production without real provider wiring
- Idempotency key on payout requests prevents duplicate withdrawals
- Available balance = postedCredits − postedDebits − reservedPayouts

## Test proof

`SettlementFoundationTests` exercises payout request + FakePayoutGateway success path and verifies balance reservation math.
