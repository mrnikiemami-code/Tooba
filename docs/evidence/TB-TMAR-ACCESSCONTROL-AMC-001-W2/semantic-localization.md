# TB-TMAR-ACCESSCONTROL-AMC-001-W2 — Semantic / localization failure channel

Mode: MIGRATE
Slice: SEMANTIC_LOCALIZATION
Parent: TB-TMAR-ACCESSCONTROL-AMC-001-W1

## Changes

- `Contracts/Errors/AccessControlErrorCodes.cs` + Endpoints catalog contributor + `.resx` / `.fa.resx`
- `AccessControlException` is code-only (no FA message payload)
- Directory / PermissionCatalog / CapabilityGate: no hardcoded FA exception text
- Endpoints: `AccessControlHttpErrors.From` via `ApiResponseFactory` — **zero** `Code.Contains` HTTP heuristics
- `AddAccessControlEndpointPresentation` registered in Host Program

## Deferred to later waves

- Full `Result<T>` handler returns + `api.From` success path (still `Results.Json` on success)
- Domain Aggregates foldering / Application Models rename / Endpoints→Domain ZERO

## Microservice

Stable error codes live in Contracts; presentation catalog ships with Endpoints — extractable without Host mapping choreography.
