# Canonical mechanisms audit — AccessControl (W0)

## Result / error mapping — `CANONICAL`

- Application handlers return `Result` / `Result<T>` (`IRequest<Result<...>>`).
- Expected failures are mapped once by `Application/Composition/AccessControlOperation.ExecuteAsync`
  into `Result.Failure<T>(new SemanticError(ex.Code))`; unknown exceptions propagate.
- Endpoints use only `ApiResponseFactory`:
  - `api.From(...)` in all three audience endpoint files,
  - `api.FromFailure(new SemanticError(AccessControlErrorCodes.SellerDevUnavailable))` for the
    non-Development short circuit in `SellerDevContextEndpoints`.
- Zero `Results.Json` / `Results.BadRequest` / `Results.Problem`, zero local `ProblemDetails`
  builder, zero endpoint `catch`-and-map block.
- `Endpoints/Errors/AccessControlHttpErrors.cs` exists but is **not referenced by production code**
  (only by a negative guard assertion). Recorded as a cohesion candidate for W1/W3 review; it is not
  a parallel mapping path in use.

## Stable error codes — `CATALOGUED` with one literal-usage defect (F2)

- Owned vocabulary: `Contracts/Errors/AccessControlErrorCodes.cs` (20 codes; 18 `access.*` plus
  `seller.dev.unavailable` / `seller.dev.not-ready`).
- Descriptor ownership: exactly one contributor —
  `Endpoints/Errors/AccessControlErrorCatalogContributor.cs` — registers all 20 codes with
  `Code`, `Classification`, explicit `HttpStatus`, `LocalizationKey` (= code), `Severity`,
  `SafeTitleFallback`. No second contributor registers any `access.*` or `seller.dev.*` code.
  Composed catalog therefore has exactly one descriptor owner per code.
- **Defect:** `Infrastructure/Directories/AccessControlDirectory.cs` writes 20 codes as raw string
  literals instead of the owned constants (see `cohesion-balance.md`).
- No `code.Contains(...)` heuristic and no `ex.Message` classification anywhere in the module.

## Localization — `CANONICAL`

- `Endpoints/Resources/AccessControlErrorResources.cs` provides `AccessControlErrorResourceSet`
  (`IErrorResourceSet`, owns the `access.` prefix) plus the `AccessControlErrorResources.Manager`
  marker.
- `AccessControlErrors.resx` and `AccessControlErrors.fa.resx` carry every owned key with matching
  names (`access.role.*`, `access.assignment.*`, `access.ceiling.*`, `access.scope.*`,
  `access.owner.*`, `access.escalation.*`, `access.validation.*`, `access.permission.*`,
  `access.authorization.*`, `access.capability.*`, `seller.dev.*`).
- No endpoint parses `Accept-Language`; no `IRequestLocaleResolver` bypass found.
- Persian text in production `.cs` files appears only inside XML-documentation comments. No
  hard-coded user-facing Persian or English message exists in Domain/Application/Infrastructure/
  Endpoints (values are supplied by `.resx` / `SafeTitleFallback`).

## Logging — `CANONICAL`

- `ILogger<T>` only. Zero `Console.WriteLine`, `Debug.WriteLine`, or third-party logger framework.
- Zero second telemetry pipeline.
- No sensitive-data logging: no password, token, OTP, reset secret, `Authorization` header, cookie,
  session secret, security stamp or payment payload is logged in the module.
- `Infrastructure/Observability/AccessControlInstrumentation.cs` (25 LOC) is a module meter facade
  over the shared `ToobaTelemetry` `Meter`; no private `ActivitySource`/`Meter` instance.

## OpenTelemetry / correlation — `CANONICAL`

- No custom correlation header, middleware, or raw `AsyncLocal`; the canonical
  `ICorrelationIdProvider` (`X-Correlation-Id`) is used.
- No `ActivitySource.StartActivity(...)` call in Application or Endpoints.
- No manual `traceparent` parsing.
- The module issues no synchronous cross-module *call* requiring `IModuleCallTracer` decoration; its
  foreign interaction is a Catalog-implemented Contracts port (in-process DI), so trace continuity is
  carried by the ambient Activity.

## Clock / identifiers

`AccessControlDirectory.cs` uses `DateTimeOffset.UtcNow` and `Guid.NewGuid()`. This matches the
prevailing repository convention for directory implementations (`CatalogDirectory`,
`CartDirectory` use `DateTimeOffset.UtcNow`; none of the directories use `Guid.NewGuid`).
`IClock`/`IIdGenerator` are used in the newer `Adapters/*DevelopmentSeedGateway` style (Offer,
Pricing). Adopting `IClock`/`IIdGenerator` here would be a determinism improvement but is **not** a
canonical-mechanism violation and is out of scope for a behaviour-preserving AMSC run (it would
change id generation from `Guid.NewGuid` to UUIDv7 and change timestamp sourcing).

## CQRS — `COMPLIANT`

- 20 `IRequest` / `IRequest<Result<...>>` records, 20 real `IRequestHandler<,>` implementations.
- Endpoints inject `ISender` (60 references) and never touch persistence or the directory.
- Registration via `AddToobaCqrsFoundation` (MediatR 12.5.0) — no second pipeline.

## Typed cross-module fault mechanism

The repository has **two** canonical expected-failure mechanisms:

1. `Tooba.BuildingBlocks.ContractOperationException` — used by Payment, Content, Inventory, Wallet
   and Order for cross-module/directory boundary faults, consumed via
   `catch (ContractOperationException ex) when (ex.Code == ...)`.
2. Module-local stable-code exceptions mapped once into `Result` — AccessControl's
   `AccessControlException` + `AccessControlOperation.ExecuteAsync` implements this shape.

`AccessControlException` is the module-local analogue, not a parallel invention. It carries no
message text (constructor is `AccessControlException(string code)` only, asserted by
`AccessControlModuleAmcW2SemanticGuardTests`), which is exactly the required "stable code, not prose"
contract. Its structural overlap with `ContractOperationException` is recorded as a
consolidation opportunity, not as a violation.
