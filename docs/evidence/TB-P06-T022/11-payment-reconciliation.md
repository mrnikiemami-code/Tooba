# 11 — Payment reconciliation

**Task:** TB-P06-T022

## Background

Config section `Tooba:PaymentReconciliation` (see `docs/operations/payments.md`):

- Enabled poll of stale Pending payments
- Provider status via adapter `VerifyAsync` (StatusQuery for webhook mode)
- Idempotent transitions; Order Paid only after verified success event

## Manual / operator

```http
POST /v1/admin/payments/{paymentId}/reconcile
```

Requires Admin authorization. Invokes `IPaymentAdminDirectory.ReconcileAsync` → same Verify path.

## Eligibility

Operational snapshot exposes `ReconcileEligible` for Pending (and related unresolved) payments.  
Admin order detail embeds this flag without exposing secrets.

## Bounded policy

- Worker batch size / pending age / poll interval from config.
- Gateway-side `VerifyMaxAttempts` for transient StatusQuery failures.
- Unresolved remain Pending and operator-visible.

## Honest status

```text
RECONCILIATION_READY = YES (foundation)
REAL_PROVIDER_RECONCILE_PROVEN = NO
```

Reconciliation path is live against the adapter boundary; no external PSP credentials were available to prove a live bank reconcile.
