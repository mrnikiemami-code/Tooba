# Physical tree — Content (TB-TMAR-HOST-CONTENT-AMC-001-R3)

Non-generated production layout after structural repair:

## Tooba.Content.Contracts
- Errors/ContentErrorCodes.cs
- root .cs: NONE

## Tooba.Content.Domain
- Aggregates/: ArticleComment, ContentArticle (+ status enum), ContentArticleHistoryEntry, ContentArticleMediaItem (+ body rules), ContentAuthor, ContentCategory, ContentTag
- Rules/: ArticlePublicationReadiness (+ codes/rules), ContentArticleLifecycleRules, ContentArticlePublicRules, ContentArticleSeoRules, ContentCategoryTreeRules, ContentTaxonomySeoRules
- root .cs: NONE

## Tooba.Content.Application
- Commands/<UseCase>/, Queries/<UseCase>/, Models/, Ports/, Validators/
- Composition/ContentOperation.cs
- root .cs: NONE

## Tooba.Content.Infrastructure
- ContentModule.cs (composition + ContentOutboxRegistration)
- Directories/*Directory.cs
- Development/ContentDevelopmentSeed.cs
- Adapters/, Grid/, Persistence/, Migrations/
- root .cs: ContentModule.cs ONLY

## Tooba.Content.Endpoints
- ContentEndpointModule.cs, GlobalUsings.cs
- Admin/, Storefront/, Errors/, Resources/

No duplicate/stale physical copies of migrated types at legacy roots.
