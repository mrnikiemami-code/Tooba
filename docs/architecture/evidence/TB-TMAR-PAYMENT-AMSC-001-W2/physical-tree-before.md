# TB-TMAR-PAYMENT-AMSC-001-W2 — physical-tree-before

Production `.cs` inventory **before** W2 (Payment `Application` only; the defect surface).

```text
Tooba.Payment.Application/
  Commands/                                     <- TECHNICAL AXIS FIRST (primary organization)
    CompleteSandboxPayment/CompleteSandboxPaymentCommand.cs          1 file  OVER_FOLDERED
    ConfirmAdminDeposit/ConfirmAdminDepositCommand.cs                1 file  OVER_FOLDERED
    InitiateStorefrontPayment/InitiateStorefrontPaymentCommand.cs    1 file  OVER_FOLDERED
    ProcessPaymentWebhook/ProcessPaymentWebhookCommand.cs            1 file  OVER_FOLDERED
    ReconcileAdminPayment/ReconcileAdminPaymentCommand.cs            1 file  OVER_FOLDERED
    ReconcileStalePayments/ReconcileStalePaymentsCommand.cs          1 file  OVER_FOLDERED
    RejectAdminDeposit/RejectAdminDepositCommand.cs                  1 file  OVER_FOLDERED
    RetryManualPayment/RetryManualPaymentCommand.cs                  1 file  OVER_FOLDERED
    RetryUnpaidPayment/RetryUnpaidPaymentCommand.cs                  1 file  OVER_FOLDERED
    SubmitManualPaymentEvidence/SubmitManualPaymentEvidenceCommand.cs 1 file OVER_FOLDERED
    UploadManualPaymentProof/UploadManualPaymentProofCommand.cs      1 file  OVER_FOLDERED
  Queries/                                      <- TECHNICAL AXIS FIRST
    GetAdminPayment/GetAdminPaymentQuery.cs                          1 file  OVER_FOLDERED
    GetStorefrontPayment/GetStorefrontPaymentQuery.cs                1 file  OVER_FOLDERED
    GetStorefrontPaymentSandboxContext/…Query.cs                     1 file  OVER_FOLDERED
    GetStorefrontWalletQuote/GetStorefrontWalletQuoteQuery.cs        1 file  OVER_FOLDERED
    ListStorefrontPaymentMethods/…Query.cs                           1 file  OVER_FOLDERED
    QueryAdminPaymentsGrid/QueryAdminPaymentsGridQuery.cs            1 file  OVER_FOLDERED
  Validators/
    PaymentFluentRules.cs                        (cross-capability helper)
    PaymentValidationCodes.cs                    (cross-capability helper)
    Admin/     5 validators                      <- AUDIENCE axis, not capability axis
    Storefront/ 9 validators
    Webhooks/  1 validator
  Orchestration/
    GlobalUsings.cs                              (single-file self-namespace global using)
    StorefrontPaymentOrchestrator.cs             (storefront capability service, 468 LOC)
  Models/
    PaymentGatewayActorContext.cs                (shared structural folder)
    PaymentGatewayOutcomes.cs                    (shared structural folder)
    StorefrontPaymentDtos.cs                     (MIXED bundle: provider codes + guest actor +
                                                  storefront DTOs + admin grid DTOs, 139 LOC)
  Ports/                                         (5 shared structural files)
  Composition/
    PaymentOperation.cs                          (W1 fault seam — unchanged by W2)
```

## Root inventory (unchanged by W2)

```text
Tooba.Payment.Contracts/      Admin/, Checkout/, Customer/, Errors/, Events/, Hold/, Ports/,
                              Resources/, Returns/, Settlement/, Storefront/
Tooba.Payment.Domain/         Aggregates/, Events/, ValueObjects/
Tooba.Payment.Endpoints/      PaymentEndpointModule.cs (root allowlist), Admin/, Storefront/,
                              Webhooks/, Errors/
Tooba.Payment.Infrastructure/ Adapters/, DependencyInjection/, Directories/, Events/, Messaging/,
                              Persistence/, Providers/, Workers/
```

## Classification states before W2

| State field | Value |
| --- | --- |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` + 17 `OVER_FOLDERED` single-file request leaves |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Payment/`, 6 projects) |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` (no file > 700 LOC; `StorefrontPaymentOrchestrator.cs` 468) |
| Structure-State | `REPAIR_REQUIRED` |
