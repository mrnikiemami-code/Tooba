# TB-TMAR-ORDER-GOLDEN-001-R3B-R1 — CQRS dispatcher removal

## Defect

R3B moved Host ops into Application CQRS, but every command handler still set
`Request with { Code = "..." }` and called `AdminOrderOperationsOrchestrator.ExecuteAsync`,
which routed via `ExecuteCoreAsync` string/`code switch`. Architect rejected that as a facade.

## Repair

- Deleted `ExecuteAsync` / `ExecuteCoreAsync` entirely.
- Added typed public Result-returning methods on the orchestrator
  (`CancelOrderAsync`, `MarkProcessingAsync`, `ApproveReturnAsync`, …) that own
  permission / cancel-block / projection checks for that operation.
- Shared helpers: `RunOperationAsync`, `RunProjectedCoreAsync`,
  `EnsureProjectedActionAllowedAsync` (expected code is a typed caller constant,
  not Application routing from `request.Code`).
- Renamed private mutation helpers to `*CoreAsync` to avoid name clashes.
- All 27 command handlers call the matching typed method with no Code rewriting.
- Endpoint wire-`Code` → MediatR Command switch remains (allowed).

## SoT

`nextTask` remains `TB-TMAR-ORDER-GOLDEN-001-R4` (not started).
