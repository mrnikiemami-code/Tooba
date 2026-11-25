# Proposal — offer-workspace-architecture-closure-296-r9-r1

## Why
The senior architect quality gate found the R9 functional repair valuable but architecture REPAIR_REQUIRED: an oversized monolithic workspace, feature-local query-param locale override with deep admin-language imports, duplicated create/apply orchestration in route pages, and a public language endpoint reusing admin DTO/query.

## What changes
- Split workspace orchestration into cohesive action/local-detail modules so the working component is below the existing source-size threshold; no behavior change.
- Replace feature-local locale override/deep import with the shared registry-backed locale layer and actual `/ru` route prefix support through shared middleware/routing; remove feature query parameter authority.
- Add a dedicated public active-language query/DTO owned by Localization.
- Extract create-and-apply orchestration to a shared feature use case with full reread value matching.
- Record unresolved owning-domain gaps (channel authority, pre-create inventory provisioning, atomic concurrency) without inventing policy or migrations.
- Verify AQ-01..AQ-08 and preserve R8/R9 behavior.

## Impact
Offer workspace frontend, shared i18n middleware/locale, Localization public read contract, seller offer API split, evidence/docs. No redesign, no Host business code, no migration, no TB-297.
