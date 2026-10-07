# TB-TMAR-PRODUCTQNA-AMSC-001-W3 — Certify

## Verdict

`COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002` / `structureState CERTIFIED` / `httpApplicability HTTP_OWNING`

Baseline: `branch = main`, `HEAD == origin/main == 70c7b46b` (W2 Structure,
`Structure-State = READY_FOR_CERTIFY`). **No production code, project structure, schema, migration,
error, localization or Contracts API change was made in this wave.** W3 independently re-verified the
migrated/repaired surface and promoted it honestly.

## Architecture sequence (authoritative)

```text
e7e28c49  W0 Analyze   (tooba-architecture-analyze)
b29340de  W1 Migrate   (tooba-architecture-migrate)
70c7b46b  W2 Structure (tooba-architecture-structure)
          W3 Certify   (tooba-architecture-certify)  <- this wave
```

The historical AMC-001 lineage (`TB-TMAR-PRODUCTQNA-AMC-001-W0..W4`) stays in the repository as
historical evidence only; `productQnAAmc001.supersededBy = TB-TMAR-PRODUCTQNA-AMSC-001-W3`.

## Verification matrix

| ARCH-COMPLETE-002 concern | State | Evidence |
|---|---|---|
| Structure (W2 gate) | `CERTIFIED` | `docs/architecture/evidence/TB-TMAR-PRODUCTQNA-AMSC-001-W2/structure.md` |
| Folder granularity | `PROFESSIONAL_SHALLOW` | `Customer/Commands` + `Storefront/Queries` capabilities; shared `Validation`/`Composition`/`Models`/`Ports`; zero single-file request leaf |
| Solution Explorer | `CANONICAL` | `/Modules/ProductQnA/` in `Tooba.slnx`, 5 project entries |
| Path ↔ namespace | `EXACT` | Only the repository-locked EF `Persistence/Migrations` exemption |
| Root allowlist | `ENFORCED` | App/Domain/Contracts root empty; Infra `ProductQnAModule.cs`; Endpoints `ProductQnAEndpointModule.cs` |
| Physical copies | `CLEAN` | No stale/duplicate home; retired `Customer/Validators`/`Storefront/Validators` absent |
| File cohesion | `COHESIVE` | Largest production file 110 LOC (`Infrastructure/Directories/ProductQaDirectory.cs`) |
| CQRS / MediatR | `COMPLIANT` | 2 requests ↔ 2 handlers, `ISender` dispatch, `AddToobaCqrsFoundation` |
| Validation coverage | `EXHAUSTIVE_2_REQUIRED_0_NO_VALIDATOR_REQUIRED` | `SubmitProductQuestionCommandValidator`, `GetPublishedQuestionsQueryValidator` |
| API result / error | `CANONICAL` | `api.Created` / `api.From` / `api.FromFailure`; zero raw `Results.*`; zero `ex.Message` classification |
| Stable codes / descriptors | 2 declared + 2 registered (unique owner) | `ProductQnAErrorCodes` + `ProductQnAErrorCatalogContributor` |
| Transport validation codes | 7 Application-owned, 0 catalogued | `Application/Validation/ProductQnAValidationCodes.cs` → foundation `validation.failed` |
| Localization | `CANONICAL` | `ProductQnAErrorResourceSet` claims `product_qna.`; 9 EN + 9 FA resx keys; zero hard-coded fault text |
| Typed faults | `CODE_CARRYING_CONTRACTOPERATIONEXCEPTION_PLUS_SEMANTICEXCEPTION` | Domain/Directory `Rejected()`; `ProductQnAOperation` known-code filter |
| Logging / telemetry | `CANONICAL` | Zero `Console.WriteLine`, zero second pipeline, zero sensitive logging |
| Correlation / trace | `CANONICAL` | Zero `StartActivity`, zero `traceparent`, zero custom correlation |
| Contracts boundary | `CONTRACTS_ONLY` | Only `Tooba.Catalog.Contracts.Ports.ICatalogReviewProductLookup` |
| Cross-module join / persistence | `ZERO` | Own `product_qna` schema, own DbContext, own outbox |
| Foreign App/Infra/Domain coupling | `ZERO` | Domain = BuildingBlocks + own Contracts only |
| Endpoint ownership | `MODULE_OWNED` (2 routes) | `ProductQnACustomerEndpoints`, `ProductQnAStorefrontEndpoints` |
| Host authority | `ZERO` business/persistence/HTTP | Host holds only `ALLOWED_COMPOSITION_ROOT` |
| Host final closure | `PRESERVED` | No `Host/ProductQnA` folder, no new Host production file |
| Schema / migrations | `UNCHANGED`, `migrationFilesChanged = 0` | Single `20260826120000_InitialProductQnA` (+ Designer) + snapshot |
| Microservice extractability | `true` | Zero foreign coupling, own schema/outbox/migrations/endpoints |
| Blocking residual debt | `ZERO` | See non-blocking watch below |

