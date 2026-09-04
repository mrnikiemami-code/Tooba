# TCX-P09-T001 shared-file conflict map

## SAFE

- A single existing module's Domain/Application/Infrastructure source, module-local tests, migrations, and model snapshot, provided the selected task owns that module.
- Feature-local frontend API and screens under `app/returns`, `app/wallet`, or `app/customer-panel` when no shell/navigation changes are required.
- Codex-only evidence under `docs/evidence/codex/TCX-*/`.
- Runtime/build artifacts inside the Codex worktree and schemas inside `tooba_codex_dev` only.

## CAUTION

- `src/backend/Host/Tooba.Host/<Feature>/*Endpoints.cs`: feature-specific but hosted in the shared Host project.
- `src/backend/Host/Tooba.Host/ToobaModuleComposition.cs`: shared dependency-registration hub.
- `src/backend/Host/Tooba.Host.Tests/*`: shared test project; low semantic conflict but common project surface.
- `src/frontend/app/admin/admin-shell.tsx`, `admin-nav-active.ts`, `admin-nav-integrity.test.ts`: canonical Admin navigation.
- `src/frontend/app/customer-panel/customer-panel-shell.tsx`, `customer-capability-shell.tsx`, and navigation tests.
- `src/frontend/app/vendor-panel/vendor-shell.tsx`, `vendor-capability-shell.tsx`, and navigation tests.
- `src/frontend/app/admin/host-client.ts`: large shared Admin client used by Catalog workspace.
- Module-local `DbContext`, migrations, and model snapshot: safe only with one clear module owner and reconciliation against new upstream migrations.
- `src/backend/Modules/Tooba.ModuleContracts`: shared cross-module contracts.

## AVOID-WHILE-P08-ACTIVE

- `src/backend/Host/Tooba.Host/Program.cs` and global Host composition/startup.
- `src/backend/Host/Tooba.Host/Content/**` and `src/backend/Modules/Content/**`.
- `src/frontend/app/admin/content*`, `src/frontend/app/content/**`, and `src/frontend/app/blogs/**`.
- Shared design-system primitives under `src/frontend/design-system/**`.
- Shared localization bootstrap/routing and language administration.
- Shared authentication, authorization adapters, capability catalogs, and SpiceDB schema.
- Root/backend/frontend package manifests and solution membership when avoidable.
- `docs/PROJECT-STATE.md`, `docs/ROADMAP.md`, `docs/ai/**`, and all global TB recovery/task/result files.
- Global frontend rewrites/origin defaults and committed machine-specific runtime configuration.

The recommended first slice requires none of the AVOID files and none of the CAUTION files if limited to typed preference domain/application/persistence behavior plus module-local tests.
