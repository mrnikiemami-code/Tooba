# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001 — Certification

## Static certification audit

PASS for the requested Host-root ownership criteria:

- six requested Host root files: ZERO
- Host Settlement Application/Domain global-using shims: ZERO
- Commerce hold source owner: Payment.Infrastructure
- Catalog persistence reach-through from hold source: ZERO
- Payment -> Catalog dependency introduced by this migration: Contracts-only
- Order -> Payment dependency introduced by the checkout adapter: Contracts-only
- moved Order worker -> Host dependency: ZERO
- configuration key/worker name/telemetry/logging semantics: preserved by source comparison
- no schema/migration/frontend change in the migration diff

## Full skill certification verdict

NOT_CERTIFIED_YET

Two reasons prevent an honest final CERTIFIED declaration from this environment:

1. Executable focused validation (dotnet build/tests) is not available in the current connector-only execution environment, and this repository has no CI workflow that runs the required build/test matrix for this branch.
2. The touched Tooba.Order.Infrastructure/OrderModule.cs / project surface already contains direct foreign *.Application dependencies (for example Payment.Application and other module Application references). The certification skill explicitly forbids declaring a touched destination surface certified while such dependencies remain, even if they pre-date this bounded migration.

Therefore the migration is complete on the branch, but must not be promoted as ARCH-COMPLETE-002 / CERTIFIED until focused builds/tests pass and the touched Order registration surface is either isolated from this migration or its foreign-Application blocker is resolved in a separate bounded repair.

## Required executable validation before promotion

- build Tooba.Catalog.Contracts / Infrastructure
- build Tooba.Payment.Infrastructure
- build Tooba.Order.Infrastructure
- build Tooba.Host
- build Tooba.Host.Tests
- run HostRootGlobalBoundaries guard plus relevant hold/expiry behavior tests

No guard weakening or broad repair loop is permitted.