## Promotion performed

| Artifact | Change |
|---|---|
| `docs/architecture/tmar-module-structure-manifests.json` | `ProductQnA` entry refreshed to the AMSC-001 lineage: `structureCertified: true`, W0→W3 commit lineage in `certificationNote`, 5 project entries; `Application.forbiddenTopLevelFolders` keeps the full `Commands`/`Queries`/`Validators` triplet. No allowlist widened, no forbidden entry removed. |
| `docs/architecture/tmar-current-state.json` | New `productQnAAmsc001W3` certification block; `productQnAAmsc001W0/W1/W2` commit SHAs reconciled to `e7e28c49`/`b29340de`/`70c7b46b`; `"ProductQnA"` present exactly once in `structureLock.certifiedModules`. |
| `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` | W3 Certify checkpoint appended (authoritative, module-local). |
| `src/backend/Host/Tooba.Host.Tests/Architecture/ProductQnAModuleAmsc001W3CertGuardTests.cs` | New durable certification lock. |

## Durable certification guard

`ProductQnAModuleAmsc001W3CertGuardTests`

- `ProductQnA_is_arch_complete_002_certified_in_sot_and_manifest`
- `ProductQnA_uses_canonical_result_localization_and_typed_fault_mechanisms`
- `ProductQnA_host_residue_is_composition_only_and_closed_folder_is_preserved`
- `ProductQnA_amsc_evidence_and_recovery_checkpoint_exist`
- `ProductQnA_amsc_lineage_commits_resolve_and_global_host_closure_is_preserved`

## Focused validation

```text
dotnet test Tooba.Host.Tests --filter FullyQualifiedName~ProductQnAModuleAmsc001W3CertGuardTests   -> see W3 commit validation
dotnet test Tooba.Host.Tests --filter FullyQualifiedName~ProductQnA                                -> 27 passed / 0 failed / 2 skipped
```

## Non-blocking watch (disclosed, not repaired here)

- **R1** The 7 transport validation codes stay Application-owned and uncatalogued (certified
  AccessControl/Cart/Fulfillment/Offer/Order/Payment/BulkInquiry precedent). Their
  `product_qna.validation.*` resx entries are preserved unchanged.
- **R2** `Tooba.ProductQnA.Domain` keeps a legitimate **self-module** `Tooba.ProductQnA.Contracts`
  reference for `ProductQnAErrorCodes.Rejected` (same precedent as Cart/AddressBook/BulkInquiry/
  PageComposition/UserPreference/OperatorProfile/Party/Localization/Wishlist/Identity). Foreign
  App/Infra/Domain coupling remains ZERO.
- **R3** Pre-existing repository failures unrelated to ProductQnA remain untouched:
  `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
  (stale `Tooba.Catalog.Contracts/Cart` project-level namespace expectation),
  `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable`,
  `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`.
  All three reproduced identically at the W1 baseline with the ProductQnA changes stashed.
- **R4** Untracked foreign Bridge result artifacts (`TB-TMAR-*-AMSC-001-W3-R*/RESULT.bridge.txt` +
  `post-result.js`) remain in the working tree; not part of ProductQnA scope.

## Stop gate

`USER_REVIEW_PRODUCTQNA_AMSC_001_W3`; `automaticNextImplementationTask = NONE`. No W4, no next module.
