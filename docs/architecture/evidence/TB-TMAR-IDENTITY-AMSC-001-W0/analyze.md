# TB-TMAR-IDENTITY-AMSC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — **Analyze only. No production code moved in this wave.**

Baseline: `branch = main`, `HEAD == origin/main == 290953e8`.

Skills: `.cursor/skills/tooba-architecture-analyze/SKILL.md` (V2) +
`.cursor/skills/tooba-architecture-structure/SKILL.md` (structure hand-off authority).

## Target analyzed

`src/backend/Modules/Identity` — 5 projects, **89 `.cs` files (84 hand-written + 5 EF migrations)**,
plus its Host composition seams and its inherited AMC guards / SoT records.

| Project | Files | Notes |
|---|---|---|
| `Tooba.Identity.Application` | 34 | 12 commands + 1 query, 6 validators, 5 DTOs + mapper, 7 ports, 2 options, 2 models, 1 composition seam, 1 validation-code owner |
| `Tooba.Identity.Contracts` | 9 | `Auth/` 3, `Actors/` 1, `Contacts/` 1, `Problems/` 4 |
| `Tooba.Identity.Domain` | 12 | `Aggregates/` 7, `Enums/` 3, `Events/` 1, `Rules/` 1 |
| `Tooba.Identity.Endpoints` | 7 | root module + `Auth/` 5 + `Errors/` 1 |
| `Tooba.Identity.Infrastructure` | 27 | incl. 5 EF migrations; `Otp/` 9, `Persistence/` 7, `Adapters/` 2, + 1 per capability |

## Prior certification context (important)

Identity is **already certified** under `ARCH-COMPLETE-002`:

- `docs/architecture/tmar-current-state.json` → `identityAmc001` =
  `COMPLETE_REFERENCE_PATTERN`, `structureCertifiedUnderArchComplete002: true`,
  `manifestCertified: true`, `microserviceExtractable: true`,
  `foreignAppInfraDomainCoupling: ZERO`.
- `docs/architecture/tmar-module-structure-manifests.json` → module `Identity`,
  `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`.
- Durable guards: `IdentityModuleAmcW1SolutionGuardTests`, `IdentityModuleAmcW2StructureGuardTests`,
  `IdentityModuleAmcW5CertGuardTests`, `IdentityValidatorCoverageGuardTests`,
  `AuthenticationV2CanonicalizationGuardTests`.
- Evidence: `docs/evidence/TB-TMAR-IDENTITY-AMC-001/w5-structure-gate.md`,
  `w6-certification.md`.

**Therefore this AMSC run is a conformance re-audit against the *current* four-skill bar
(the bar CustomerProfile / Cart / Content / BulkInquiry / AddressBook / AccessControl /
Fulfillment / Order / Offer were certified against), not a first-time evacuation.**
The prior certificate used the older folder vocabulary (`Contracts/Problems`) and left the
canonical-semantics gaps listed under *Blockers*. No regression may be introduced in this run.

## Structured state fields

