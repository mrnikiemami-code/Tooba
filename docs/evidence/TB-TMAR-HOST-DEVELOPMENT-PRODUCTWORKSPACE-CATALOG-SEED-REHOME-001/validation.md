# Validation — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001

## Build (focused, touched projects only)

| Project | Result |
| --- | --- |
| `Tooba.Catalog.Infrastructure` | Build succeeded, 0 errors |
| `Tooba.Catalog.Application` | Build succeeded, 0 errors |
| `Tooba.Tax.Infrastructure` | Build succeeded, 0 errors |
| `Tooba.Tax.Contracts` | Build succeeded, 0 errors |
| `Tooba.Inventory.Infrastructure` | Build succeeded, 0 errors |
| `Tooba.Party.Infrastructure` | Build succeeded, 0 errors |
| `Tooba.Party.Contracts` | Build succeeded, 0 errors |
| `Tooba.Offer.Infrastructure` (parity-only reference) | Build succeeded, 0 errors |
| `Tooba.Host` | Build succeeded, 0 errors |

## Focused tests

| Filter | Result |
| --- | --- |
| `HostDevelopmentEnricherClosureGuardTests` | PASS |
| `HostDevelopmentAmcGuardTests` | PASS |
| `HostCustomerProfileEvacuationGuardTests` | PASS |
| `HostAdminAmcW32PwShellFinalGuardTests` | PASS |
| `HostCartResidualGuardTests` | PASS |

Combined focused run: `Passed! Failed: 0, Passed: 35, Total: 35`.

## Pre-existing unrelated failures (not caused by this task, verified at clean HEAD)

`TmarSourceSizeAndInfraAppTests` (3 failures) — the scan walks the repository root and therefore also
scans the sibling git worktree `.tmp-baseline` (verified: `git worktree list` shows
`SarvNewVer/.tmp-baseline`; `.gitignore` line 37 ignores `.tmp-*` for git but the guard's directory
exclusion list does not include it). Stashing all Wave 1 changes and re-running the same filter at
clean HEAD reproduced the identical 3 failures.

`SupportFoundationTests` (2) and `ReviewsFoundationTests` (1) — also reproduced at clean HEAD with all
Wave 1 changes stashed.

No repair was attempted: per the task's bounded rule these are unrelated pre-existing failures, and the
one-repair rule is reserved for a deterministic failure caused by this change. None of the three
families touches the Wave 1 surface.

## Manual parity verification

- `ProductWorkspaceDevelopmentBootstrap.cs`: 428 → 175 LOC.
- New Catalog files: 30 / 66 / 262 / 247 LOC — all under the 800 LOC new-file threshold.
- Host/Development production file count: 5 → 5.
- No EF migration added (`git status` shows no `Migrations/` change).
- No route/endpoint/frontend file changed.

## Touched-surface certification checklist

| Check | Result |
| --- | --- |
| Cohesive responsibility per touched file | PASS |
| Correct capability folder (`Development/`) | PASS |
| Path ↔ namespace exact | PASS |
| No root dump | PASS |
| No hard-coded user-facing localized error text added | PASS |
| No foreign `.Application`/`.Infrastructure`/`.Domain` in Catalog workspace seeds | PASS |
| No foreign `DbContext`/`DbSet` in Catalog workspace seeds | PASS (own `CatalogDbContext` only) |
| No parallel canonical mechanism | PASS |
| No unintended schema/behavior change | PASS |
