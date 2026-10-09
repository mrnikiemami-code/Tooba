# TB-TMAR-STORY-AMSC-001 — Wave 1 (Migrate)

- **Skill:** `tooba-architecture-migrate` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Story/Tooba.Story.*`
- **Starting HEAD:** `0c73390a3211e0ee9057e9234d62d3e4f14b5e4e` (Wave 0)
- **Branch:** `main`
- **Files moved:** 0 — **Files deleted:** 0
- **Observable behavior / API shape / schema changed:** NONE

---

## 1. Source target

`src/backend/Modules/Story` — five projects, 40 production `.cs` files, 2 `.resx`, 5 migration files.
Wave 0 classified the module `FOUNDATION_READY` / `ownership = correct` / `MUST_SPLIT = NONE`, with
exactly four real defects to close: **B1** (validation-code localization gap), **B2** (validator matrix
not exhaustive), **B3** (English XML docs) and the deferred structural handoff **B4** (owned by W2).

Wave 1 closes B1, B2 and B3. It performs **no ownership correction, no file move and no file split**,
because Wave 0 proved there is nothing to relocate — Story already owns every responsibility it
exercises, and Host owns zero Story business files.

## 2. Destination modules

None. This is a re-verification/hardening migration inside the already-certified Story module, not an
evacuation. Host was not touched.

## 3. Foundation readiness

`FOUNDATION_READY` (module is `structureCertified: true` under `ARCH-COMPLETE-002`; all five projects
exist and are capability-foldered). No foundation was created or extended.

## 4–6. Responsibility split / ownership map / files moved

NONE. Every touched file stayed in its owning project and folder; only its documentation and its
localization resources changed.

## 7–9. Files moved / created / deleted / path map / namespace changes

| Action | File |
| --- | --- |
| modified | `Tooba.Story.Contracts/Errors/StoryErrorCodes.cs` |
| modified | `Tooba.Story.Application/Stories/StoryFailureMapper.cs` |
| modified | `Tooba.Story.Application/Stories/Composition/StoryOperation.cs` |
| modified | `Tooba.Story.Application/Stories/Ports/IAdminStoryGridPort.cs` |
| modified | `Tooba.Story.Application/Stories/Validators/StoryValidators.cs` |
| modified | `Tooba.Story.Endpoints/Errors/StoryHttpErrors.cs` |
| modified | `Tooba.Story.Endpoints/Seller/IStorySellerAuthorizer.cs` |
| modified | `Tooba.Story.Endpoints/Resources/StoryErrors.resx` |
| modified | `Tooba.Story.Endpoints/Resources/StoryErrors.fa.resx` |
| modified | `Tooba.Story.Infrastructure/Adapters/AdminStoryGridAdapter.cs` |
| modified | `Tooba.Story.Infrastructure/Grid/StoryAdminGridPolicies.cs` |
| added (test) | `Host/Tooba.Host.Tests/Architecture/StoryModuleAmsc001W1MigrateGuardTests.cs` |
| modified (test) | `Host/Tooba.Host.Tests/Architecture/StoryModuleAmcW5ValidatorGuardTests.cs` |

Namespace changes: **NONE**. No file moved, so no path↔namespace pair changed.

## 10. Contracts reused/created

No new contract. `StoryErrorCodes` (5 stable codes) is unchanged in value and ownership. Two new
**validation** codes were added to the existing Application-owned `StoryValidationCodes` class
(`story.locale.invalid`, `story.market.invalid`) — the same class and the same key prefix the module
already used for its other 8 validation codes.

## 11–13. Illegal references / joins / replacement mechanism

NONE found and NONE introduced. The module still references only `Tooba.BuildingBlocks`,
`Tooba.ModuleContracts` and `Tooba.Persistence`; the W1 guard re-proves this at both the `using` and
`csproj` `ProjectReference` level.

## 14. CQRS / MediatR state

UNCHANGED — 25 real `IRequest<T>` / `IRequestHandler<,>`, all dispatched from Endpoints through
`ISender`. The new validator is discovered by the existing
`AddToobaCqrsFoundation` → `ValidationBehavior` pipeline; no second MediatR pipeline was registered.

## 15. Validation matrix (closed)

Wave 0 carried a per-input provenance matrix and found exactly one gap: `GetPublicStoriesQuery`
classified the caller-controlled `locale` and `market` query strings as `NO_VALIDATOR_REQUIRED`
although both are unbounded text reaching `StoryRules.MatchesLocale` / `MatchesMarket` and the DB
filter. A nullable/optional filter is **not** automatically exempt.

| | Wave 0 | Wave 1 |
| --- | --- | --- |
| endpoint-reachable requests | 25 | 25 |
| `VALIDATOR_REQUIRED` | 15 | **16** |
| `NO_VALIDATOR_REQUIRED` | 10 | **9** |
| validator classes | 16 | 17 |

`GetPublicStoriesQueryValidator` enforces **transport shape only** — empty allowed (meaning "no
filter"), bounded length (`StoryRules.LocaleMaxLength` / `MarketMaxLength`), alphanumeric — and
emits stable machine codes. It owns no authorization, ownership, existence, pricing or domain rule.
`StoryRules.MatchesLocale` / `MatchesMarket` semantics are untouched.

The 9 remaining `NO_VALIDATOR_REQUIRED` rows are unchanged and each still carries no malformable
transport shape: `Guid` route ids, actor ids derived from the authorization seam, and bodyless
state transitions (`Submit`/`Remove`/`Approve`/`Enable`/`Disable`).

## 16. Endpoint ownership

UNCHANGED — 25 routes across `/v1/admin/stories`, `/v1/seller/stories`, `/v1/storefront/stories`.
Host route count `ZERO`.

## 17. Localization state (defect B1 closed)

`Localization-State = CANONICAL` for the touched surface.

Wave 0 found that `StoryValidationCodes` declared 8 stable validation machine codes but only one
(`story.reviewStatus.invalid`) had a resource, so a client rendering `validationErrors` keys saw raw
machine codes for the other 7. The certified sibling **Promotion** resolves exactly this by shipping
its own validation keys in `PromotionErrors.resx` / `.fa.resx`.

Wave 1 adds bilingual entries (EN + FA) for **all 10** declared validation codes and re-verifies the
**5** stable error codes, in the module-owned resource pair:

```text
StoryErrors.resx / StoryErrors.fa.resx
  story.missing                     (existing)
  story.cta.rejected                (existing)
  story.mutation.rejected           (existing)
  story.tenant.missing              (existing)
  story.reviewStatus.invalid        (existing)
  story.title.required              (existing)
  story.ids.required                (added)
  story.itemIds.required            (added)
  story.mediaType.required          (added)
  story.grid.request.required       (added)
  story.rejectionReason.required    (added)
  story.schedule.range.invalid      (added)
  story.locale.invalid              (added)
  story.market.invalid              (added)