| Field | Value |
|---|---|
| Foundation-State | `FOUNDATION_READY` (5 projects; manifest `structureCertified: true`) |
| Ownership-State | `correct` |
| File-Cohesion-State | `COHESIVE` (largest hand-written file `IdentityLifecycleService.cs` = 546 LOC < 800 ceiling) |
| Oversized/God-File-State | `NONE` (546 / 322 / 277 / 239 LOC hand-written; 3 remaining are EF-generated migrations/snapshot) |
| Localization-State | `CANONICAL_PRESENTATION_WITH_HARDCODED_DOMAIN_FAULT_TEXT` (no `IdentityErrors.fa.resx`) |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `PARTIAL` — 9 catalogued codes; 3 OTP delivery codes emitted as raw `InvalidOperationException` string literals and **unregistered** in the catalog |
| Logging-State | `CANONICAL` (`ILogger<T>` only; no `Console`/`Debug`/file writers) |
| Sensitive-Logging-State | `NONE` (no secret/token/OTP value is logged; OTP delivery stores only outcome + correlation) |
| OpenTelemetry-State | `CANONICAL` (`ToobaTelemetry.Meter` via `OtpDeliveryInstrumentation`) |
| Correlation-Trace-State | `CANONICAL` (no parallel header/AsyncLocal; presentation via `IProblemDetailsContextProvider`) |
| CQRS-State | `COMPLIANT` (13 endpoint-reachable requests, real `IRequestHandler<,>`, `ISender` in Endpoints) |
| Validator-Coverage-State | `EXHAUSTIVE` per the *current* durable guard (6 required present / 7 no-validator-required) — **reclassified stricter below** |
| Contracts-Boundary-State | `CLEAN` (module-boundary semantics only) |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` (one Contracts reference: `Tooba.CustomerProfile.Contracts`) |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (own `identity` schema: `users`, `login_identifiers`, `external_identity_bindings`, `mfa_factor_enrollments`, `auth_sessions`, `auth_challenges`, `password_credentials`, `outbox`) |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` + `GLOBAL_HOST_AUTH_PLATFORM_BOUNDARY` (Architect-accepted) |
| Schema-Migration-State | `UNCHANGED` (no schema/migration edit in scope) |
| Behavior-Preservation-Risk | `LOW` (route/response/status/error-code set preserved; no persistence or domain rule touched) |
| Canonical-Reference-Used | `BulkInquiry.Contracts` (`Errors/` + `Resources/` co-location, `BulkInquiryErrorCodes/CatalogContributor/ResourceSet`), `Offer` (`Contracts/Errors/OfferErrorCodes.cs`), `CustomerProfile` (`Application/Composition/<Module>Operation.cs` catching `ContractOperationException`), `AddressBook/Cart/Content` (capability-first `Application/<Capability>/{Commands,Queries,Validators,Models}`), BuildingBlocks (`Result`, `ApiResponseFactory`, `SemanticError`, `ContractOperationException`, `IErrorCatalogContributor`, `IErrorResourceSet`) |
| Final-Disposition | `READY_TO_MIGRATE` |
| Structure-Handoff-State | `REQUIRED` |

### Folder-Granularity-State

`PROFESSIONAL_SHALLOW` — with two residual deviations:

1. `Tooba.Identity.Contracts/Problems/` is the pre-`ARCH-COMPLETE-002` folder name for the stable
   error-code + catalog + resource-set capability. The current canonical vocabulary is
   `Contracts/Errors/` (Offer, BulkInquiry, AccessControl, Cart, Catalog, Content, Fulfillment,
   AddressBook, CustomerProfile, Order, Party, Media, …). The folder holds **no**
   "ProblemDetails presentation" types — only machine codes, the catalog contributor, the resource
   set, and one typed fault — so the name mis-describes the capability.
2. `Tooba.Identity.Application/Validators/IdentityValidationCodes.cs` sits in a technical-axis root
   while its six consumers are all `Auth/Validators/*`. The code owner should live with the
   capability (`Auth/Validators/`).

No single-file request leaf folders. No technical-axis-first `Commands/`/`Queries/` root.
`Application/{Models,Ports,Options}` are genuinely cross-capability shared folders (used by Auth
use cases **and** by Infrastructure adapters) and stay shared.

## Responsibility map

