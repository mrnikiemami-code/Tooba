# TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001 — Architecture Lock Evidence

## Previous gap

Order had reached `COMPLETE_REFERENCE_PATTERN` (Endpoints, CQRS, Result/Contracts, FluentValidation coverage) and had completed physical structure hardening for `Tooba.Order.Endpoints` (STRUCTURE-001) and `Tooba.Order.Infrastructure` (STRUCTURE-002). However, the structural rules were only implicit:

- `ARCH-COMPLETE-001` did not name physical structure/folder/namespace quality at all.
- Organization guards were per-project and Order-only; there was no reusable gate mechanism for future TMAR module closures.
- Nothing prevented a future module (or Order itself) from adding capability files back at a project root.

## New lock version

- Architecture lock version advanced: `ARCH-COMPLETE-001` → `ARCH-COMPLETE-002`.
- Definition marker:
  `COMPLETE_REFERENCE_PATTERN_REQUIRES_ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT`
- A module can no longer qualify as `COMPLETE_REFERENCE_PATTERN` on build/CQRS/endpoint ownership alone; coherent physical organization is part of the definition.

## Rules locked (ARCH-COMPLETE-002)

- `APPLICATION_CAPABILITY_FOLDERS` — capability-specific files never at Application root; no `*Contracts.cs` dumping at root; validators live with the request or under a shared `Validation` capability; business validation stays out of FluentValidation.
- `ENDPOINTS_CAPABILITY_FOLDERS` — capability `*Endpoints.cs` never at Endpoints root; Admin/Customer/Seller/Storefront grouping visible; root holds the composition entry only.
- `INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS` — capability/integration files never at Infrastructure root; persistence under `Persistence`, migrations under `Persistence/Migrations`, foreign adapters under a coherent `Integrations/<Module>` path; one integration not fragmented across competing top-level folders.
- `PATH_NAMESPACE_ALIGNMENT` — every file namespace equals project name + relative folder path.
- `ROOT_ALLOWLIST` — each project has an explicit root `.cs` allowlist; a new root file fails the gate unless the manifest is deliberately updated.
- `NO_NAMESPACE_ALIAS_WORKAROUND` — alias workarounds used only to hide folder debt are forbidden (the justified `ReservationCycleEntity` Domain type alias is documented and allowed).

## Guards

Order durable guards preserved and exercised:

- `OrderApplicationOrganizationGuardTests`
- `OrderEndpointOrganizationGuardTests`
- `OrderInfrastructureOrganizationGuardTests`
- `OrderEndpointValidatorCoverageGuardTests`
- Host ownership/reverse-audit guards (`HostModuleEndpointOwnershipTests` and related)

## Reusable manifest mechanism

- New: `src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs`.
- Data-driven from `docs/architecture/tmar-module-structure-manifests.json` (module → projects → `rootAllowlist`, `forbiddenRootFiles`, `forbiddenTopLevelFolders`), plus a repo-wide path↔namespace alignment scan.
- Supports Application root allowlist, Endpoints root allowlist, Infrastructure root allowlist, namespace prefix derived from relative path, and framework folder ignores (`bin`, `obj`).
- Order is the first certified manifest; the gate asserts that no other HTTP-owning module is silently certified and that `structureLock.certifiedModules == ["Order"]`.

## Order certification

- Order remains `COMPLETE_REFERENCE_PATTERN` and additionally becomes `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
- No production business code changed; only tests/guards/architecture test infrastructure, docs and SoT/lock metadata.

## Explicit non-certification of unaudited modules

- Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion, Offer are listed in `uncertifiedHttpOwningModules`.
- Inventory is internal-only; it is also not structure-certified under ARCH-COMPLETE-002.
- No mass refactor of those modules was performed; each requires a future reverify task.

## No business changes

- Production business logic changes: none.
