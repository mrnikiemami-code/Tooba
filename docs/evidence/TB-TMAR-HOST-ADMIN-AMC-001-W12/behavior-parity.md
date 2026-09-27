# Behavior parity — W12

Preserved:

- Exact methods/paths: POST category-change-preview, PUT primary-category
- Admin authorization via ICatalogAdminAuthorizer
- Actor binding for history
- Preview locale default fa-IR
- Preview/replace JSON success shapes (`CategoryChangeImpactReport` / `CategoryChangeImpact`)
- Level-3 assignability; orphan detection; newly required; variant impact; Additional promotion; readiness blockers; safety unpublish
- Same-primary no-op
- Variants not hard-deleted
- Tenant isolation via Catalog DbContext

Documented typed normalization: failure envelope moves from Host `{title,errorCode}` to ProblemDetails via ApiResponseFactory (same pattern as W7–W11).