| Responsibility | Current location | Correct owner |
|---|---|---|
| `/v1/auth` HTTP routes + transport models + tenant/throttle guards | `Endpoints/Auth` | same |
| Stable machine error codes | `Contracts/Problems/IdentityErrorCodes.cs` | same capability, canonical folder `Contracts/Errors/` |
| Error descriptor catalog contributor | `Contracts/Problems/IdentityErrorCatalogContributor.cs` | same capability, canonical folder `Contracts/Errors/` |
| Error resource set + `.resx` | `Contracts/Problems/IdentityErrorResourceSet.cs` + `Contracts/Resources/IdentityErrors.resx` | same; **missing `.fa.resx`** |
| Typed duplicate-identifier fault (public boundary fault) | `Contracts/Problems/IdentityDuplicateIdentifierFault.cs` | same capability, canonical folder `Contracts/Errors/` |
| Auth CQRS requests/handlers | `Application/Auth/{Commands,Queries}` | same |
| Auth transport validators | `Application/Auth/Validators` | same |
| Validation machine codes | `Application/Validators/IdentityValidationCodes.cs` | `Application/Auth/Validators/` |
| Typed-fault → `Result` seam | `Application/Composition/IdentityOperation.cs` | same |
| Shared application ports/options/models | `Application/{Ports,Options,Models}` | same (shared, cross-capability) |
| Identity aggregates/enums/events/rules | `Domain/*` | same |
| Persistence (`IdentityDbContext`, migrations, outbox registration) | `Infrastructure/Persistence` | same |
| Auth/session/challenge/credential lifecycle | `Infrastructure/Sessions` | same |
| OTP login + OTP delivery providers + instrumentation | `Infrastructure/Otp` | same |
| Password hashing | `Infrastructure/PasswordHashing` | same |
| Contact / actor-identifier / external-identity / MFA adapters | `Infrastructure/{Contacts,Adapters,ExternalIdentity,Mfa}` | same |
| Security-event sink | `Infrastructure/SecurityEvents` | same |
| Module DI composition root | `Infrastructure/IdentityModule.cs` | same (root allowlist) |
| Global HTTP auth/session boundary, middleware, throttle seam | `Host/Authentication` | **retained** — `KEEP_AS_GLOBAL_HOST_PLATFORM_BOUNDARY` (Architect-accepted, `docs/architecture/41-authentication-http-boundary.md`) |
| Module composition + route mapping + migration descriptor | `Host/Program.cs`, `Host/Composition`, `Host/Tooba.MigrationRunner` | allowed `HOST_COMPOSITION_ROOT` |

No `MUST_SPLIT` file. No Host business or persistence authority.

## Ownership map (Host references — verified complete set)

