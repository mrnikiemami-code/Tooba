# TB-TMAR-PAYMENT-PRECERT-VALIDATION-001 — Payment storefront transport validation

Parent: `TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1` (ARCHITECT_ACCEPTED at
`1ef08cb5b35f7104baee74b6ad1d0822d1feff24`)

Scope: storefront slice only. This task does **not** certify Payment under
ARCH-COMPLETE-002 and does **not** close Payment validator coverage.

## 1. Storefront endpoint-reachable request inventory (exhaustive, exactly 10)

| # | Request | Classification |
| - | - | - |
| 1 | `InitiateStorefrontPaymentCommand` | VALIDATOR_REQUIRED_PRESENT |
| 2 | `GetStorefrontWalletQuoteQuery` | VALIDATOR_REQUIRED_PRESENT |
| 3 | `GetStorefrontPaymentQuery` | VALIDATOR_REQUIRED_PRESENT |
| 4 | `GetStorefrontPaymentSandboxContextQuery` | VALIDATOR_REQUIRED_PRESENT |
| 5 | `CompleteSandboxPaymentCommand` | VALIDATOR_REQUIRED_PRESENT |
| 6 | `SubmitManualPaymentEvidenceCommand` | VALIDATOR_REQUIRED_PRESENT |
| 7 | `RetryManualPaymentCommand` | VALIDATOR_REQUIRED_PRESENT |
| 8 | `RetryUnpaidPaymentCommand` | VALIDATOR_REQUIRED_PRESENT |
| 9 | `UploadManualPaymentProofCommand` | VALIDATOR_REQUIRED_PRESENT |
| 10 | `ListStorefrontPaymentMethodsQuery` | NO_VALIDATOR_REQUIRED_NO_INPUT |

Totals: 9 VALIDATOR_REQUIRED_PRESENT, 1 NO_VALIDATOR_REQUIRED_NO_INPUT.

The inventory is derived from the only storefront endpoint owner,
`Tooba.Payment.Endpoints/Storefront/PaymentStorefrontEndpoints.cs` (10 `MapGroup("/v1/storefront")`
routes, all `ISender sender` based). The guard test extracts every
`new <Command|Query>` construction from that file and requires the set to equal this manifest exactly.

## 2. Nine validators added

Folder: `src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Storefront/`
Namespace: `Tooba.Payment.Application.Validators.Storefront`

1. `InitiateStorefrontPaymentCommandValidator`
2. `GetStorefrontWalletQuoteQueryValidator`
3. `GetStorefrontPaymentQueryValidator`
4. `GetStorefrontPaymentSandboxContextQueryValidator`
5. `CompleteSandboxPaymentCommandValidator`
6. `SubmitManualPaymentEvidenceCommandValidator`
7. `RetryManualPaymentCommandValidator`
8. `RetryUnpaidPaymentCommandValidator`
9. `UploadManualPaymentProofCommandValidator`

Shared transport helpers live one level up in `Tooba.Payment.Application/Validators`:
`PaymentValidationCodes` (stable `payment.validation.*` machine codes) and
`PaymentFluentRules` (required id / optional id shape / required non-blank / optional non-blank).

## 3. NO_VALIDATOR_REQUIRED rationale

`ListStorefrontPaymentMethodsQuery` is a parameterless `sealed record` with no request payload.
It carries no input to shape, so no transport validator is applicable.
The guard test asserts `IValidator<ListStorefrontPaymentMethodsQuery>` is **not** registered.

## 4. Validation scope — transport shape only

Validated:
- required `Guid` => `!= Guid.Empty`
- nullable `Guid` => `!= Guid.Empty` when supplied; `null` allowed
- required string => non-null, non-empty, non-whitespace
- optional string => `null` allowed; supplied whitespace rejected
- `UploadManualPaymentProofCommand.Content` => not null only; the stream is **never read or sought**
  (proven with a non-seekable stream that throws on `Read`/`Seek`)

Deliberately **not** validated here: ownership, authorization, DB existence, gateway availability,
payment state, eligibility, payable amount, provider allowlist, outcome allowlist, file size/media policy.

## 5. Discovery / pipeline evidence

- FluentValidation only, no custom validation framework.
- Discovery is the existing central registration in `Tooba.BuildingBlocks/TmarFoundation.cs`:
  `services.AddValidatorsFromAssembly(assembly)` for each module assembly, invoked from
  `Host/Program.cs` `AddToobaCqrsFoundation(...)` which already registers
  `Tooba.Payment.Application` via `InitiateStorefrontPaymentCommand`'s assembly.
- MediatR remains `12.5.0`.
- No endpoint-specific middleware, no manual validator invocation from endpoints or handlers.
- The guard test builds the foundation DI container and asserts each of the nine
  `IValidator<T>` resolves to its exact concrete validator type.

## 6. Focused test result

```
dotnet test src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-build \
  --filter "FullyQualifiedName~PaymentStorefrontValidator"
Passed! - Failed: 0, Passed: 16, Skipped: 0, Total: 16
```

Focused in-memory, no web host, no database.

## 7. Focused architecture guard result

```
dotnet test src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-build \
  --filter "FullyQualifiedName~PaymentArchitectureGuardTests|FullyQualifiedName~PaymentValidatorCoverageGuardTests"
Passed! - Failed: 0, Passed: 19, Skipped: 0, Total: 19
```

## 8. Project build result

```
dotnet build src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-restore
Build succeeded. 0 Error(s)
```

## 9. Admin + Webhook closed by follow-up task

`Validators/Admin` and `Validators/Webhooks` were **not** part of this slice; they were added by
`TB-TMAR-PAYMENT-PRECERT-VALIDATION-002`.

- `paymentValidationStorefront` = `9_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED`
- `paymentValidationAdminWebhook` at this task's acceptance = `PENDING_TB_TMAR_PAYMENT_PRECERT_VALIDATION_002`
- `paymentValidationOverall` at this task's acceptance = `PARTIAL_9_OF_15_REQUIRED_PRESENT`
- After `TB-TMAR-PAYMENT-PRECERT-VALIDATION-002`: overall = `COMPLETE_15_OF_15_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED`

## 10. Protected architecture state preserved

- Payment -> Host dependency = ZERO
- Host Payment residue = exactly `HostPaymentAdminAuthorizer` + `HostPaymentStorefrontAuthorizer`
- `PaymentContractBridge` intact; focused directory split intact
- Payment structure certification = PENDING (not certified in
  `tmar-module-structure-manifests.json`, not added to `structureLock.certifiedModules`,
  still in `uncertifiedHttpOwningModules`)
- Cart/Order/StoreContext/Offer certifications unchanged
- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`
- No schema/migration change; Settlement untouched
