# 14 — Admin payment operations

**Task:** TB-P06-T022  
**UI lock:** existing Admin panel patterns only (Info fields; no redesign).

## Endpoints

```http
GET  /v1/admin/payments/{paymentId}
POST /v1/admin/payments/{paymentId}/reconcile
```

Authorization: `AdminPanelAccess.RequireAuthorizedAsync`.

## Fields exposed (no secrets)

| Field | Source |
|---|---|
| PaymentId | Operational snapshot |
| Order / CheckoutId | Snapshot |
| Status | Aggregate status |
| Amount / Currency | Aggregate |
| ProviderCode | e.g. `fake` / `webhook` |
| ProviderRequestReference | Attempt reference |
| ProviderTransactionReference | Verified txn if any |
| CreatedAt / UpdatedAt / CompletedAt | Timestamps |
| LastFailureCode | Safe failure category |
| ReconcileEligible | Operator action hint |

## Order detail embedding

`GET /v1/admin/orders/{checkoutId}` includes `payment` (`AdminPaymentOpsView`) composed in `AdminPanelComposer`.

Frontend (`admin-screens.tsx` / `admin-api.ts`) maps these into existing `Info` labels — PaymentId, provider, references, timestamps, safe failure — without new layout/CSS redesign.

## Explicitly not exposed

- WebhookSigningSecret / StatusQueryApiKey
- Raw sensitive webhook payloads
- Arbitrary payment state mutation endpoints
