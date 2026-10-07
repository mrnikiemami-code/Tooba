# TB-TMAR-PRODUCTQNA-AMSC-001-W0 — Canonical mechanism discovery

Every mechanism below was verified against **current repository reality** (not memory). Where ProductQnA
diverges, the divergence is listed as a W1/W2 target; no new/parallel mechanism is proposed.

## 1. Result / expected failures

Canonical: `Tooba.BuildingBlocks.Results.Result` / `Result<T>` (`IsSuccess`/`IsFailure`/`Errors` of
`SemanticError`), `Tooba.BuildingBlocks.SemanticException`, `Tooba.BuildingBlocks.SemanticError`.

ProductQnA state: `ProductQnAOperation.ExecuteAsync<T>` maps `SemanticException` → `Result.Failure<T>`.
**Divergence:** it does not map the code-carrying `ContractOperationException` under a known-code filter.

## 2. Typed contract fault (dominant ARCH-COMPLETE-002 convention)

Canonical: `Tooba.BuildingBlocks.ContractOperationException` — a **sealed** `Exception` carrying a typed
`Code` property (`Message` is the code; classification is never prose-based).

Reference users (verified by search):
`Order`, `Payment`, `Pricing`, `Content`, `Fulfillment`, `Notification`, `Media`, `Party`,
`PageComposition`, `Localization`, `CustomerProfile`, `Inventory`, `BulkInquiry`, `OperatorProfile`.

Two equally accepted seam shapes exist in certified modules:

| Shape | Modules | Seam |
|---|---|---|
| `catch (ContractOperationException ex) when (ModuleErrorCodes.IsKnown(ex.Code))` | Pricing, Payment, Notification, Media, Party, PageComposition, Localization, Inventory, OperatorProfile, Fulfillment | `*Operation.ExecuteAsync` |
| `catch (ContractOperationException ex) when (IsKnown(ex.Code))` with a private helper | BulkInquiry | `BulkInquiryOperation.ExecuteAsync` |

ProductQnA state: Domain/Directory throw `SemanticException`; `ProductQnAErrorCodes` has no `IsKnown`.
**Divergence:** ProductQnA must adopt the typed `ContractOperationException` + known-code seam.

## 3. Stable error codes

Canonical: `Tooba.<Module>.Contracts.Errors.<Module>ErrorCodes` — the single canonical home for
module-owned stable-code identity; values are never renamed/repurposed.

ProductQnA state: 2 semantic codes (`product_qna.rejected`, `product_qna.not_found`), 1 shared
cross-cutting code (`customer.session.required`), and 7 transport validation codes.
**Divergence:** transport validation codes must not live in the Contracts stable-code class.

## 4. Validation codes

Canonical: transport/input-shape codes in `Application/Validation/<Module>ValidationCodes.cs` (or the
capability `Validators/` folder), **never** registered in the error catalog — the pipeline
`ValidationBehavior<,>` (`src/backend/BuildingBlocks/Tooba.BuildingBlocks/TmarFoundation.cs`) throws a
`ValidationException`, and `SafeErrorMapper.MapValidation` maps every failure through the single
Foundation-owned `validation.failed` descriptor (400, `ErrorClassification.Validation`).

Verified owners: `AccessControl/Validation`, `BulkInquiry/Validation`, `Cart/Validation`, `Offer/Validation`,
`Order/Validation`, `Content/Validators`, `Catalog/Validators`, `Fulfillment/Validators`,
`Payment/Validators`, `Notification/Validators`, `Settlement/Validators`, `Media/Assets/Validators`,
`Identity/Auth/Validators`, `Localization/Languages/Validators`.

**Consequence (preserved behavior):** today the 7 validation codes are registered in the module catalog
with HTTP 400 / `ErrorClassification.Validation`. Removing those registrations does **not** change the
observable response: the validation pipeline never routes a `ValidationException` through those
descriptors — it routes through `validation.failed`, which is also 400/Validation. Observable HTTP status,
classification and the machine code set inside `validationErrors` are preserved.

## 5. Error catalog / mapping

Canonical: `IErrorCatalogContributor` + `ErrorDescriptor` + `IErrorDefinitionCatalog` + `ISafeErrorMapper`
(`SafeErrorMapper`); duplicate **ownership** is a fail-fast error (`ErrorDefinitionCatalog`).
`ErrorCatalogUniqueCodeGuardTests` machine-verifies single ownership across all production contributors.

ProductQnA state: one contributor, 9 descriptors, no duplicate ownership.
**Preserved in W1:** the descriptor registration set is byte-for-byte preserved; `product_qna.not_found`
keeps its `NotFound`/404 descriptor. No duplicate `not_found` descriptor is introduced.

## 6. Localization

Canonical: module `IErrorResourceSet` + `*.resx` + `*.fa.resx` + `IErrorMessageLocalizer` +
`IRequestLocaleResolver`.

ProductQnA state: `ProductQnAErrorResourceSet` owns the `product_qna.` key space; both cultures carry all
9 keys. **Preserved in W1:** every key and both-culture text stay unchanged.

## 7. API response mapping

Canonical: `Tooba.BuildingBlocks.Presentation.ApiResponseFactory` (`From`, `From<T>`, `Created`,
`FromFailure`, `FromException`, `FromSemanticException`, `FromPlatformException`).

ProductQnA state: `api.Created(...)`, `api.From(...)`, `api.FromFailure(...)` only. Canonical — no change.

## 8. Logging / observability

Canonical: `ILogger<T>` + `Tooba.BuildingBlocks.Observability.Logging.ObservabilityLogScope` +
`ObservabilityLogScopeKeys`; `ToobaTelemetry` (`ActivitySource`/`Meter` named `Tooba`).

ProductQnA state: no logging, no telemetry, no `Console.WriteLine`. Canonical — no change.

## 9. Tracing / correlation

Canonical: `Tooba.BuildingBlocks.Observability.Correlation` (`ICorrelationIdProvider`,
`CorrelationIdConstants.HeaderName = X-Correlation-Id`), `Observability.Tracing`
(`IModuleCallTracer`/`ModuleCallTracer`), `IProblemDetailsContextProvider`.

ProductQnA state: no custom correlation, no `StartActivity`, no `traceparent` parsing; the single
cross-module call goes through a Contracts port (no tracer decoration required). Canonical — no change.

## 10. CQRS foundation

Canonical: `Tooba.BuildingBlocks.ToobaCqrsRegistration.AddToobaCqrsFoundation` (MediatR 12.5.0,
`ValidationBehavior`, `LoggingBehavior`, `TracingBehavior`).

ProductQnA state: registered by Host; `ISender` dispatch from both endpoints. Canonical — no change.

## 11. Source-size / cohesion guard

Canonical: `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json`,
`TmarSourceSizeGuard`, `ARCH-SIZE-001` (800 LOC), `ARCH-MODULE-FILE-001`.

ProductQnA state: largest hand-written file 107 LOC; no baseline entry. Compliant — no change.

## Canonical-Reference-Used

`Offer` / `BulkInquiry` / `Payment` / `Pricing` / `Content` for the typed-fault seam;
`AccessControl` / `Cart` / `BulkInquiry` / `Offer` for `Application/Validation/` + `*ValidationCodes.cs`;
`Tooba.BuildingBlocks` for `Result`, `ApiResponseFactory`, `ContractOperationException`,
`SemanticException`, `IErrorCatalogContributor`, `IErrorResourceSet`.

No parallel mechanism is proposed anywhere in the wave plan.
