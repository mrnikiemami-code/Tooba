# Tooba — Content & Page Composition Architecture

Status:

```text
P00 architecture design — candidate for later ADR; not an ADR lock
```

Task:

```text
TB-P00-T013
```

Documentation only. No CMS, editor, routes, SEO engine, or AI ingestion code.

```text
Content != Blog
Content != Page Composition
Content != Catalog
Content != Media
Content != SEO Engine
```

```text
Content = semantic/editorial knowledge and publishable material
Page Composition = arrangement/configuration of approved sections/components for a page experience
```

Blog is a content type or presentation pattern, not the root architecture.

## B. Content Capability Scope

Content covers Article, Buying Guide, News/Editorial, FAQ, Policy/Help, Brand/Category editorial, Campaign copy, Landing **content**, Knowledge article, and future types.

Do not create a new module/table per page type. Types are registrations on one Content capability.

## C. Content Type Model

Conceptual: Content Type; Content Item; structured fields/payload; relationships; taxonomy; locale variant; publishing state; version/revision; SEO metadata **input/reference**; knowledge eligibility.

Avoid one untyped JSON blob as the whole model. Structured types with validation. Storage not locked.

## D. Multilingual Content

Do **not** use `TitleFa` / `TitleEn` / `TitleAr` columns.

Locale-aware variants: source locale, translation status, fallback, localized slug/title/body/metadata, completeness. Independent per-locale publish is `NEEDS_LATER_P00_DETAIL`.

```text
Locale != Market != Currency
```

Localization is not pricing.

## E. Publishing Lifecycle

Candidate (not locked): Draft, In Review, Approved, Scheduled, Published, Unpublished, Archived.

Need: publish-at / unpublish-at, author/editor, reviewer, revision, rollback, publication history.

Live published copy must not be the only editable copy.

## F. Versioning / Revisions

Immutable revision history; current draft vs published revision; rollback; compare; audit. Do not force event-sourcing. Working copy ≠ live.

## G. Scheduling

Schedule publish/unpublish. Scheduler is platform; Content owns intended times and resulting states. Fail closed if clock/job fails (remain unpublished).

## H. Taxonomy

Tags/categories for editorial navigation and related content. Not Catalog category (commercial taxonomy). Mapping via opaque ids/contracts if needed.

## I. Content Relationships

Related articles, FAQ attached to product **by id**, series. No ORM navigation into Catalog tables.

## J. Structured vs Rich Content

Typed fields + constrained rich text. Not arbitrary HTML/JS as domain model. Sanitization at composition/render.

## K. Page Composition

Owns layout trees: page instance, section instances, ordered slots, bound content/media/catalog **ids**, theme region hints.

Does not own article body source or product identity.

## L. Reusable Section Types

Approved section catalog (hero, FAQ list, product rail, rich text, CTA). No unbounded executable widgets. New types = registered, not ad-hoc code in content.

## M. Landing Pages

Landings are Page Composition instances binding Content/Catalog/Media. Default is not one-off hardcoded pages.

## N. Page Composition vs Routing

Composition is not the HTTP router. SEO/routing package owns URL/canonical/hreflang **technical** policy; Content/Composition supply slugs and metadata **inputs**.

## O. Theme Integration

Theme per store (T003) styles approved sections. Theme does not own content write model. Template (Shopeiva) is not architecture truth.

## P. Catalog Integration

Content/PDP may **reference** Product/Brand/Category ids. Catalog remains product truth. No joins.

## Q. Dynamic Collections

Merchandising/query collections are merchandising or catalog projections, not Content owning product lists as SoT. Composition may bind a collection id.

## R. Content & Search

Search indexes **published** content feeds. Search is not CMS. Unpublished must not leak via search.

## S. Content & SEO

SEO engine owns technical SEO (canonical, sitemap contribution policy, structured data composition). Content provides titles/descriptions/slugs as inputs. Do not lock SEO here (later P00-14).

## T. Content & AI/RAG

AI may retrieve **approved published** knowledge only. Draft/unpublished ineligible. No unrestricted DB access (T000).

## U. Catalog & AI Knowledge

Product facts come from Catalog projections, not by scraping live tables. Content supplements editorial knowledge.

## V. Tenant / Edition Scope

