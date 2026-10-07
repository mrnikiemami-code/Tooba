# TB-TMAR-PARTY-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

## Scope

Bounded canonical-mechanism completion for `src/backend/Modules/Party/Tooba.Party.*` + one inbound-boundary repair on the Promotion consumer, starting head `2477bbb3` (W0). Zero ownership moves, zero schema change, zero behavior delta.

## Changes

### 1. Declared-code guard (canonical seam, Media/Inventory/OperatorProfile/PageComposition precedent)

`Tooba.Party.Contracts/Errors/PartyErrorCodes.cs`: added the private `KnownCodes` HashSet (11 declared codes, `StringComparer.Ordinal`) and `public static bool IsKnown(string? code)`. The 10 pre-existing codes and their wire literals are unchanged; the doc-comment header (seller.settings.* Host parity note) is preserved.

### 2. Dual-mechanism typed-fault seam

`Tooba.Party.Application/Composition/PartyOperation.cs`:
- `ExecuteAsync<T>` now catches `ContractOperationException` **when `PartyErrorCodes.IsKnown(ex.Code)`** (→ `Result.Failure<T>(new SemanticError(ex.Code))`) in addition to the unchanged `SemanticException` mapping.
- Added the value-less `ExecuteAsync(Func<Task>)` overload mirroring the accepted OperatorProfile/PageComposition shape.
- Added `ArgumentNullException.ThrowIfNull(action)` guards on both overloads.
- Classification is by typed code only — no message heuristics (`ex.Message` absent). Unknown contract codes and unknown exceptions propagate untouched to the canonical global exception boundary.
- Dormant seam: the module raises no `ContractOperationException` today, so HTTP behavior is identical (same class of accepted no-behavior-delta as PageComposition W1).

### 3. Inbound boundary repair (Promotion → Party.Contracts only)

Defect (from W0): `Tooba.Promotion.Infrastructure.csproj` referenced `Tooba.Party.Application.csproj` and `MerchandisingCampaignDevelopmentSeed.cs` consumed `Tooba.Party.Application.Models/Ports` (`IPartyDirectory`, `IPartyLookup`, `PartyReference` via `CreateOrganizationAsync`). Foreign-Application coupling on the consumer blocks microservice extraction of Party.

Repair (behavior-preserving):
- New `Tooba.Party.Contracts/Ports/IPartyDevelopmentDirectory.cs` — narrow Contracts-side development-seed port (`EnsureDevelopmentOrganizationAsync` returning a new stable `PartyDevelopmentOrganization(PartyId, DisplayName, LegalName)` record; `SearchIdsByDisplayNameAsync`) mirroring semantics the module already publishes through `IPartyDevelopmentSeedGateway`/`IPartyLookup`.
- New `Tooba.Party.Infrastructure/Development/PartyDevelopmentDirectoryAdapter.cs` — adapter delegating to the same Party-owned `IPartyDirectory` (registered in `PartyModule.AddServices` as `IPartyDevelopmentDirectory → PartyDevelopmentDirectoryAdapter`).
- `MerchandisingCampaignDevelopmentSeed.cs`: usings now `Tooba.Party.Contracts.Ports` only; `IPartyDirectory`+`IPartyLookup` pair replaced by the single `IPartyDevelopmentDirectory`; `EnsureOosOfferAsync` signature updated accordingly; the lookup-then-create sequence keeps identical semantics (search first; create when absent).
- `Tooba.Promotion.Infrastructure.csproj`: `Tooba.Party.Application` ProjectReference removed; the `Tooba.Party.Contracts` reference remains.

Party production behavior is unchanged; Promotion's Development-seed behavior is unchanged (same directory operations through the new port).

### 4. Durable guard

New `src/backend/Host/Tooba.Host.Tests/Architecture/PartyModuleAmsc001W1MigrateGuardTests.cs` pins:
- `IsKnown` + `KnownCodes` + all 11 code members + all 11 wire literals + exactly 11 `public const string` declarations;
- dual-mechanism `PartyOperation` mapping, value-less overload, `ex.Message` absence;
- `IPartyDevelopmentDirectory` Contracts surface existence, `PartyModule` adapter registration, Promotion csproj free of `Tooba.Party.Application`, and zero `Tooba.Party.Application` text in any Promotion.Infrastructure source file.

## Behavior-Preservation Checklist

- Routes/methods/shapes/status codes: unchanged (0 endpoint files touched).
- Stable error codes + descriptor registration + resx keys: unchanged.
- Validation semantics: unchanged (validators untouched).
- Authorization seam: unchanged.
- Persistence/schema/migrations: unchanged (0 migration files touched).
- Seed behavior: identical operations via the new Contracts port.
- Host: untouched.

## Focused Validation

- `dotnet build Tooba.Party.Endpoints` (pulls Contracts/Application/Domain/Infrastructure) = 0 errors / 0 warnings.
- `dotnet build Tooba.Promotion.Infrastructure` = 0 errors (5 warnings pre-existing nullable notes; verified unchanged content classes).
- New W1 migrate guard (with full Host.Tests rebuild).
- `PartyModuleAmcW1SolutionGuardTests` + `PartyModuleAmcW2StructureGuardTests` + `PartyModuleAmcW3CqrsGuardTests` + `PartyModuleAmcW4CertGuardTests` re-run as regression evidence.
