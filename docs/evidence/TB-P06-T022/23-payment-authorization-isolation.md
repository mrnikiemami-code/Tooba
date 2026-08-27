# 23 — Payment authorization isolation

**Task:** TB-P06-T022

## Boundaries

| Actor | Payment inspect | Payment mutate / reconcile | Notes |
|---|---|---|---|
| Owning customer | Own payment via directory guard | Initiate/verify own flow only | Foreign payment denied |
| Foreign customer | Rejected | Rejected | Cross-buyer isolation |
| Seller | No payment state mutation API | Fulfillment only | Cannot force Paid |
| Admin | Operational GET | Reconcile with AdminPanelAccess | Secrets still hidden |
| Anonymous | Denied | Denied | Standard auth |

## Cross-tenant

Payment lookups run under current tenant / commerce context. Cross-tenant payment inspect must fail closed.

## Admin

- `GET /v1/admin/payments/{id}` and `POST …/reconcile` require authorized admin session.
- No arbitrary status PATCH endpoint added.

## Related tests

Foundation payment visibility + admin access tests; production policy does not weaken auth.

```text
PAYMENT_AUTHORIZATION_ISOLATION = LIVE (foundation + admin gates)
```
