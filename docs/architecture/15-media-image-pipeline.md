# Tooba — Media & Image Pipeline Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T016
```

Documentation only. No upload APIs, storage, transforms, CDN, UI, schemas, or Shopeiva integration.

```text
Media != Catalog
Media != Content
Media != Page Composition
Media != CDN Vendor
Media != Image Processing Vendor
```

```text
Backend/module boundary != UI boundary
Locale != Market != Currency
```

## A. Core Separation

Media owns: asset lifecycle, asset identity, metadata, transform requests/definitions, derived variants, delivery references, processing state.

Business modules reference Media by **opaque IDs/contracts**. They do not store binaries, vendor SDKs, or raw CDN URLs as the core architecture.

## B. Media Asset

Conceptual `MediaAsset` (not a schema): `MediaAssetId`; tenant/deployment scope; media type; original object reference; content type; file size; width/height; duration; checksum; created at/by; processing status; safety/validation status; metadata.

## C. Original Asset Principle

**Preserve a high-quality original.** Derived variants must not become the only master.

```text
Original → metadata extraction → validation → transform request → derived variant → cache/CDN
```

Variants must be regenerable from the original + transform version.

## D. Asset Types

Candidates: Image, Video, Document, Icon, Logo, future media. Initial implementation may focus on images. Do not overbuild unused types now.

## E. Storage Boundary

Conceptual `IMediaObjectStore` (or equivalent). Backends may later include local/object storage, S3-compatible, cloud blob, future vendor.

Business domains must not depend on vendor SDKs. **No provider choice now.**

## F. Transform Boundary

Conceptual `IImageTransformService` (or equivalent). Transforms may include resize, crop, fit, cover, contain, quality, format, rotation, background, focal-point-aware crop.

**No processing library/provider choice now.**

## G. Variant Model

Derived variant identity dimensions (conceptual): AssetId, width, height, crop/fit mode, format, quality profile, focal point / crop intent, transform version.

Variants must be **deterministic and cacheable**. Avoid duplicate image records with no lineage.

## H. Responsive Images

Architecture must support `srcset`, `sizes`, multiple widths, responsive breakpoints, device-appropriate delivery.

Frontend requests appropriate variants, not one huge original everywhere. Exact breakpoints are **not** locked here.

## I. Modern Formats

Preserve AVIF, WebP, JPEG, PNG, SVG where safe. Format negotiation may depend on client/CDN capability. Do not assume one output format. SVG needs special safety handling.

## J. Focal Point / Crop Intent

Preserve focal point, safe area, preferred crop, subject position. Especially for hero banners, product cards, mobile crops, category tiles, landing sections.

Do not make every frontend component invent crop logic independently. Crop intent lives with the asset (and/or a named transform profile), applied through Media.

## K. Semantic Media Roles

Consumers may attach roles such as ProductPrimary, ProductGallery, VariantImage, BrandLogo, CategoryHero, ContentInline, LandingHero, Banner, Thumbnail, OGImage.

```text
Media owns asset
Consumer owns placement/role relationship
```

Do not put all role semantics inside Media.

## L. Catalog Integration

Catalog may reference primary image, gallery, variant-specific image, swatch, document/manual by Media IDs.

Catalog does not store binaries or processing logic. Media does not own Product semantics. No direct DB joins.

## M. Content Integration

Content may embed/reference Media. Published content must reference approved/available assets. Draft content may reference draft/private assets. Public rendering must not leak private/draft media.

## N. Page Composition Integration

Reusable sections (Hero, Banner, Editorial Card, Category Tile, Brand Grid, Product Carousel) reference Media IDs and **presentation intent**, not arbitrary vendor URLs as core architecture.

## O. Theme / Tenant Branding

Single-Store tenants may have logo, favicon, brand imagery, default OG image, theme backgrounds, campaign assets. Tenant-scoped. One shared publish; no per-tenant asset build. No cross-tenant leakage.

## P. Upload Security

Required: content-type validation; extension vs actual file; size/dimension limits; malware scanning hook; decompression-bomb protection; safe image decoding; SVG sanitization/restriction; metadata stripping policy; tenant/user authorization.

Do not implement scanner/vendor now.

## Q. EXIF / Metadata

Possible: EXIF, orientation, GPS, camera, ICC, copyright.

Privacy-sensitive metadata (e.g. GPS) may need stripping. Preserve orientation/color correctness. Exact retention policy: `NEEDS_LATER_P00_DETAIL`.

## R. SEO Metadata

Support inputs for alt, caption, title, copyright/credit, semantic role, width/height.

Ownership:

- asset-level defaults (optional);
- **placement-level** alt (preferred for meaning);
- localized alt.

Hard principle: the same image can require different alt text in different contexts. One global `AltText` on the asset is insufficient as the only model.

## S. Accessibility

Meaningful alt; decorative-image semantics; known dimensions; reduced layout shift; keyboard-accessible controls; captions/transcripts later for video. Editors guided/validated where alt is required.

## T. Core Web Vitals

Protect LCP, CLS, bandwidth, render latency via: responsive variants; known dimensions/aspect ratio; priority hints only where appropriate; lazy load below fold; **preload for true LCP**; optimized formats; CDN caching.

Do not globally lazy-load LCP images.

## U. CDN Boundary

Delivery URLs generated through a controlled Media delivery service/configuration. Do not scatter vendor-specific URLs across business data.

Need future: CDN host migration, signed/private delivery, cache busting/versioning, tenant host integration. **No vendor choice.**

## V. Public vs Private Media

Classes: public storefront; private/admin-only; draft; future customer-uploaded private documents.

Do not expose everything through a public bucket. Authorization/delivery differs by classification.

## W. Cache Strategy

Derived variants are cache candidates. Prefer **immutable/versioned URLs**. Long-lived CDN/browser cache; transform versioning; new URL/version after content change. Avoid reprocessing the same transformation.

## X. On-Demand vs Pre-Generated Variants

Both on-demand, pre-generated, and **hybrid** remain possible. Consider shared hosting, CPU, first-request latency, CDN hit rate, popular sizes vs rare editorial transforms.

Exact initial mix: `NEEDS_LATER_P00_DETAIL`. Architecture stays hybrid-ready.

## Y. Processing Pipeline

```text
Upload → validation → persist original → metadata extraction → security checks
→ processing state → standard derivatives if needed → available → publish/delivery
```

Idempotent processing jobs. No implementation now.

## Z. Failure Handling

Candidate states (not locked enum): Uploaded, Processing, Ready, Failed, Quarantined, Deleted/Archived.

Rendering must degrade gracefully if a derivative fails (fallback variant, placeholder, skip decorative).

## AA. Deletion Lifecycle

Analyze: soft delete, archive, retention, reference check, orphan cleanup, physical deletion.

Do not immediately delete an asset still referenced by published content/products. Use contracts / reference index / events — **no cross-module table joins**.

## AB. Orphan Detection

Detect unreferenced originals, orphaned variants, failed processing artifacts, stale temp uploads. Cleanup tenant-scoped and safe.

## AC. Duplicate Detection

Checksum/hash may support duplicate upload detection, storage deduplication, integrity.

Physical dedupe ≠ logical asset identity. Do not present dedupe as shared **cross-tenant business ownership** unless security/privacy is safe.

## AD. Versioning / Replacement

Replacement may create a new `MediaAsset` or a new asset version. Published/historical references may need stable version identity.

Avoid mutable binary replacement that silently changes archived, order, or legal evidence. Exact policy: `NEEDS_LATER_P00_DETAIL`.

## AE. Product Image History

Order may snapshot a **Media reference** for historical UI. Must not rely on current Product media if exact historical rendering is required. Do not force Order to copy binaries. Preserve versioned/stable Media references.

## AF. Search Integration

Search may consume primary image **delivery reference** and image availability as **projection**. Search does not own Media truth. Index should not hardcode fragile vendor URLs.

See `docs/architecture/14-search-indexing.md`.

## AG. SEO / Structured Data Integration

SEO may consume primary image, OG image, dimensions, product/article images from Media contracts. Structured-data image URLs must resolve publicly and consistently. SEO does not own Media.

See `docs/architecture/13-seo-architecture.md`.

## AH. AI Integration

Future AI may consume **approved** images/documents where product scope allows. Do not automatically expose all Media.

Need: AI eligibility, authorization, published status, tenant, source relationship. AI media ingestion is a **projection**.

## AI. Analytics

Analytics may capture asset load failures, image performance, hero impressions, media interaction, future video plays. Media is not Analytics truth owner.

## AJ. Admin UX

Professional Media Admin (later): library, upload progress, processing status, crop/focal editor, alt/caption, usage/references, filters, bulk ops, replace/version, preview, responsive preview, tenant isolation.

Do not reduce Media to a raw file list. No UI now.

## AK. Product Authoring UX

When Admin/Seller edits a Product, media management is **inside the Product workspace** even though Media is a separate backend module.

```text
Backend/module boundary != UI boundary
```

Do not force a separate Media CRUD screen for normal product-image workflows. UI orchestrates Catalog + Media contracts.

## AL. Content Authoring UX

Content/Composition editors need integrated pick/upload/crop. Do not expose backend module seams as workflow friction.

## AM. Seller Media

Marketplace seller uploads: authorization, seller-scoped quotas/policies, moderation, product association, quality checks.

Seller-uploaded images must **not** automatically override canonical Catalog media without governance.

## AN. Media Quality Policy

Future configurable rules: min resolution, aspect guidance, background, file-size, compression profile, logo transparency, marketplace product image quality. Do not hardcode policy yet.

## AO. External Media Import

Future sources: supplier feed, seller feed, legacy CMS, remote URL, template/demo assets.

Safe ingestion: download through controlled service → validate → scan → normalize → store internally → record provenance.

Do not hotlink external untrusted media as long-term truth by default.

## AP. Media Provenance

Preserve: uploaded by, import source, supplier, seller, original filename, source reference, created time, license/credit where applicable. No legal claims here.

## AQ. Observability

Telemetry: upload failures, processing latency, transform failures, cache hit/miss where observable, CDN errors, broken references, variant generation volume, storage growth, tenant usage. No sensitive metadata leakage. See `docs/architecture/18-observability-logging-audit.md`.

## AR. Quotas / Abuse

Future: file size quota, storage quota, rate limit, transform abuse protection, dimension limits, request-cost controls. Do not implement now.

## AS. Data Ownership Matrix

Marks: `OWNER` | `REFERENCE` | `PLACEMENT_OWNER` | `CONSUMER` | `PROJECTION` | `NOT_OWNER`

| Fact | Media | Catalog | Content | Page Composition | SEO | Search | Tenant/Theme | Order | AI | Analytics |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Original binary | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Asset metadata | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER |
| Transform variant | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER | CONSUMER |
| Focal point | OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Product primary-image role | NOT_OWNER | PLACEMENT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | PROJECTION | NOT_OWNER | REFERENCE | NOT_OWNER | NOT_OWNER |
| Article placement | NOT_OWNER | NOT_OWNER | PLACEMENT_OWNER | NOT_OWNER | CONSUMER | PROJECTION | NOT_OWNER | NOT_OWNER | PROJECTION | NOT_OWNER |
| Landing hero placement | NOT_OWNER | NOT_OWNER | NOT_OWNER | PLACEMENT_OWNER | CONSUMER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Alt text | OWNER (default) | PLACEMENT_OWNER | PLACEMENT_OWNER | PLACEMENT_OWNER | CONSUMER | NOT_OWNER | PLACEMENT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| OG image | REFERENCE | REFERENCE | REFERENCE | REFERENCE | CONSUMER | NOT_OWNER | PLACEMENT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Search thumbnail | OWNER (delivery) | REFERENCE | NOT_OWNER | NOT_OWNER | NOT_OWNER | PROJECTION | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Tenant logo | OWNER (asset) | NOT_OWNER | NOT_OWNER | NOT_OWNER | CONSUMER | NOT_OWNER | PLACEMENT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER |
| Historical media ref | OWNER (version) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | REFERENCE | NOT_OWNER | NOT_OWNER |
| AI media projection | OWNER (eligibility flag later) | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | PROJECTION | NOT_OWNER |
| Performance analytics | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | NOT_OWNER | OWNER |

Placement-level localized alt is PLACEMENT_OWNER on the consuming domain; Media may store optional asset-level default only.

## AT. Failure Matrix

| Case | Fail upload? | Quarantine? | Fallback? | Retry? | Customer-visible degradation? | Operational alert? |
| --- | --- | --- | --- | --- | --- | --- |
| Invalid upload | Yes | Optional | N/A | No (fix file) | Authoring error only | Low |
| Malware/suspicious | Yes | Yes | N/A | No until cleared | No public | Yes |
| Oversized image | Yes (or reject) | No | N/A | After resize by user | Authoring error | Low |
| Decode failure | Yes | Optional | N/A | No | Authoring error | Medium |
| Transform failure | No (original kept) | No | Placeholder / other size | Yes | Possible missing variant | Yes |
| CDN unavailable | N/A | N/A | Origin/signed fallback if designed | Infra | Possible broken images | Yes |
| Missing original | N/A | N/A | Placeholder | Restore from backup | Yes | Yes |
| Missing variant | N/A | N/A | Next size / original-gated | Generate | Possible quality drop | Medium |
| Draft asset requested publicly | Deny | N/A | Not found / placeholder | N/A | No leak | Medium |
| Cross-tenant asset request | Deny | N/A | Deny | N/A | No | Yes |
| Broken product reference | N/A | N/A | Product without image | Repair refs | Card without image | Medium |
| Deleted referenced asset | N/A | N/A | Placeholder; block hard-delete if published refs | Restore | Yes until repaired | Yes |
| SVG unsafe | Yes | Yes | N/A | After sanitize policy | Authoring error | Yes |
| Remote import failure | Fail import | No | Keep existing asset | Retry job | Authoring/ops | Medium |

Public storefront **fail closed** on draft/private/cross-tenant. Transform failure is not a reason to discard the original.

## AU. Testing Strategy — Architecture Level

Future: tenant isolation, authorization, file validation, malformed images, SVG safety, responsive variants, focal-point crop, idempotent transform, cache/version, missing-variant fallback, draft/private access, product-media and content-media integration, cross-tenant CDN leakage, large-image performance, LCP/CLS safeguards.

No tests now.

## AV. Decision Summary

### RECOMMENDED_FOR_ADR

1. Media is separate from Catalog/Content/Page Composition.
2. Preserve high-quality original assets.
3. Derived variants are deterministic and regenerable.
4. Storage provider behind internal abstraction.
5. Transform provider/library behind internal abstraction.
6. Responsive image variants are first-class.
7. Focal point/crop intent is preserved.
8. Consumer owns semantic placement role; Media owns asset lifecycle.
9. Placement-level localized alt text; one global asset AltText is insufficient.
10. Public/private/draft media are distinct security classes.
11. CDN/vendor URLs do not leak into business-domain ownership.
12. Immutable/versioned delivery URLs preferred for cacheability.
13. On-demand/pre-generated hybrid architecture remains possible.
14. Upload security/validation/quarantine hooks are mandatory.
15. Media architecture protects LCP/CLS and visual quality.
16. Product/Content authoring UX integrates Media without exposing backend module seams.
17. Tenant/seller media isolation is mandatory.
18. Search/SEO/AI consume Media projections/contracts only.
19. Safe deletion/reference/orphan cleanup is required.
20. Media operations require observability and reconciliation.

### NEEDS_LATER_P00_DETAIL

- Exact on-demand vs pre-generated mix
- EXIF retention policy
- Asset replace vs version policy
- Quality-policy numbers
- Delivery host / signed-URL strategy

### DEFERRED

- Implementation, vendor/CDN/library choice, UI, Shopeiva import, AI indexing, final ADR
