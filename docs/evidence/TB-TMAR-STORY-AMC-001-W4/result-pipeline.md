# TB-TMAR-STORY-AMC-001-W4 — Result&lt;T&gt; / ApiResponseFactory pipeline

Mode: MIGRATE
Slice: RESULT_PIPELINE
Parent: TB-TMAR-STORY-AMC-001-W3

## Goal

Align Story HTTP-owning CQRS with COMPLETE_REFERENCE_PATTERN Result presentation so expected failures leave handlers as `Result` / `Result<T>` and endpoints reduce to `api.From` / `api.Created` (Content reference).

## Changes

1. Added `Stories/Composition/StoryOperation.cs` — maps `SemanticException` → `Result` by stable code; unknown exceptions propagate.
2. All endpoint-reachable MediatR requests now `IRequest<Result<…>>`; handlers wrap composer via `StoryOperation.ExecuteAsync`.
3. Get-by-id queries map null → `StoryErrorCodes.Missing` via `NotFoundIfNull`.
4. Admin/Seller/Storefront endpoints: remove SemanticException catch/remap; use `StoryHttpErrors.ResolveTenantId` + `api.From` / `api.Created`.
5. Durable guard: `StoryModuleAmcW4ResultGuardTests`.

## Behavior preserved

- Same routes and MediatR ownership.
- Domain/Infra still throw typed `SemanticException(StoryErrorCodes.*)`; no message-text classification.
- Schema/migrations untouched.
- Host Story folder remains ABSENT / CLOSED_HOST_ZERO.

## Microservice note

Result pipeline is module-local. No foreign Application/Infrastructure/Domain references introduced. Cross-process extraction remains Contracts + module binaries only.

## Validation

- `dotnet build` Story.Endpoints + focused W4 guard.
