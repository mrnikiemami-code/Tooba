# Analyze — Host/ProductQnA AMC-001

## Target

`src/backend/Host/Tooba.Host/ProductQnA/` (1 file: `ProductQnAEndpoints.cs`)

## True ownership

| Responsibility | Owner |
|---|---|
| Product Q&A aggregate + schema | ProductQnA.Domain / Infrastructure |
| Product Q&A use cases | ProductQnA.Application CQRS |
| Product Q&A HTTP | ProductQnA.Endpoints (Storefront + Customer) |
| Bulk inquiry aggregate + schema | BulkInquiry.Domain / Infrastructure |
| Bulk inquiry use cases | BulkInquiry.Application CQRS |
| Bulk inquiry HTTP | BulkInquiry.Endpoints (Storefront) |
| Published product lookup | Catalog.Contracts `ICatalogReviewProductLookup` |
| Customer actor | module resolver (`ICurrentAuthenticatedUser` + Dev header; no guest) |
| Host | composition only |

## Coupling / blockers

1. Host file is **MUST_SPLIT**: ProductQnA routes + BulkInquiry route in one Host file.
2. Endpoints call Application directories directly (no MediatR).
3. Failures use `InvalidOperationException` + ad-hoc JSON (`product_qna.rejected`, `bulk_inquiry.rejected`).
4. Infrastructure uses `Catalog.Application` (`ICatalogLookupGateway`) instead of Catalog.Contracts.
5. Modules lack Endpoints / Contracts.Errors / CQRS surface.

## Final disposition

`READY_TO_MIGRATE` → split and evacuate Host folder to ProductQnA + BulkInquiry Endpoints/CQRS (`HOST_ZERO`).
