# TB-TMAR-BULKINQUIRY-AMSC-001-W1 — Migrate

## Verdict

`READY_FOR_CERTIFICATION` (behavior-preserving canonicalization; structure handoff owned by W2)

## Scope

Canonicalize the module's cross-cutting seams without changing routes, status codes, response shape,
stable error codes, localization keys, schema or domain behavior.

## 1. Validator-code ownership + placement

The 5 transport/input-shape codes were living inside `Contracts.Errors.BulkInquiryErrorCodes` and were
also registered as `ErrorDescriptor`s in the module catalog. Repository AMSC precedent
(AccessControl, Cart, Fulfillment, Settlement, Offer) keeps validator codes in
`Application/Validation/<Module>ValidationCodes.cs` and does **not** register them in the error catalog;
the canonical `ValidationBehavior` maps them through the foundation `validation.failed` descriptor.

| Before | After |
|---|---|
| `Contracts/Errors/BulkInquiryErrorCodes.cs` (6 codes) | `Contracts/Errors/BulkInquiryErrorCodes.cs` (1 semantic code: `bulk_inquiry.rejected`) |
| `Contracts/Errors/BulkInquiryErrorCatalogContributor.cs` (6 descriptors) | 1 descriptor (`bulk_inquiry.rejected`) |
| `Application/Storefront/Validators/SubmitBulkInquiryCommandValidator.cs` | `Application/Validation/SubmitBulkInquiryCommandValidator.cs` |
| — | `Application/Validation/BulkInquiryValidationCodes.cs` (5 codes, unchanged string values) |

The `.resx` / `.fa.resx` entries for the 5 validation keys are **unchanged** (localization data preserved).

## 2. Typed Domain/Infrastructure faults

Domain threw `SemanticException(SemanticError(code))`. The dominant ARCH-COMPLETE-002 Domain convention
(Order, Payment, Settlement, Inventory, Content, Fulfillment) is the typed, code-carrying
`ContractOperationException`. W1 switched the Domain/Directory fault type to that mechanism.

> **W3-R1 truth correction:** an earlier revision of this evidence wrongly stated that W1 removed the
> `Domain → Contracts` project reference. That removal did **not** happen — commit `9e9e37df` did not
> touch `Tooba.BulkInquiry.Domain.csproj`. The Domain project **retains** its own-module reference to
> `Tooba.BulkInquiry.Contracts` for the single stable constant `BulkInquiryErrorCodes.Rejected`. This is
> legitimate **self-module layering**, not cross-module coupling (foreign App/Infra/Domain coupling
> remains ZERO). Repository reality wins over the stale prose.

| Before | After |
|---|---|
| `Domain` throws `SemanticException` ×9 | `Domain` throws `ContractOperationException(BulkInquiryErrorCodes.Rejected)` via a single private `Rejected()` helper |
| `Domain/…csproj` → own-module `Tooba.BulkInquiry.Contracts` | **unchanged** — reference retained for `BulkInquiryErrorCodes.Rejected` (self-module, not cross-module); Domain → BuildingBlocks + own Contracts only |
| `Infrastructure/BulkInquiryDirectory` throws `SemanticException` | throws `ContractOperationException(BulkInquiryErrorCodes.Rejected)`; drops the duplicate inline `"Published"` comparison in favour of one private `IsPublished` |
| `BulkInquiryOperation` maps only `SemanticException` | maps `ContractOperationException` **when the code is a declared BulkInquiry code**, plus `SemanticException` for Application/Infrastructure guard sites; unknown codes/exceptions propagate to the global boundary |

Classification remains typed-code-only — never message/prose heuristics.

## 3. Guard + behavior-test alignment

| File | Change |
|---|---|
| `BulkInquiryModuleAmcW2StructureGuardTests` | validator path → `Application/Validation/`; `Storefront/Validators` must not return |
| `BulkInquiryModuleAmcW3CqrsGuardTests` | validator path + `BulkInquiryValidationCodes` ownership assertion |
| `BulkInquiryModuleAmcW4CertGuardTests` | capability-folder assertion → `Application/Validation/` |
| `ProductQnAAndBulkInquiryTests.Invalid_quantity_is_rejected` | asserts `ContractOperationException` + `Code == "bulk_inquiry.rejected"` (same observable rejection) |

## Behavior preservation

- Route `POST /v1/storefront/products/{slug}/bulk-inquiries` — unchanged.
- `201 Created` + `Location` via `ApiResponseFactory.Created` — unchanged.
- Stable codes `bulk_inquiry.rejected` + 5 `bulk_inquiry.validation.*` — unchanged values.
- Localization keys + both-culture text — unchanged.
- Domain invariants (name/phone/quantity/address/email/company/notes) — unchanged.
- `bulk_inquiry` schema, table, columns, index, migration IDs — untouched.
- DI lifetimes, outbox registration, Host composition seams — untouched.

## Focused validation

- `dotnet build Tooba.Host.Tests` → 0 errors.
- `dotnet test --filter FullyQualifiedName~BulkInquiry` → **14 passed / 0 failed / 2 skipped** (skips are Postgres Testcontainers).

## Known pre-existing failures (not caused by this wave)

`TmarCompleteReferenceStructureGateTests` (3 failures) — Catalog-related stale expectations:
`Certified_modules_satisfy_root_allowlists_and_namespace_alignment` (`Tooba.Catalog.Contracts.Cart`),
`Manifest_is_well_formed_and_only_declared_modules_are_certified` and
`Uncertified_modules_are_explicitly_not_claimed` (manifest now lists `Catalog` as certified).
BulkInquiry is not involved; left untouched.

## Structure-Handoff-State

`REQUIRED` — W2 owns the physical/visual gate.
