# TB-TMAR-SUPPORT-AMSC-001-W3 — Validation Evidence

Independent input-provenance / set-equality matrix and the focused build/test results, all re-derived
from disk at wave start `main` @ `aaa15b03e5cfa2ed1bef1c8244e0c621c3786980` (`HEAD == origin/main`).

Wave 3 changed **no production file**. The only source edits are test guards (certification guard added,
one W2 manifest assertion repointed to `modules[]`, the repository-global certified-module pin lists
extended).

## 1. The 17-row request → route → validator matrix

Derived from the actual endpoint request construction on disk (not from prose or historical counts).

| # | Route + verb | Request | Class | Validator / reason |
| --- | --- | --- | --- | --- |
| 1 | GET `/v1/customer/support/tickets` | `ListCustomerTicketsQuery` | VALIDATOR_REQUIRED | `ListCustomerTicketsQueryValidator` (free-text `status`) |
| 2 | POST `/v1/customer/support/tickets` | `CreateCustomerTicketCommand` | VALIDATOR_REQUIRED | `CreateCustomerTicketCommandValidator` |
| 3 | GET `/v1/customer/support/tickets/{ticketId:guid}` | `GetCustomerTicketQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived actor |
| 4 | POST `/v1/customer/support/tickets/{ticketId:guid}/replies` | `ReplyCustomerTicketCommand` | VALIDATOR_REQUIRED | `ReplyCustomerTicketCommandValidator` |
| 5 | POST `/v1/customer/support/tickets/{ticketId:guid}/close` | `CloseCustomerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived actor |
| 6 | POST `/v1/customer/support/tickets/{ticketId:guid}/reopen` | `ReopenCustomerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived actor |
| 7 | GET `/v1/seller/support/tickets` | `ListSellerTicketsQuery` | VALIDATOR_REQUIRED | `ListSellerTicketsQueryValidator` (free-text `status`) |
| 8 | POST `/v1/seller/support/tickets` | `CreateSellerTicketCommand` | VALIDATOR_REQUIRED | `CreateSellerTicketCommandValidator` |
| 9 | GET `/v1/seller/support/tickets/{ticketId:guid}` | `GetSellerTicketQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived seller party |
| 10 | POST `/v1/seller/support/tickets/{ticketId:guid}/replies` | `ReplySellerTicketCommand` | VALIDATOR_REQUIRED | `ReplySellerTicketCommandValidator` |
| 11 | POST `/v1/seller/support/tickets/{ticketId:guid}/close` | `CloseSellerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived seller party |
| 12 | POST `/v1/seller/support/tickets/{ticketId:guid}/reopen` | `ReopenSellerTicketCommand` | NO_VALIDATOR_REQUIRED | `:guid` route constraint + server-derived seller party |
| 13 | GET `/v1/admin/support/tickets` | `ListAdminTicketsQuery` | VALIDATOR_REQUIRED | `ListAdminTicketsQueryValidator` (5 free-text filters + search) |
| 14 | GET `/v1/admin/support/tickets/{ticketId:guid}` | `GetAdminTicketQuery` | NO_VALIDATOR_REQUIRED | `:guid` route constraint only |
| 15 | POST `/v1/admin/support/tickets/{ticketId:guid}/replies` | `ReplyAdminTicketCommand` | VALIDATOR_REQUIRED | `ReplyAdminTicketCommandValidator` |
| 16 | PATCH `/v1/admin/support/tickets/{ticketId:guid}` | `PatchAdminTicketCommand` | VALIDATOR_REQUIRED | `PatchAdminTicketCommandValidator` |
| 17 | GET `/v1/admin/support/demo-preview` | `GetSupportDemoPreviewQuery` | NO_VALIDATOR_REQUIRED | parameterless + `Development`-gated at the endpoint |

Set equality: **17 shipped routes == 17 endpoint-reachable requests == 9 VALIDATOR_REQUIRED + 8
NO_VALIDATOR_REQUIRED**, each classified exactly once. Optional input was **not** treated as evidence of
safety; every exemption rests on a `:guid` route constraint and/or a server-derived actor/party.

## 2. Discovery / pipeline evidence

- `AddToobaCqrsFoundation(typeof(Tooba.Support.Application.Tickets.Commands.CreateCustomerTicketCommand).Assembly)`
  runs `AddValidatorsFromAssembly` over the Support Application assembly.
- The single `ValidationBehavior<TRequest,TResponse>` is registered exactly once by the Foundation.
- All 17 endpoint-reachable requests are dispatched through `ISender` (17/17 `sender.Send(` call sites).
- Invalid input surfaces through the canonical `validation.failed` envelope with the stable
  `support.validation.*` codes.

## 3. Focused build / test results (exact counts)

| Command | Result |
| --- | --- |
| `dotnet build Tooba.Host.Tests` | 0 errors (pre-existing xUnit2013 analyzer warnings only) |
| `SupportModuleAmsc001W1MigrateGuardTests` | **9 passed / 0 failed / 0 skipped** |
| `SupportModuleAmsc001W2StructureGuardTests` | **8 passed / 0 failed / 0 skipped** |
| `SupportModuleAmsc001W3CertGuardTests` (this wave) | **13 passed / 0 failed / 0 skipped** |
| `PricingModuleAmsc001W3R3CertGuardTests` (repository-global pin) | **passed** after the pin extension |
| `TmarCompleteReferenceStructureGateTests` | 2 of 3 pass; the remaining failure is the pre-existing `Tooba.Catalog.Contracts.Cart` namespace deviation |

## 4. Pre-existing repository-global failures (unchanged baseline)

| Failure | Cause | Relation to Support |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | known `Tooba.Catalog.Contracts.Cart` vs `Tooba.Catalog.Contracts` namespace deviation (documented in the gate source as out of scope for a module-local certification) | NONE |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | stale recovery pin: a hard-coded 16-entry `certifiedModules` expectation and an obsolete `Current Grid work checkpoint (CURRENT` text pin | NONE |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | same stale repository-global recovery pin | NONE |

These three names fail identically at the wave starting head `aaa15b03` and are the exact failures the
immediately preceding certified modules (`Returns` W3, `Pricing` W3-R3, `Promotion` W3-R3) recorded and
left untouched, because the repository-global recovery/SoT region is not owned by a module-local certify
wave. After the Support promotion, the repository-global certified-module pin lists were extended to
include `Support` exactly as the Returns/Promotion precedents did (`TmarCompleteReferenceStructureGateTests`
×3 pins, `PricingModuleAmsc001W3R3CertGuardTests` ×2 arrays, plus the W2 structure-guard manifest
assertion repointed to `modules[]`). No assertion was weakened and no baseline was widened.

## 5. Production-change proof

```text
git diff --stat aaa15b03 <W3 HEAD>
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md               | appended checkpoint
docs/architecture/tmar-current-state.json                     | Support W0/W1/W2/W3 + structureLock
docs/architecture/tmar-module-structure-manifests.json        | Support promoted into modules[]; preCertModules emptied
src/backend/Host/Tooba.Host.Tests/Architecture/SupportModuleAmsc001W2StructureGuardTests.cs | manifest assertion repointed
src/backend/Host/Tooba.Host.Tests/Architecture/SupportModuleAmsc001W3CertGuardTests.cs      | new durable cert guard
src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs   | certified-module pins extended
src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W3R3CertGuardTests.cs    | certified-module pins extended
```

- `src/backend/Modules/Support/**` production files changed: **0**
- Production `.cs` files changed anywhere: **0**
- Migrations changed: **0** (three migration IDs, order, Up/Down and snapshot semantics untouched)
- SoT edit shape: `"Story"` → `"Story", "Support"` inside `structureLock.certifiedModules`, `W0.commit`
  filled in, and three appended top-level members (`supportAmsc001W1`, `supportAmsc001W2`,
  `supportAmsc001W3`); no unrelated history rewritten.
- Manifest edit shape: the `Support` entry moved from `preCertModules` into `modules[]` with
  `structureCertified: true`; `preCertModules` is now empty; per-project allowlists/forbidden lists are
  byte-unchanged.

## 6. Certification guards exercised

| Guard | Tests | Scope |
| --- | --- | --- |
| `SupportModuleAmsc001W1MigrateGuardTests` | 9 | single code home, typed fault seam, bilingual coverage, 9-validator matrix, Contracts-only |
| `SupportModuleAmsc001W2StructureGuardTests` | 8 | capability-first shallow layout, exact path↔namespace, manifest↔disk allowlists, `/Modules/Support/` grouping |
| `SupportModuleAmsc001W3CertGuardTests` | 13 | SoT/manifest certification + wave lineage, 17 module routes / 0 Host routes, CQRS/validator matrix + discovery + fail-closed set-equality parser, canonical result/localization/catalog, Contracts-only microservice boundary + unchanged schema, wave evidence + Host closure |
| `SupportArchitectureGuardTests` (module suite) | module suite | module architecture invariants |
| `ErrorCatalogUniqueCodeGuardTests` | global | composed production catalog uniqueness |

Verdict: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
