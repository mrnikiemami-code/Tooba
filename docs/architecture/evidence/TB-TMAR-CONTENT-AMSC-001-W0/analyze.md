# TB-TMAR-CONTENT-AMSC-001-W0 — Analyze

## Verdict

`READY_TO_MIGRATE` — ownership is correct, foundation is ready; four bounded canonical-mechanism
defects must be repaired before structure/certification.

## Scope

`src/backend/Modules/Content/Tooba.Content.*` (Contracts / Domain / Application / Infrastructure /
Endpoints), 89 production files, 5 projects, already `structureCertified: true` under the historical
`TB-TMAR-HOST-CONTENT-AMC-001-R4` lineage.

## Structured state fields

| Field | Value |
|---|---|
| Foundation-State | `FOUNDATION_READY` (5 projects; capability-first Application; solution group `/Modules/Content/`) |
| Ownership-State | `correct` |
| File-Cohesion-State | `COHESIVE` (largest `ContentDirectory.cs` 721 LOC < 800 ceiling; `OVERSIZED_ONLY` watch only) |
| Oversized/God-File-State | `ContentDirectory.cs` 721 LOC `OVERSIZED_ONLY`; `ContentDevelopmentSeed.cs` 551 LOC `OVERSIZED_ONLY`; no `MULTI_RESPONSIBILITY_COHESION_VIOLATION` |
| Localization-State | `HARDCODED_TEXT` (14 hard-coded Persian fault messages) |
| API-Result-Pattern-State | `AD_HOC` (2× `Results.Ok()` void-success bypass) |
| Stable-Error-Code-State | `UNREGISTERED_CODES` (14 hard-coded-message fault sites have no stable code) |
| Logging-State | `CANONICAL` |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` |
| Validator-Coverage-State | `EXHAUSTIVE` (18 validators over 53 endpoint-reachable requests; full matrix at W3) |
| Contracts-Boundary-State | `CLEAN` (Contracts = Errors + Storefront DTO) |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` (Localization.Contracts + Media.Contracts) |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (own `content` schema; `ContentDbContext`) |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT_ONLY` |
| Schema-Migration-State | `UNCHANGED` (10 migrations + snapshot; not touched) |
| Behavior-Preservation-Risk | `LOW` (canonicalization + code relocation only) |
| Canonical-Reference-Used | Offer (`Application/Validation/OfferValidationCodes`), BuildingBlocks (`ApiResponseFactory`, `IErrorCatalogContributor`, `IErrorResourceSet`), Cart/Fulfillment (typed `ContractOperationException`) |
| Structure-Handoff-State | `REQUIRED` |
| Final-Disposition | `READY_TO_MIGRATE` |

## Canonical mechanisms (verified present)

- API result: `Tooba.BuildingBlocks.Presentation.ApiResponseFactory` (`From`, `From<T>`, `Created`, `FromFailure`).
- Error catalog: `ContentErrorCatalogContributor` — **63 descriptors**, one owner per code; zero uncatalogued codes.
- Localization: `ContentErrorResources` + `ContentErrors.resx` / `ContentErrors.fa.resx` (63 keys each).
- Typed faults: `Tooba.BuildingBlocks.ContractOperationException` + `Application/Composition/ContentOperation` seam.
- CQRS: MediatR `IRequest<T>` / `IRequestHandler<,>`; `ISender` in endpoints; `AddToobaCqrsFoundation`.
- Validation codes: `Application/Validators/ContentValidationCodes.cs` (15 codes).
- Host: composition-only (`Program.cs` DI + route map, `ContentDevelopmentSeedHost` thin seam, `ContentModule`).

## Defects (blocking canonical AMSC certification)

### D1 — Hard-coded Persian fault messages (HIGH)

`ContentArticle.cs` lines 191, 280, 296, 298, 300, 302, 304, 306, 308, 310, 312 and
`ContentArticleMediaItem.cs` lines 56, 58, 65 throw `new ContractOperationException("<Persian text>")`.

Because the code is the literal Persian sentence, `ContentOperation` maps it into
`new SemanticError("<Persian text>")`, which resolves to **no** catalog descriptor → wrong/opaque
presentation and a localization violation. The sibling aggregates (`ContentAuthor`, `ContentCategory`,
`ContentTag`, `ArticleComment`) correctly use stable codes.

### D2 — Error-code classes owned inside Domain aggregates (MEDIUM)

`ContentArticleErrorCodes`, `ContentAuthorErrorCodes`, `ContentCategoryErrorCodes`, `ContentTagErrorCodes`
and `ArticleCommentCodes` are declared inside `Domain/Aggregates/*.cs`, while the catalog owner
`ContentErrorCodes` lives in `Contracts/Errors/`. The canonical single-owner model keeps all stable codes
in `Contracts/Errors/ContentErrorCodes.cs` and the catalog contributor in `Endpoints/Errors/`.

### D3 — `Results.Ok()` bypass of the canonical factory (MEDIUM)

`ContentAuthorEndpoints.cs:80` and `ContentCategoryEndpoints.cs:105` return
`result.IsSuccess ? Results.Ok() : api.From(result)` for void commands (204). This bypasses
`ApiResponseFactory` and has no canonical void-success method.

### D4 — Endpoints → Domain coupling (MEDIUM)

`ContentArticleCommentEndpoints.cs` imports `Tooba.Content.Domain.Aggregates` and
`Tooba.Content.Domain.Rules` (uses the `ArticleCommentStatus` enum + a Rules symbol). Endpoints must
depend on Application + Contracts only; the Domain dependency must be removed.

## Non-defects (verified clean)

- Foreign App/Infra/Domain coupling: **ZERO**.
- Cross-module joins: **ZERO**; cross-module boundary is Contracts-only (Localization, Media).
- `Console.WriteLine` / second telemetry / custom correlation / `ex.Message` classification: **ZERO**.
- `ActivitySource` / `traceparent` / parallel correlation: **ZERO**.
- Sensitive logging: **NONE**.
- Host Content folder: **ABSENT**; Host holds only composition seams.
- Solution Explorer: canonical `/Modules/Content/` with all 5 projects.

## W1–W3 plan

- **W1 Migrate** — D1: replace all 14 hard-coded messages with stable codes (extend
  `ContentErrorCodes` + contributor + both `.resx`, preserving the existing Persian text as the `fa`
  resource value). D2: relocate the 5 per-aggregate error-code classes into
  `Contracts/Errors/ContentErrorCodes.cs`. D3: canonicalize void-success. D4: remove Endpoints → Domain.
- **W2 Structure** — verify capability-first shallow trees, path↔namespace exactness, root allowlists,
  solution grouping, no single-file request leaves; add durable structure guard.
- **W3 Certify** — full ARCH-COMPLETE-002 verification, manifest + SoT + Master Recovery checkpoint,
  durable cert guard, focused tests.

## Behavior preservation

Routes, status codes, response shapes, success contracts, schema/migrations, DI, and the observable
Persian error text are preserved (the text simply moves from an exception literal to the canonical `fa`
resource). No business-rule change.

## Evidence

- This file: `docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W0/analyze.md`.
