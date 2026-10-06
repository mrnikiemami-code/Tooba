# TB-TMAR-OPERATORPROFILE-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

## Verdict

`READY_FOR_STRUCTURE` — canonical mechanism completion, zero ownership moves, zero schema change.

## Canonical seams added

1. **Declared-code guard**: `Contracts/Errors/OperatorProfileErrorCodes.cs` now declares all 6 module codes in a `KnownCodes` set and exposes `IsKnown(string?)` — the certified Media/Inventory precedent. Values unchanged (`operator.profile.rejected`, `operator.profile.validation.{actor_required,display_name,first_name,last_name,bio}`).
2. **Typed-fault seam**: `Application/Composition/OperatorProfileOperation.cs` now maps **both** typed fault mechanisms by declared stable code:
   - `catch (ContractOperationException ex) when (OperatorProfileErrorCodes.IsKnown(ex.Code))` → `Result.Failure<T>(new SemanticError(ex.Code))`
   - `catch (SemanticException ex)` → `Result.Failure<T>(ex.Error)`
   - value-less `ExecuteAsync(Func<Task>)` overload added.
   Unknown contract codes and unknown exceptions propagate untouched to the canonical global exception boundary. No message parsing anywhere.
3. **Structured log-code alignment**: handler failure logs now carry the stable code in a structured placeholder — `logger.LogInformation("{OperatorProfileUpsertEvent}", OperatorProfileErrorCodes.ProfileRejected)` / `{OperatorProfileGetEvent}` — matching the Media W1 precedent; success events unchanged.

## Behavior delta

`ACCEPTED_NO_BEHAVIOR_DELTA` — the module raises only `SemanticException` today (3 aggregate sites + 1 directory guard), so the added `ContractOperationException` mapping is a dormant safety seam for future typed faults; observable HTTP behavior (routes, shapes, status codes, codes, localization) is unchanged. Log event literal → structured code-carrying placeholder is log-field evolution with the same Information level and no client contract impact.

## Preserved unchanged

Routes `GET/PUT /v1/admin/operator/profile`; `OperatorProfileAdminResponse` shape; `Result<T>` envelope via `ApiResponseFactory.From`; validator codes and rules; authorization seam `IOperatorProfileAdminAuthorizer` (Host thin adapter); `operator_profile` schema and migration `20260827215300_InitialOperatorProfile`; Contracts `IActorDisplayLookup` port; seed behavior.

## Verification

- `dotnet build` Tooba.OperatorProfile.Endpoints (transitively Contracts/Application/Domain/Infrastructure) = 0 errors/0 warnings.
- `dotnet build` Tooba.Host = 0 errors.
- New durable guard `OperatorProfileModuleAmsc001W1MigrateGuardTests` (3 facts) — pins the `IsKnown` set, dual-mechanism mapping, no message-heuristic classification, and structured log-code shape.

## Structure handoff

`Structure-Handoff-State = REQUIRED` — W2 re-verifies capability-first shallow structure under the current structure skill.
