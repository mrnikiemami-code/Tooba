# TB-TMAR-CONTENT-AMSC-001-W1 — Migrate

## Verdict

`READY_FOR_CERTIFICATION` (behavior-preserving canonicalization). All four W0 defects repaired.

## Changes

### D1 — Hard-coded Persian fault messages → stable codes

14 throw sites now use stable machine codes instead of Persian literals:

| File | Codes introduced |
|---|---|
| `Domain/Aggregates/ContentArticle.cs` | `ArticleInvalidAuthorDisplayName`, `ArticleInvalidOptionalField`, `ArticleInvalidSlug`, `ArticleInvalidTitle`, `ArticleInvalidExcerpt`, `ArticleInvalidBody`, `ArticleInvalidLocale`, `ArticleInvalidSeoTitle`, `ArticleInvalidSeoDescription`, `ArticleInvalidCategory` |
| `Domain/Aggregates/ContentArticleMediaItem.cs` | `ArticleInvalidGalleryAltText`, `ArticleInvalidGalleryCaption`, `ArticleInvalidGalleryMetadata` |

13 new codes added to `Contracts/Errors/ContentErrorCodes.cs`, registered in
`Endpoints/Errors/ContentErrorCatalogContributor.cs` (`ErrorClassification.Validation`, HTTP 400), and
resourced in both `ContentErrors.resx` (English) and `ContentErrors.fa.resx` (**original Persian wording
preserved verbatim**). Catalog now owns 76 descriptors, one per code.

### D2 — Single canonical error-code owner

Removed the five Domain-local code classes (`ContentArticleErrorCodes`, `ContentAuthorErrorCodes`,
`ContentCategoryErrorCodes`, `ContentTagErrorCodes`, `ArticleCommentCodes`). All stable codes now live
only in `Contracts/Errors/ContentErrorCodes.cs`; every reference re-pointed (Domain aggregates, Domain
Rules, Infrastructure directories, Application, and Host tests). `Tooba.Content.Domain` now references
`Tooba.Content.Contracts` (established precedent: AccessControl / AddressBook / Cart / Identity).

### D3 — Canonical void-success

`ContentAuthorEndpoints.DeactivateAsync` and `ContentCategoryEndpoints.ArchiveAsync` now return
`api.From(await sender.Send(...))` — `ApiResponseFactory.From(Result)` maps success to 204, exactly the
previous behavior, with no `Results.Ok()` bypass.

### D4 — Endpoints → Domain coupling removed

`ContentArticleCommentEndpoints` no longer imports `Tooba.Content.Domain.*`. The `ArticleCommentStatus`
enum moved to `Contracts/Enums/ArticleCommentStatus.cs` (stable boundary contract, canonical
`Contracts/Enums` precedent from AccessControl). `ArticleCommentModels`, `IArticleCommentDirectory`,
`ListArticleCommentsQuery` and `ArticleCommentDirectory` re-pointed to `Contracts.Enums`. Persisted
column semantics unchanged (`HasConversion<string>()`).

### Additional canonical hygiene

- `ContentArticleBodyRules` moved out of `Aggregates/ContentArticleMediaItem.cs` (second type in an
  aggregate file) into its own cohesive `Domain/Rules/ContentArticleBodyRules.cs`.
- `ContentValidationCodes`: removed an accidental duplicate `MediaAssetIdRequired`; added
  `CommentStatusInvalid` for the endpoint enum-parse failure path.

## Behavior preservation

- Routes, HTTP methods, success status codes (204 void success, 201 created, raw DTO JSON) unchanged.
- Stable error codes unchanged for all pre-existing codes; observable Persian messages preserved via
  the canonical `fa` resource (the text simply moved from an exception literal to the resource).
- Schema, migrations, DI, permissions, and telemetry untouched.

## Validation

- `dotnet build src/backend/Tooba.slnx` → **succeeded**, 0 errors.
- `dotnet test Tooba.Host.Tests --filter ~Content` → **54 passed, 0 failed, 14 skipped** (Postgres
  Testcontainers integration tests skipped in this environment).
- Architecture guards (`HostContentAmc*`) pass.

## Test maintenance (justified, non-weakening)

Six assertions in `ContentCategoryTreeRulesTests` / `ContentArticleCommentModerationTests` still expected
`InvalidOperationException` from the pre-R2 era; the domain correctly throws the canonical typed
`ContractOperationException` (as asserted by `HostContentAmcR2GuardTests`). Assertions updated to the
canonical exception type and the canonical `ContentErrorCodes.*` symbols — same failure semantics,
stronger canonical alignment.

## Residual debt

None blocking. `ContentDirectory.cs` (721 LOC) remains `OVERSIZED_ONLY` single-responsibility below the
800 LOC ceiling.
