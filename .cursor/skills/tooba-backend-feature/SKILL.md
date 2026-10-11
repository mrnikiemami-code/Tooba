---
name: tooba-backend-feature
description: Lightweight architecture-first workflow for an authorized new backend feature or bounded bug fix in Tooba. Check ownership, CQRS, validation, Result, typed errors, module boundaries, DI and focused tests before accepting changes; NOT an AMSC recertification.
---

# Tooba Backend Feature (lightweight)

Scope: `src/backend/**`. Trigger for each authorized backend feature implementation or bug fix. This skill supplements, never replaces, the existing Analyze/Migrate/Structure/Complete/Certify skills and repository architecture locks.

## Before coding — decide ownership
1. Read `AGENTS.md`, active task, relevant module architecture/Contracts, existing endpoint/request/handler, registration, error catalog, and focused tests. Confirm branch, scope and locks.
2. Identify ONE canonical owning module and reuse its current capability folders and APIs. Trace consumers and public contract expectations. Never place feature business authority in Host or add foreign Application/Infrastructure/Domain references.
3. Plan only the smallest coherent change. If cross-module redesign, schema migration, architecture recertification or approved-contract change is needed, STOP for Architect decision. Do not silently start AMSC waves.

## Implement without introducing new debt
- HTTP ownership stays in module Endpoints; use `ISender` → real MediatR request and handler, canonical `Result`/`SemanticError` and `ApiResponseFactory` (`From`/`Created` etc.). Preserve actual HTTP status, body and Location contracts.
- Add FluentValidation for any endpoint-reachable untrusted transport input that requires validation; document and prove real exemptions. Validators return stable machine codes, not prose.
- Business errors use module-owned stable typed codes, canonical descriptors and EN/FA resources; no user-facing Persian/English hardcoded exception strings, no message-text fault classification or `InvalidOperationException` as business transport mapping. Comments/sample data are not necessarily errors.
- Preserve Contracts-only cross-module boundaries, no foreign DbContext/schema SQL joins, and no new cyclic references. Confirm exact file path↔namespace and cohesive capability placement.
- Wire every new service/port through the owning module's DI with the correct lifetime; avoid duplicate instances, captive dependencies and cycles. Do not replace existing contracts with ad-hoc shortcuts.
- Preserve tenancy/authorization, idempotency, transaction/outbox semantics, currency and financial invariants where relevant. Do not bypass business authority with development-only shortcuts.
- Keep changes minimal: no unrelated cleanup, speculative abstractions, broad refactoring or silent baseline/guard weakening.

## Prove and report
1. Run smallest relevant compile, tests and DI/service-resolution verification for modified paths; add focused regression coverage for new/changed behavior. Never claim an unrun test passed.
2. Check the actual diff for unwanted dependencies, hardcoded UI/error prose, HTTP mapping drift, duplicate service registrations, schema changes, unapproved files and broken architecture guards.
3. Report owner/capability, exact changed files, contract and behavior impacts, tests actually run, known pre-existing failures, and any unresolved debt. `Worker PASS != Architect ACCEPT`.
4. STOP when task scope, a frozen surface, current architecture lock, or a business contract cannot be preserved. Do not auto-issue another task, restart AMSC, or call a feature `ARCH-COMPLETE-002` certified just because tests pass.
