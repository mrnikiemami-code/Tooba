# TB-TMAR-RETURNS-AMSC-001-W3 — Validation Evidence

Independent input-provenance / set-equality matrix and the focused build/test results, all re-derived from
disk at wave start `main` @ `6cab1b873d25d34bb941cc8d51777109cca3f2d4` (`HEAD == origin/main`).

Wave 3 changed **no production file**. The only source edits are test guards (certification guard added,
one W2 manifest assertion repointed, the repository-global certified-module pin lists extended).

## 1. The 11-row request → route → validator matrix

Derived from the actual endpoint request construction on disk (not from prose or historical counts).

| # | Route + verb | Request | Client-bound inputs | Class | Validator / reason |
| --- | --- | --- | --- | --- | --- |
| 1 | GET `/v1/customer/returns` | `ListCustomerReturnsQuery` | `CustomerId` (server-derived from authenticated customer) | NO_VALIDATOR_REQUIRED | actor derived from session; no bound transport value |
| 2 | GET `/v1/customer/returns/{returnRequestId:guid}` | `GetCustomerReturnQuery` | `ReturnRequestId` (`:guid`-constrained), `CustomerId` (server-derived) | NO_VALIDATOR_REQUIRED | route-constrained identifier + server-derived actor |
| 3 | POST `/v1/customer/returns` | `CreateReturnCommand` | body (`OrderId`, items, reason, quantities) + server-derived `CustomerId` | VALIDATOR_REQUIRED | `CreateReturnCommandValidator` |
| 4 | GET `/v1/seller/returns` | `ListSellerReturnsQuery` | `SellerPartyId` (server-derived by authorizer) | NO_VALIDATOR_REQUIRED | scope produced by `RequireSellerPartyIdAsync`; no bound transport value |
| 5 | GET `/v1/seller/returns/{returnRequestId:guid}` | `GetSellerReturnQuery` | `ReturnRequestId` (`:guid`), `SellerPartyId` (server-derived) | NO_VALIDATOR_REQUIRED | route-constrained identifier + server-derived seller party |
| 6 | POST `/v1/seller/returns/{returnRequestId:guid}/approve` | `ApproveReturnCommand` | `ReturnRequestId` (`:guid`), `SellerPartyId` (server-derived), body | VALIDATOR_REQUIRED | `ApproveReturnCommandValidator` |
| 7 | POST `/v1/seller/returns/{returnRequestId:guid}/reject` | `RejectReturnCommand` | `ReturnRequestId` (`:guid`), `SellerPartyId` (server-derived), body (`Reason`) | VALIDATOR_REQUIRED | `RejectReturnCommandValidator` |
| 8 | GET `/v1/admin/returns` | `ListAdminReturnsQuery` | optional server-side filters | NO_VALIDATOR_REQUIRED | optional server-side filter routed to the query engine; no malformable transport shape; reachable only after the admin authorizer |
| 9 | POST `/v1/admin/returns/query` | `QueryAdminReturnsGridQuery` | body (`Filters`, `Sort`, `Page`, `PageSize`, `Locale`) | VALIDATOR_REQUIRED | `QueryAdminReturnsGridQueryValidator` |
| 10 | GET `/v1/admin/returns/{returnRequestId:guid}` | `GetAdminReturnQuery` | `ReturnRequestId` (`:guid`) | NO_VALIDATOR_REQUIRED | route-constrained identifier only |
| 11 | POST `/v1/admin/returns/{returnRequestId:guid}/retry-refund` | `RetryReturnRefundCommand` | `ReturnRequestId` (`:guid`) | NO_VALIDATOR_REQUIRED | route-constrained identifier only; no bound body |

Set equality: **11 shipped routes == 11 endpoint-reachable requests == 4 VALIDATOR_REQUIRED + 7
NO_VALIDATOR_REQUIRED**, each classified exactly once. Optional input was **not** treated as evidence of
safety; every exemption rests on a `:guid` route constraint and/or a server-derived actor/party.

Validators emit stable machine codes only (zero `WithMessage(`), and no business rule is duplicated between
the validator and the handler.

## 2. Discovery / pipeline evidence

- `AddToobaCqrsFoundation(typeof(Tooba.Returns.Application.ReturnRequests.Commands.CreateReturnCommand).Assembly)`
  runs `AddValidatorsFromAssembly` over the Returns Application assembly.
- The single `ValidationBehavior<TRequest,TResponse>` is registered exactly once by the Foundation; Returns
  registers no second pipeline behavior and no custom dispatcher.
- All 11 endpoint-reachable requests are dispatched through `ISender` (11/11 `sender.Send(` call sites); no
  endpoint reaches persistence or a directory directly and no Host bypass exists.
- Invalid input surfaces through the canonical `SafeErrorMapper` validation path with the stable
  `returns.validation.*` codes.

