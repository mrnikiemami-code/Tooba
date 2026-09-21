# Behavior Preservation Audit

Baseline: 247865f73d498d6fe3d1857e2007644412524cdc

## Preserved
- Route families: seller/admin/customer fulfillments; customer/seller/admin returns
- Success response shapes via ApiResponseFactory.From → raw JSON value (same as prior Results.Json success)
- Seller auth semantics (global + category all-lines + missing/forbidden)
- Admin fulfillment/returns grid/work-queue query shapes
- Refund destination parse defaults to OriginalPayment when blank

## Status-code notes
Expected business failures now go through central ProblemDetails (SemanticError catalogs) instead of ad-hoc `{title,errorCode}` JSON; success payloads unchanged.

## Accidental behavior change target
0 (within repair scope)
