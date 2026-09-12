# TB-P10-T004-R14 — Capability

Canonical backend:

- `IPaymentDirectory.HasSucceededPaymentForCheckoutAsync`
- Checkout GET overlays `PaymentState=Paid` + `CanInitiatePayment=false` when any Succeeded Payment exists (covers projection lag).
- Payment page: `CanSubmitManualEvidence` / `CanRetryManual` / `CanRetryUnpaid` are false when Status=Succeeded.

Frontend consumes `canInitiatePayment` and those flags. It does not invent pay-again from copy.

Allowed: no payment yet; Failed same-key retry; Expired unpaid reopen after EnsureOrderSupply; Rejected manual RestoreDeposit.

Forbidden: any new initiation/evidence/retry after Succeeded.
