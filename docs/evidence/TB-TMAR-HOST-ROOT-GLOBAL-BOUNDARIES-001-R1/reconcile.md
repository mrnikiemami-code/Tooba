# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1 — Reconciliation

## Start condition

- Latest `origin/main` at start: `a2e01cf59525591793feb5a2e8cc6719979252c4`
- Authorization work (`TB-TMAR-HOST-AUTHORIZATION-AMC-001`, commits `c5bf4548`, `a2e01cf5`)
  verified present on `origin/main` (7 files under
  `Tooba.AccessControl.Infrastructure/Authorization/`).
- ChatGPT branch `origin/chatgpt/host-root-boundaries-001` head =
  `70d1375969af80177983735b02614056d427d6f9`.
- Merge-base of that branch and `origin/main` = `3ad429dcfef9b8b254ca81e02a4097d520a3eae7`
  (the recorded base before parallel Authorization work). The branch descends from it.
- Both states clean and unambiguous → no `RECOVERY_CONFLICT`.

## Safe reconciliation

- Integration branch created **from latest `origin/main`**:
  `cursor/host-root-global-boundaries-001-r1`.
- `origin/main` was never rebased, reset, cleaned, or force-pushed.
- The stale branch was **not** blindly merged; the bounded six-file diff was
  reconciled onto latest `main` through a normal merge commit.
- Merge commit: `aa034703` (parents `a2e01cf5` latest main, `70d13759` ChatGPT branch).
- Automatic merge succeeded with **zero conflicts**.
- `Program.cs` auto-merged; both the Authorization composition line and the
  root-boundary removal hunks are present. `docs/architecture/tmar-current-state.json`
  auto-merged; `hostAuthorizationAmc` and `hostRootGlobalBoundaries001` both retained.

## Authorization preservation

- `HEAD == origin/main` before branching; the merge only adds the branch delta on
  top. No Authorization commit was dropped.
- Authorization files, migrations, guards, and ops doc remain on the integration
  branch and on the final `main`.

## Build-repair surfaced by reconciliation

The completed Authorization AMC removed `GlobalUsings.SettlementApp.cs`, which had
provided `global using Tooba.Settlement.Infrastructure.DependencyInjection;`.
`Host/Composition/ToobaModuleComposition.cs` was written against that shim and
referenced `SettlementModule` via `using Tooba.Settlement.Infrastructure;`.

Repair (single deterministic fix): the composition file now names the provider
namespace explicitly.

```text
- using Tooba.Settlement.Infrastructure;
+ using Tooba.Settlement.Infrastructure.DependencyInjection;
```

This preserves composition-only behavior and removes the last `GlobalUsings.Settlement*`
dependency path.
