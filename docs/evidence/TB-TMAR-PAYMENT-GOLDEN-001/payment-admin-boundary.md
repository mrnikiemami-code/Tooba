# payment-admin-boundary

Task: TB-TMAR-PAYMENT-GOLDEN-001
Verdict: VERIFIED

- Tooba.Payment.Endpoints owns Storefront/Admin/Webhooks HTTP via ISender → Application CQRS.
- Host retains MapPaymentEndpoints(), authorizer/access/media/supply/enrichment adapters, and PaymentReconciliationHostedService as scheduler-only.
- Cross-module: Wallet.Contracts IWalletOrderPaymentPort; checkout/media/supply/order enrichment via Application ports (Host adapters).
- PaymentExceptionMapper: STABLE_CODES_ONLY (exact match); no Contains/prose heuristics.
- Deleted: Host PaymentWebhookEndpoints, StorefrontPaymentComposer, AdminPaymentsGridQueryEngine.
