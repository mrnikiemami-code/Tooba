# Recovery SoT — TB-TMAR-PAYMENT-GOLDEN-001-R1

| Criterion | Value |
|---|---|
| Payment-R1-Endpoints-Project | REAL_AND_WIRED |
| Payment-R1-Webhook | MODULE_ENDPOINTS_CQRS |
| Payment-R1-Admin-DetailActions | MODULE_ENDPOINTS_CQRS |
| Payment-R1-Reconciliation-Host | SCHEDULER_ONLY |
| Payment-R1-CQRS | MEDIATR_12_5_REAL |
| Payment-R1-Error-Classification | STABLE_CODES_ONLY |
| Payment-R1-Storefront-Plan | CONTRACTS_TARGET_EXPLICIT |
| Payment-R1-AdminGrid-Plan | CONTRACTS_TARGET_EXPLICIT |
| Payment-R1-Architecture-Guards | ENFORCED |
| **Payment-State** | **IN_PROGRESS_GOLDEN_R2_READY** |
| Recovery-Next-Task | TB-TMAR-PAYMENT-GOLDEN-001-R2 |

Note: Storefront payment HTTP + admin grid HTTP were also moved into Payment.Endpoints during this wave; Architect COMPLETE remains deferred to R2 per R1 Result contract. Parent TB-TMAR-PAYMENT-GOLDEN-001 remains INCOMPLETE (not Architect ACCEPT). Last accepted COMPLETE remains Wallet.