Content/composition scoped to resolved tenant/store. Marketplace vs Single-Store share the capability; marketplace may have seller-scoped editorial later (`NEEDS_LATER`).

## W. Global vs Tenant Content

Platform-global help vs tenant storefront content. Do not mix namespaces. Global ≠ all tenants automatically.

## X. Content Permissions

SpiceDB: author, review, publish, compose pages. Not `IsAdmin`. Identity is not CMS.

## Y. Workflow / Approval

Review/approve before publish. B2B/legal pages may require extra assurance later.

## Z. Preview

Preview draft/scheduled against theme without publishing. Preview tokens fail closed; not public URLs by default.

## AA. Media Integration

Media ids on content/sections. Media owns binaries/derivatives. Placement-level alt and composition presentation intent stay with Content/Page Composition. See `docs/architecture/15-media-image-pipeline.md`.

## AB. Link Management

Internal links by resource id preferred over raw paths. Broken-link handling later.

## AC. Redirect / Slug History

Preserve slug change → redirect. Historical URLs for SEO. Ownership split with SEO package later.

## AD. Personalization / Audience Targeting

Composition may later vary by audience. Not Identity credentials. Default: same page for locale/market context only.

## AE. Cache & Invalidation

Cache published composition by tenant+locale+page revision. Invalidate on publish. No Redis required initially. No cross-tenant cache.

## AF. Content Events

Published, unpublished, revised. Feed Search, AI eligibility, CDN. Outbox-ready.

## AG. Admin UX

Professional authoring, not generic CRUD grids. Preview, locale variants, schedule, section picker from **approved** catalog.

## AH. Page Builder Safety

No arbitrary scripts in sections. Only registered types. Sanitize rich text.

## AI. Accessibility

Authored content and sections must be able to meet a11y (alt via Media, headings, lang). Not optional later for production UX.

## AJ. Performance / Core Web Vitals

Composition must not imply unbounded nested widgets. Media uses transformed variants. Details with frontend package.

## AK. Analytics

Content/page ids as first-party events. Analytics does not own CMS. See `docs/architecture/16-first-party-analytics.md`.

## AL. Content Import / Migration

Preserve import of structured content later. Shopeiva demo copy is not requirements.

## AM. Data Ownership Matrix

| Concept | Owner |
| --- | --- |
| Editorial body / types / locale variants | Content |
| Layout / section instances | Page Composition |
| Product identity | Catalog |
| Assets | Media |
| Search index | Search |
| Technical SEO | SEO (later) |
| Publish permission | SpiceDB |
| Theme chrome | Theme / frontend |

## AN. Failure Matrix

| Case | Direction | Fail closed? |
| --- | --- | --- |
| Unpublished requested publicly | 404/not available | Yes |
| Unknown section type | Skip/fail render policy | Yes for executable |
| Missing catalog ref | Degrade section, not join tables | Yes for data |
| Tenant mismatch | Deny | Yes |
| Draft in search/AI | Must not appear | Yes |
| Scheduler fail | Stay unpublished | Yes |

## AO. Testing Strategy — Architecture Level

Future: locale variants, publish isolation, preview auth, section allowlist, search leak of drafts, tenant isolation. No tests now.

## AP. Decision Summary

### RECOMMENDED_FOR_ADR

1. Content != Blog-only.
2. Content != Page Composition.
3. Typed content model, not only untyped JSON.
4. Locale variants, not TitleFa/TitleEn columns.
5. Draft vs published revisions; no live-only overwrite.
6. Approved reusable sections; no arbitrary executable content.
7. Catalog/Media/Search by contract/id only.
8. AI/RAG from approved published content only.
9. Tenant-scoped storefront content.
10. SpiceDB for author/publish/compose.
11. SEO technical engine separate; content supplies inputs.
12. Theme styles sections; does not own write model.
13. Admin is professional authoring, not skeleton CRUD.
14. Accessibility and performance are production constraints.

### NEEDS_LATER_P00_DETAIL

- Per-locale independent publish
- Seller-authored marketplace content
- Slug/redirect vs SEO ownership split
- Section catalog v1 list
- Scheduler technology

### DEFERRED

- CMS implementation, editor, routes, SEO engine, RAG ingest, Shopeiva import, ADR