```

Descriptor ownership is deliberately unchanged: validation codes travel inside the canonical
foundation `validation.failed` envelope (`SafeErrorMapper.MapValidation` → `MappedSafeError.ValidationErrors`)
and are **not** registered as `ErrorDescriptor` entries — exactly like Returns and Promotion. No stable
error code was renamed, repurposed, duplicated or re-owned; `ErrorDefinitionCatalog` fail-fast
duplicate detection is untouched.

## 18. API result / error mapping state

UNCHANGED — every endpoint still injects `ApiResponseFactory` and returns `api.From(result)` /
`api.Created(location, result)`. Zero `Results.Json/BadRequest/Problem`, zero local `ProblemDetails`
builder, zero `catch`-and-map in endpoints. Adding resx entries cannot change a response shape: the
envelope already carried the machine codes.

## 19. Logging / telemetry state (incl. sensitive-data check)

UNCHANGED — zero `ILogger`, zero `Console/Debug.WriteLine`, zero second telemetry pipeline in the
module. No sensitive value is logged or added to a comment.

## 20. Correlation / trace continuity state

UNCHANGED — no parallel correlation, no `AsyncLocal`, no manual `traceparent` handling.

## 21. File cohesion / decomposition performed

NONE required. Wave 0 found no `MUST_SPLIT`. `Infrastructure/Directory/StoryDirectory.cs` (672 LOC)
remains single-responsibility persistence for one aggregate, below the 800-LOC ceiling, and is
flagged `WATCH` only.

## 22. Host residue / authority

UNCHANGED and preserved. Host owns only the allowed composition root lines and the legitimate
`Host/Security/Seller/HostStorySellerAuthorizer.cs` platform adapter. `src/backend/Host/Tooba.Host/Story`
remains **ABSENT**. Host was not edited in this wave.

## 23. Persistence / schema state

UNCHANGED — one `StoryDbContext`, schema `story`, tables `stories` / `story_items` /
`outbox_messages`. No migration was added, regenerated, reordered or edited. `AddStoryReviewOwnership`
remains the latest migration.

## 24. DI / composition updates

NONE in production. The new validator is picked up by the existing foundation assembly scan; no
registration line changed.

## 25. Behavior-preservation evidence

- Zero production file was moved, renamed or deleted.
- Zero endpoint signature, route, status code, success shape or error code changed.
- The only executable production change is an **additive** validator that rejects previously-unvalidated
  malformed transport text; empty/absent values stay valid, so every previously successful request
  still succeeds.
- Bilingual resource entries are additive; existing keys and values are untouched.
- Documentation-only edits on the remaining 9 files.

## 26. Focused builds / tests

```text
dotnet build src/backend/Modules/Story/Tooba.Story.Endpoints/Tooba.Story.Endpoints.csproj  -> 0 errors
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj                     -> 0 errors
```

## 27. Guards added / updated

- **Added** `StoryModuleAmsc001W1MigrateGuardTests` (6 facts): Persian-doc standard 32 compliance on
  the whole touched production surface; bilingual localization of every declared validation and error
  code; `GetPublicStoriesQueryValidator` provenance closure + exact validator count; typed fault seam
  with zero message-text classification; zero foreign module edge at `using` **and** `ProjectReference`
  level; AMSC W1 SoT record.
- **Updated (tightened)** `StoryModuleAmcW5ValidatorGuardTests`: matrix closed from
  `15 + 10` to `16 + 9`, `GetPublicStoriesQuery` moved to `VALIDATOR_REQUIRED`, and the new validator
  added to the DI-resolution set. No assertion was weakened or removed.
- No baseline was widened.

## 28. Residual debt

- **B4 (structure)** — intentionally deferred to W2: composition-entry placement
  (`Infrastructure/StoryModule.cs` vs `Infrastructure/DependencyInjection/`), the `StoryOutboxRegistration`
  split, `Infrastructure/Directory/` → `Directories/`, and the `Stories/Composition/` + `Stories/StoryFailureMapper.cs`
  technical-axis placement. Any such move must update the manifest, `HostDevelopmentMigrationSeamGuardTests`,
  the Host grid guards and the Story structure/cert guards atomically.
- **B5 (AMSC lineage)** — W2/W3 add the remaining SoT records, `certificationNote` and Master Recovery
  checkpoint.
- **B6 (module-scoped AMSC guard)** — W2/W3 add the structure and certification guards.
- Non-blocking: `Infrastructure/Directory/StoryDirectory.cs` (672 LOC) stays `WATCH`.

## 29. Certification readiness

`READY_TO_STRUCTURE`. Ownership is correct, foundation is ready, B1/B2/B3 are closed with zero
observable behavior change, no illegal coupling exists, and focused builds pass. Physical structure
authority is handed to Wave 2 (`tooba-architecture-structure`), which must return
`Structure-State = READY_FOR_CERTIFY` before Wave 3 certification.
