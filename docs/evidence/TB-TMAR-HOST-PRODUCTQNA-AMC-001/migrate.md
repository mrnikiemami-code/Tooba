# Migrate — Host/ProductQnA AMC-001

## Waves

1. **Split**: Host mixed file → ProductQnA HTTP + BulkInquiry HTTP
2. **Contracts**: `ProductQnA.Contracts.Errors` + `BulkInquiry.Contracts.Errors`
3. **Foundation**: module Endpoints (Storefront/Customer) + resx/error contributors
4. **CQRS**: GetPublishedQuestionsQuery, SubmitProductQuestionCommand, SubmitBulkInquiryCommand
5. **Domain/Infra**: typed `SemanticException`; Catalog via `ICatalogReviewProductLookup` (Catalog.Contracts)
6. **Host ZERO**: deleted `Host/ProductQnA`; Program maps both module endpoint modules

## Behavior preserved

- Routes/verbs unchanged
- Public QnA JSON keeps `questions` (not `items`)
- Error codes: `customer.session.required`, `product_qna.rejected`, `bulk_inquiry.rejected`
- No guest fallback on customer QnA submit
- Schema/migrations unchanged; frontend unchanged
