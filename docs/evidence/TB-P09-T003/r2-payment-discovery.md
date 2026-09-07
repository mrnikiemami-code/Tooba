# R2 payment discovery

Inspected Payment module (LOCAL DEV):

| Provider | Role |
|----------|------|
| fake / fake-fail | sandbox Verify |
| wallet | full-wallet immediate Verify |
| **manual (new)** | card-to-card; Verify succeeds only after Admin ConfirmDeposit |
| webhook / fail-closed | Production |

Status model: Created → Pending → Succeeded|Failed|Cancelled|Expired.

Canonical success path reused: `ConfirmDeposit` → gateway Confirm ack → `VerifyAsync` → `ApplyVerifiedSuccess` → `payment.succeeded.v1` → Order Paid + Fulfillment ReadyToFulfill.

No FE/Order status hack.
