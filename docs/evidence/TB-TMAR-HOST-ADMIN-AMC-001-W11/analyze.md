# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W11

## Structured state (pre-migrate)

| Field | Value |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; extend `Variants/` capability beside Attributes/*) |
| Ownership-State | MUST_SPLIT (mixed Attribute Host file: variants + category-change) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION |
| Oversized/God-File-State | CatalogAttributeEndpoints.cs — five variant routes + category-change |
| Localization-State | EXCEPTION_MESSAGE_BASED on variant surface (IOE Persian title + generic catalog.variant*.invalid) |
| API-Result-Pattern-State | RAW_RESULTS / AD_HOC (`Results.Json`, PlatformHttpException ToError) |
| Stable-Error-Code-State | STRING_HEURISTIC — failures collapse to catalog.variant_axes/variant/preview/apply/readiness.invalid + ex.Message |
| Logging-State | CANONICAL |
| Sensitive-Logging-State | NONE |
| OpenTelemetry-State | CANONICAL |
| Correlation-Trace-State | CANONICAL |
| CQRS-State | MISSING on five variant routes |
| Validator-Coverage-State | GAPS (none on variant HTTP) |
| Contracts-Boundary-State | Host injects `IOfferLookupGateway` (Offer.Contracts) directly into endpoints — move behind Catalog Application port |
| Cross-Module-Coupling-State | LEGAL_CONTRACTS_ONLY once Infrastructure adapter wraps Offer.Contracts; Catalog→Offer.App/Infra/Domain ZERO |
| Cross-Module-Join-State | NONE (counts via gateway; no Catalog DB join to Offer) |
| Persistence-Ownership-State | HOST_OWNED HTTP → CatalogDirectory (correct once focused `IProductVariantDirectory`) |
| Endpoint-Ownership-State | HOST_OWNED (W11 target: MODULE_OWNED) |
| Host-Residue-State | Partial file retain for category-change only (W12) |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW–MEDIUM (typed error codes replace generic variant*.invalid + ex.Message; OfferCount/ReferencedByOffers preserved) |
| Canonical-Reference-Used | W10 ProductValues; ApiResponseFactory; CatalogErrorCodes; Offer.Contracts IOfferLookupGateway |
| Final-Disposition | READY_TO_MIGRATE |

## Target

Five Admin routes under `/v1/admin/catalog/products/{productId}`:

1. PUT `/variant-axes`
2. GET `/variants/editor`
3. POST `/variants/preview`
4. PUT `/variants/apply`
5. GET `/variants/readiness`

## Responsibility map

- DOMAIN_RULE: IsVariantAxisAllowed; active definitions/options; effective schema IsVariantAxis; MaxVariantCombinations=200; fingerprint; archive-not-hard-delete; single-default; archived cannot be default
- APPLICATION_USE_CASE: set axes; editor; preview; apply (+ history); readiness; Offer enrichment orchestration
- PERSISTENCE: Products + ProductVariantAxes + Variants + AttributeValues + AttributeDefinitions/Options + Primary category + LocalizedTexts + ProductHistory
- INTEGRATION_ADAPTER: Offer count lookup via Contracts-only port (`IVariantOfferLookup` → Offer.Contracts gateway)
- HTTP_ENDPOINT: five Admin routes → Catalog.Endpoints
- AUTHORIZATION_ADAPTER: ICatalogAdminAuthorizer
- ACTOR_BINDING: CatalogActorRequestBinding (W10 pattern) for history on apply
- RETAIN Host: category-change-preview + primary-category

## Ownership

Catalog owns variant-axis configuration, editor/readiness, preview, apply matrix, Catalog-side persistence/orchestration, stable variant errors, five HTTP routes. Offer owns Offer data/count authority. Catalog must NOT query Offer persistence.

## Illegal dependencies (current Host variant surface)

- Endpoints → ICatalogDirectory
- Endpoints → IOfferLookupGateway
- PlatformHttpException catch + Results.Json
- InvalidOperationException message → catalog.variant*.invalid
- Enum.TryParse throw IOE on invalid patch status
- Host AdminPanelAccess instead of ICatalogAdminAuthorizer

## Offer boundary plan

- Application port `IVariantOfferLookup.CountOffersByCatalogVariantIdsAsync`
- Infrastructure adapter depends on `Tooba.Offer.Contracts` only
- Handlers enrich editor OfferCount, preview ReferencedByOffers, apply OfferCount
- Endpoints inject neither Offer nor directories
- Catalog→Offer.Application/Infrastructure/Domain = ZERO

## Target paths

```
Application/Variants/{Commands,Queries,Models,Ports,Validators}
Infrastructure/ProductVariantDirectory.cs
Infrastructure/Adapters/VariantOfferLookupAdapter.cs
Endpoints/Admin/Variants/CatalogProductVariantAdminEndpoints.cs
```

## Migration order

1. Disposition map (this folder)
2. Error codes + catalog + resx
3. Port + ProductVariantDirectory (Result + transaction) + Offer adapter
4. CQRS + validators
5. Catalog.Endpoints map + actor bind + module registration
6. CatalogDirectory thin wrappers (one-way delegate)
7. Strip five variant routes/records/Offer helpers from Host; relocate Seller SetProductVariantAxesRequest
8. Repair W8–W10-R1 retention assertions; add W11 guards
9. Evidence + SoT
10. Focused builds/tests → commit → push

## Final disposition

`READY_TO_MIGRATE` — five-route Variant Axes + Variant Matrix Admin slice only. Do not start W12.
