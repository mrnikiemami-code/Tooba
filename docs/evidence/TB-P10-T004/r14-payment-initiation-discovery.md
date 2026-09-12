# TB-P10-T004-R14 — Initiation discovery

| Entry | File | After Succeeded |
| --- | --- | --- |
| Online/manual/wallet initiate | `PaymentDirectory.InitiateAsync` + `StorefrontPaymentComposer.InitiateAsync` | reject unless same idempotency key replay |
| Sandbox complete | `VerifyAsync` / `CompleteSandboxAsync` | idempotent Verify, no new payment |
| Manual evidence | `SubmitManualEvidenceAsync` | reject |
| Proof upload | `RegisterProofAssetAsync` | already Pending-only; Succeeded invalid_state |
| Manual retry | `RetryManualAfterRejectionAsync` | reject if Succeeded |
| Unpaid retry | `ReopenExpiredForRetryAsync` | Expired-only |
| Payment page CTA | `canSubmitManualEvidence` / `canRetry*` | false when Succeeded |
| Handoff pay | `CanInitiatePayment` | false when Succeeded |
| Admin | confirm/reject existing manual only | not a customer initiate |

| State | online initiate | manual initiate | retry | API |
| --- | --- | --- | --- | --- |
| no Payment | yes | yes | n/a | 200 new Payment |
| Initiated/Pending | same key replay | same | no new checkout pay | 200 existing |
| AwaitingAdmin | no new Payment | evidence only if Pending | no | evidence 200 / initiate new key 200 existing pending or new if none succeeded |
| Failed | same-key retry | retry when policy | yes | 200 new attempt |
| Rejected (Failed manual) | — | RetryManual | yes | 200 |
| Expired | unpaid retry | — | yes if supply | 200 reopen |
| Succeeded | no | no | no | 409 `payment.already_succeeded` |
| Cancelled | no | no | no | payable/guard reject |
