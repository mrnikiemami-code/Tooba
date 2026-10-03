# TB-TMAR-BULKINQUIRY-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Storefront bulk-inquiry POST dispatches through MediatR `ISender` → Application handler → `BulkInquiryOperation` (`SemanticException` → `Result`) → `ApiResponseFactory.Created`. Zero `Results.Json`. Zero endpoint `catch (SemanticException)`.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| `SubmitBulkInquiryCommand` | `SubmitBulkInquiryCommandValidator` | VALIDATOR_REQUIRED_PRESENT |

## Error ownership

- Catalog/resource set/resx: `Tooba.BulkInquiry.Contracts`
- Registration: `BulkInquiryModule` (Infrastructure)
- Endpoints Errors/Resources: ABSENT
- Validator codes live in `BulkInquiryErrorCodes` and catalog descriptors

## Coupling

Zero foreign Application/Infrastructure/Domain. Catalog lookup remains `Catalog.Contracts` only.