## 3. Focused build / test results (exact counts)

| Command | Result |
| --- | --- |
| `dotnet build Tooba.Host.Tests` | 0 errors (1 pre-existing xUnit2013 analyzer warning in an unrelated guard) |
| `dotnet build Tooba.Returns.Tests` | 0 errors |
| `ReturnsModuleAmsc001W1MigrateGuardTests` | **12 passed / 0 failed / 0 skipped** |
| `ReturnsModuleAmsc001W2StructureGuardTests` | **7 passed / 0 failed / 0 skipped** |
| `ReturnsModuleAmsc001W3CertGuardTests` (this wave) | **6 passed / 0 failed / 0 skipped** |
| `Tooba.Returns.Tests` (module suite) | **16 passed / 0 failed / 0 skipped** |
| Returns + ErrorCatalog filter (`FullyQualifiedName~Returns|~ErrorCatalog`) | **77 passed / 0 failed / 2 skipped** |
| Broader gate filter (`~Returns|~TmarCompleteReferenceStructureGateTests|~TmarDurableGuardTests|~ErrorCatalogUniqueCodeGuardTests`) | **84 passed / 3 failed / 2 skipped** |

## 4. The 3 broader-run failures (pre-existing, unrelated, unchanged baseline)

| Failure | Cause | Relation to Returns |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | known `Tooba.Catalog.Contracts.Cart` vs `Tooba.Catalog.Contracts` namespace deviation (documented in the gate source itself as out of scope for a module-local certification) | NONE |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | stale recovery pin: a hard-coded 16-entry `certifiedModules` expectation and an obsolete `Current Grid work checkpoint (CURRENT` text pin | NONE |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | same stale repository-global recovery pin | NONE |

Reproduction proof: with the W3 SoT/manifest edits stashed and only the test files present, the exact same
five names fail (`TmarCompleteReferenceStructureGateTests` ×3, `TmarDurableGuardTests` ×2 —
`5 failed / 13 passed / 18 total`). The two `TmarDurableGuardTests` failures are byte-identical to the
baseline recorded by the immediately preceding certified module (`Pricing` W3-R3 and `Promotion` W3-R3,
which both recorded `61 passed / 3 failed / 2 skipped`).

After the W3 certification is applied, the Returns promotion requires the same repository-global pin repoint
that the `Promotion` certification performed at commit `6bf74745`
(`ProductWorkspace` → `ProductWorkspace, Promotion`): the three
`TmarCompleteReferenceStructureGateTests` pins now include `Returns`, leaving only the pre-existing
`Tooba.Catalog.Contracts.Cart` namespace deviation. No assertion was weakened and no baseline was widened —
the pin lists were extended to match reality, exactly as the Promotion precedent did.

## 5. Production-change proof

```text
git diff --stat 6cab1b87 <W3 HEAD>
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md               |   4 +
docs/architecture/tmar-current-state.json                     | 115 +++++-
docs/architecture/tmar-module-structure-manifests.json        |  26 ++--
src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W2StructureGuardTests.cs | 15 +--
src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs   |  7 +-
```

- `src/backend/Modules/Returns/**` production files changed: **0**
- `src/backend/Modules/Returns/**` files moved/renamed: **0**
- Production `.cs` files changed anywhere: **0**
- Migrations changed: **0** (three migration IDs, order, Up/Down and snapshot semantics untouched)
- SoT edit shape: one list insertion (`"Promotion"` → `"Promotion", "Returns"` inside
  `structureLock.certifiedModules`) plus one appended top-level `returnsAmsc001W3` member; no unrelated
  history rewritten.
- Manifest edit shape: the `Returns` entry moved from `preCertModules` into `modules[]` with
  `structureCertified: true`; `preCertModules` is now empty; per-project allowlists/forbidden lists are
  byte-unchanged.

## 6. Certification guards exercised

| Guard | Tests | Scope |
| --- | --- | --- |
| `ReturnsModuleAmsc001W1MigrateGuardTests` | 12 | single code home, typed fault seam, bilingual coverage, 4-validator matrix, Contracts-only |
| `ReturnsModuleAmsc001W2StructureGuardTests` | 7 | capability-first shallow layout, exact path↔namespace, manifest↔disk allowlists, `/Modules/Returns/` grouping |
| `ReturnsModuleAmsc001W3CertGuardTests` | 6 | SoT/manifest certification + wave lineage, 11 module routes / 0 Host routes, CQRS/validator matrix + discovery, canonical result/localization/catalog/logging, Contracts-only microservice boundary + unchanged schema, wave evidence + Host closure |
| `Tooba.Returns.Tests/Architecture/ReturnsArchitectureGuardTests` | module suite | module architecture invariants |
| `ErrorCatalogUniqueCodeGuardTests` | global | composed production catalog uniqueness |

Verdict: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
