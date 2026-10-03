# TB-TMAR-BULKINQUIRY-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only.

## Ownership

| Surface | Owner |
|---|---|
| Storefront bulk-inquiry POST | BulkInquiry.Endpoints.Storefront |
| Submit CQRS command + validator | BulkInquiry.Application |
| BulkPurchaseInquiry aggregate + status enum | BulkInquiry.Domain |
| Directory / DbContext / Outbox | BulkInquiry.Infrastructure |
| Stable error codes | BulkInquiry.Contracts.Errors |
| Host BulkInquiry HTTP | CLOSED_HOST_ZERO (evacuated with ProductQnA split) |

## Foreign coupling

- Foreign Application/Infrastructure/Domain: **ZERO**
- Cross-module: `Catalog.Contracts.ICatalogReviewProductLookup` only (allowed Contracts seam)
- Self-contained persistence schema `bulk_inquiry`

## Blockers for COMPLETE_REFERENCE_PATTERN

1. **Solution Explorer:** projects under flat `/Modules/`; Contracts + Endpoints missing from slnx; need `/Modules/BulkInquiry/`.
2. **God / root dumps / structure:**
   - Domain root `BulkPurchaseInquiry.cs` (aggregate + `BulkInquiryStatus` enum)
   - Application root `BulkInquiryContracts.cs` + technical-axis `Commands/` (handler+validator co-located)
   - Infrastructure root Directory + root `Migrations/` + Outbox in Module file
3. **Error ownership:** Catalog + resx in Endpoints; codes in Contracts — move catalog/resx to Contracts; register in Infrastructure.
4. **API result:** Endpoints use `Results.Json` + `catch SemanticException` — need `IRequest<Result>` + `BulkInquiryOperation` + `ApiResponseFactory.Created`/`From`.
5. **Validator codes:** ad-hoc `bulk_inquiry.validation.*` strings not in Contracts.Errors / catalog descriptors.
6. **Structure-Handoff-State:** `REQUIRED`

## Wave plan

| Wave | Focus |
|---|---|
| W1 | `/Modules/BulkInquiry/` slnx + Contracts/Endpoints |
| W2 | Split Domain/App/Infra physical tree (capability-first) |
| W3 | Contracts catalog/resx + Result/Operation + validator codes + thin endpoints |
| W4 | Structure + Certify + SoT/manifest |

## Microservice extractability

Blocked until Solution Explorer, structure, Result/API mapping, and Contracts-owned catalog close. Foreign coupling already Contracts-only to Catalog — strong extractability once quality gates pass.