| Host location | Reference | Classification |
|---|---|---|
| `Program.cs` L19, L417 | `using Tooba.Identity.Endpoints;` + `app.MapIdentityModuleEndpoints(enableCors: true)` | `ALLOWED_COMPOSITION_ROOT` |
| `Program.cs` L155, L157 | `IIdentityHttpSession` / `IIdentityAuthThrottle` → Host session + throttle seam | `ALLOWED_COMPOSITION_ROOT` (presentation seam wiring) |
| `Program.cs` L185 | `RegisterAuthUserCommand` assembly for `AddToobaCqrsFoundation` | `ALLOWED_COMPOSITION_ROOT` |
| `Host/Composition/ToobaModuleComposition.cs` | `new IdentityModule()` | `ALLOWED_COMPOSITION_ROOT` |
| `Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | `IdentityDbContext` schema descriptor | `ALLOWED_COMPOSITION_ROOT` |
| `Host/Development/MarketplaceDevelopmentBootstrap.cs` | `IdentityDbContext` via `DbContextFactory` for dev bootstrap | `ALLOWED_COMPOSITION_ROOT` (development seeding) |
| `Host/Admin/Development/AdminDevActorBootstrap.cs` | `IIdentityAuthenticationService` + `IdentityDuplicateIdentifierFault` | `GLOBAL_HOST_AUTH_PLATFORM_BOUNDARY` (dev actor bootstrap; classified + guarded by `HostAdminCanon007/Canonical` guards) |
| `Host/Authentication/**` | no Identity Application/Domain/Infrastructure reference | `GLOBAL_HOST_AUTH_PLATFORM_BOUNDARY` — asserted by `AuthenticationV2CanonicalizationGuardTests` |

No Host endpoint, no Host persistence, no Host business policy.

## Current illegal dependencies

**ZERO.**

- No Identity production file references a foreign `*.Application`, `*.Infrastructure` or `*.Domain`.
- The single cross-module project reference is `Tooba.Identity.Application →
  Tooba.CustomerProfile.Contracts` (legal, Contracts-only), used by
  `Application/Auth/Queries/GetAuthMeQuery.cs` (`ICustomerProfileDirectory`, `CustomerProfileSnapshot`).
  This is a genuine **module-boundary** dependency and must remain Contracts-only.
- `Tooba.Identity.Domain → Tooba.Identity.Contracts` exists so the domain can share
  `LoginIdentifierKind`/`OtpPurpose` primitives; **the same shape is used by 8 of the 10 already
  AMSC-certified modules** (Cart, Content, BulkInquiry, AddressBook, CustomerProfile, AccessControl,
  Order, Party). Not a violation.
- `Endpoints` references `Application` + `Contracts` only (no Infrastructure) — asserted by
  `IdentityValidatorCoverageGuardTests.Endpoints_dispatch_through_ISender_not_service_ports`.

## Cross-module join inventory

**NONE.** Every read/write goes through `IdentityDbContext` against the `identity` schema only.
No EF navigation crosses a module boundary. No raw SQL joins module-owned tables.

## CQRS / MediatR gaps

**NONE.** Endpoint → `ISender.Send` → `IRequest<Result<T>>` → real `IRequestHandler<,>`, registered via
`AddToobaCqrsFoundation`. No endpoint touches `IdentityDbContext`, a directory, or an infrastructure
service directly.

## Validation classification matrix

The inherited guard classifies 6 of 13 as `VALIDATOR_REQUIRED` and 7 as
`NO_VALIDATOR_REQUIRED`. The 7 are: `LoginWithPasswordCommand`, `RefreshAuthSessionCommand`,
`LogoutSessionCommand`, `LogoutAllSessionsCommand`, `RequestPasswordResetCommand`,
`CompleteOtpLoginCommand`, `GetAuthMeQuery`.

**Analyze finding — the 7 are *not* equivalent:**

| Request | Transport input beyond server-trusted session state | Verdict |
|---|---|---|
| `LogoutAllSessionsCommand` | none (`UserId` + `IsAuthenticated` from principal) | `NO_VALIDATOR_REQUIRED` — correct |
| `LogoutSessionCommand` | `BearerSessionId` parsed from the `Authorization` header (`Guid.TryParse`) | `NO_VALIDATOR_REQUIRED` — header presence/format is a transport guard, handler already fails closed |
| `GetAuthMeQuery` | none (all members from principal) | `NO_VALIDATOR_REQUIRED` — correct |
| `RequestPasswordResetCommand` | `IdentifierKind` (enum-parsed, invalid kinds intentionally swallowed) + `Identifier` | `NO_VALIDATOR_REQUIRED` — enumeration-safe by design; a validator would change locked behavior |
| `LoginWithPasswordCommand` | `IdentifierKind` + `Identifier` + `Password` from the request body | **should be `VALIDATOR_REQUIRED`** (shape only; must not change the locked 401 collapse) |
| `RefreshAuthSessionCommand` | `RefreshToken` from the body | **should be `VALIDATOR_REQUIRED`** (shape only; must not change the locked 401 collapse) |
| `CompleteOtpLoginCommand` | `Identifier` + `ChallengeId` + `Secret` from the body | **should be `VALIDATOR_REQUIRED`** (shape only) |

**Decision (W1/W2):** add three *shape-only* validators
(`LoginWithPasswordCommandValidator`, `RefreshAuthSessionCommandValidator`,
`CompleteOtpLoginCommandValidator`) using the existing `IdentityValidationCodes` codes, plus
`IdentifierKindRequired` on `LoginWithPasswordCommand`. They must **not** short-circuit the
enumeration-safe collapse: an unknown `identifierKind` still has to reach the handler and return
`identity.authentication.failed` (401), never `identity.validation.failed` (400). Therefore the
`IdentifierKind` rule is **not** added; only `Identifier`/`Password`/`RefreshToken`/`ChallengeId`/
`Secret` presence is enforced. The durable coverage guard is updated to
`9 required present / 4 no-validator-required` with the same runtime assertions.

## Localization findings

**Canonical mechanism present and used for the API error surface:**

- `IdentityErrorResourceSet : IErrorResourceSet` owns the `identity.` key space; registered in
  `IdentityModule.AddServices` as `IErrorResourceSet`.
- `Contracts/Resources/IdentityErrors.resx` holds all 9 catalogued keys.
- `IdentityErrorCatalogContributor` pins HTTP semantics with `LocalizationKey = code`; the fallback
  title is stable English, not user-facing prose.

**Violations / gaps:**

1. **`IdentityErrors.fa.resx` does not exist.** Every other module with a `*Errors.resx` ships a
   `.fa.resx` sibling (AccessControl, AddressBook, BulkInquiry, Cart, Catalog, Content,
   CustomerProfile, Fulfillment, Localization, Media, Offer, OperatorProfile, Order, PageComposition,
   Party, Pricing, ProductQnA, Reviews, Story, Support, UserPreference, Wallet, Wishlist). A Persian
   client therefore falls back to the English descriptor title for all 9 Identity codes.
2. **Hard-coded Persian fault text in `Domain/Rules/LoginIdentifierNormalizer.cs`** (5 sites:
   `"شناسهٔ ورود پس از پیرایش تهی است."`, `"شناسهٔ ورود پس از نرمال‌سازی تهی است."`,
   `"قالب ایمیل برای هویت نامعتبر است."`, `"شمارهٔ تلفن پس از نرمال‌سازی رقم معتبری ندارد."`,
   `"شناسهٔ ملی پس از نرمال‌سازی تهی است."`) plus `"گونهٔ شناسه پشتیبانی نمی‌شود."` in the
   `ArgumentOutOfRangeException`. These messages are never surfaced to a client (every call site maps
   the fault to `identity.validation.failed`), but they are untyped prose in a Domain rule.
3. **Hard-coded English code literals in `Infrastructure/Otp/OtpDeliveryProviderSender.cs`** —
   `"identity.otp.delivery.rate_limited"`, `"identity.otp.delivery.invalid_destination"`,
   `"identity.otp.delivery.unavailable"`, `"identity.otp.delivery.unconfigured"` — raw strings instead
   of `IdentityErrorCodes` constants, and three of the four have **no catalog descriptor**.
4. Persian XML-doc comments throughout Domain/Infrastructure are documentation, not user-facing text.
   **Not a violation** — `AGENTS.md`/repo convention permits Persian doc comments; only *client-facing*
   strings are governed.
5. No `ex.Message`-based classification anywhere.

## API result / error mapping findings

- All 13 endpoints present failures through `ApiResponseFactory` (`api.From(result)`,
  `api.FromFailure(new SemanticError(code))`). No `Results.Problem`, no `new ProblemDetails`, no
  `application/problem+json` literal — asserted by
  `AuthenticationV2CanonicalizationGuardTests.Authentication_problem_helper_uses_canonical_factory_not_a_parallel_pipeline`.
- Register success is an intentional **raw DTO with `201`** (`Results.Json(result.Value, 201)`), not an
  envelope — locked contract, preserved.
- `IdentityAuthHttpProblem` is a thin auth-boundary helper (tenant spoof + throttle) that delegates
  final presentation to the canonical factory; it is not a parallel pipeline.
- **Gaps:**
  - 3 OTP-delivery machine codes are emitted but **not catalogued** → they would fall through
    `SafeErrorMapper` to the generic fallback instead of the intended `400`/`429`.
  - `OtpDeliveryProviderSender` throws **raw `InvalidOperationException`**; classification is done by
    `RequestOtpLoginCommandHandler`'s local `catch (InvalidOperationException)` → the code literal is
    discarded and every OTP delivery failure collapses to `identity.otp.delivery.unavailable`. The
    rate-limited/invalid-destination/unconfigured distinctions are therefore **unreachable** today.
  - `ChangePasswordCommandHandler` classifies with a local `catch (InvalidOperationException)` /
    `catch (ArgumentException)` pair instead of the canonical operation seam, while
    `IdentityOperation.ExecuteAsync` performs the *same* classification for the other handlers —
    two parallel classification paths in one module.
- Duplicate descriptor ownership: **none** (`IdentityErrorCatalogContributor` is the single owner of
  the `identity.*` descriptors; `FoundationErrorCatalogContributor` owns only `customer.session.required`
  which Identity does not register).

## Logging / sensitive-data findings

- Only `ILogger<T>` via `Microsoft.Extensions.Logging`; 9 structured, parameterless event-name
  messages (`identity.register.succeeded`, `identity.login.succeeded|failed`,
  `identity.otp_login.succeeded|failed`, `identity.refresh.failed`). No `Console.WriteLine`, no
  `Debug.WriteLine`, no second logger framework, no string-concatenated messages.
- **No sensitive value is logged**: no password, no refresh token, no access token, no OTP secret,
  no `Authorization` header, no security stamp. `IdentityDuplicateIdentifierFault.NormalizedValue` is
  documented diagnostic-only and is never logged or returned.
- `IdentitySecurityEvent` records carry `EventName` + `UserId` + `OccurredAt` only.
- `OtpDeliveryOutcome` carries an optional `CorrelationId` — a provider-side handle, not a secret.

## OpenTelemetry / correlation findings

- Single telemetry abstraction: `OtpDeliveryInstrumentation` uses `ToobaTelemetry.Meter` and creates
  the counter `tooba.identity.otp.delivery`. No second `ActivitySource`/`Meter`.
- No `StartActivity` bypass, no manual `traceparent` parsing, no `Guid.NewGuid()` used as a
  correlation identity.
- Trace/correlation ids reach `ProblemDetails` through the canonical
  `IProblemDetailsContextProvider`; Identity contributes no parallel context provider.
- No cross-module call in the Identity request path requires `IModuleCallTracer` decoration beyond the
  ordinary Contracts read in `GetAuthMeQuery`.

## File cohesion / splitting plan

`COHESIVE` — **no file requires splitting.** Structural refinements only:

1. `Contracts/Problems/{IdentityErrorCodes,IdentityErrorCatalogContributor,IdentityErrorResourceSet,IdentityDuplicateIdentifierFault}.cs`
   → `Contracts/Errors/*.cs` (namespace `Tooba.Identity.Contracts.Problems` →
   `Tooba.Identity.Contracts.Errors`). `Contracts/Resources/IdentityErrors.resx` stays
   (matches `BulkInquiry`/`Catalog`/`Media`/`OperatorProfile`/`Party`/`UserPreference`/`Wishlist`).
2. `Application/Validators/IdentityValidationCodes.cs` → `Application/Auth/Validators/IdentityValidationCodes.cs`.
3. Delete the four **unreferenced** internal transport records in
   `Endpoints/Auth/IdentityAuthHttpModels.cs` (`RegisterResponse`, `SessionResponse`,
   `AcceptedResponse`, `MeResponse`) — the endpoints serialize the Application DTOs directly
   (`Results.Json(result.Value, 201)` / `api.From`). Verified zero references repo-wide.
4. Normalize 12 duplicate `using Tooba.Identity.Contracts.Auth;` directives across 8 files
   (CS0105 ×12) — pure hygiene, zero behavior change.

No ownership migration, no file split, no new project, no new folder beyond the two renames above.

## Exact target paths / namespaces

```text
Modules/Identity/
  Tooba.Identity.Contracts/        Errors/IdentityErrorCodes.cs
                                   Errors/IdentityErrorCatalogContributor.cs
                                   Errors/IdentityErrorResourceSet.cs
                                   Errors/IdentityDuplicateIdentifierFault.cs
                                   Resources/IdentityErrors.resx
                                   Resources/IdentityErrors.fa.resx        (NEW)
                                   Auth/  Actors/  Contacts/               (unchanged)
  Tooba.Identity.Domain/           Aggregates/ Enums/ Events/ Rules/       (unchanged)
  Tooba.Identity.Application/      Composition/IdentityOperation.cs
                                   Auth/{Commands,Queries,Models,Validators}/
                                   Auth/Validators/IdentityValidationCodes.cs  (moved)
                                   Models/ Options/ Ports/                 (shared, unchanged)
  Tooba.Identity.Infrastructure/   IdentityModule.cs                       (root allowlist)
                                   Authentication/ Contacts/ Adapters/ ExternalIdentity/
                                   Mfa/ Otp/ PasswordHashing/ Persistence/ SecurityEvents/ Sessions/
  Tooba.Identity.Endpoints/        IdentityEndpointModule.cs               (root allowlist)
                                   Auth/ Errors/                           (unchanged)
```

Solution Explorer: `<Folder Name="/Modules/Identity/">` in `src/backend/Tooba.slnx` already groups all
five projects; ordering is `Domain, Contracts, Application, Infrastructure, Endpoints` (same convention
as `UserPreference`, `Story`, `Media`, `Localization`, `Party`, …). **Verified, no change required.**

## Behavior-preservation checklist

| Contract | Must remain |
|---|---|
| Routes / methods | 13 `/v1/auth/*` routes, unchanged |
| Success shapes | register → raw `AuthRegisterDto` + `201`; login/refresh/otp-complete → raw `AuthSessionDto`; accepted/challenge/me → raw Application DTOs; logout → `Result` envelope |
| Status codes | 400 / 401 / 409 / 429 mapping of the 9 catalogued codes |
| Stable error codes | the 9 existing `identity.*` codes byte-identical |
| Error-code semantics | `identity.authentication.failed` still collapses disabled/locked/invalid-kind (enumeration-safe) |
| Validation semantics | validators stay shape-only; no DB/ownership/authorization/business rule enters transport |
| Authorization/session | bearer session id parse, `IsAuthenticated` guards, security-stamp/credential-version checks |
| Business rules | password policy, refresh rotation + reuse detection, challenge lifetime/consume, identifier normalization (trim + kind-specific fold), security-stamp bump on credential change |
| State transitions | `Active/Disabled/Locked`, `Unverified/Verified`, challenge `Succeeded/InvalidOrExpired` |
| Persistence semantics | `identity` schema, table/column/index names, 5 migrations and their `Up/Down` |
| Schema | `UNCHANGED` — no migration added, edited or reordered |
| Telemetry | event names `identity.register.succeeded`, `identity.login.succeeded|failed`, `identity.refresh.failed`, `identity.otp_login.succeeded|failed`, `tooba.identity.otp.delivery` counter, security-event names |
| Correlation/trace | `X-Correlation-Id` + W3C trace ids via the canonical provider |
| Localization keys | the 9 existing `identity.*` resource keys and their English semantics |
| Public Contracts | `Tooba.Identity.Contracts.*` type names/signatures (one namespace rename `Problems` → `Errors`) |
| Frontend | frozen; no DTO/envelope change observable by the storefront |

## Migration order (W1 → W2 → W3)

1. **W1 (Migrate — canonical semantics, behavior-preserving)**
   - add 3 missing OTP-delivery codes to `IdentityErrorCodes` + catalog contributor
     (`rate_limited` → 429, `invalid_destination` → 400 Business, `unconfigured` → 400 Business);
   - `OtpDeliveryProviderSender` throws `ContractOperationException(IdentityErrorCodes.*)` instead of
     raw `InvalidOperationException` + string literal;
   - `IdentityOperation.ExecuteAsync` gains `catch (ContractOperationException ex)` →
     `SemanticError(ex.Code)` (the CustomerProfile canonical shape);
   - `RequestOtpLoginCommandHandler` and `ChangePasswordCommandHandler` drop their local try/catch
     classification in favour of the single operation seam;
   - replace the 6 hard-coded Persian fault messages in `LoginIdentifierNormalizer` with
     `IdentityErrorCodes.ValidationFailed` (diagnostic message only; exception types unchanged so every
     existing `catch (ArgumentException)` still behaves identically);
   - add `Resources/IdentityErrors.fa.resx` (Persian titles for all 12 keys);
   - remove the 12 duplicate `using` directives.
2. **W2 (Structure)**
   - `Contracts/Problems/` → `Contracts/Errors/` with namespace rename and 32-file `using` update;
   - `Application/Validators/IdentityValidationCodes.cs` → `Application/Auth/Validators/`;
   - delete the 4 unreferenced `IdentityAuthHttpModels` response records;
   - add 3 shape-only validators and reclassify the coverage manifest to
     `9 required present / 4 no-validator-required`;
   - verify `/Modules/Identity/` solution grouping + path↔namespace exactness;
   - add the W2 structure guard test.
3. **W3 (Certify)**
   - add `IdentityModuleAmsc001W3CertGuardTests` (manifest lock, SoT lock, `Errors/` folder,
     `.fa.resx` presence + parity, catalog uniqueness, operation-seam single path, solution grouping);
   - promote/refresh the Identity records in `tmar-current-state.json` and
     `tmar-module-structure-manifests.json`;
   - update `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md`;
   - run the focused + full Host test suites and the Identity-related guards;
   - commit + push.

## Verification plan

- `dotnet build src/backend/Tooba.slnx` — 0 errors.
- Focused: `IdentityModuleAmcW1SolutionGuardTests`, `IdentityModuleAmcW2StructureGuardTests`,
  `IdentityModuleAmcW5CertGuardTests`, `IdentityValidatorCoverageGuardTests`,
  `AuthenticationV2CanonicalizationGuardTests`, `IdentityFoundationTests`,
  `IdentityLifecycleTests`, `AuthenticationHttpTests`, `AuthSecurityHttpTests`,
  `OtpDeliveryProviderTests`, `StorefrontAccountIdentityTests`, `CheckoutIdentityContractTests`,
  `ArchitectureBoundaryTests`, `HostAdminCanon007GuardTests`,
  `HostAdminCanonicalCertificationGuardTests`, `HostAdminAmcW29PwIdentityGuardTests`,
  `HostAdminAmcCheckoutIdentityGuardTests`.
- Broad: `dotnet test src/backend/Host/Tooba.Host.Tests`.
- The 12 CS0105 warnings must be gone; no new warning introduced.

## Certification blockers (must be closed in W1/W2/W3)

1. `Contracts/Problems/` non-canonical capability folder name (pre-`ARCH-COMPLETE-002` vocabulary).
2. Missing `IdentityErrors.fa.resx` (no Persian localization for the 9 catalogued codes).
3. Three OTP-delivery machine codes emitted but not catalogued; emitted as raw string literals.
4. Raw `InvalidOperationException` + `ex.Message`-free-but-string-code classification in
   `OtpDeliveryProviderSender`, and a duplicate classification path in
   `ChangePasswordCommandHandler` (two parallel seams in one module).
5. Six hard-coded Persian fault messages in a Domain rule.
6. `IdentityValidationCodes.cs` in a technical-axis root instead of its capability.
7. Four unreferenced transport response records in `Endpoints/Auth/IdentityAuthHttpModels.cs`.
8. Twelve duplicate `using` directives (CS0105).
9. Validator coverage: 3 endpoint-reachable requests with real transport input classified
   `NO_VALIDATOR_REQUIRED`.

**No blocker requires an architecture, product, schema or security decision.
No blocker requires touching Host. Final-Disposition = `READY_TO_MIGRATE`.**
