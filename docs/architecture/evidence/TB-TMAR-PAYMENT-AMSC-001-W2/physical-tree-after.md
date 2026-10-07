# TB-TMAR-PAYMENT-AMSC-001-W2 — physical-tree-after

Production `.cs` inventory **after** W2 (`Application` only; the other four projects were already
canonical and are untouched).

```text
Tooba.Payment.Application/
  Admin/                                        <- capability (admin payment operations)
    Commands/   ConfirmAdminDepositCommand.cs, ReconcileAdminPaymentCommand.cs,
                RejectAdminDepositCommand.cs
    Models/     AdminPaymentGridDtos.cs         (admin grid read models)
    Queries/    GetAdminPaymentQuery.cs, QueryAdminPaymentsGridQuery.cs
    Validators/ ConfirmAdminDepositCommandValidator.cs, GetAdminPaymentQueryValidator.cs,
                QueryAdminPaymentsGridQueryValidator.cs, ReconcileAdminPaymentCommandValidator.cs,
                RejectAdminDepositCommandValidator.cs
  Storefront/                                   <- capability (storefront payment journey)
    Commands/   CompleteSandboxPaymentCommand.cs, InitiateStorefrontPaymentCommand.cs,
                RetryManualPaymentCommand.cs, RetryUnpaidPaymentCommand.cs,
                SubmitManualPaymentEvidenceCommand.cs, UploadManualPaymentProofCommand.cs
    Queries/    GetStorefrontPaymentQuery.cs, GetStorefrontPaymentSandboxContextQuery.cs,
                GetStorefrontWalletQuoteQuery.cs, ListStorefrontPaymentMethodsQuery.cs
    Validators/ 9 storefront validators (1:1 with the 9 required storefront requests)
    Models/     StorefrontPaymentDtos.cs        (storefront DTO bundle)
    Orchestration/ GlobalUsings.cs, StorefrontPaymentOrchestrator.cs
  Webhooks/                                     <- capability (PSP callback ingestion)
    Commands/   ProcessPaymentWebhookCommand.cs
    Validators/ ProcessPaymentWebhookCommandValidator.cs
  Reconciliation/                               <- capability (stale-payment worker cycle)
    Commands/   ReconcileStalePaymentsCommand.cs
  Composition/  PaymentOperation.cs             (W1 fault seam — unchanged)
  Models/       PaymentGatewayActorContext.cs, PaymentGatewayOutcomes.cs   (cross-capability)
  Ports/        5 port files                    (cross-capability)
  Validators/   PaymentFluentRules.cs, PaymentValidationCodes.cs           (cross-capability)
```

## Tree depth

```text
Application / <Capability> / Commands   / <file>   depth 3   CANONICAL
Application / <Capability> / Queries    / <file>   depth 3
Application / <Capability> / Validators / <file>   depth 3
Application / <Capability> / Models     / <file>   depth 3
Application / <Capability> / Orchestration / <file> depth 3
Application / Composition|Models|Ports|Validators / <file>  depth 2
```

No capability tree exceeds `capability -> technical axis -> files`. No request/use-case child
folder survives on any axis (`Assert.Empty(Directory.GetDirectories(axisRoot))`).

## Classification states after W2

| State field | Value |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` |
| Structure-State | `READY_FOR_CERTIFY` |

## Explicitly preserved (out of scope for W2)

- `Application/Validators/PaymentFluentRules.cs` + `PaymentValidationCodes.cs` stay at the Application
  root as the **cross-capability** structural folder (same role as `FulfillmentFluentRules` in
  Fulfillment and `ContentValidationCodes` in Content). Flattening them into one capability would have
  created a false owner.
- `Application/Models/PaymentGatewayActorContext.cs` + `PaymentGatewayOutcomes.cs` stay at the
  Application root because they are consumed by the Infrastructure directories/providers
  (`PaymentDirectory`, `PaymentExpiryDirectory`, `WalletPaymentGateway`, `WebhookPaymentGateway`,
  `PaymentModule` DI) as well as by Application — not by the storefront capability alone.
- `Composition/PaymentOperation.cs` (W1) is untouched.
