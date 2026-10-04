# TB-TMAR-OFFER-AMC-001 — W3 CERTIFY

- Task: `TB-TMAR-OFFER-AMC-001` (ARCHITECT_DIRECT_AMSC)
- Module: `Modules/Offer`
- Skill: `Certify` (fourth of four)
- Starting base: `a622d59e` (`HEAD == origin/main` after W2)
- Production code changes in this wave: `ZERO` (SoT + evidence only)

## 1. Certification gates

| Gate | Verdict | Evidence |
| --- | --- | --- |
| Module ownership correct | `PASS` | W0 §4 — every Offer responsibility is Offer-owned; no foreign-owned type inside Offer |
| Host residue | `PASS` | only the thin `Host/Security/Seller/HostOfferSellerAuthorizer.cs` adapter; no Host reference inside Offer |
| Endpoint ownership | `PASS` | `OfferEndpointModule.cs` at Endpoints root owns `MapOfferModuleEndpoints`; no Host route maps Offer |
| CQRS / MediatR | `PASS` | 9 commands + 2 queries are real `IRequest<T>`/`IRequestHandler<,>`; `MEDIATR_12_5`; assembly registered in `Program.cs` |
| Validation coverage | `PASS` | 5/5 required validators present (co-located), 1 query `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| Contracts-only boundary | `PASS` | Offer references foreign `*.Contracts` only; foreign modules consume Offer through `*.Contracts` ports only |
| No cross-module persistence/join | `PASS` | single `offer` schema; no foreign `DbSet`, no cross-schema SQL join |
| API result pattern | `PASS` | `ApiResponseFactory` only; no raw `Results.Json`, no local `ProblemDetails` mapper |
| Stable error codes + localization | `PASS` | 17 codes, one `ErrorDescriptor` each, `LocalizationKey == code`, `OfferErrorResourceSet` `offer.*` prefix, `.resx` + `.fa.resx` |
| Logging / telemetry / correlation | `PASS` | `IModuleCallTracer` for every cross-module call; no `Console.WriteLine`; no `StartActivity` in Application/Endpoints; no `DateTime.UtcNow` / `Guid.NewGuid()` bypass |
| Sensitive logging | `PASS` | none |
| Folder granularity | `PASS` | `PROFESSIONAL_SHALLOW` capability-first; zero technical-axis-first Application roots |
| Path ↔ namespace | `PASS` | exact path-derived equality for every production `.cs` |
| Root allowlist | `PASS` | 4 of 5 production projects have no root `.cs`; Endpoints allowlist is exactly `OfferEndpointModule.cs` |
| Visual Studio solution grouping | `PASS` | all six Offer projects under `/Modules/Offer/`, each exactly once |
| Manifest ↔ disk exactness | `PASS` | 1 entry, 5 production projects, 5 root allowlists equal disk, forbidden folders absent |
| Schema preservation | `PASS` | `offer` schema, 3 migrations and `OfferDbContext` model unchanged |
| Durable guards | `PASS` | 6 new facts; 2 stale guards repaired to documented intent; no guard weakened |
| Source of Truth | `PASS` | `offerAmc001` added; `offerArchComplete002Structure` superseded; `completeReferenceModules[Offer]` updated |
| Microservice extractability | `PASS` | lifts with `Tooba.BuildingBlocks` + declared foreign `*.Contracts` only |
| Behavior preservation | `PASS` | routes, status codes, DTO shapes, error codes, validation codes, schema, telemetry unchanged |

## 2. Behaviour preservation — explicit ledger

| Surface | State |
| --- | --- |
| routes `GET/POST /offers`, `GET/PATCH /offers/{offerId:guid}`, `POST|PUT .../price`, `POST|PUT .../inventory` | unchanged |
| HTTP methods, status codes, raw-DTO success shape, `Created` location | unchanged |
| 17 stable error codes + classifications + HTTP statuses + localization keys + `.resx` semantics | unchanged |
| 22 validation codes and messages | unchanged |
| request/response DTO semantics; `OfferReference` / `SellerOfferDetailPage` | unchanged |
| seller authorization semantics (`IOfferSellerAuthorizer.RequireAuthorizedAsync`) | unchanged |
| domain invariants and `Result` failure strategy | unchanged |
| `offer` schema, 3 migrations, `OfferDbContext` model | unchanged |
| `IClock` / `IIdGenerator` usage; telemetry span names and dimensions | unchanged |
| `ReturnPolicyOptions` section `Tooba:ReturnPolicy` and defaults | unchanged |
| `ReturnPolicyResolver` logic | moved verbatim (file boundary only) |

## 3. Coupling ledger (microservice readiness)

| Edge | State |
| --- | --- |
| Offer → foreign Domain / Application / Infrastructure | `ZERO` |
| Offer → foreign `*.Contracts` | legal: Catalog, Inventory, Party, Pricing |
| foreign → Offer | `*.Contracts` only |
| Cross-module joins | `NONE` |
| Host reference inside Offer | `NONE` |

Offer can be extracted as a microservice with only `Tooba.BuildingBlocks` and the four declared
`*.Contracts` edges. No shared database object, no shared `DbContext`, no in-process-only coupling.

## 4. Honest disclosures

1. **`ReturnPolicyResolver` remains in `Tooba.Offer.Contracts`.** The W0 plan proposed
   `Application/ReturnPolicy`. Evidence forced a change: `Tooba.Order.Infrastructure` constructs it as a
   default checkout seam and may only reference Offer through `*.Contracts`
   (`ReturnFoundationTests.Return_is_not_order_and_modules_do_not_reference_each_other_infrastructure`).
   Moving it would have created a foreign Application edge or duplicated logic. The multi-responsibility
   file was split in place instead. Recorded in W1 §2.
2. **Persian display text lives in `Tooba.Offer.Contracts`.** `ResolvedReturnPolicy.LabelFa`,
   `ReturnPolicyOptions` docs and `SellerOfferPanelDtos` carry Persian literals. This is a repo-wide
   `*.Contracts` pattern and is locked by `Host.Tests/OfferReturnPolicyResolverTests` as observable
   behaviour. Domain / Application / Infrastructure are strictly Persian-free and enforced by guard.
   Resource-based conversion is a repo-wide localization decision, deferred as a follow-up (not an AMC
   blocker).
3. **93 pre-existing `Tooba.Host.Tests` failures are NOT repaired by this task.** They reproduce on the
   untouched `HEAD` tree (foreign-module namespace drift in `Tooba.Catalog.Contracts/Cart`, Content
   contract-fault test drift, manifest/slnx/baseline drift). None of the failing test names reference
   Offer. They are outside this task's module scope and are disclosed rather than hidden. Full evidence in
   W1 §4.

## 5. Verdict

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
MANIFEST_DISK_EXACT
MICROSERVICE_EXTRACTABLE
```

`Worker PASS != Architect ACCEPT`. This wave records the technical certification result; Architect ACCEPT
is not claimed here.
